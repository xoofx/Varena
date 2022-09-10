// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace Varena;

/// <summary>
/// The manager for creating <see cref="VirtualBuffer"/> and <see cref="VirtualArray{T}"/>.
/// </summary>
public sealed class VirtualArenaManager : IDisposable
{
    private readonly List<VirtualArena> _arenas;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of this manager.
    /// </summary>
    public VirtualArenaManager() : this(VirtualMemoryHandler.Instance)
    {
    }


    /// <summary>
    /// Creates a new instance of this manager.
    /// </summary>
    /// <param name="defaultCommitPageSizeMultiplier">The number of OS page to commit when allocating memory. If set to 0, it will try to commit 64 KiB at a time.</param>
    public VirtualArenaManager(uint defaultCommitPageSizeMultiplier) : this(VirtualMemoryHandler.Instance, defaultCommitPageSizeMultiplier)
    {
    }

    /// <summary>
    /// Creates a new instance of this manager.
    /// </summary>
    /// <param name="handler">The virtual memory handler to use for this manager.</param>
    /// <param name="defaultCommitPageSizeMultiplier">The number of OS page to commit when allocating memory. If set to 0, it will try to commit 64 KiB at a time.</param>
    public VirtualArenaManager(VirtualMemoryHandler handler, uint defaultCommitPageSizeMultiplier = 0)
    {
        Handler = handler;
        _arenas = new List<VirtualArena>();
        // By default, if defaultCommitPageSizeMultiplier == 0, commit per 64 KiB
        var commitMultiplier = (1 << 16) / handler.PageSize;
        commitMultiplier = commitMultiplier > 0 ? commitMultiplier : 1;
        DefaultCommitPageSizeMultiplier = defaultCommitPageSizeMultiplier == 0 ? commitMultiplier : defaultCommitPageSizeMultiplier;
    }

    /// <summary>
    /// Gets the associated virtual memory handler.
    /// </summary>
    public VirtualMemoryHandler Handler { get; }

    /// <summary>
    /// Gets a boolean indicating whether this instance is disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Gets the default multiplier used to commit a block of virtual memory pages. By default, this multiplier tries to commit 64 KiB.
    /// </summary>
    public uint DefaultCommitPageSizeMultiplier { get; }

    /// <summary>
    /// Get a list of the <see cref="VirtualArena"/> (either <see cref="VirtualBuffer"/> or <see cref="VirtualArray{T}"/>) managed by this manager.
    /// </summary>
    /// <returns>A list of <see cref="VirtualArena"/>.</returns>
    public List<VirtualArena> GetArenas()
    {
        var arenas = new List<VirtualArena>();
        GetArenasTo(arenas);
        return arenas;
    }

    /// <summary>
    /// Get a list of the <see cref="VirtualArena"/> (either <see cref="VirtualBuffer"/> or <see cref="VirtualArray{T}"/>) managed by this manager.
    /// </summary>
    /// <param name="outputList">The list to receive the list of <see cref="VirtualArena"/>.</param>
    public void GetArenasTo(List<VirtualArena> outputList)
    {
        lock (_arenas)
        {
            outputList.AddRange(_arenas);
        }
    }

    /// <summary>
    /// Creates a byte buffer of the specified size. Note that the memory is reserved but not allocated/committed.
    /// </summary>
    /// <param name="name">The name of this arena.</param>
    /// <param name="capacityInBytes">The capacity in bytes.</param>
    /// <param name="commitPageSizeMultiplier">The multiplier used to commit a block of virtual memory pages. By default (== 0), this multiplier tries to commit 64 KiB.</param>
    /// <returns>The byte buffer reserved with the specified capacity.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacityInBytes"/> is == 0.</exception>
    /// <exception cref="VirtualMemoryException">If it was not possible to reserve the memory.</exception>
    public VirtualBuffer CreateBuffer(string name, nuint capacityInBytes, uint commitPageSizeMultiplier = 0)
    {
        if (capacityInBytes == 0) throw new ArgumentOutOfRangeException(nameof(capacityInBytes), "Must be > 0");
        VerifyNotDisposed();

        commitPageSizeMultiplier = commitPageSizeMultiplier == 0 ? DefaultCommitPageSizeMultiplier : commitPageSizeMultiplier;
        capacityInBytes = VirtualMemoryHelper.AlignToUpper(capacityInBytes, Handler.PageSize * commitPageSizeMultiplier);
        var range = Handler.TryReserve(capacityInBytes);
        if (range.IsNull) throw new VirtualMemoryException($"Cannot reserve {capacityInBytes} bytes");

        var arena = new VirtualBuffer(this, name, range, commitPageSizeMultiplier);

        AddArena(arena);
        return arena;
    }

    /// <summary>
    /// Creates a data array of the specified size. Note that the memory is reserved but not allocated/committed.
    /// </summary>
    /// <typeparam name="T">The element type of the array.</typeparam>
    /// <param name="name">The name of this arena.</param>
    /// <param name="capacityInBytes">The capacity in bytes.</param>
    /// <param name="commitPageSizeMultiplier">The multiplier used to commit a block of virtual memory pages. By default (== 0), this multiplier tries to commit 64 KiB.</param>
    /// <returns>The byte buffer reserved with the specified capacity.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacityInBytes"/> is == 0.</exception>
    /// <exception cref="VirtualMemoryException">If it was not possible to reserve the memory.</exception>
    public VirtualArray<T> CreateArray<T>(string name, nuint capacityInBytes, uint commitPageSizeMultiplier = 0) where T: unmanaged
    {
        if (capacityInBytes == 0) throw new ArgumentOutOfRangeException(nameof(capacityInBytes), "Must be > 0");
        VerifyNotDisposed();
        commitPageSizeMultiplier = commitPageSizeMultiplier == 0 ? DefaultCommitPageSizeMultiplier : commitPageSizeMultiplier;

        unsafe
        {
            // Make sure that the capacity requested can hold at least one T and fit into one page
            capacityInBytes = VirtualMemoryHelper.AlignToUpper(Math.Max(capacityInBytes, (uint)sizeof(T)), Handler.PageSize * commitPageSizeMultiplier);
            var range = Handler.TryReserve(capacityInBytes);
            if (range.IsNull) throw new VirtualMemoryException($"Cannot reserve {capacityInBytes} bytes");

            var arena = new VirtualArray<T>(this, name, range, commitPageSizeMultiplier);

            AddArena(arena);
            return arena;
        }
    }

    /// <summary>
    /// Dispose this manager and all created <see cref="VirtualArena"/>.
    /// </summary>
    public void Dispose()
    {
        lock (_arenas)
        {
            if (_disposed) return;
            _disposed = true;
        }

        ReleaseUnmanagedResources();
    }

    private void AddArena(VirtualArena arena)
    {
        lock (_arenas)
        {
            _arenas.Add(arena);
        }
    }

    internal void RemoveArena(VirtualArena arena)
    {
        lock (_arenas)
        {
            var indexOf = _arenas.IndexOf(arena);
            if (indexOf >= 0)
            {
                _arenas.RemoveAt(indexOf);
            }
        }
    }
        
    private void ReleaseUnmanagedResources()
    {
        lock (_arenas)
        {
            while (_arenas.Count > 0)
            {
                var arena = _arenas[^1];
                arena.Dispose();
            }
            _arenas.Clear();
        }
    }

    private void VerifyNotDisposed()
    {
        if (_disposed) throw new InvalidOperationException("Cannot create an arena if the manager is disposed");
    }
}