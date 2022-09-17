// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Varena;

/// <summary>
/// A region of virtual memory that is either <see cref="VirtualBuffer"/> or a <see cref="VirtualArray{T}"/>.
/// </summary>
public abstract class VirtualArena : IDisposable
{
    private readonly VirtualMemoryHandler _handler;
    private nuint _committedBytes;
    private nuint _allocatedBytes;
    private VirtualMemoryFlags _currentFlags;
    private VirtualMemoryRange _range;
    private readonly uint _commitPageSizeMultiplier;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of this arena.
    /// </summary>
    /// <param name="manager">The associated manager.</param>
    /// <param name="name">The name of this arena.</param>
    /// <param name="range">The range of virtual memory.</param>
    /// <param name="commitPageSizeMultiplier">The commit page multiplier.</param>
    protected VirtualArena(VirtualArenaManager manager, string name, VirtualMemoryRange range, uint commitPageSizeMultiplier)
    {
        Manager = manager;
        Name = name;
        _range = range;
        _commitPageSizeMultiplier = commitPageSizeMultiplier;
        _handler = manager.Handler;
        _currentFlags = VirtualMemoryFlags.ReadWrite;
    }

    /// <summary>
    /// Gets the associated manager.
    /// </summary>
    public VirtualArenaManager Manager { get; }

    /// <summary>
    /// Gets the name of this arena.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the base address of this arena.
    /// </summary>
    public IntPtr BaseAddress => _range.BaseAddress;

    /// <summary>
    /// Gets the memory range.
    /// </summary>
    public VirtualMemoryRange Range => _range;

    /// <summary>
    /// Gets the protection flags.
    /// </summary>
    public VirtualMemoryFlags Flags => _currentFlags;

    /// <summary>
    /// Gets the number of bytes allocated.
    /// </summary>
    public nuint AllocatedBytes => _allocatedBytes;

    /// <summary>
    /// Gets the number of bytes committed.
    /// </summary>
    public nuint CommittedBytes => _committedBytes;

    /// <summary>
    /// Gets the total capacity in bytes.
    /// </summary>
    public nuint CapacityInBytes => _range.Size;

    /// <summary>
    /// Gets the remaining number of available bytes that can be allocated.
    /// </summary>
    public nuint AvailableInBytes => CapacityInBytes - AllocatedBytes;

    /// <summary>
    /// Gets the multiplier used to commit a group of OS pages when committing memory.
    /// </summary>
    public uint CommitPageSizeMultiplier => _commitPageSizeMultiplier;

