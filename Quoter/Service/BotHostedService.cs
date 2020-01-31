using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quoter.Class;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Quoter.Service
{
    public class BotHostedService : IHostedService
    {
        private readonly IBotService _bot;
        private readonly BotConfiguration _config;
        private CancellationTokenSource _tokenSource;
        private readonly string _helpPriceMsg = $"`/price [幣種]` - 查看幣種幣種USDT價格{Environment.NewLine}例如: `/price btc`{Environment.NewLine}{Environment.NewLine}`/price [幣種A] [幣種B]` - 查看幣種A的幣種B價格{Environment.NewLine}例如: `/price eth btc`{Environment.NewLine}{Environment.NewLine}`/price` 也提供簡單指令 `/p` ";

        public BotHostedService(IBotService botService,
            BotConfiguration config)
        {
            _bot = botService;
            _config = config;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _bot.Me = await _bot.Client.GetMeAsync();
            _bot.Client.OnMessage += OnMessage;
            this._tokenSource = new CancellationTokenSource();
            _bot.Client.StartReceiving(cancellationToken: this._tokenSource.Token);
        }

        private async void OnMessage(object? sender, MessageEventArgs e)
        {
            if (e.Message.Type == MessageType.Text)
            {
                await ReplyMessageAsync(e);
            }
        }

        private async Task ReplyMessageAsync(MessageEventArgs e)
        {
            var receiveMessageText = e.Message.Text.ToLower();

            if (!receiveMessageText.StartsWith("/")
                && receiveMessageText.Contains("@")
                && !receiveMessageText.Contains(_config.BotId))
                return;

            if (receiveMessageText.Equals("/pin")
                && (e.Message.Chat.Type == ChatType.Group
                    || e.Message.Chat.Type == ChatType.Supergroup))
            {   
                var admins = await _bot.Client.GetChatAdministratorsAsync(e.Message.Chat.Id);
                var adminIds = admins.Select(x => x.User.Id).ToArray();
                if (adminIds.Contains(e.Message.From.Id)
                    && adminIds.Contains(_bot.Me.Id))
                {
                    Console.WriteLine($"Chat:{e.Message.Chat.LastName} {e.Message.Chat.FirstName}({e.Message.Chat.Id}),User:{e.Message.From.LastName} {e.Message.From.FirstName} ({e.Message.From.Id}) Want Pin, He/She is Admin, Pined.");

                    var sendMessage = await _bot.Client.SendTextMessageAsync(e.Message.Chat.Id, GetPinMessage(), ParseMode.Markdown);
                    try
                    {
                        await _bot.Client.PinChatMessageAsync(e.Message.Chat.Id, sendMessage.MessageId, disableNotification: true);
                        _ = Task.Run(() => EditPinMessage(sendMessage));
                    }
                    catch
                    {
                        //大概是沒權限吧，ignored
                    }
                }
                else
                {
                    Console.WriteLine($"Chat:{e.Message.Chat.LastName} {e.Message.Chat.FirstName}({e.Message.Chat.Id}),User:{e.Message.From.LastName} {e.Message.From.FirstName} ({e.Message.From.Id}) Want Pin, He/She is not Admin!!!");
                }
            }
            else if (receiveMessageText.StartsWith("/price")
            || receiveMessageText.StartsWith("/p ")
            || receiveMessageText.Equals("/p"))
            {
                string sendMessageText;

                if (receiveMessageText.Equals("/price")
                    || receiveMessageText.Equals("/p"))
                {
                    sendMessageText = _helpPriceMsg;
                }
                else if (receiveMessageText.Split(' ').Length >= 2
                         && receiveMessageText.Split(' ').Length <= 3)
                {
                    if (receiveMessageText.Split(' ').Length == 2)
                    {
                        receiveMessageText += " USDT";
                    }

                    var symbol = receiveMessageText
                        .Replace("/p ", string.Empty)
                        .Replace("/price ", string.Empty)
                        .Replace(" ", string.Empty);
                    sendMessageText = $"{GetPriceFromBinance(symbol, receiveMessageText.Split(' ').Last().ToUpper())}";
                }
                else
                {
                    sendMessageText = "錯誤指令。";
                }

                await _bot.Client.SendTextMessageAsync(e.Message.Chat.Id,
                    sendMessageText,
                    ParseMode.Markdown,
                    replyToMessageId: e.Message.MessageId);
            }
        }

        private void EditPinMessage(Message sendMessage)
        {
            do
            {
                Thread.Sleep(10 * 1000);
                _bot.Client.EditMessageTextAsync(sendMessage.Chat.Id, sendMessage.MessageId, GetPinMessage(), ParseMode.Markdown);
            } while (true);
         
            // ReSharper disable once FunctionNeverReturns
        }

        private decimal GetLatestUsdRate()
        {
            var downloadString = new WebClient().DownloadString("https://api-pub.bitfinex.com/v2/ticker/fUSD");
            if (string.IsNullOrWhiteSpace(downloadString))
                return 0;

            var splitData = downloadString.Split(',');
            if (splitData.Length < 10)
                return 0;

            Console.WriteLine($"splitData[9].ToDecimal(): {splitData[9]}");

            return Convert.ToDecimal(splitData[9]);
        }

        private string GetPinMessage()
        {
            return $"`{(GetLatestUsdRate() * 100).ToString().TrimEnd('0')}`% | {Environment.NewLine}{GetPinCryptoPrice()}{DateTime.UtcNow.AddHours(8):MM/dd HH:mm:ss}";
        }

        private string GetPinCryptoPrice()
        {
            string[] symbols =
            {
                "BTCUSDT",
                "ETHUSDT",
                "MCOUSDT",
                "XRPUSDT"
            };

            var returnObj = string.Empty;

            foreach (var symbol in symbols)
            {
                var price = GetPriceFromBinance(symbol, "USDT").Split(' ').First();
                Console.WriteLine($"symbol.price.ToDecimal: {symbol} | {price}");
                returnObj += $"{symbol.Replace("USDT", string.Empty)} `{Convert.ToDecimal(price)}` | {Environment.NewLine}";
                Thread.Sleep(200);
            }

            return returnObj;
        }

        private static string GetPriceFromBinance(string symbol, string toSymbol)
        {
            var uSymbol = symbol.ToUpper();
            var returnObj = string.Empty;
            try
            {
                var wc = new WebClient();
                var downloadString = wc.DownloadString($"https://api.binance.com/api/v3/ticker/price?symbol={uSymbol}");
                
                if (!string.IsNullOrWhiteSpace(downloadString))
                {
                    var json = JsonConvert.DeserializeObject<CryptoPrice>(downloadString);
                    if (string.IsNullOrWhiteSpace(json.msg))
                    {
                        returnObj = $"{json.price.TrimEnd('0').TrimEnd('.')} {toSymbol.ToUpper()}";
                    }
                }
            }
            catch (WebException we)
            {
                using (var sr = new StreamReader(we.Response.GetResponseStream()))
                {
                    var json = JsonConvert.DeserializeObject<CryptoPrice>(sr.ReadToEnd());
                    if (!string.IsNullOrWhiteSpace(json.msg))
                    {
                        returnObj = "輸入錯誤，請重新查詢。";
                    }
                }
            }

            return returnObj;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _tokenSource.Cancel();
            _bot.Client.StopReceiving();
            return Task.CompletedTask;
        }
    }
}
