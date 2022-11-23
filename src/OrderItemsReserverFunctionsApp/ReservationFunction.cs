using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;

namespace OrderItemsReserverFunctionsApp
{
    public class ReservationFunction
    {
		static IConfiguration _configuration;

		[FunctionName("ReservationFunction")]
        public static async Task Run([ServiceBusTrigger("orderitems", Connection = "OrderItemsBusConnectionString")]string myQueueItem, 
			ILogger log, 
			ExecutionContext context)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

			_configuration = GetAppConfiguration(context);

			ReservedItem reservedItem = JsonConvert.DeserializeObject<ReservedItem>(myQueueItem);

			bool success = await TryUploadItem(reservedItem);

			if (!success)
				await SendFailureEmail(reservedItem);
		}

		private static IConfiguration GetAppConfiguration(ExecutionContext context)
		{
			return new ConfigurationBuilder()
				.SetBasePath(context.FunctionAppDirectory)
				.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build();
		}

		private static async Task<bool> TryUploadItem(ReservedItem reservedItem)
		{
			for (int i = 0; i < 3; i++)
			{
				try
				{
					await UploadItem(reservedItem);
					return true;
				}
				catch (Exception)
				{
					continue;
				}
			}

			return false;
		}

		private static async Task UploadItem(ReservedItem reservedItem)
		{
			BlobServiceClient blobServiceClient = new BlobServiceClient(_configuration["ReservationsBlobStorageConnectionString"]);
			BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("reservations");
			await containerClient.CreateIfNotExistsAsync();

			BlobClient blobClient = containerClient.GetBlobClient(Guid.NewGuid().ToString());

			using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reservedItem))))
			{
				await blobClient.UploadAsync(ms);
			}
		}

		private static async Task SendFailureEmail(ReservedItem reservedItem)
		{
			using (var client = new HttpClient())
			{
				string functionAppUri = _configuration["EmailSnderAppUri"];

				var serializedItem = JsonConvert.SerializeObject(reservedItem);

				var content = new StringContent(serializedItem, Encoding.UTF8, "application/json");

				HttpResponseMessage response = await client.PostAsync(functionAppUri, content);
				if (response.IsSuccessStatusCode)
				{
					Console.WriteLine("Succesfully sent order error details");
				}
				else
				{
					Console.WriteLine("Failed sending order error dedails");
				}
			}
		}
	}
}
