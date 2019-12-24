using System;
using System.Threading.Tasks;
using System.Web.Http;
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
                log.LogInformation("Parse succeeded!");
            }
            catch (Exception)
            {
                log.LogError("Parse failed :(");
                return new BadRequestResult();
            }


            var message = update.Message;
            string chatid = message.Chat.Id.ToString().ToLower();
            chatid = chatid.Remove(0, 1);
            log.LogInformation(chatid);

            var container = serviceClient.GetContainerReference(chatid);
            container.CreateIfNotExistsAsync().Wait();

       
            CloudBlockBlob allBlob = container.GetBlockBlobReference("all.txt");
            CloudBlockBlob userBlob = container.GetBlockBlobReference("users.json");
            int all;
           
            
            if (allBlob.ExistsAsync().Result)
            {
                string allStr;
                allStr = await allBlob.DownloadTextAsync();
                all = Convert.ToInt32(allStr);
            }
            else
            {
                all = 0;
            }
            
            string jsonStr;
            Users users;
            if (userBlob.ExistsAsync().Result)
            {   
                jsonStr = await userBlob.DownloadTextAsync();
                users = JsonConvert.DeserializeObject<Users>(jsonStr);
            }
            else
            {
                users = new Users();
            }
           

            string senderName = message.From.FirstName;
            string senderUserName = message.From.Username;
            User user;
            if (users.Accounts.ContainsKey(senderUserName))
            {
                user = users.Accounts[senderUserName];
            }
            else
            {
                user = new User
                {
                    Name = senderName,
                    Incs = 0,
                    FirstInc = DateTime.Today
                };
                users.Accounts[senderUserName] = user;              
            }

            // Handling the message

            string[] msg = message.Text.Split(" ");
            
            if (msg[0] == "/inc1" | msg[0] == "/dec1")
            {
                int inc = DefIncrement(msg);

                user.Incs += inc;
                all += inc;
                
                await botClient.SendTextMessageAsync(message.Chat.Id, "All: " + all.ToString() + " " + senderName + ": " + user.Incs.ToString());
            }
            jsonStr = JsonConvert.SerializeObject(users);
            await userBlob.UploadTextAsync(jsonStr);
            await allBlob.UploadTextAsync(all.ToString());

            return new OkResult();
        }
        public static int DefIncrement(string[] msg)
        {
            int inc = 0;
            int sign = 0;
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
