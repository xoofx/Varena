namespace Varena.Tests;
using NUnit.Framework;

public class TestVirtualArenaManager
{
    [Test]
    public void TestCreateBuffer()
    {
        var manager = new VirtualArenaManager();
        var buffer = manager.CreateBuffer("Hello", 1 << 20);
        Assert.AreNotEqual(IntPtr.Zero, buffer.BaseAddress);
        Assert.AreEqual((nuint)0, buffer.AllocatedBytes);
        Assert.AreEqual((nuint)(1<<20),buffer.CapacityInBytes);

        var span = buffer.AllocateRange(1024);
        span[0] = 1;
        Assert.AreEqual((byte)1, span[0]);
        Assert.AreEqual((nuint)1024, buffer.AllocatedBytes);

        var span2 = buffer.AllocateRange(1024);
        span2[0] = 2;
        Assert.AreEqual((nuint)2048, buffer.AllocatedBytes);
        Assert.AreEqual((byte)2, span2[0]);

        // Check that commit is equal to 64 KiB
        Assert.AreEqual((nuint)(1 << 16), buffer.CommittedBytes);

        Assert.AreEqual(1, manager.GetArenas().Count, "Expecting 1 arena");

        manager.Dispose();

        Assert.AreEqual(IntPtr.Zero, buffer.BaseAddress);
        Assert.AreEqual((nuint)0, buffer.CapacityInBytes);
        Assert.AreEqual((nuint)0, buffer.AllocatedBytes);

        Assert.AreEqual(0, manager.GetArenas().Count, "Expecting arenas to be empty");

        // Should be ok to double call them
        manager.Dispose();
        buffer.Dispose();
    }


    [Test]
    public void TestCreateArray()
    {
        var manager = new VirtualArenaManager();
        var buffer = manager.CreateArray<int>("Hello", 1 << 20);
        Assert.AreNotEqual(IntPtr.Zero, buffer.BaseAddress);
        Assert.AreEqual((nuint)0, buffer.AllocatedBytes);
        Assert.AreEqual((nuint)(1 << 20), buffer.CapacityInBytes);

        var span = buffer.AllocateRange(1024);
        span[0] = 1;
        Assert.AreEqual((int)1, span[0]);
        Assert.AreEqual((nuint)(sizeof(int)*1024), buffer.AllocatedBytes);

        var span2 = buffer.AllocateRange(1024);
        span2[0] = 2;
        Assert.AreEqual((nuint)(sizeof(int)*2048), buffer.AllocatedBytes);
        Assert.AreEqual((int)2, span2[0]);

        // Check that commit is equal to 64 KiB
        Assert.AreEqual((nuint)(1 << 16), buffer.CommittedBytes);

        Assert.AreEqual(1, manager.GetArenas().Count, "Expecting 1 arena");

        Assert.IsFalse(manager.IsDisposed);
        Assert.IsFalse(buffer.IsDisposed);

        manager.Dispose();

        Assert.IsTrue(manager.IsDisposed);
        Assert.IsTrue(buffer.IsDisposed);
        Assert.AreEqual(IntPtr.Zero, buffer.BaseAddress);
        Assert.AreEqual((nuint)0, buffer.CapacityInBytes);
        Assert.AreEqual((nuint)0, buffer.AllocatedBytes);
        Assert.AreEqual(0, manager.GetArenas().Count, "Expecting arenas to be empty");

        // Should be ok to double call them
        manager.Dispose();
        buffer.Dispose();

        Assert.IsTrue(manager.IsDisposed);
        Assert.IsTrue(buffer.IsDisposed);
    }

    [Test]
    public void TestCreateBufferWithCustomMultiplier()
    {
        using var manager = new VirtualArenaManager(1);
        var buffer = manager.CreateBuffer("Hello", 1 << 20);
        _ = buffer.AllocateRange(1024);
        Assert.AreEqual((nuint)manager.Handler.PageSize, buffer.CommittedBytes);

        var buffer2 = manager.CreateBuffer("Hello", 1 << 20, 2);
        _ = buffer2.AllocateRange(1024);
        Assert.AreEqual((nuint)manager.Handler.PageSize * 2, buffer2.CommittedBytes);
    }

    [Test]
    public void TestCannotAllocateAfterDispose()
    {
        var manager = new VirtualArenaManager();
        manager.Dispose();

        Assert.Throws<InvalidOperationException>(() =>
            {
                manager.CreateBuffer("Hello", 1 << 20);
            }
        );

        Assert.Throws<InvalidOperationException>(() =>
            {
                manager.CreateArray<int>("Hello", 1 << 20);
            }
        );
    }

    [Test]
    public void TestCreateBufferInvalidArg()
    {
        var manager = new VirtualArenaManager();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                manager.CreateBuffer("Hello", 0);
            }
        );

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                manager.CreateArray<int>("Hello", 0);
            }
        );
    }
}