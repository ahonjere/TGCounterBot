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
            GetUsers(ref users, ref all, ref blob);
           
            User sender = new User();
            GetSender(message, ref sender, ref users);
            string msg_to_send = "";
            // Handling the message
            if ( message.Text.Contains("@aukte123Bot") )
            {
                message.Text = message.Text.Remove(message.Text.Length - 12);
                
            }
            message.Text = message.Text.ToLower();
            string[] msg = message.Text.Split(" ");
            

            if (msg[0] == "/inc1" | msg[0] == "/dec1")
            {
                int inc = GetIncrement(msg);

                sender.Incs += inc;
                all.Incs += inc;

                msg_to_send = "All: " + all.Incs.ToString() + " " + sender.Name + ": " + sender.Incs.ToString();
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
                        sender = users.Accounts["all"];
                    }
                }
                double incs_per_day = sender.Incs / (DateTime.Today - sender.FirstInc).TotalDays;
                incs_per_day = Math.Round(incs_per_day, 2);
                msg_to_send = sender.Name + " Incs: " + sender.Incs + ". Incs per day: " + incs_per_day.ToString();
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

           