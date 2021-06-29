using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Storage;

namespace ASFGameBuyPlugin
{
    internal static class Commands
    {
        internal static async Task<string> BuyGameCommandAsync(ulong steamID, string botsQuery, uint appID, uint subID, bool isBundle)
        {
            if (string.IsNullOrWhiteSpace(botsQuery))
                throw new ArgumentNullException($"{botsQuery} is empty");

            if (appID == 0)
                throw new ArgumentException($"{appID} is zero");

            if (subID == 0)
                throw new ArgumentException($"{subID} is zero");

            HashSet<Bot>? bots = Bot.GetBots(botsQuery);
            if (bots == null || bots.Count == 0)
                return $"Bots by query \"{botsQuery}\" not found";

            StringBuilder stringBuilder = new();

            foreach (var bot in bots)
            {
                if (!bot.IsConnectedAndLoggedOn)
                {
                    stringBuilder.AppendLine($"<{bot.BotName}> Is not logged on");
                    continue;
                }

                if (!bot.HasAccess(steamID, BotConfig.EAccess.Operator))
                {
                    stringBuilder.AppendLine($"<{bot.BotName}> Has no access from SteamID: {steamID}");
                    continue;
                }

                SteamStore steamStore = new(bot);


                if (await steamStore.BuyGameAsync(appID, subID, isBundle))
                {
                    stringBuilder.AppendLine($"<{bot.BotName}> Purchased {appID} / {subID}");
                    bot.ArchiLogger.LogGenericInfo($"Purchased {appID} / {subID}");
                }
                else
                {
                    stringBuilder.AppendLine($"<{bot.BotName}> Unable to purchase {appID} / {subID}");
                    bot.ArchiLogger.LogGenericInfo($"Unable to purchase {appID} / {subID}");
                }

                await Task.Delay(Constants.BuyDelay);
            }

            return stringBuilder.ToString();
        }
    }
}
