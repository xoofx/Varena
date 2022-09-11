# Varena [![Build Status](https://github.com/xoofx/Varena/workflows/ci/badge.svg?branch=main)](https://github.com/xoofx/Varena/actions) [![Coverage Status](https://coveralls.io/repos/github/xoofx/Varena/badge.svg?branch=main)](https://coveralls.io/github/xoofx/Varena?branch=main) [![NuGet](https://img.shields.io/nuget/v/Varena.svg)](https://www.nuget.org/packages/Varena/)

<img align="right" width="160px" height="160px" src="img/varena.png">

Varena is a .NET library that provides a fast and lightweight arena allocator using virtual memory.
  
## Features

- Create very large continuous byte buffers and arrays (e.g 1 TiB) of data without committing the virtual memory to the physical memory.
  - Only use the memory that is allocated, not the total capacity reserved!
- Fast bump allocator with a commit per block (default is 64 KiB of memory committed)
- Use `VirtualBuffer` for manipulating bytes.
- Use `VirtualArray<T>` for manipulating a dynamic array of unmanaged data.
- Compatible with `.NET6.0+` and on all platforms (Windows, Linux, macOS)

## Usage

```csharp
using Varena;

// The manager keeps track of all created arenas (buffers, arrays)
using var manager = new VirtualArenaManager();

// Create a byte buffer and reserve 1 GiB of continuous memory (but not yet allocated)
var arena1 = manager.CreateBuffer("Arena1", 1 << 30);
// Allocate 1024 bytes -> The arena commits a block 64 KiB of memory (configurable)
var span = arena1.AllocateRange(1024);
span[0] = 1;
// Allocate 2048 bytes -> The arena keeps allocating in the previous commit block of 64 KiB
var span2 = arena1.AllocateRange(2048);
span2[0] = 1;

// Create a data array and reserve 1 MiB of continuous memory (but not yet allocated)
var arena2 = manager.CreateArray<Guid>("Arena2", 1 << 20);
// Allocate sizeof(Guid) * 1024 bytes -> The arena commits a block 64 KiB of memory (configurable)
var span3 = arena2.AllocateRange(1024);
span3[0] = Guid.NewGuid();
```

### Documentation

You will find more details about how to use Varena in this [user guide](https://github.com/xoofx/Varena/blob/main/doc/readme.md).

## License

This software is released under the [BSD-Clause 2 license](https://opensource.org/licenses/BSD-2-Clause). 

## Author

Alexandre Mutel aka [xoofx](http://xoofx.com).
