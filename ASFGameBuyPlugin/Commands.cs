﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Storage;

namespace ASFGameBuyPlugin
{
    internal static class Commands
    {
        internal static async Task<string> BuyGameCommandAsync(ulong steamID, string botsQuery, uint appID, uint subID)
        {
            if (string.IsNullOrWhiteSpace(botsQuery))
                throw new ArgumentNullException($"{nameof(botsQuery)} is empty");

            if (appID == 0)
                throw new ArgumentException($"{nameof(appID)} is zero");

            if (subID == 0)
                throw new ArgumentException($"{nameof(subID)} is zero");

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


                if (await steamStore.BuyGameAsync(appID, subID))
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

        internal static async Task<string> BuyBundleCommandAsync(ulong steamID, string botsQuery, uint bundleID)
        {
            if (string.IsNullOrWhiteSpace(botsQuery))
                throw new ArgumentNullException($"{nameof(botsQuery)} is empty");

            if (bundleID == 0)
                throw new ArgumentException($"{nameof(bundleID)} is zero");

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

                if (await steamStore.BuyBundleAsync(bundleID))
                {
                    stringBuilder.AppendLine($"<{bot.BotName}> Purchased bundle {bundleID}");
                    bot.ArchiLogger.LogGenericInfo($"Purchased bundle {bundleID}");
                }
                else
                {
                    stringBuilder.AppendLine($"<{bot.BotName}> Unable to purchase bundle {bundleID}");
                    bot.ArchiLogger.LogGenericInfo($"Unable to purchase bundle {bundleID}");
                }

                await Task.Delay(Constants.BuyDelay);
            }

            return stringBuilder.ToString();
        }

        internal static async Task<string> BuyInGameCommandAsync(ulong steamID, string botsQuery, uint appID, uint itemID, uint quantity=1)
        {
            if (string.IsNullOrWhiteSpace(botsQuery))
                throw new ArgumentNullException($"{nameof(botsQuery)} is empty");

            if (appID == 0)
                throw new ArgumentException($"{nameof(appID)} is zero");

            if (itemID == 0)
                throw new ArgumentException($"{nameof(itemID)} is zero");

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

                if (await steamStore.BuyInGameItemAsync(appID, itemID, quantity))
                {
                    stringBuilder.AppendLine($"<{bot.BotName}> Purchased item {appID} / {itemID}");
                    bot.ArchiLogger.LogGenericInfo($"Purchased item {appID} / {itemID}");
                }
                else
                {
                    stringBuilder.AppendLine($"<{bot.BotName}> Unable to purchase item {appID} / {itemID}");
                    bot.ArchiLogger.LogGenericInfo($"Unable to purchase item {appID} / {itemID}");
                }

                await Task.Delay(Constants.BuyDelay);
            }

            return stringBuilder.ToString();
        }

        internal static string ClearCartCommand(ulong steamID, string botsQuery)
        {
            if (string.IsNullOrWhiteSpace(botsQuery))
                throw new ArgumentNullException($"{nameof(botsQuery)} is empty");


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

                if (steamStore.ClearCart())
                {
                    stringBuilder.AppendLine($"<{bot.BotName}> Cart cleared");
                    bot.ArchiLogger.LogGenericInfo($"Cart cleared");
                }
                else
                {
                    stringBuilder.AppendLine($"<{bot.BotName}> Unable to clear cart");
                    bot.ArchiLogger.LogGenericInfo($"Unable to clear cart");
                }
            }

            return stringBuilder.ToString();
        }

        internal static string HelpCommand()
        {
            return "gbphelp – get Game Buy Plugin help" +
                "";
        }
    }
}
