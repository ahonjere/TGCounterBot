using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using Telegram.Bot.Types;


namespace CloudTgBotCore3
{
    public static partial class TelegramEndpoint
    {
        // Returns string[] with format {botApiKey, storageAccountConnStr}
        public static string[] GetKeys(ref ILogger log)
        {
            string[] keys = new string[2];
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
            
            string storageAccountConnStr;
            try
            {
                storageAccountConnStr = Environment.GetEnvironmentVariable("StorageAccountConnectionString");
                keys.SetValue(storageAccountConnStr, 1);
            }
            catch (Exception)
            {
                log.LogError("No storage account defined");
            }

            return keys;
        }
        // Reads all user data from cloud blob. If there is none, creates new blob with user "all".
        public static void GetUsers(ref Users users, ref User all, ref CloudBlockBlob blob)
        {
            string jsonStr;
            if (blob.ExistsAsync().Result)
            {
                jsonStr = blob.DownloadText();
                users = JsonConvert.DeserializeObject<Users>(jsonStr);
                if (users.Accounts.ContainsKey("all"))
                {
                    all = users.Accounts["all"];
                }
            }
            else
            {
                users = new Users();
                all = new User
                {
                    Name = "all",
                    Incs = 0,
                    FirstInc = DateTime.Today
                };

                users.Accounts.Add("All", all);
            }
        }
        // Pulls the data of sender from database.
        public static void GetSender(Message message, ref User sender, ref Users users)
        {
            string senderName = message.From.FirstName;
            string senderUserName = message.From.Username;
            string senderId = message.From.Id.ToString();
            if (users.Accounts.ContainsKey(senderId))
            {
                sender = users.Accounts[senderId];
            }
            else
            {
                sender = new User
                {
                    Name = senderName,
                    UserName = senderUserName,
                    Incs = 0,
                    FirstInc = DateTime.Today
                };
                users.Accounts[senderId] = sender;
            }
        }
        // Returns amount of increments.
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
        // Returns leaderboard in form of string.
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
    }
}
