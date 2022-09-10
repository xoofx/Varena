using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Varena.Tests
{
    public class TestVirtualMemoryHelper
    {

        [TestCase(VirtualMemoryFlags.None, "---")]
        [TestCase(VirtualMemoryFlags.Read, "r--")]
        [TestCase(VirtualMemoryFlags.Write, "-w-")]
        [TestCase(VirtualMemoryFlags.Execute, "--x")]
        [TestCase(VirtualMemoryFlags.ReadWrite, "rw-")]
        [TestCase(VirtualMemoryFlags.All, "rwx")]
        [TestCase(VirtualMemoryFlags.Write | VirtualMemoryFlags.Execute, "-wx")]
        [TestCase(VirtualMemoryFlags.Read | VirtualMemoryFlags.Execute, "r-x")]
        [TestCase((VirtualMemoryFlags)0x10203040, "???")]
        public void TestFlagsToText(VirtualMemoryFlags flags, string expected)
        {
            Assert.AreEqual(expected, flags.ToText());
        }

        [TestCase(0Ul, "0 B")]
        [TestCase(1Ul, "1 B")]
        [TestCase(1024Ul, "1 KiB")]
        [TestCase(1025Ul, "1025 B")]
        [TestCase(1ul << 20, "1 MiB")]
        [TestCase(999ul << 10, "999 KiB")]
        [TestCase(2ul << 30, "2 GiB")]
        [TestCase(3ul << 40, "3 TiB")]
        [TestCase(4ul << 50, "4096 TiB")]
        public void TestByteCountToText(ulong value, string expected)
        {
            Assert.AreEqual(expected, VirtualMemoryHelper.ByteCountToText(value));
        }
    }
}