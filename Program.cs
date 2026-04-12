//Network/Program.cs
using System;
using System.Threading.Tasks;
using NetForge.Test;

namespace NetForge
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                //Program Entry or Test to perform
                await PacketLoopbackTest.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Execution had errors, please see result.");
                Console.WriteLine(ex);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}