// A simple C-ABI wrapper for TigerBeetle client

const std = @import("std");
const assert = std.debug.assert;
const Allocator = std.mem.Allocator;
const GeneralPurposeAllocator = std.heap.GeneralPurposeAllocator(.{});

const tb = @import("tigerbeetle/src/tigerbeetle.zig");


const StateMachine = @import("tigerbeetle/src/state_machine.zig").StateMachine;
const Operation = StateMachine.Operation;
const MessageBus = @import("tigerbeetle/src/message_bus.zig").MessageBusClient;
const Message = @import("tigerbeetle/src/message_pool.zig").MessagePool.Message;
const IO = @import("tigerbeetle/src/io.zig").IO;
const config = @import("tigerbeetle/src/config.zig");

const vsr = @import("tigerbeetle/src/vsr.zig");
const Header = vsr.Header;
const Client = vsr.Client(StateMachine, MessageBus);
const NativeCallback = fn (
    operation: u8,
    results: [*c]const u8,
    size: usize,
) callconv(.C) void;

pub const log_level: std.log.Level = .err;

const Results = struct {
    pub const SUCCESS = 0;
    pub const ALREADY_INITIALIZED = 1;
    pub const IO_URING_FAILED = 2;
    pub const INVALID_ADDRESS = 3;
    pub const ADDRESS_LIMIT_EXCEEDED = 4;
    pub const INVALID_HANDLE = 5;
    pub const MESSAGE_POOL_EXHAUSTED = 6;

    pub const TICK_FAILED = 8;
    pub const OUT_OF_MEMORY = 9;
};

const Context = struct {
    client: Client,
    io: *IO,
    message_bus: MessageBus,
    addresses: []std.net.Address,

    fn create(
        client_id: u128,
        cluster: u32,
        addresses_raw: []const u8,
    ) !*Context {
        const context = try allocator.create(Context);
        errdefer allocator.destroy(context);

        context.io = &io;

        context.addresses = try vsr.parse_addresses(allocator, addresses_raw);
        errdefer allocator.free(context.addresses);
        assert(context.addresses.len > 0);

        context.message_bus = try MessageBus.init(
            allocator,
            cluster,
            context.addresses,
            client_id,
            context.io,
        );
        errdefer context.message_bus.deinit();

        context.client = try Client.init(
            allocator,
            client_id,
            cluster,
            @intCast(u8, context.addresses.len),
            &context.message_bus,
        );
        errdefer context.client.deinit();

        context.message_bus.set_on_message(*Client, &context.client, Client.on_message);

        return context;
    }

    fn destroy(context: *Context) void {
        context.client.deinit();
        context.message_bus.deinit();
        allocator.free(context.addresses);
        allocator.destroy(context);
    }
};

// Globals
var gp: GeneralPurposeAllocator = undefined;
var allocator: *Allocator = undefined;
var io: IO = undefined;
var initialized: bool = false;

// C-ABI exports

pub export fn TB_Init() callconv(.C) u32 {
    if (initialized) {
        return Results.ALREADY_INITIALIZED;
    }

    gp = GeneralPurposeAllocator{};
    errdefer _ = gp.deinit();

    allocator = &gp.allocator;

    io = IO.init(32, 0) catch {
        return Results.IO_URING_FAILED;
    };
    errdefer io.deinit();

    initialized = true;
    return Results.SUCCESS;
}

pub export fn TB_Deinit() callconv(.C) u32 {
    io.deinit();
    _ = gp.deinit();

    gp = undefined;
    allocator = undefined;
    io = undefined;
    initialized = false;
    return Results.SUCCESS;
}

pub export fn TB_CreateClient(client_id: u128, cluster: u32, addresses_raw: [*c]u8, handle: *usize) callconv(.C) u32 {
    var context = Context.create(client_id, cluster, std.mem.spanZ(addresses_raw)) catch |err| switch (err) {
        error.AddressInvalid, error.PortInvalid, error.PortOverflow, error.AddressHasTrailingComma, error.AddressHasMoreThanOneColon => return Results.INVALID_ADDRESS,
        error.AddressLimitExceeded => return Results.ADDRESS_LIMIT_EXCEEDED,
        error.OutOfMemory => return Results.OUT_OF_MEMORY,
    };

    handle.* = @ptrToInt(context);
    return Results.SUCCESS;
}

pub export fn TB_DestroyClient(handle: usize) callconv(.C) u32 {
    if (handle == 0) return Results.INVALID_HANDLE;
    const context = @intToPtr(*Context, handle);

    context.destroy();
    return Results.SUCCESS;
}

pub export fn TB_GetMessage(handle: usize, message_handle: *usize, body_buffer: *usize, body_buffer_len: *usize) callconv(.C) u32 {
    if (handle == 0) return Results.INVALID_HANDLE;
    const context = @intToPtr(*Context, handle);

    var message = context.client.get_message() orelse {
        return Results.MESSAGE_POOL_EXHAUSTED;
    };

    var buffer = message.buffer[@sizeOf(Header)..];

    message_handle.* = @ptrToInt(message);
    body_buffer.* = @ptrToInt(buffer.ptr);
    body_buffer_len.* = buffer.len;

    return Results.SUCCESS;
}

pub export fn TB_UnrefMessage(handle: usize, message_handle: usize) callconv(.C) u32 {
    if (handle == 0) return Results.INVALID_HANDLE;
    const context = @intToPtr(*Context, handle);

    if (message_handle == 0) return Results.INVALID_HANDLE;
    const message = @intToPtr(*Message, message_handle);

    context.client.unref(message);
    return Results.SUCCESS;
}

pub export fn TB_Request(handle: usize, operation: u8, message_handle: usize, message_body_size: usize, callback_ptr: NativeCallback) callconv(.C) u32 {
    if (handle == 0) return Results.INVALID_HANDLE;
    const context = @intToPtr(*Context, handle);

    if (message_handle == 0) return Results.INVALID_HANDLE;
    const message = @intToPtr(*Message, message_handle);
    defer context.client.unref(message);

    const user_data = std.meta.cast(u128, @ptrToInt(callback_ptr));
    context.client.request(user_data, on_callback, @intToEnum(StateMachine.Operation, operation), message, message_body_size);
    return Results.SUCCESS;
}

pub export fn TB_Tick(handle: usize) callconv(.C) u32 {
    if (handle == 0) return Results.INVALID_HANDLE;
    const context = @intToPtr(*Context, handle);

    context.client.tick();
    context.io.tick() catch return Results.TICK_FAILED;

    return Results.SUCCESS;
}

pub export fn TB_RunFor(handle: usize, ms: u32) callconv(.C) u32 {
    if (handle == 0) return Results.INVALID_HANDLE;
    const context = @intToPtr(*Context, handle);

    context.io.run_for_ns(ms * std.time.ns_per_ms) catch return Results.TICK_FAILED;
    return Results.SUCCESS;
}

fn on_callback(user_data: u128, operation: StateMachine.Operation, results: anyerror![]const u8) void {
    var callback = @intToPtr(NativeCallback, std.meta.cast(usize, user_data));
    var data = results catch return;
    callback(@enumToInt(operation), data.ptr, data.len);
}

