using CryptoExchange.Net.Sockets;
using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects;
using Kucoin.Net.Objects.Models;
using Kucoin.Net.Objects.Models.Futures.Socket;
using Kucoin.Net.Objects.Models.Spot.Socket;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace KuCoinAPI
{
    public class KuCoinFutureAPI
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
        public KuCoinFutureAPI(string APIKey, string APISecret, string APIPassPhrase)
        {
            this.APIKey = APIKey;
            this.APISecret = APISecret;
            this.APIPassPhrase = APIPassPhrase;
        }

        // Destructor
        ~KuCoinFutureAPI()
        {
            Client.Dispose();
            SocketClient.Dispose();
        }

        public async Task<bool> Connect()
        {
            Client = new KucoinClient(new KucoinClientOptions()
            {
                ApiCredentials = new KucoinApiCredentials(APIKey, APISecret, APIPassPhrase),
                FuturesApiOptions = new KucoinRestApiClientOptions
                {
                    BaseAddress = KucoinApiAddresses.Default.FuturesAddress
                }
            });

            SocketClient = new KucoinSocketClient(new KucoinSocketClientOptions()
            {
                ApiCredentials = new KucoinApiCredentials(APIKey, APISecret, APIPassPhrase),
                FuturesStreamsOptions = new KucoinSocketApiClientOptions
                {
                    BaseAddress = KucoinApiAddresses.Default.FuturesAddress
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
            DateTime time = (await Client.FuturesApi.ExchangeData.GetServerTimeAsync(ct: CTS.Token)).Data;
            if (time == new DateTime(1, 1, 1, 0, 0, 0)) return 0;
            return (long)((time.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds);
        }

        public async Task<JsonObject> GetBalances()
        {
            List<JsonNode> AccountInfo = new();
            CancellationTokenSource CTS = new(5000);
            var resultXBT = await Client.FuturesApi.Account.GetAccountOverviewAsync("XBT", ct: CTS.Token);
            if (resultXBT.Success == true) AccountInfo.Add(JsonNode.Parse(JsonSerializer.Serialize(resultXBT.Data)));
            var resultUSDT = await Client.FuturesApi.Account.GetAccountOverviewAsync("USDT", ct: CTS.Token);
            if (resultUSDT.Success == true) AccountInfo.Add(JsonNode.Parse(JsonSerializer.Serialize(resultUSDT.Data)));
            return new JsonObject
            {
                { "Data", JsonNode.Parse(JsonSerializer.Serialize(AccountInfo.ToArray())) }
            };
        }

        public async Task<bool> SubscribeAccountUpdates(Action<DataEvent<KucoinStreamOrderMarginUpdate>> OnMarginUpdate,
            Action<DataEvent<KucoinStreamFuturesBalanceUpdate>> OnBalanceUpdate,
            Action<DataEvent<KucoinStreamFuturesWithdrawableUpdate>> OnWithdrawableUpdate,
            CancellationToken CT = default)
        {
            var result = await SocketClient.FuturesStreams.SubscribeToBalanceUpdatesAsync(
                onOrderMarginUpdate: OnMarginUpdate,
                onBalanceUpdate: OnBalanceUpdate,
                onWithdrawableUpdate: OnWithdrawableUpdate,
                ct: CT);
            if (result.Success != true)
            {
                CT.ThrowIfCancellationRequested();
                return false;
            }
            return true;
        }

        public async Task<bool> SubscribeOrderUpdates(Action<DataEvent<KucoinStreamFuturesOrderUpdate>> OnOrderUpdate,
            Action<DataEvent<KucoinStreamStopOrderUpdateBase>> OnStopOrderUpdate,
            CancellationToken CT = default)
        {
            var result1 = await SocketClient.FuturesStreams.SubscribeToOrderUpdatesAsync(
                null,
                onData: OnOrderUpdate,
                ct: CT);
            var result2 = await SocketClient.FuturesStreams.SubscribeToStopOrderUpdatesAsync(OnStopOrderUpdate, CT);
            if (result1.Success != true || result2.Success != true)
            {
                CT.ThrowIfCancellationRequested();
                return false;
            }
            return true;
        }

        public async Task<bool> SubscribeMarketPrice(string Symbol,
            Action<DataEvent<KucoinStreamFuturesMarkIndexPrice>> OnNewPrice,
            Action<DataEvent<KucoinStreamFuturesFundingRate>> OnNewFundingRate,
            CancellationToken CT = default)
        {
            var result = await SocketClient.FuturesStreams.SubscribeToMarketUpdatesAsync(
                symbol: Symbol,
                onMarkIndexPriceUpdate: OnNewPrice,
                onFundingRateUpdate: OnNewFundingRate,
                ct: CT);
            Console.WriteLine(JsonSerializer.Serialize(result));
            if (result.Success != true)
            {
                CT.ThrowIfCancellationRequested();
                return false;
            }
            return true;
        }

        public async Task<JsonObject> GetOrderDetails(string OrderID)
        {
            CancellationTokenSource CTS = new(5000);
            var result = await Client.FuturesApi.Trading.GetOrderAsync(OrderID, CTS.Token);
            return JsonNode.Parse(JsonSerializer.Serialize(result)).AsObject();
        }

        public async Task<JsonObject> PlaceOrder(string Symbol, OrderSide Side, NewOrderType Type, int Leverage,
            decimal Quantity, decimal? Price = null, TimeInForce? TimeInForce = null, bool? PostOnly = null,
            bool? Hidden = null, bool? Iceberg = null, decimal? VisibleSize = null, string? Remark = null,
            StopType? StopType = null, StopPriceType? StopPriceType = null, decimal? StopPrice = null,
            bool? ReduceOnly = null, bool? CloseOrder = null, bool? ForceHold = null, string? NewClientOrderId = null)
        {
            CancellationTokenSource CTS = new(5000);
            var result = await Client.FuturesApi.Trading.PlaceOrderAsync(Symbol, Side, Type, Leverage, Quantity, Price,
                TimeInForce, PostOnly, Hidden, Iceberg, VisibleSize, Remark, StopType, StopPriceType, StopPrice,
                ReduceOnly, CloseOrder, ForceHold, NewClientOrderId, CTS.Token);
            return JsonNode.Parse(JsonSerializer.Serialize(result)).AsObject();
        }

        public async Task<JsonObject> CancelOrder(string ClientOrderId)
        {
            CancellationTokenSource CTS = new(5000);
            var result = await Client.FuturesApi.Trading.CancelOrderAsync(ClientOrderId, CTS.Token);
            return JsonNode.Parse(JsonSerializer.Serialize(result)).AsObject();
        }
    }
}
