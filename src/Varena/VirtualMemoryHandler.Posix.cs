// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Varena;

/// <summary>
/// Implementation of the handler for all Posix platforms (Linux, MacOS...)
/// </summary>
internal sealed class PosixVirtualMemoryHandler : VirtualMemoryHandler
{
    protected override VirtualMemoryRange TryReserveImpl(nuint size)
    {
        var anonFlags = OperatingSystem.IsMacOS() ? MapFlags.MAP_ANON : MapFlags.MAP_ANONYMOUS;
        var ptr = mmap(IntPtr.Zero, size, MemoryProtection.PROT_NONE, anonFlags | MapFlags.MAP_PRIVATE, 0, 0);
        ptr = (long)ptr == -1 ? IntPtr.Zero : ptr;
        return new VirtualMemoryRange(ptr, ptr == IntPtr.Zero ? 0 : size);
    }

    protected override bool TryCommitImpl(VirtualMemoryRange range, VirtualMemoryFlags flags)
    {
        return mprotect(range.BaseAddress, range.Size, GetMemoryProtection(flags)) == 0;
    }

    protected override bool TryUnCommitImpl(VirtualMemoryRange range)
    {
        return mprotect(range.BaseAddress, range.Size, MemoryProtection.PROT_NONE) == 0;
    }

    protected override bool TryProtectImpl(VirtualMemoryRange range, VirtualMemoryFlags flags)
    {
        return mprotect(range.BaseAddress, range.Size, GetMemoryProtection(flags)) == 0;
    }

    protected override bool TryFreeImpl(VirtualMemoryRange range)
    {
        return munmap(range.BaseAddress, range.Size) == 0;
    }

    private static MemoryProtection GetMemoryProtection(VirtualMemoryFlags flags)
    {
        var protect = MemoryProtection.PROT_NONE;

        if ((flags & VirtualMemoryFlags.Execute) != 0) protect |= MemoryProtection.PROT_EXEC;
        if ((flags & VirtualMemoryFlags.Read) != 0) protect |= MemoryProtection.PROT_READ;
        if ((flags & VirtualMemoryFlags.Write) != 0) protect |= MemoryProtection.PROT_WRITE;
        return protect;
    }

    [DllImport("libc", EntryPoint = nameof(mmap))]
    private static extern IntPtr mmap(IntPtr addr, nuint length, MemoryProtection prot, MapFlags flags, int fd, nint offset);

    [DllImport("libc", EntryPoint = nameof(munmap))]
    private static extern int munmap(IntPtr addr, nuint length);

    [DllImport("libc", EntryPoint = nameof(mprotect))]
    private static extern int mprotect(IntPtr addr, nuint len, MemoryProtection prot);

    [Flags]
    private enum MapFlags
    {
        MAP_PRIVATE = 0x02,
        MAP_ANONYMOUS = 0x20, // Linux
        MAP_ANON = 0x1000 // macOS
    }

    [Flags]
    enum MemoryProtection
    {
        PROT_NONE = 0,
        PROT_READ = 1,
        PROT_WRITE = 2,
        PROT_EXEC = 4,
    }
}