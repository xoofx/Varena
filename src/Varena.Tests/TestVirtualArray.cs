using NUnit.Framework;
using System;

namespace Varena.Tests
{
    public class TestVirtualArray
    {
        [Test]
        public void TestAllocate()
        {
            using var manager = new VirtualArenaManager();
            var buffer = manager.CreateArray<Guid>("Guids", 1 << 20);

            var guid = Guid.NewGuid();

            // Test Allocate
            ref var nextGuid = ref buffer.Allocate();
            nextGuid = guid;
            Assert.AreEqual(guid, buffer[0]);

            // Test Allocate(index)
            var guid2 = Guid.NewGuid();
            ref var nextGuid2 = ref buffer.Allocate(out var nextIndex);
            nextGuid2 = guid2;
            Assert.AreEqual(1, nextIndex);
            Assert.AreEqual(guid2, buffer[1]);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    var test = buffer[2];
                }
            );

            // Test AllocateRange
            var rangeGuid = buffer.AllocateRange(2);
            rangeGuid[0] = guid;
            rangeGuid[1] = guid2;
            Assert.AreEqual(guid, buffer[2]);
            Assert.AreEqual(guid2, buffer[3]);

            Assert.AreEqual(4, buffer.Count);

            // Test AsSpan
            var allGuids = buffer.AsSpan();
            Assert.AreEqual(4, allGuids.Length);
            Assert.AreEqual(guid, allGuids[0]);
            Assert.AreEqual(guid2, allGuids[1]);
            Assert.AreEqual(guid, allGuids[2]);
            Assert.AreEqual(guid2, allGuids[3]);

            // Test AsSpan index/count
            var someGuids = buffer.AsSpan(1, 2);
            Assert.AreEqual(guid2, someGuids[0]);
            Assert.AreEqual(guid, someGuids[1]);

            // Test AllocateRange(index)
            var rangeGuid2 = buffer.AllocateRange(2, out var rangeIndex);
            Assert.AreEqual(4, rangeIndex);
            Assert.AreEqual(6, buffer.Count);

            // Free the memory
            buffer.Reset();
            Assert.AreEqual(0, buffer.Count);
        }

        [Test]
        public void TestAllocateLoop()
        {
            using var manager = new VirtualArenaManager();
            var buffer = manager.CreateArray<byte>("Bytes", 1 << 20);
            for (int i = 0; i < (1 << 20); i++)
            {
                _ = buffer.Allocate(out var index);
                Assert.AreEqual(i, index);
            }
            Assert.AreEqual(1 << 20, buffer.Count);
            Assert.AreEqual((nuint)(1 << 20), buffer.CommittedBytes);
            Assert.AreEqual((nuint)(1 << 20), buffer.AllocatedBytes);
            Assert.AreEqual((nuint)(1 << 20), buffer.CapacityInBytes);
            Assert.AreEqual((nuint)0, buffer.AvailableInBytes);

            Assert.Throws<VirtualMemoryException>(() => buffer.Allocate());
        }

        [Test]
        public void TestAppend()
        {
            using var manager = new VirtualArenaManager();
            var array = manager.CreateArray<byte>("Bytes", 1 << 20);
            for (int i = 0; i < 1024; i++)
            {
                if ((i & 1) == 0)
                {
                    array.Append((byte)i);
                }
                else
                {
                    array.AppendByRef((byte)i);
                }
            }
            Assert.AreEqual(1024, array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                Assert.AreEqual((byte)i, array[i]);
            }

            array.Reset(VirtualArenaResetKind.KeepAllCommitted);

            array.AppendRange(new byte[] { 255, 254, 253, 252 });
            Assert.AreEqual(4, array.Count);
            Assert.AreEqual((byte)255, array[0]);
            Assert.AreEqual((byte)254, array[1]);
            Assert.AreEqual((byte)253, array[2]);
            Assert.AreEqual((byte)252, array[3]);
        }

        [Test]
        public void TestInvalidArguments()
        {
            using var manager = new VirtualArenaManager();
            var buffer = manager.CreateArray<Guid>("Guids", 1 << 20);

            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.AllocateRange(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.AllocateRange(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer[0]);
        }
    }
}