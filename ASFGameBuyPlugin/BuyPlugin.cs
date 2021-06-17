using System;
using System.Threading.Tasks;
using System.Composition;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;

namespace ASFGameBuyPlugin
{
    [Export(typeof(IPlugin))]
    public class BuyPlugin: 
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
                    return await Commands.BuyGameCommandAsync(bot, steamID);
                default:
                    return null;
            }
        }
    }
}
