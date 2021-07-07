using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Composition;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Core;
using SteamKit2;
using Newtonsoft.Json;

namespace ASFGameBuyPlugin
{
    [Export(typeof(IPlugin))]
    public sealed class BuyPlugin: 
        IPlugin,
        IBotCommand,
        IBotConnection
    {
        private ConcurrentDictionary<Bot, SteamStore> SteamStoreStorage = new();

        const string PLUGIN_NAME = "Game Buy Plugin";

        public string Name => PLUGIN_NAME;
        public Version Version => typeof(BuyPlugin).Assembly.GetName().Version ?? new(0, 0, 0, 0);

        public void OnLoaded() 
        {
            if (!Directory.Exists(Constants.PLUGIN_PATH))
            {
                ASF.ArchiLogger.LogGenericWarning($"Plugin is running in the wrong directory. Please use the correct: \"{Constants.PLUGIN_PATH}\". Plugin will use default in-memory configuration");
                return;
            }

            if (!File.Exists(Constants.ConfigPath))
            {
                ASF.ArchiLogger.LogGenericWarning($"Unable to found configuration file. Please ensure that the file exists and correct: \"{Constants.ConfigPath}\". Plugin will use default in-memory configuration");
                return;
            }

            string configContent = File.ReadAllText(Constants.ConfigPath);
            if (string.IsNullOrWhiteSpace(configContent))
            {
                ASF.ArchiLogger.LogGenericWarning($"File \"{Constants.ConfigPath}\" is empty. Plugin will use default in-memory configuration");
                return;
            }

            var config = Helpers.DeserializeJson<JsonData.Configuration>(configContent);
            if (config == null || config.BuyTimeout == 0)
            {
                ASF.ArchiLogger.LogGenericWarning($"Please use valid file \"{Constants.ConfigPath}\". Plugin will use default in-memory configuration");
                return;
            }

            GlobalParameters.BuyDelay = TimeSpan.FromSeconds(config.BuyTimeout);
        }

        public void OnBotDisconnected(Bot bot, EResult reason) { }
        public void OnBotLoggedOn(Bot bot)
        {
            if (!SteamStoreStorage.ContainsKey(bot))
            {
                if (!SteamStoreStorage.TryAdd(bot, new(bot)))
                    throw new InvalidOperationException($"{nameof(SteamStoreStorage)} unable to add SteamStore");
            }
        }

        public async Task<string?> OnBotCommand(Bot bot, ulong steamID, string message, string[] args)
        {
            string command = args[0].ToLower();

            switch (command)
            {
                case Commands.BUY_GAME:
                    if (args.Length != 4)
                        return $"Wrong arguments. Please use \"{Commands.BuyGameListing}\"";

                    if (!uint.TryParse(args[2], out uint appID))
                        return $"{nameof(appID)} must be a valid number";

                    if (!uint.TryParse(args[3], out uint subID))
                        return $"{nameof(subID)} must be a valid number";

                    return await Commands.BuyGameCommandAsync(steamID, args[1], SteamStoreStorage, appID, subID);
                case Commands.BUY_BUNDLE:
                    if (args.Length != 3)
                        return $"Wrong arguments. Please use \"{Commands.BuyBundleListing}\"";

                    if (!uint.TryParse(args[2], out uint bundleID))
                        return $"{nameof(bundleID)} must be a valid number";

                    return await Commands.BuyBundleCommandAsync(steamID, args[1], SteamStoreStorage, bundleID);
                case Commands.BUY_ITEM:
                    if (args.Length < 4 || args.Length > 5)
                        return $"Wrong arguments. Please use \"{Commands.BuyItemListing}\" or \"{Commands.BuyItemListingQuantity}\"";

                    if (!uint.TryParse(args[2], out appID))
                        return $"{nameof(appID)} must be a valid number";

                    if (!uint.TryParse(args[3], out uint itemID))
                        return $"{nameof(itemID)} must be a valid number";

                    uint quantity = 1;
                    if (args.Length == 5)
                        if (!uint.TryParse(args[4], out quantity))
                            return $"{nameof(quantity)} must be a valid number";

                    return await Commands.BuyInGameCommandAsync(steamID, args[1], SteamStoreStorage, appID, itemID, quantity);
                case Commands.CLEAR_CART:
                    if (args.Length != 2)
                        return $"Wrong argument. Please use \"{Commands.ClearCartListing}\"";

                    return Commands.ClearCartCommand(steamID, args[1], SteamStoreStorage);

                case Commands.HELP:
                    return Commands.HelpCommand();
                default:
                    return null;
            }
        }
    }

    internal static class Constants
    {
        internal const string PLUGIN_PATH = "plugins/GameBuyPlugin";
        internal const string CONFIG_NAME = "gbpconfig.json";
        internal static string ConfigPath = $"{PLUGIN_PATH}/{CONFIG_NAME}";
    }

    internal static class GlobalParameters
    {
        internal static TimeSpan BuyDelay = TimeSpan.FromSeconds(15);
    }

    internal static class Helpers
    {
        internal static T? DeserializeJson<T>(string content) where T: class
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                ASF.ArchiLogger.LogGenericError($"{nameof(content)} is empty");
                return null;
            }

            try
            {
                var value = JsonConvert.DeserializeObject<T>(content);
                return value;
            }
            catch (JsonException exception)
            {
                ASF.ArchiLogger.LogGenericError($"{nameof(content)} is invalid: {exception.Message}");
                return null;
            }
        }
    }
}