    /// <summary>
    /// Gets a boolean indicating if this arena is disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Disposes this instance and removes it from the associated manager.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Manager.RemoveArena(this);
        ReleaseUnmanagedResources();
    }

    /// <summary>
    /// Resets the memory allocated by this arena, decommit the memory but keep the arena opened for further allocations. 
    /// </summary>
    /// <exception cref="VirtualMemoryException">If this method fails to uncommit the memory used by this arena.</exception>
    public void Reset()
    {
        Reset(VirtualArenaResetKind.Default);
    }

    /// <summary>
    /// Resets the memory allocated by this arena with the specified reset options.
    /// </summary>
    /// <param name="resetKind">The kind of reset.</param>
    /// <exception cref="VirtualMemoryException">If this method fails to uncommit the memory used by this arena.</exception>
    public void Reset(VirtualArenaResetKind resetKind)
    {
        // We decommit only if we have something to decommit.
        if (CommittedBytes > 0)
        {
            bool decommitSuccess = true;
            switch (resetKind)
            {
                case VirtualArenaResetKind.Default:
                    // We decommit the entire committed bytes
                    decommitSuccess = _handler.TryUnCommit(new VirtualMemoryRange(this.BaseAddress, _committedBytes));
                    _committedBytes = 0;
                    break;
                case VirtualArenaResetKind.KeepAllCommitted:
                    break;
                case VirtualArenaResetKind.KeepMinimalCommitted:
                    // We decommit only entire committed bytes
                    var newCommittedBytes = this.CommitPageSizeMultiplier * this._handler.PageSize;
                    var bytesToDecommit = _committedBytes - newCommittedBytes;
                    if (bytesToDecommit > 0)
                    {
                        decommitSuccess = _handler.TryUnCommit(new VirtualMemoryRange((nint)this.BaseAddress + (nint)(newCommittedBytes), bytesToDecommit));
                    }
                    _committedBytes = newCommittedBytes;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resetKind), resetKind, null);
            }

            if (!decommitSuccess)
            {
                throw new VirtualMemoryException($"Unable to un-commit the memory range for the arena `{Name}`");
            }
        }

        _allocatedBytes = 0;
        ResetImpl();
    }

    /// <summary>
    /// Protects the range of the allocated memory with the specified protection flags.
    /// </summary>
    /// <param name="flags">The protection flags to apply.</param>
    /// <exception cref="VirtualMemoryException"></exception>
    public void Protect(VirtualMemoryFlags flags)
    {
        if (_committedBytes > 0 && !_handler.TryProtect(_range with { Size = _committedBytes }, flags))
        {
            throw new VirtualMemoryException($"Unable to protect the memory range for the arena `{Name}` with the flags {flags}");
        }
        _currentFlags = flags;
    }

    /// <summary>
    /// Display a string representation of this arena.
    /// </summary>
    /// <returns>The string representation of this arena.</returns>
    public override string ToString()
    {
        if (IntPtr.Size == 8)
        {
            FormattableString format =
                $"VirtualArena {{ Name = \"{Name}\", BaseAddress = 0x{(ulong)BaseAddress:x16}, Allocated = {VirtualMemoryHelper.ByteCountToText(AllocatedBytes)}, Committed = {VirtualMemoryHelper.ByteCountToText(CommittedBytes)}, Available = {VirtualMemoryHelper.ByteCountToText(AvailableInBytes)}, Flags = {Flags.ToText()} }}";
            return format.ToString(CultureInfo.InvariantCulture);

        }
        else
        {

            FormattableString format =
                $"VirtualArena {{ Name = \"{Name}\", BaseAddress = 0x{(uint)BaseAddress:x8}, Allocated = {VirtualMemoryHelper.ByteCountToText(AllocatedBytes)}, Committed = {VirtualMemoryHelper.ByteCountToText(CommittedBytes)}, Available = {VirtualMemoryHelper.ByteCountToText(AvailableInBytes)}, Flags = {Flags.ToText()} }}";
            return format.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Implement this method when a reset occurs.
    /// </summary>
    protected virtual void ResetImpl()
    {
    }

    /// <summary>
    /// Implement this method when a dispose occurs.
    /// </summary>
    protected virtual void DisposeImpl()
    {
    }

    /// <summary>
    /// Allocates the number of bytes requested.
    /// </summary>
    /// <param name="size">The number of bytes to allocate.</param>
    /// <returns>A base pointer to the memory allocated.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe void* UnsafeAllocate(nuint size)
    {
        var currentAllocatedBytes = _allocatedBytes;
        var requestedOffset = currentAllocatedBytes + size;
        if (requestedOffset > _committedBytes)
        {
            EnsureCapacity(requestedOffset);
        }
        else
        {
            _allocatedBytes = requestedOffset;
        }

        return (byte*)BaseAddress + (long)currentAllocatedBytes;
    }

    private void EnsureCapacity(nuint requestedOffset)
    {
        var nextSize = VirtualMemoryHelper.AlignToUpper(requestedOffset - _committedBytes, _handler.PageSize * _commitPageSizeMultiplier);
        var nextOffset = _committedBytes + nextSize;

        if (nextOffset > CapacityInBytes)
        {
            throw new VirtualMemoryException($"Out of memory in arena `{Name}` Requested = {nextOffset} bytes while the maximum Capacity = {CapacityInBytes}");
        }

        if (!_handler.TryCommit(new VirtualMemoryRange(new IntPtr(Range.BaseAddress.ToInt64() + (long)_committedBytes), nextSize), _currentFlags))
        {
            throw new VirtualMemoryException($"Virtual memory range 0x{_committedBytes:X16}-0x{(_committedBytes + nextSize):X16} ({nextSize} bytes) in arena `{Name}` cannot be accessed with flags {_currentFlags.ToText()}");
        }

        _committedBytes += nextSize;
        _allocatedBytes = requestedOffset;
    }

    private void ReleaseUnmanagedResources()
    {
        var freeResult = _handler.TryFree(Range);
        Debug.Assert(freeResult, "Unable to free memory");
        _range = default;
        _currentFlags = VirtualMemoryFlags.None;
        _committedBytes = 0;
        _allocatedBytes = 0;
        DisposeImpl();
    }
}