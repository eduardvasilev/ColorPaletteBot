using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace ColorPaletteBot.Webhook.Services
{
    public interface IUpdateService
    {
        Task ProcessAsync(Update update);
    }
}
