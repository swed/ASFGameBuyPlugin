using System;
using System.Threading.Tasks;
using System.Composition;
using System.Reflection;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;

namespace ASFGameBuyPlugin
{
    [Export(typeof(IPlugin))]
    public sealed class BuyPlugin: 
        IPlugin,
        IBotCommand
    {
        const string PLUGIN_NAME = "Game Buy Plugin";

        public string Name => PLUGIN_NAME;
        public Version Version => typeof(BuyPlugin).Assembly.GetName().Version ?? new(0, 0, 0, 0);

        public void OnLoaded() { }

        public async Task<string?> OnBotCommand(Bot bot, ulong steamID, string message, string[] args)
        {
            string command = args[0];

            switch (command)
            {
                case "buygame":
                    return await Commands.BuyGameCommandAsync(steamID, args[1], uint.Parse(args[2]), uint.Parse(args[3]));
                default:
                    return null;
            }
        }
    }

    internal static class Constants
    {
        internal static TimeSpan BuyDelay = TimeSpan.FromSeconds(15);
    }
}
