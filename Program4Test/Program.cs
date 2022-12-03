using CryptoExchange.Net.Sockets;
using Kucoin.Net.Objects.Models;
using Kucoin.Net.Objects.Models.Futures.Socket;
using Kucoin.Net.Objects.Models.Spot.Socket;
using System;
using System.Linq;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Program4Test
{
    class Program
    {
        private static readonly ManualResetEvent ExitEvent = new(false);
        private static KuCoinAPI.KuCoinFutureAPI kf;
        //private static KuCoinAPI.KuCoinSpotAPI kf;

        static async Task Main(string[] _)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            AssemblyLoadContext.Default.Unloading += DefaultOnUnloading;
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;

            // Spot
            //string ApiKey = "638b485727af89000179e11e";
            //string ApiSecret = "4f63a576-92d7-4ecb-927b-6ee293654e33";
            
            // Future
            string ApiKey = "637b0f9bf1843c000161ecaa";
            string ApiSecret = "ce624207-12d2-4d4b-b493-dceecee72906";

            // Testnet
            //string ApiKey = "634a792b41a5330001d1f669";
            //string ApiSecret = "31caaa1c-0675-433a-9ac7-51010266d199";

            string ApiPassPhrase = "Aliezm1372";
            kf = new(ApiKey, ApiSecret, ApiPassPhrase);

            // Connect
            bool conn = await Task.Run(() => kf.Connect());
            Console.WriteLine("Connection Status: " + conn);

            // Time
            long time = await Task.Run(() => kf.GetServerTime());
            Console.WriteLine("Epoch Time: " + time);

            // Balance
            JsonObject bal = await Task.Run(() => kf.GetBalances());
            foreach (JsonObject obj in bal["Data"].AsArray().Cast<JsonObject>())
            {
                if (obj["Asset"].GetValue<string>() == "USDT")
                {
                    Console.WriteLine("USDT Balance: " + obj["Available"].GetValue<decimal>());
                    break;
                }
            }

            // Subscribe
            bool Sub = await Task.Run(() => kf.SubscribeAccountUpdates(OnMarginUpdate, OnBalanceUpdate, OnWithdrawableUpdate));
            Console.WriteLine("Subscribe Account Updates: " + Sub);
            bool Sub2 = await Task.Run(() => kf.SubscribeOrderUpdates(OnOrderUpdate, OnStopOrderUpdate));
            Console.WriteLine("Subscribe Order Updates: " + Sub2);
            bool Sub3 = await Task.Run(() => kf.SubscribeMarketPrice("XBTUSDTM", OnNewPrice, OnNewFundingRate));
            Console.WriteLine("Subscribe BTC Price: " + Sub3);

            ExitEvent.WaitOne();
        }

        private static void OnNewPrice(DataEvent<KucoinStreamFuturesMarkIndexPrice> obj)
        {
            Console.WriteLine("Price Update:");
            Console.WriteLine(JsonSerializer.Serialize(obj.Data));
        }

        private static void OnNewFundingRate(DataEvent<KucoinStreamFuturesFundingRate> obj)
        {
            Console.WriteLine("Finding Rate Update:");
            Console.WriteLine(JsonSerializer.Serialize(obj.Data));
        }

        private static void OnMarginUpdate(DataEvent<KucoinStreamOrderMarginUpdate> obj)
        {
            Console.WriteLine("Margin Update:");
            Console.WriteLine(JsonSerializer.Serialize(obj.Data));
        }

        private static void OnBalanceUpdate(DataEvent<KucoinStreamFuturesBalanceUpdate> obj)
        {
            Console.WriteLine("Balance Update:");
            Console.WriteLine(JsonSerializer.Serialize(obj.Data));
        }

        private static void OnWithdrawableUpdate(DataEvent<KucoinStreamFuturesWithdrawableUpdate> obj)
        {
            Console.WriteLine("Withdrawable Update:");
            Console.WriteLine(JsonSerializer.Serialize(obj.Data));
        }

        private static void OnOrderUpdate(DataEvent<KucoinStreamFuturesOrderUpdate> obj)
        {
            Console.WriteLine("Order Update:");
            Console.WriteLine(JsonSerializer.Serialize(obj.Data));
            Task<JsonObject> o = Task.Run(() => kf.GetOrderDetails(obj.Data.OrderId));
            o.Wait();
            Console.WriteLine(o.Result.ToString());
        }

        private static void OnStopOrderUpdate(DataEvent<KucoinStreamStopOrderUpdateBase> obj)
        {
            Console.WriteLine("Stop Order Update:");
            Console.WriteLine(JsonSerializer.Serialize(obj.Data));
            Task<JsonObject> o = Task.Run(() => kf.GetOrderDetails(obj.Data.Id));
            o.Wait();
            Console.WriteLine(o.Result.ToString());
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
