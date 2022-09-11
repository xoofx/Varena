// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Varena;

/// <summary>
/// Implementation of the handler for all the Windows platform.
/// </summary>
internal sealed class WindowsVirtualMemoryHandler : VirtualMemoryHandler
{
    protected override VirtualMemoryRange TryReserveImpl(nuint size)
    {
        var ptr = VirtualAlloc(IntPtr.Zero, size, AllocationType.Reserve, MemoryProtection.ReadWrite);
        return new VirtualMemoryRange(ptr,  ptr == IntPtr.Zero ? 0 : size);
    }

    protected override bool TryCommitImpl(VirtualMemoryRange range, VirtualMemoryFlags flags)
    {
        var ptr = VirtualAlloc(range.BaseAddress, range.Size, AllocationType.Commit, GetMemoryProtection(flags));
        return ptr == range.BaseAddress;
    }

    protected override bool TryUnCommitImpl(VirtualMemoryRange range)
    {
        return VirtualFree(range.BaseAddress, range.Size, FreeType.Decommit) != 0;
    }

    protected override bool TryProtectImpl(VirtualMemoryRange range, VirtualMemoryFlags flags)
    {
        return VirtualProtect(range.BaseAddress, range.Size, GetMemoryProtection(flags), out _) != 0;
    }

    protected override bool TryFreeImpl(VirtualMemoryRange range)
    {
        return VirtualFree(range.BaseAddress, 0, FreeType.Release) != 0;
    }

    private static MemoryProtection GetMemoryProtection(VirtualMemoryFlags flags)
    {
        if (flags == VirtualMemoryFlags.None)
        {
            return MemoryProtection.NoAccess;
        }

        if ((flags & VirtualMemoryFlags.Execute) != 0)
        {
            if ((flags & (VirtualMemoryFlags.Write)) != 0) return MemoryProtection.ExecuteReadWrite;
            if ((flags & (VirtualMemoryFlags.Read)) != 0) return MemoryProtection.ExecuteRead;
            return MemoryProtection.Execute;
        }

        if ((flags & (VirtualMemoryFlags.Write)) != 0) return MemoryProtection.ReadWrite;
        return MemoryProtection.ReadOnly;
    }

    [DllImport("kernel32", EntryPoint = nameof(VirtualAlloc), SetLastError = true)]
    private static extern IntPtr VirtualAlloc(IntPtr lpAddress, nuint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

    [DllImport("kernel32", EntryPoint = nameof(VirtualFree), SetLastError = true)]
    private static extern int VirtualFree(IntPtr lpAddress, nuint dwSize, FreeType dwFreeType);

    [DllImport("kernel32", EntryPoint = nameof(VirtualProtect), SetLastError = true)]
    private static extern int VirtualProtect(IntPtr lpAddress, nuint dwSize, MemoryProtection newProtect, out MemoryProtection oldProtect);
    
    [Flags]
    private enum AllocationType
    {
        Commit = 0x1000,
        Reserve = 0x2000,
        Decommit = 0x4000,
        Release = 0x8000,
        Reset = 0x80000,
        Physical = 0x400000,
        TopDown = 0x100000,
        WriteWatch = 0x200000,
        LargePages = 0x20000000
    }

    [Flags]
    enum MemoryProtection
    {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        GuardModifierflag = 0x100,
        NoCacheModifierflag = 0x200,
        WriteCombineModifierflag = 0x400
    }

    [Flags]
    enum FreeType
    {
        Decommit = 0x4000,
        Release = 0x8000,
    }
}