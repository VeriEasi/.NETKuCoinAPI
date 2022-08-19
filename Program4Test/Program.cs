using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace Program4Test
{
    class Program
    {
        private static readonly ManualResetEvent ExitEvent = new(false);

        static async Task Main(string[] _)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            AssemblyLoadContext.Default.Unloading += DefaultOnUnloading;
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;

            string ApiKey = "62b80bbe2b968a000153959b";
            string ApiSecret = "0e28b56c-7c67-44dc-9454-6c02fd8f08e7";
            string ApiPassPhrase = "Aliezm1372";
            KuCoinAPI.KuCoinSpotAPI kc = new(ApiKey, ApiSecret, ApiPassPhrase);

            // Connect
            bool conn = await Task.Run(() => kc.Connect());
            if (!conn)
            {
                Console.WriteLine("Not Connected");
                return;
            }
        }

        private static void CurrentDomainOnProcessExit(object sender, EventArgs eventArgs)
        {
            ExitEvent.Set();
        }

        private static void DefaultOnUnloading(AssemblyLoadContext assemblyLoadContext)
        {
            ExitEvent.Set();
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            ExitEvent.Set();
        }
    }
}
