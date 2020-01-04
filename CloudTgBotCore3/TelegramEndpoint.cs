using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
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

            string botApiKey;
            try
            {
                // Gets a variable, in local environment from local.settings, in Azure from Functions environment variables
                botApiKey = Environment.GetEnvironmentVariable("TelegramBotApiKey");
            }
            catch (Exception)
            {
                log.LogError("No Telegram bot key defined");
                return new InternalServerErrorResult();
            }
            string storageAccountKey;
            string storageAccountConnStr;
            try
            {
                // Gets a variable, in local environment from local.settings, in Azure from Functions environment variables
                storageAccountKey = Environment.GetEnvironmentVariable("StorageAccountKey");
                storageAccountConnStr = Environment.GetEnvironmentVariable("StorageAccountConnectionString");
            }
            catch (Exception)
            {
                log.LogError("No storage account defined");
                return new InternalServerErrorResult();
            }
            var botClient = new Telegram.Bot.TelegramBotClient(botApiKey);

            string jsonContent = await req.ReadAsStringAsync();

            CloudStorageAccount account = CloudStorageAccount.Parse(storageAccountConnStr);
            CloudBlobClient serviceClient = account.CreateCloudBlobClient();

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
            string chatid = "";

            try
            {
                chatid = message.Chat.Id.ToString().ToLower();
            }
            catch (Exception)
            {
                return new OkResult();
            }
            chatid = chatid.Remove(0, 1);

            var container = serviceClient.GetContainerReference(chatid);
            container.CreateIfNotExistsAsync().Wait();

           
            CloudBlockBlob blob = container.GetBlockBlobReference("users.json");
   

            string jsonStr;
            Users users;
            User all;
            if (blob.ExistsAsync().Result)
            {
                jsonStr = await blob.DownloadTextAsync();
                users = JsonConvert.DeserializeObject<Users>(jsonStr);
                if (users.Accounts.ContainsKey("all"))
                {
                    all = users.Accounts["all"];
                }
                else
                {
                    all = new User
                    {
                        Name = "all",
                        Incs = 0,
                        FirstInc = DateTime.Today
                    };

                    users.Accounts["all"] = all;
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
                
                users.Accounts["All"]  = all;
            }


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

            jsonStr = JsonConvert.SerializeObject(users);
            await blob.UploadTextAsync(jsonStr);

            return new OkResult();
        }
       
    }
}

           