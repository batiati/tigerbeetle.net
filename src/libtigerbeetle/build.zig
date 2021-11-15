const std = @import("std");

pub fn build(b: *std.build.Builder) void {

    var target = b.standardTargetOptions(.{
        .default_target = .{
            .cpu_model = .baseline,
        }
    });
    
    const mode = b.standardReleaseOptions();


    const lib = b.addSharedLibrary("tigerbeetle", "src/lib.zig", .unversioned);
    lib.setOutputDir("../TigerBeetle/Native/runtimes/linux-x64/native");
    lib.setBuildMode(mode);
    lib.setTarget(target);
    lib.install();

    var main_tests = b.addTest("src/lib.zig");
    main_tests.setBuildMode(mode);

    const test_step = b.step("test", "Run library tests");
    test_step.dependOn(&main_tests.step);
}
