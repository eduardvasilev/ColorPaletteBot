using Telegram.Bot;

namespace ColorPaletteBot.Webhook.Services
{
    public interface IBotService
    {
        TelegramBotClient Client { get; }
    }
}
