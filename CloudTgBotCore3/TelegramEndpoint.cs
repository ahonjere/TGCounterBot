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
    public static class TelegramEndpoint
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
            
            // write a blob to the container
            CloudBlockBlob allBlob = container.GetBlockBlobReference("all");
            string incStr;
            if ( allBlob.ExistsAsync().Result )
            {
                incStr = await allBlob.DownloadTextAsync();
            }
            else 
            {
                incStr = "0";
            }
            int incs = Convert.ToInt32(incStr);
            

            string senderName = message.From.FirstName;
            CloudBlockBlob personalBlob = container.GetBlockBlobReference(senderName);
            string personalIncStr;
            if (personalBlob.ExistsAsync().Result)
            {
                personalIncStr = await personalBlob.DownloadTextAsync();
            }
            else
            {
                personalIncStr = "0";
            }
            int personalIncs = Convert.ToInt32(personalIncStr);
            
            // Handling the message
            
            string[] msg = message.Text.Split(" ");
            int inc;
            if (msg[0] == "/inc1" | msg[0] == "/dec1")
            {
                inc = DefIncrement(msg);

                incs += inc;
                personalIncs += inc;

                incStr = incs.ToString();
                personalIncStr = personalIncs.ToString();

                await botClient.SendTextMessageAsync(message.Chat.Id, "All: " + incStr + " " + senderName + ": " + personalIncStr);
                allBlob.UploadTextAsync(incStr).Wait();
                personalBlob.UploadTextAsync(personalIncStr).Wait();
            }
            
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
