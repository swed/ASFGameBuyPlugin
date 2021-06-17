﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam;

namespace ASFGameBuyPlugin
{
    internal class SteamStore
    {
        private Bot Bot;
        internal SteamStore(Bot bot) => Bot = bot;

        internal async Task<Dictionary<string, string>[]?> GetAllSubID(uint appID)
        {
            if (appID == 0)
                throw new ArgumentException("appID cannot be 0");

            if (!Bot.IsConnectedAndLoggedOn)
            {
                Bot.ArchiLogger.LogGenericError("Is currently not logged on");
                return null;
            }

            const string STORE_URL = "https://store.steampowered.com/app/{0}/";
            Uri storeUri = new(string.Format(STORE_URL, appID));
            var response = await Bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(storeUri);
            if (response == null || response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Bot.ArchiLogger.LogGenericError($"Unable to download app {appID} page");
                return null;
            }

            const string BUY_FORM_SELECTOR = ".game_area_purchase_game_wrapper > .game_area_purchase_game form";
            var buyForms = response.Content.QuerySelectorAll(BUY_FORM_SELECTOR);

            if (buyForms == null || buyForms.Length == 0)
            {
                Bot.ArchiLogger.LogGenericError($"No buy form found");
                return null;
            }

            List<Dictionary<string, string>> serializableForms = new(buyForms.Length);
            foreach (var form in buyForms)
            {
                if (form == null)
                    continue;

                var formElements = form.QuerySelectorAll("input");
                if (formElements == null || formElements.Length == 0)
                    continue;

                Dictionary<string, string> serializableForm = new();
                foreach (var formElement in formElements)
                {
                    if (formElement == null)
                        continue;

                    serializableForm.Add(formElement.NodeName, formElement.NodeValue);
                }

                serializableForms.Add(serializableForm);
            }

            return serializableForms.ToArray();
        }
    }
}