// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Varena;

/// <summary>
/// An arena exposed as an array of elements.
/// </summary>
/// <remarks>
/// This class is not thread-safe for performance reasons. You need to protect its usage if you are allocating from multiple threads.
/// </remarks>
/// <typeparam name="T">The type of element of this array.</typeparam>
public sealed class VirtualArray<T> : VirtualArena where T: unmanaged
{
    private int _count;

    internal VirtualArray(VirtualArenaManager manager, string name, VirtualMemoryRange range, uint commitPageSizeMultiplier) : base(manager, name, range, commitPageSizeMultiplier)
    {
    }

    /// <summary>
    /// Gets the number of elements of type <typeparamref name="T"/> in this virtual array.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets a reference to the value at the specified index.
    /// </summary>
    /// <param name="index">Index </param>
    /// <returns>A reference to the value at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If the index is out of bounds.</exception>
    public ref T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count) throw new ArgumentOutOfRangeException(nameof(index));
            unsafe
            {
                return ref *(((T*)BaseAddress) + index);
            }
        }
    }

    /// <summary>
    /// Allocates a single element.
    /// </summary>
    /// <returns>A reference to the allocated element.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Allocate()
    {
        unsafe
        {
            ref var localRef = ref *(T*)base.UnsafeAllocate((nuint)sizeof(T));
            localRef = default;
            _count++;
            return ref localRef;
        }
    }

    /// <summary>
    /// Allocates a single element.
    /// </summary>
    /// <param name="index">The index of the allocated element.</param>
    /// <returns>A reference to the allocated element.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Allocate(out int index)
    {
        index = _count;
        ref var localRef = ref Allocate();
        return ref localRef;
    }

    /// <summary>
    /// Allocates a range of elements.
    /// </summary>
    /// <param name="count">The number of elements to allocate.</param>
    /// <returns>A span to the range of elements allocated.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If count &lt;= 0 </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AllocateRange(int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "count must be > 0");
        unsafe
        {
            var span = new Span<T>(base.UnsafeAllocate((nuint)count * (uint)sizeof(T)), count);
            span.Fill(default);
            _count += count;
            return span;
        }
    }

    /// <summary>
    /// Allocates a range of elements.
    /// </summary>
    /// <param name="count">The number of elements to allocate.</param>
    /// <param name="firstIndex">The index of the first allocated element.</param>
    /// <returns>A span to the range of elements allocated.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If count &lt;= 0 </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AllocateRange(int count, out int firstIndex)
    {
        firstIndex = _count;
        return AllocateRange(count);
    }

    /// <summary>
    /// Gets the span over all the elements allocated.
    /// </summary>
    /// <returns>The span over all the elements allocated.</returns>
    public Span<T> AsSpan()
    {
        unsafe
        {
            return new Span<T>((void*)base.BaseAddress, _count);
        }
    }

    /// <summary>
    /// Gets the span over the specified range.
    /// </summary>
    /// <param name="index">The first index of the range.</param>
    /// <param name="count">The number of elements in the range.</param>
    /// <returns>The span over the specified range.</returns>
    public Span<T> AsSpan(int index, int count)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), $"The index must be > 0");
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), $"The count must be > 0");
        if ((index + count) > Count) throw new ArgumentOutOfRangeException(nameof(index), $"The index + count ({index + count}) must be <= {Count}");

        unsafe
        {
            return new Span<T>((T*)base.BaseAddress + index, count);
        }
    }

    /// <inheritdoc />
    protected override void ResetImpl()
    {
        _count = 0;
    }

    /// <inheritdoc />
    protected override void DisposeImpl()
    {
        _count = 0;
    }
}