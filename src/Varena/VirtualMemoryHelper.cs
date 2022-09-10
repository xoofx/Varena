// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Varena;

/// <summary>
/// A helper class.
/// </summary>
public static class VirtualMemoryHelper
{
    /// <summary>
    /// Converts a protection flags to a protection string (e.g <see cref="VirtualMemoryFlags.Read"/> will return "r--")
    /// </summary>
    /// <param name="flags">The protection flags.</param>
    /// <returns>A string representation of the flags (e.g "rwx" or "rw-") </returns>
    public static string ToText(this VirtualMemoryFlags flags)
    {
        return flags switch
        {
            VirtualMemoryFlags.All => "rwx",
            VirtualMemoryFlags.Read => "r--",
            VirtualMemoryFlags.Write => "-w-",
            VirtualMemoryFlags.ReadWrite => "rw-",
            VirtualMemoryFlags.Execute => "--x",
            (VirtualMemoryFlags.Read | VirtualMemoryFlags.Execute) => "r-x",
            (VirtualMemoryFlags.Write | VirtualMemoryFlags.Execute) => "-wx",
            VirtualMemoryFlags.None => "---",
            _ => "???"
        };
    }

    /// <summary>
    /// Converts the specified value in bytes to a string representation to the closest KiB, MiB, GiB, TiB.
    /// </summary>
    /// <param name="value">A value in number of bytes.</param>
    /// <returns>A friendly string representation.</returns>
    public static string ByteCountToText(ulong value)
    {
        FormattableString format;
        if (value == 0) return "0 B";

        if ((value & ((1ul << 40) - 1)) == 0) // TiB
        {
            format = $"{value >> 40} TiB";
        }
        else if ((value & ((1ul << 30) - 1)) == 0) // GiB
        {
            format = $"{value >> 30} GiB";
        }
        else if ((value & ((1ul << 20) - 1)) == 0) // MiB
        {
            format = $"{value >> 20} MiB";
        }
        else if ((value & ((1ul << 10) - 1)) == 0) // KiB
        {
            format = $"{value >> 10} KiB";
        }
        else
        {
            format = $"{value} B";
        }

        return format.ToString(CultureInfo.InvariantCulture);
    }
    
    /// <summary>
    /// Aligns up the specified value with the specified alignment.
    /// </summary>
    /// <param name="value">The value to align up.</param>
    /// <param name="align">The requested alignment.</param>
    /// <returns>The aligned value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint AlignToUpper(nuint value, uint align)
    {
        var nextValue = ((value + align - 1) / align) * align;
        return nextValue;
    }
}