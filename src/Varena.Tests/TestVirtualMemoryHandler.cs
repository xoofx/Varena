using NUnit.Framework;

namespace Varena.Tests
{
    public class TestVirtualMemoryHandler
    {

        [Test]
        public void TestRanges()
        {
            var handler = VirtualMemoryHandler.Instance;

            Assert.Throws<ArgumentException>(() => { handler.TryFree(new VirtualMemoryRange((IntPtr)0, 0)); });
            Assert.Throws<ArgumentException>(() => { handler.TryFree(new VirtualMemoryRange((IntPtr)handler.PageSize, 0)); });

            Assert.Throws<ArgumentException>(() => { handler.TryFree(new VirtualMemoryRange((IntPtr)1, 0)); });
            Assert.Throws<ArgumentException>(() => { handler.TryFree(new VirtualMemoryRange((IntPtr)handler.PageSize, 1)); });
        }

        [Test]
        public void TestProtections()
        {
            var handler = VirtualMemoryHandler.Instance;
            var range = handler.TryReserve(1);
            try
            {
                Assert.AreEqual((nuint)handler.PageSize, range.Size);

                Assert.IsTrue(handler.TryCommit(range, VirtualMemoryFlags.All));


                // These are the only one that we support on Windows so we don't try
                // the other permutations (e.g Write+Execute)
                foreach (var flags in new[]
                         {
                             VirtualMemoryFlags.Execute,
                             VirtualMemoryFlags.All,
                             VirtualMemoryFlags.Execute | VirtualMemoryFlags.Read,
                             VirtualMemoryFlags.Read,
                             VirtualMemoryFlags.Write,
                         })
                {
                    Assert.IsTrue(handler.TryProtect(range, flags), $"Unable to protect {flags}");
                }
            }
            finally
            {
                handler.TryFree(range);
            }
        }
    }
}