# TigerBeetle.Net

A C# client for [TigerBeetle](https://github.com/coilhq/tigerbeetle)

**[Compatible with .Net Standard 2.1](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)**

*TigerBeetle is a financial accounting database designed for mission-critical safety and performance to power the future of financial services.*

Watch an introduction to TigerBeetle on [Zig SHOWTIME](https://www.youtube.com/watch?v=BH2jvJ74npM) for more details:

[![A million financial transactions per second in Zig](https://img.youtube.com/vi/BH2jvJ74npM/0.jpg)](https://www.youtube.com/watch?v=BH2jvJ74npM)

# Motivation

*This project is a work in progress (WIP)*

- Learn more about TigerBettle's architecture, by studying its source code.

- Learn more about new features introduced in dotNet, like slices, ranges, and buffers.

# Native client

This project offers two client backends: **native** or **managed**.

The **native client backend** is a library binding to the original [client.zig](https://github.com/coilhq/tigerbeetle/blob/main/src/vsr/client.zig) implementation. It shows pair performance with the Zig benchmark, without additional memory copies or marshaling overhead.

See the experimental [libtigerbeetle implementation](src/libtigerbeetle/src/lib.zig) for more details about the binding interface.

# Managed client

The **managed client backend** is a pure C# implementation based on the same principles regarding performance and memory efficiency adopted by TigerBeetle. In many places, this C# version is just a line-by-line port of the Zig code. 

# Benchmarking

1. Clone this repo

> ```bash
> $ git clone --recurse-submodules https://github.com/batiati/tigerbeetle.net
> $ cd tigerbeetle.net
> ```

2. Chmod the scripts

> ```bash
> $ chmod +x scripts/*.sh
> ```

3. Install [Zig](https://ziglang.org/)

> ```bash
> $ scripts/install_zig.sh
> ```

4. Install [DotNet](https://docs.microsoft.com/en-us/dotnet/)

> ```
> $ scripts/install_dotnet.sh
> ```

5. Run the `benchmark.sh` script passing `zig`, `native` or `managed` as argument to run each version.

> ```bash
> $ scripts/benchmark.sh native
> ```

... Or run the full benchmark to run all three versions in a row.


> ```bash
> $ scripts/full_benchmark.sh
> ```

### 1. One million transactions, 5.000 per batch

Currently, the managed client runs about to ~40% slower than the native client, using the default parameters.

```
MAX_TRANSFERS = 1_000_000;
IS_TWO_PHASE_COMMIT = false;
BATCH_SIZE = 5_000;
```

Zig original benchmark
> ![5000 batches in zig](./assets/5000_zig.JPG)

C# native client
> ![5000 in C#](./assets/5000_dotnet.JPG)

C# managed client


### 2. Half million transactions, 1.000 per batch

The C# version performs better with smaller batches.

```
MAX_TRANSFERS = 500_000;
IS_TWO_PHASE_COMMIT = false;
BATCH_SIZE = 1000;
```

Zig
> ![1000 batches in zig](./assets/1000_zig.JPG)

C#
> ![1000 in C#](./assets/1000_dotnet.JPG)

### 3. Two-phase transactions, only 2 per batch

Pretty close with only 2 transactions per batch.

```
MAX_TRANSFERS = 1_000;
IS_TWO_PHASE_COMMIT = true;
BATCH_SIZE = 2;
```

Zig
> ![500 batches in zig](./assets/2_twophase_zig.JPG)

C#
> ![500 in C#](./assets/2_twophase_dotnet.JPG)

### 4. Profiling

The profiler shows most of the time spent on waiting for IO operations.

![Profiler](./assets/Profiler_CPU.JPG)

TigerBeetle uses `io_uring`, and we use whatever the DotNet SDK implementation does. [Maybe in future releases, DotNet will support `io_uring`](https://github.com/dotnet/runtime/issues/51985)

### 5. Windows and old Linuxes

It can run on environments where native code isn't compatible or allowed.
The managed client does not depend on native code, and it can run on Windows and old Linux distributions.

## TODO List

- [ ] Benchmarks scripts.
- [ ] Build and embeed the native library.
- [ ] Error handling and reconnection for the managed client.
- [ ] More tests
- [ ] Code cleanup

## License

Licensed under the Apache License, Version 2.0 (the "License"); you may not use these files except in compliance with the License. You may obtain a copy of the License at

https://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
