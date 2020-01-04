using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;


namespace CloudTgBotCore3
{
    public static partial class TelegramEndpoint
    {
        public static string[] GetKeys(ILogger log)
        {
            string[] keys = new string[3];
            string botApiKey;
            try
            {
                // Gets a variable, in local environment from local.settings, in Azure from Functions environment variables
                botApiKey = Environment.GetEnvironmentVariable("TelegramBotApiKey");
                keys.SetValue(botApiKey, 0);
            }
            catch (Exception)
            {
                log.LogError("No Telegram bot key defined");
            }
            
            string storageAccountKey;
            string storageAccountConnStr;
            try
            {
                // Gets a variable, in local environment from local.settings, in Azure from Functions environment variables
                storageAccountKey = Environment.GetEnvironmentVariable("StorageAccountKey");
                storageAccountConnStr = Environment.GetEnvironmentVariable("StorageAccountConnectionString");
                keys.SetValue(storageAccountKey, 1);
                keys.SetValue(storageAccountConnStr, 2);
            }
            catch (Exception)
            {
                log.LogError("No storage account defined");
            }

            return keys;
        }
        public static string GetLeaderboard(Users users)
        {
            List<KeyValuePair<string, int>> incs = new List<KeyValuePair<string, int>>();


            foreach (var pair in users.Accounts)
            {
                if (pair.Value != users.Accounts["all"])
                {
                    incs.Add(new KeyValuePair<string, int>(pair.Value.Name, pair.Value.Incs));
                }
            }

            incs.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            string msg_to_send = "";
            int i = 1;
            foreach (var pair in incs)
            {
                msg_to_send += i + ". " + pair.Key + ": " + pair.Value + Environment.NewLine;
                ++i;
            }
            return msg_to_send;


        }
        public static int GetIncrement(string[] msg)
        {
            int inc;
            int sign;
            if (msg[0] == "/inc1")
            {
                sign = 1;
            }
            else
            {
                sign = -1;
            }
            if (msg.Length == 1)
            {
                inc = sign;
            }
            else
            {
                int.TryParse(msg[1], out inc);
                inc *= sign;
            }
            return inc;
        }

    }
}
