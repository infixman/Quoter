using Telegram.Bot;
using Telegram.Bot.Types;

namespace Quoter.Service
{
    public interface IBotService
    {
        TelegramBotClient Client { get; }
        User Me { get; set; }
    }
}
