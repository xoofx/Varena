using NUnit.Framework;
using System;

namespace Varena.Tests;

public class TestVirtualArena
{
    [Test]
    public void TestCreateBufferReset()
    {
        using var manager = new VirtualArenaManager();
        var buffer = manager.CreateBuffer("Hello", 1 << 20);
        Assert.AreEqual("Hello", buffer.Name);
        Assert.AreEqual(VirtualMemoryFlags.ReadWrite, buffer.Flags);
        var span = buffer.AllocateRange(1024);
        Assert.AreEqual((nuint)1024, buffer.AllocatedBytes);

        buffer.Reset();
        Assert.AreEqual((nuint)0, buffer.AllocatedBytes);
        Assert.AreEqual((nuint)0, buffer.CommittedBytes);
        Assert.AreEqual((nuint)1<<20, buffer.AvailableInBytes);

        var span2 = buffer.AllocateRange(1024);
        Assert.AreEqual((nuint)1024, buffer.AllocatedBytes);
        Assert.AreEqual((nuint)1 << 16, buffer.CommittedBytes);
    }

    [Test]
    public void TestCreateArrayReset()
    {
        using var manager = new VirtualArenaManager();
        var array = manager.CreateArray<int>("Hello", 1 << 20);
        Assert.AreEqual("Hello", array.Name);
        Assert.AreEqual(VirtualMemoryFlags.ReadWrite, array.Flags);
        var span = array.AllocateRange(1024);
        Assert.AreEqual((nuint)1024 * sizeof(int), array.AllocatedBytes);
        Assert.AreEqual(1024, array.Count);

        array.Reset();
        Assert.AreEqual((nuint)0, array.AllocatedBytes);
        Assert.AreEqual((nuint)0, array.CommittedBytes);
        Assert.AreEqual((nuint)1 << 20, array.AvailableInBytes);
        Assert.AreEqual(0, array.Count);

        var span2 = array.AllocateRange(1024);
        Assert.AreEqual((nuint)1024 * sizeof(int), array.AllocatedBytes);
        Assert.AreEqual((nuint)1 << 16, array.CommittedBytes);
        Assert.AreEqual(1024, array.Count);
    }

    [Test]
    public void TestAllocationOutOfCapacity()
    {
        using var manager = new VirtualArenaManager();
        var buffer = manager.CreateBuffer("Hello", 1 << 20);
        _ = buffer.AllocateRange(1 << 20);
        Assert.Throws<VirtualMemoryException>(() => { buffer.AllocateRange(1); });
    }

    [Test]
    public void TestToString()
    {
        using var manager = new VirtualArenaManager();
        var buffer = manager.CreateBuffer("Hello", 1 << 20);
        var arenaAsText = buffer.ToString();
        // VirtualArena { Name = "Hello", BaseAddress = 0x000001e239060000, Allocated = 0 B, Committed = 0 B, Available = 1 MiB, Flags = rw- }
        Console.WriteLine(arenaAsText);
        StringAssert.IsMatch("VirtualArena \\{ Name = \"Hello\", BaseAddress = 0x[0-9a-f]+, Allocated = 0 B, Committed = 0 B, Available = 1 MiB, Flags = rw- \\}", arenaAsText);
    }

    [Test]
    public void TestProtect()
    {
        using var manager = new VirtualArenaManager();
        var buffer = manager.CreateBuffer("Hello", 1 << 20);
        var span = buffer.AllocateRange(1024);
        buffer.Protect(VirtualMemoryFlags.All);
        Assert.AreEqual(VirtualMemoryFlags.All, buffer.Flags);
        span[0] = 1;
        buffer.Protect(VirtualMemoryFlags.Read);

        Assert.AreEqual((byte)1, span[0]);
        Assert.AreEqual(VirtualMemoryFlags.Read, buffer.Flags);
    }

    [Test]
    public void TestCommitPageSizeMultiplier()
    {
        using var manager = new VirtualArenaManager();
        var buffer = manager.CreateBuffer("Hello", 1 << 20);
        var defaultCommitSizeMultiplier = (1 << 16) / manager.Handler.PageSize;
        Assert.AreEqual(defaultCommitSizeMultiplier, buffer.CommitPageSizeMultiplier);

        var buffer2 = manager.CreateBuffer("Hello", 1 << 20, 1);
        Assert.AreEqual(1, buffer2.CommitPageSizeMultiplier);
    }
}