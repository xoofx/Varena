// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace Varena;

/// <summary>
/// An arena exposed as a buffer of bytes.
/// </summary>
/// <remarks>
/// This class is not thread-safe for performance reasons. You need to protect its usage if you are allocating from multiple threads.
/// </remarks>
public sealed class VirtualBuffer : VirtualArena
{
    internal VirtualBuffer(VirtualArenaManager manager, string name, VirtualMemoryRange range, uint commitPageSizeMultiplier) : base(manager, name, range, commitPageSizeMultiplier)
    {
    }

    /// <summary>
    /// Gets a reference to the byte at the specified index.
    /// </summary>
    /// <param name="index">The index to get the reference.</param>
    /// <returns>A reference to the byte at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If index is out of bounds.</exception>
    public ref byte this[nint index]
    {
        get
        {
            if ((nuint)index >= (nuint)AllocatedBytes) throw new ArgumentOutOfRangeException(nameof(index));
            unsafe
            {
                return ref *(((byte*)BaseAddress) + index);
            }
        }
    }

    /// <summary>
    /// Allocates a range of memory.
    /// </summary>
    /// <param name="count">The number of bytes to allocate.</param>
    /// <returns>The span of the allocated memory.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If count &lt;= 0.</exception>
    public Span<byte> AllocateRange(int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "count must be > 0");
        unsafe
        {
            var span = new Span<byte>(base.UnsafeAllocate((nuint)count), count);
            span.Fill(0);
            return span;
        }
    }

    /// <summary>
    /// Allocates a range of memory.
    /// </summary>
    /// <param name="count">The number of bytes to allocate.</param>
    /// <param name="firstIndex">The index of the first byte.</param>
    /// <returns>The span of the allocated memory.</returns>
    public Span<byte> AllocateRange(int count, out ulong firstIndex)
    {
        firstIndex = AllocatedBytes;
        return AllocateRange(count);
    }

    /// <summary>
    /// Gets the span over all the elements allocated.
    /// </summary>
    /// <returns>The span over all the elements allocated.</returns>
    /// <exception cref="VirtualMemoryException">If this buffer has allocated more than <see cref="int.MaxValue"/> and so cannot be represented by a Span.</exception>
    public Span<byte> AsSpan()
    {
        if (AllocatedBytes > int.MaxValue) throw new VirtualMemoryException($"Cannot convert to a span. The size ({AllocatedBytes} bytes) of the arena `{Name}` must be <= {int.MaxValue}");
        unsafe
        {
            return new Span<byte>((void*)base.BaseAddress, (int)AllocatedBytes);
        }
    }

    /// <summary>
    /// Gets the span over the a specified range.
    /// </summary>
    /// <param name="index">The first index (bytes) of the range.</param>
    /// <param name="count">The number of bytes in the range.</param>
    /// <returns>The span over all the elements allocated.</returns>
    /// <exception cref="VirtualMemoryException">If this buffer has allocated more than <see cref="int.MaxValue"/> and so cannot be represented by a Span.</exception>
    public Span<byte> AsSpan(nint index, int count)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), $"The index must be > 0");
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), $"The count must be > 0");
        if ((nuint)(index + count) > AllocatedBytes) throw new ArgumentOutOfRangeException(nameof(index), $"The index + count ({index+count}) must be <= {AllocatedBytes}");
        unsafe
        {
            return new Span<byte>((byte*)base.BaseAddress + index, count);
        }
    }
}