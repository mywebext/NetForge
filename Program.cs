// NetForge/Program.cs
using System;
using System.Threading.Tasks;
using NetForge.Test;


namespace NetForge
{
    public partial class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                string projectRoot = Program.ResolveProjectRoot(); //@"C:\Users\Ken\source\repos\NetForge";

                // Ensure enum manifest exists and is current before running tests/work.
                await EnumManifestService.EnsureCurrentAsync(projectRoot);

                // Program Entry or Test to perform
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