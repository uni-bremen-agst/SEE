# profiling2gxl
Converts profiling output to gxl and jlg (optional) format.


## Requirements
- .NET 6.0
- Visual Studio 2022 (lower versions may work as well)

### Usage

```
profiling2gxl --file <file_path> --format <format>
```

### Options

| Option | Default | Required | Description |
|---|---|---|---|
| --file <PATH> | - | Yes | The file containing profiler data |
| --format <FORMAT> | - | Yes | The format of the given profiler data |
| --output <PATH> | <PATH_TO_FILE>.gxl | No | The name of the gxl file |
| --jlg <PATH> | - | No | The name of the jlg file. If omitted, no jlg is created |
| --help | - | No | Display the help text |
