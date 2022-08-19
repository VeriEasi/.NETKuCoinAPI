using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects;
using Kucoin.Net.Objects.Models.Spot;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.Json;
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
                ApiCredentials = new KucoinApiCredentials(
                    apiKey: APIKey,
                    apiSecret: APISecret,
                    apiPassPhrase: APIPassPhrase),
                SpotApiOptions = new KucoinRestApiClientOptions
                {
                    BaseAddress = KucoinApiAddresses.TestNet.SpotAddress
                }
            });

            SocketClient = new KucoinSocketClient(new KucoinSocketClientOptions()
            {
                ApiCredentials = new KucoinApiCredentials(
                    apiKey: APIKey,
                    apiSecret: APISecret,
                    apiPassPhrase: APIPassPhrase),
                SpotStreamsOptions = new KucoinSocketApiClientOptions
                {
                    BaseAddress = KucoinApiAddresses.TestNet.SpotAddress
                }
            });

            long checkConnect = await Task.Run(() => GetServerTime());
            if (checkConnect == 0) return false;
            return true;
        }

        public async Task<long> GetServerTime()
        {
            DateTime time = (await Client.SpotApi.ExchangeData.GetServerTimeAsync()).Data;
            if (time == new DateTime(1, 1, 1, 0, 0, 0)) return 0;
            return (long)((time.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds);
        }

        public async Task<List<JObject>> GetAccountInfo()
        {
            List<KucoinAccount> temp = (List<KucoinAccount>)(await Client.SpotApi.Account.GetAccountsAsync()).Data;
            List<JObject> ret = new();
            if (temp == null) return ret;
            for (int i = 0; i < temp.Count; ++i) ret.Add(JObject.Parse(JsonSerializer.Serialize(temp[i])));
            return ret;
        }

        public async Task<JObject> PlaceOrder(
            string Symbol, OrderSide Side, NewOrderType Type, decimal? Quantity = default,
            decimal? Price = default, decimal? QuoteQuantity = default, TimeInForce? TimeInForce = default,
            TimeSpan? CancelAfter = default, bool? PostOnly = default, bool? Hidden = default,
            bool? IceBerg = default, decimal? VisibleIceBergSize = default,
            string? Remark = default, string? ClientOrderId = default, SelfTradePrevention? SelfTradePrevention = default)
        {
            KucoinNewOrder temp = (await Client.SpotApi.Trading.PlaceOrderAsync(
                    symbol: Symbol, side: Side, type: Type, quantity: Quantity, price: Price,
                    quoteQuantity: QuoteQuantity, timeInForce: TimeInForce, cancelAfter: CancelAfter,
                    postOnly: PostOnly, hidden: Hidden, iceBerg: IceBerg, visibleIceBergSize: VisibleIceBergSize,
                    remark: Remark, clientOrderId: ClientOrderId, selfTradePrevention: SelfTradePrevention)).Data;

            JObject ret = new();
            if (temp == null) ret.Add("error", "failed");
            else ret.Add("ID", temp.Id);
            return ret;
        }

        public async Task<JObject> CancelOrder(string OrderID)
        {
            KucoinCanceledOrders temp = (await Client.SpotApi.Trading.CancelOrderAsync(orderId: OrderID)).Data;
            JObject ret = new();
            List<string> ids = new();
            if (temp == null) ret.Add("IDs", JToken.FromObject(ids));
            else
            {
                var list = temp.CancelledOrderIds.GetEnumerator();
                while (list.MoveNext()) ids.Add(list.Current);
                ret.Add("IDs", JToken.FromObject(ids));
            }
            
            return ret;
        }

        public async Task<JObject> CancelAllOrders(string? Symbol = default, TradeType? TradeType = default)
        {
            KucoinCanceledOrders temp = (await Client.SpotApi.Trading.CancelAllOrdersAsync(symbol: Symbol, tradeType: TradeType)).Data;
            JObject ret = new();
            List<string> ids = new();
            if (temp == null) ret.Add("IDs", JToken.FromObject(ids));
            else
            {
                var list = temp.CancelledOrderIds.GetEnumerator();
                while (list.MoveNext()) ids.Add(list.Current);
                ret.Add("IDs", JToken.FromObject(ids));
            }

            return ret;
        }
    }
}
