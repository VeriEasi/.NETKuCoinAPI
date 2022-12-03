using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects;
using Kucoin.Net.Objects.Models.Spot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace KuCoinAPI
{
    public class KuCoinSpotAPI
    {
        // Private Fields:
#nullable enable
        private KucoinClient? Client { get; set; }
        private KucoinSocketClient? SocketClient { get; set; }
#nullable disable
        private string APIKey { get; set; }
        private string APISecret { get; set; }
        private string APIPassPhrase { get; set; }

        // Constructor:
        public KuCoinSpotAPI(string APIKey, string APISecret, string APIPassPhrase)
        {
            this.APIKey = APIKey;
            this.APISecret = APISecret;
            this.APIPassPhrase = APIPassPhrase;
        }

        // Destructor
        ~KuCoinSpotAPI()
        {
            Client.Dispose();
            SocketClient.Dispose();
        }

        public async Task<bool> Connect()
        {
            Client = new KucoinClient(new KucoinClientOptions()
            {
                ApiCredentials = new KucoinApiCredentials(APIKey, APISecret, APIPassPhrase),
                SpotApiOptions = new KucoinRestApiClientOptions
                {
                    BaseAddress = KucoinApiAddresses.Default.SpotAddress
                }
            });

            SocketClient = new KucoinSocketClient(new KucoinSocketClientOptions()
            {
                ApiCredentials = new KucoinApiCredentials(APIKey, APISecret, APIPassPhrase),
                SpotStreamsOptions = new KucoinSocketApiClientOptions
                {
                    BaseAddress = KucoinApiAddresses.Default.SpotAddress
                }
            });

            // Check Connection
            long checkConnect = await Task.Run(() => GetServerTime());
            if (checkConnect == 0) return false;
            return true;
        }

        public async Task<long> GetServerTime()
        {
            CancellationTokenSource CTS = new(5000);
            DateTime time = (await Client.SpotApi.ExchangeData.GetServerTimeAsync(ct: CTS.Token)).Data;
            if (time == new DateTime(1, 1, 1, 0, 0, 0)) return 0;
            return (long)((time.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds);
        }

        public async Task<JsonObject> GetBalances()
        {
            List<JsonNode> AccountInfo = new();
            CancellationTokenSource CTS = new(5000);
            var var = await Client.SpotApi.Account.GetAccountsAsync(ct: CTS.Token);
            if (var.Success == true)
                foreach (KucoinAccount KA in var.Data) AccountInfo.Add(JsonNode.Parse(JsonSerializer.Serialize(KA)));
            return new JsonObject
            {
                { "Data", JsonNode.Parse(JsonSerializer.Serialize(AccountInfo.ToArray())) }
            };
        }

        public async Task<JsonObject> PlaceOrder(string Symbol, OrderSide Side, NewOrderType Type, decimal? Quantity = default,
            decimal? Price = default, decimal? QuoteQuantity = default, TimeInForce? TimeInForce = default,
            TimeSpan? CancelAfter = default, bool? PostOnly = default, bool? Hidden = default,
            bool? IceBerg = default, decimal? VisibleIceBergSize = default,
            string? Remark = default, string? ClientOrderId = default, SelfTradePrevention? SelfTradePrevention = default)
        {
            CancellationTokenSource CTS = new(5000);
            var result = await Client.SpotApi.Trading.PlaceOrderAsync(Symbol, Side, Type, Quantity, Price,
                    QuoteQuantity, TimeInForce, CancelAfter, PostOnly, Hidden, IceBerg, VisibleIceBergSize,
                    Remark, ClientOrderId, SelfTradePrevention, CTS.Token);
            return JsonNode.Parse(JsonSerializer.Serialize(result)).AsObject();
        }

        public async Task<JsonObject> CancelOrder(string OrderID)
        {
            CancellationTokenSource CTS = new(5000);
            var result = await Client.SpotApi.Trading.CancelOrderAsync(OrderID, CTS.Token);
            return JsonNode.Parse(JsonSerializer.Serialize(result)).AsObject();
        }
    }
}
