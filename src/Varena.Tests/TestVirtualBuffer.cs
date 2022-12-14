using NUnit.Framework;
using System;

namespace Varena.Tests
{
    public class TestVirtualBuffer
    {
        [Test]
        public void TestAllocate()
        {
            using var manager = new VirtualArenaManager();
            var buffer = manager.CreateBuffer("Bytes", 1 << 20);

            var span = buffer.AllocateRange(1 << 20, out var index);
            Assert.AreEqual(0, index);
            Assert.AreEqual(1 << 20, span.Length);
            Assert.AreEqual((nuint)(1 << 20), buffer.AllocatedBytes);
            Assert.AreEqual((nuint)0, buffer.AvailableInBytes);

            for (int i = 0; i < span.Length; i++)
            {
                span[i] = (byte)i;
            }

            var allSpan = buffer.AsSpan();
            Assert.AreEqual(1 << 20, allSpan.Length);

            Assert.AreEqual((byte)0, buffer[0]);
            Assert.AreEqual((byte)1, buffer[1]);
            Assert.AreEqual((byte)(buffer.AllocatedBytes - 1), buffer[(int)buffer.AllocatedBytes - 1]);

            var subSpan = buffer.AsSpan(1, 2);
            Assert.AreEqual((byte)1, subSpan[0]);
            Assert.AreEqual((byte)2, subSpan[1]);
            Assert.AreEqual(2, subSpan.Length);

            // Free the memory
            buffer.Reset();
            Assert.AreEqual((nuint)0, buffer.AllocatedBytes);
        }

        [Test]
        public void TestAppend()
        {
            using var manager = new VirtualArenaManager();
            var buffer = manager.CreateBuffer("Bytes", 1 << 20);

            for (int i = 0; i < 1024; i++)
            {
                buffer.Append((byte)i);
            }
            Assert.AreEqual((nuint)1024, buffer.AllocatedBytes);
            var span = buffer.AsSpan(0, 1024);
            for (int i = 0; i < span.Length; i++)
            {
                Assert.AreEqual((byte)i, span[i], "Invalid data in the buffer");
            }

            buffer.Reset(VirtualArenaResetKind.KeepAllCommitted);
            buffer.AppendRange(new byte[] { 255, 254, 253, 252,});

            Assert.AreEqual((nuint)4, buffer.AllocatedBytes);
            Assert.AreEqual((byte)255, buffer[0]);
            Assert.AreEqual((byte)254, buffer[1]);
            Assert.AreEqual((byte)253, buffer[2]);
            Assert.AreEqual((byte)252, buffer[3]);
        }

        [Test]
        public void TestInvalidArguments()
        {
            using var manager = new VirtualArenaManager();
            var buffer = manager.CreateBuffer("Bytes", 1 << 20);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer.AllocateRange(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer[0]);
            buffer.AllocateRange(1024);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer.AsSpan(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer.AsSpan(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer.AsSpan(1024, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer.AsSpan(1023, 2));
        }
    }
}