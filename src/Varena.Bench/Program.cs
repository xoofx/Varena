using BenchmarkDotNet.Attributes;

namespace Varena.Bench;

/// <summary>
/// Small benchmark to check the performance of allocating into a Virtual Array vs the .NET Managed new().
/// Should be very close as they both use a bump pointer (but more noise with .NET due to triggered GC collections)
/// </summary>
public class Program
{
    private readonly VirtualArenaManager _virtualArenaManager;
    private readonly VirtualArray<EmptyStruct> _vArray;
    public Program()
    {
        _virtualArenaManager = new VirtualArenaManager();
        // Reserve 256 MB
        _vArray = _virtualArenaManager.CreateArray<EmptyStruct>("MyArena", 256 << 20);
    }

    [Benchmark(Baseline = true)]
    public EmptyClass BenchManaged()
    {
        return new EmptyClass();
    }


    [Benchmark]
    public void BenchArena()
    {
        _vArray.Allocate();
        // We reset the allocation before reaching the 256 MB
        if (_vArray.AllocatedBytes > (255 << 20))
        {
            _vArray.Reset();
        }
    }

    static void Main(string[] args)
    {
        BenchmarkDotNet.Running.BenchmarkRunner.Run<Program>();
    }
        
    // 24 bytes (header, vtable, min size)
    public class EmptyClass
    {
    }

    // 24 bytes
    private struct EmptyStruct
    {
#pragma warning disable CS0169
        private long _u1, _u2, _u3;
#pragma warning restore CS0169
    }
}