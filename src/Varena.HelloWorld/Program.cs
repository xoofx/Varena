using Varena;

Console.WriteLine("Hello, World from Varena!");
{
    Console.WriteLine("Press enter to reserve 1 GiB of memory");
    Console.ReadLine();
    using var manager = new VirtualArenaManager();
    Console.WriteLine($"PageSize = {manager.Handler.PageSize}");
    var buffer = manager.CreateBuffer("Hello", 1 << 30);
    Console.WriteLine(buffer);
    Console.WriteLine("Allocate 1024 bytes (will commit 64 KiB)");
    var span = buffer.AllocateRange(1024);
    span[1] = 1;
    Console.WriteLine("Buffer after allocation");
    Console.WriteLine(buffer);
    Console.WriteLine("Press enter to free the memory");
    Console.ReadLine();
}
Console.WriteLine("Press enter to exit the program");
Console.ReadLine();