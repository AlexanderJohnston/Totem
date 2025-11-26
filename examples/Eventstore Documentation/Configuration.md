# EventStoreDB 22.10 — Configuration

EventStoreDB has a number of configuration options that can be changed. This document describes the available configuration methods and common autoconfigured options.

## Configuration options

When you don't change the configuration, EventStoreDB will use sensible defaults, but they might not suit your needs. You can instruct EventStoreDB to use a different set of options. There are multiple ways to configure the EventStoreDB server described below.

### Version and help

Check the installed EventStoreDB version using the `--version` command-line option. For example:

```bash
$ eventstored --version
EventStoreDB version 22.10.0.0 (tags/oss-v22.10.0/242b35056, Thu, 10 Nov 2022 13:45:56 +0000)
```

The full list of available options is available from the currently installed server using the `--help` command-line option.

### Configuration file

Use a configuration file when you want the server to run with the same set of options every time. YAML files are useful for large installations because you can distribute or generate them from a configuration management system.

The configuration file has a YAML-compatible format. A basic YAML configuration file looks like:

```yaml
---
Db: "/volumes/data"
Log: "/esdb/logs"
```

Tips:
- You need to use the three dashes (`---`) and proper spacing in your YAML file.
- The default configuration file name is `eventstore.conf`. It is located in `/etc/eventstore/` on Linux and in the server installation directory on Windows.
- To tell EventStoreDB to use a different configuration file, pass the file path on the command line with `--config=filename`, or set the `CONFIG` environment variable.

### Environment variables

You can set options with environment variables. All variables are prefixed with `EVENTSTORE_` and normally follow the pattern `EVENTSTORE_{option}`. For example, setting `EVENTSTORE_LOG` instructs the server to use a custom location for log files.

Environment variables override options specified in configuration files.

### Command line

Command-line options override both configuration files and environment variables. For example:

```bash
eventstored --log /tmp/eventstore/logs
```

will override the default log files location.

### Testing the configuration

When more than one method is used to configure the server, it may be hard to determine the effective configuration. Use the `--what-if` option to print the effective configuration applied from defaults, configuration files, environment variables, and command-line parameters to the console.

Click here to see a WhatIf example

Note:
- Version 21.6 introduced a stricter configuration check: the server will not start when an unknown configuration option is passed via the configuration file, environment variable, or command line.

Examples that will prevent the server from starting:
- `--UnknownConfig` on the command line
- `EVENTSTORE_UnknownConfig` through an environment variable
- `UnknownConfig: value` in the config file

Output on stdout will be:
```
Error while parsing options: The option UnknownConfig is not a known option. (Parameter 'UnknownConfig')
```

## Autoconfigured options

Some options are configured at startup to make better use of available resources on larger machines. These options include:

- StreamInfoCacheCapacity
- ReaderThreadsCount
- WorkerThreads

### StreamInfoCacheCapacity

This option sets the maximum number of entries to keep in the stream info cache. The cache contains information about any stream that has recently been read or written to. Having entries in this cache significantly improves write and read performance for cached streams on larger databases.

- Default behavior: The cache dynamically resizes according to the amount of free memory. The option is set to `0` by default, which enables dynamic resizing.
- Minimum: 100,000 entries.
- Default on previous versions: 100,000 entries.

Format / Syntax:
- Command line: `--stream-info-cache-capacity`
- YAML: `StreamInfoCacheCapacity`
- Environment variable: `STREAM_INFO_CACHE_CAPACITY`

### ReaderThreadsCount

This option configures the number of reader threads available to EventStoreDB. More reader threads allow more concurrent reads to be processed.

- Default behavior: The reader threads count is autoconfigured at startup to twice the number of available processors, with a minimum of 4 and a maximum of 16 threads.
- Option default: `0` (enables autoconfiguration).
- Default on previous versions: 4 threads.

Format / Syntax:
- Command line: `--reader-threads-count`
- YAML: `ReaderThreadsCount`
- Environment variable: `READER_THREADS_COUNT`

Warning:
- Increasing the reader threads count too high can cause read timeouts if your disk cannot handle the increased load.

### WorkerThreads

The `WorkerThreads` option configures the number of threads available to the pool of worker services.

- Default behavior: At startup the number of worker threads will be set to 10 if there are more than 4 reader threads. Otherwise, it will be set to 5 threads.
- Option default: `0` (enables autoconfiguration).
- Default on previous versions: 5 threads.

Format / Syntax:
- Command line: `--worker-threads`
- YAML: `WorkerThreads`
- Environment variable: `WORKER_THREADS`
