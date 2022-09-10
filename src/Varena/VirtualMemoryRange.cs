// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace Varena;

/// <summary>
/// A range of virtual memory.
/// </summary>
/// <param name="BaseAddress">The base address of this range.</param>
/// <param name="Size">The size of this range.</param>
public readonly record struct VirtualMemoryRange(IntPtr BaseAddress, nuint Size)
{
    /// <summary>
    /// Gets a boolean indicating whether this range is null (BaseAddress == 0 and Size == 0)
    /// </summary>
    public bool IsNull => BaseAddress == IntPtr.Zero && Size == 0;

    /// <summary>
    /// Overrides the string representation of this instance.
    /// </summary>
    /// <returns>A string representation of this instance.</returns>
    public override string ToString()
    {
        return (IntPtr.Size == 8) ? $"{nameof(VirtualMemoryRange)} {{ BaseAddress = 0x{(ulong)BaseAddress:x16}, Size = {VirtualMemoryHelper.ByteCountToText(Size)} }}" : $"{nameof(VirtualMemoryRange)} {{ BaseAddress = 0x{(ulong)BaseAddress:x8}, Size = {VirtualMemoryHelper.ByteCountToText(Size)} }}";
    }
}