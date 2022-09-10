using NUnit.Framework;

namespace Varena.Tests
{
    public class TestVirtualMemoryRange
    {
        [Test]
        public void TestBaseAddress()
        {
            var range = new VirtualMemoryRange((IntPtr)1, 2) { BaseAddress = (IntPtr)3 };
            Assert.AreEqual((IntPtr)3, range.BaseAddress);
        }

        [Test]
        public void TestToString()
        {
            var range = new VirtualMemoryRange((IntPtr)1, 2);
            var text = range.ToString();
            Console.WriteLine(text);
            Assert.AreEqual(IntPtr.Size == 8 ? "VirtualMemoryRange { BaseAddress = 0x0000000000000001, Size = 2 B }" : "VirtualMemoryRange { BaseAddress = 0x00000001, Size = 2 B }", text);
        }
    }
}