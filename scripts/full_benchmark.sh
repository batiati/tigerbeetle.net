#!/usr/bin/env bash
set -e

scripts/benchmark.sh zig
scripts/benchmark.sh native
scripts/benchmark.sh managed