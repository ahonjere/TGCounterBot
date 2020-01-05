using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot.Types;



namespace CloudTgBotCore3
{
    public static partial class TelegramEndpoint
    {
        [FunctionName("TelegramEndpoint")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "EndpointName")]HttpRequest req, ILogger log)
        {
            string[] keys =  GetKeys(ref log);
            
            string botApiKey = keys[0];
            string storageAccountConnStr = keys[1];

            CloudStorageAccount account = CloudStorageAccount.Parse(storageAccountConnStr);
            CloudBlobClient serviceClient = account.CreateCloudBlobClient();

            var botClient = new Telegram.Bot.TelegramBotClient(botApiKey);

            string jsonContent = await req.ReadAsStringAsync();

            // Class provided by the Telegram.Bot library
            Update update;
            try
            {
                update = JsonConvert.DeserializeObject<Update>(jsonContent);
            }
            catch (Exception)
            {
                log.LogError("Parse failed :(");
                return new BadRequestResult();
            }

            var message = update.Message;
            string chatid = message.Chat.Id.ToString().ToLower();
            // If chat is group, the first character is '-', which cannot be the 
            // first character of container name in Azure.
            chatid = chatid.Remove(0, 1);

            var container = serviceClient.GetContainerReference(chatid);
            container.CreateIfNotExistsAsync().Wait();

            CloudBlockBlob blob = container.GetBlockBlobReference("users.json");
   
            Users users = new Users();
            User all = new User();
            GetUsers(ref users, ref all, blob);
           
            string senderName = message.From.FirstName;
            string senderUserName = message.From.Username;
            string senderId = message.From.Id.ToString();
            User user;

            if (users.Accounts.ContainsKey(senderId))
            {
                user = users.Accounts[senderId];
            }
            else
            {
                user = new User
                {
                    Name = senderName,
                    UserName = senderUserName,
                    Incs = 0,
                    FirstInc = DateTime.Today
                };
                users.Accounts[senderId] = user;
            }

            // Handling the message
           
            string[] msg = message.Text.Split(" ");
            string msg_to_send = "";

            if (msg[0] == "/inc1" | msg[0] == "/dec1")
            {
                int inc = GetIncrement(msg);

                user.Incs += inc;
                all.Incs += inc;

                msg_to_send = "All: " + all.Incs.ToString() + " " + senderName + ": " + user.Incs.ToString();
            }

            if (msg[0] == "/leaderboard")
            {
                msg_to_send = GetLeaderboard(users);
            }
            if (msg[0] == "/stats")
            {
             
                if (msg.Length == 2)
                {
                    if (msg[1] == "all")
                    {
                        user = users.Accounts["all"];
                    }
                }
                double incs_per_day = user.Incs / (DateTime.Today - user.FirstInc).TotalDays;
                incs_per_day = Math.Round(incs_per_day, 2);
                msg_to_send = user.Name + " Incs: " + user.Incs + ". Incs per day: " + incs_per_day.ToString();
            }
            if (msg_to_send != "")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, msg_to_send);
            }

            string jsonStr = JsonConvert.SerializeObject(users);
            await blob.UploadTextAsync(jsonStr);

            return new OkResult();
        }
       
    }
}

           