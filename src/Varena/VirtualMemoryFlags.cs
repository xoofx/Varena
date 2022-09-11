// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace Varena;

/// <summary>
/// The virtual memory protection flags.
/// </summary>
[Flags]
public enum VirtualMemoryFlags
{
    /// <summary>
    /// No protection defined.
    /// </summary>
    None = 0,

    /// <summary>
    /// The memory can be read.
    /// </summary>
    Read = 1 << 0,

    /// <summary>
    /// The memory can be written to.
    /// </summary>
    Write = 1 << 1,

    /// <summary>
    /// The memory can be executed.
    /// </summary>
    Execute = 1 << 2,

    /// <summary>
    /// The memory can be read and written to.
    /// </summary>
    ReadWrite = Read | Write,

    /// <summary>
    /// The memory can be read, written to and executed.
    /// </summary>
    /// <remarks>
    /// Warning: on macOS, this protection is converted to Execute only.
    /// </remarks>
    [Obsolete("This enum will be removed in a future version. Use ReadWrite or Execute exclusively. 'All' is not supported on macOS and converted to Execute only.")]
    All = ReadWrite | Execute,
}