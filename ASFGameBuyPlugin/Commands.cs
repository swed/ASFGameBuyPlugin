using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam;

namespace ASFGameBuyPlugin
{
    internal static class Commands
    {
        internal static async Task<string> BuyGameCommandAsync(Bot bot, ulong steamID)
        {
            if (!bot.IsConnectedAndLoggedOn)
                return $"<{bot.BotName}> Is not logged on";

            SteamStore steamStore = new(bot);
            var subIDs = await steamStore.GetAllSubID(623280);
            if (subIDs == null)
                return $"<{bot.BotName}> {nameof(steamStore.GetAllSubID)} return null";

            StringBuilder stringBuilder = new();
            foreach (var subID in subIDs)
            {
                if (subID == null)
                    continue;

                foreach (var keyValue in subID)
                    stringBuilder.AppendLine($"{keyValue.Key}: {keyValue.Value}");

                stringBuilder.AppendLine("----------");
            }

            return stringBuilder.ToString();
        }
    }
}
