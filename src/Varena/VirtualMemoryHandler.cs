// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace Varena;

/// <summary>
/// Base class for interfacing with the low level OS virtual memory functions.
/// </summary>
public abstract class VirtualMemoryHandler
{
    /// <summary>
    /// Gets an instance of this type for the current platform.
    /// </summary>
    public static readonly VirtualMemoryHandler Instance = OperatingSystem.IsWindows() ? new WindowsVirtualMemoryHandler() : new PosixVirtualMemoryHandler();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    protected VirtualMemoryHandler()
    {
        PageSize = (uint)Environment.SystemPageSize;
    }

    /// <summary>
    /// Gets the OS size of a page in bytes.
    /// </summary>
    public uint PageSize { get; }

    /// <summary>
    /// Tries to reserve the specified amount of memory in bytes.
    /// </summary>
    /// <param name="size">The amount of memory in bytes to reserve.</param>
    /// <returns>A range of memory. Check <see cref="VirtualMemoryRange.IsNull"/> to verify that the reservation was successful.</returns>
    public VirtualMemoryRange TryReserve(nuint size)
    {
        size = VirtualMemoryHelper.AlignToUpper(size, PageSize);
        return TryReserveImpl(size);
    }


    /// <summary>
    /// Tries to commit the specified range of the memory (previously reserved by <see cref="TryReserve"/> and associated protection flags).
    /// </summary>
    /// <param name="range">The range of the memory to commit.</param>
    /// <param name="flags">The protection flags to apply.</param>
    /// <returns><c>true</c> if successful; <c>false</c> otherwise.</returns>
    public bool TryCommit(VirtualMemoryRange range, VirtualMemoryFlags flags)
    {
        VerifyRange(range);
        return TryCommitImpl(range, flags);
    }

    /// <summary>
    /// Tries to un-commit the specified range of the memory (previously reserved by <see cref="TryReserve"/>).
    /// </summary>
    /// <param name="range">The range of the memory to un-commit.</param>
    /// <returns><c>true</c> if successful; <c>false</c> otherwise.</returns>
    public bool TryUnCommit(VirtualMemoryRange range)
    {
        VerifyRange(range);
        return TryUnCommitImpl(range);
    }

    /// <summary>
    /// Tries to protect the specified range of the memory (previously reserved by <see cref="TryReserve"/> and associated protection flags).
    /// </summary>
    /// <param name="range">The range of the memory to protect.</param>
    /// <param name="flags">The protection flags to apply.</param>
    /// <returns><c>true</c> if successful; <c>false</c> otherwise.</returns>
    public bool TryProtect(VirtualMemoryRange range, VirtualMemoryFlags flags)
    {
        VerifyRange(range);
        return TryProtectImpl(range, flags);
    }

    /// <summary>
    /// Tries to free the specified range of the memory (previously reserved by <see cref="TryReserve"/>.
    /// </summary>
    /// <param name="range">The range of the memory to free.</param>
    /// <returns><c>true</c> if successful; <c>false</c> otherwise.</returns>
    public bool TryFree(VirtualMemoryRange range)
    {
        VerifyRange(range);
        return TryFreeImpl(range);
    }


    /// <summary>
    /// Implementation of the <see cref="TryReserve"/>.
    /// </summary>
    /// <param name="size">The amount of memory in bytes to reserve.</param>
    /// <returns>A range of memory. Check <see cref="VirtualMemoryRange.IsNull"/> to verify that the reservation was successful.</returns>
    protected abstract VirtualMemoryRange TryReserveImpl(nuint size);

    /// <summary>
    /// Implementation of the <see cref="TryCommit"/>.
    /// </summary>
    /// <param name="range">The range of the memory to commit.</param>
    /// <param name="flags">The protection flags to apply.</param>
    /// <returns><c>true</c> if successful; <c>false</c> otherwise.</returns>
    protected abstract bool TryCommitImpl(VirtualMemoryRange range, VirtualMemoryFlags flags);

    /// <summary>
    /// Implementation of the <see cref="TryUnCommit"/>.
    /// </summary>
    /// <param name="range">The range of the memory to un-commit.</param>
    /// <returns><c>true</c> if successful; <c>false</c> otherwise.</returns>
    protected abstract bool TryUnCommitImpl(VirtualMemoryRange range);

    /// <summary>
    /// Implementation of the <see cref="TryProtect"/>.
    /// </summary>
    /// <param name="range">The range of the memory to protect.</param>
    /// <param name="flags">The protection flags to apply.</param>
    /// <returns><c>true</c> if successful; <c>false</c> otherwise.</returns>
    protected abstract bool TryProtectImpl(VirtualMemoryRange range, VirtualMemoryFlags flags);

    /// <summary>
    /// Implementation of the <see cref="TryFree"/>.
    /// </summary>
    /// <param name="range">The range of the memory to free.</param>
    /// <returns><c>true</c> if successful; <c>false</c> otherwise.</returns>
    protected abstract bool TryFreeImpl(VirtualMemoryRange range);

    private void VerifyRange(VirtualMemoryRange range)
    {
        var baseAddress = (ulong)range.BaseAddress;
        if (baseAddress == 0 || (baseAddress % PageSize) != 0)
        {
            throw new ArgumentException($"The BaseAddress (0x{baseAddress:X16}) of the memory range  must be a multiple of PageSize = {PageSize}", nameof(range));
        }

        if (range.Size == 0 || (range.Size % PageSize) != 0)
        {
            throw new ArgumentException($"The Size ({range.Size}) of the memory range be a multiple of PageSize = {PageSize}", nameof(range));
        }
    }
}