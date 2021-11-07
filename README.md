# TigerBeetle.Net

A pure C# client for [TigerBeetle](https://github.com/coilhq/tigerbeetle)

**[Compatible with .Net Standard 2.1](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)**

*TigerBeetle is a financial accounting database designed for mission critical safety and performance to power the future of financial services.*

Watch an introduction to TigerBeetle on [Zig SHOWTIME](https://www.youtube.com/watch?v=BH2jvJ74npM) for more details:

[![A million financial transactions per second in Zig](https://img.youtube.com/vi/BH2jvJ74npM/0.jpg)](https://www.youtube.com/watch?v=BH2jvJ74npM)

# Motivation

*This project is a work in progress (WIP)*

- Learn more about TigerBettle's architecture, by studying its source code.

- Learn more about new features introduced in dotNet, like slices, ranges, and buffers.

## Performance

This C# implementation is based on the same principles regarding performance and memory efficiency adopted by TigerBeetle. In many places, this C# version is just a line-by-line port of the Zig code. 

Currently, the same benchmark runs ~30% slower than the Zig implementation.

## License

Licensed under the Apache License, Version 2.0 (the "License"); you may not use these files except in compliance with the License. You may obtain a copy of the License at

https://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
