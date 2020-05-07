using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xamarin.ExposureNotifications.Proto;
using System.Net.Http;
using System.Net;
using Google.Protobuf;

namespace ExposureNotification.Backend.Functions
{
	public class Keys
	{
		[FunctionName("Keys")]
		public async Task<IActionResult> KeyJson(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "keys")] HttpRequest req)
		{
			if (!ulong.TryParse(req.Query?["since"], out var since))
				since = 0;

			if (!int.TryParse(req.Query?["skip"], out var skip))
				skip = 0;

			if (!int.TryParse(req.Query?["take"], out var take))
				take = 1000;

			var keysResponse = await Startup.Database.GetKeysAsync(since, skip, take);

			return new OkObjectResult(keysResponse);
		}

		[FunctionName("KeyFiles")]
		public async Task<HttpResponseMessage> KeyFile(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "keyfile")] HttpRequest req)
		{
			if (!long.TryParse(req.Query?["since"], out var since))
				since = 0;

			if (!int.TryParse(req.Query?["batchNum"], out var batchNum))
				batchNum = 1;

			var region = req?.Query?["region"] ?? string.Empty;


			using var ms = new System.IO.MemoryStream();

			// Get keys as a protobuf file for apple
			var file = await Startup.Database.GetKeysFileAsync(since, batchNum, region);
			// Serialize protobuf to the memory stream
			file.WriteTo(ms);
			// Back up stream to send from start
			ms.Seek(0, System.IO.SeekOrigin.Begin);

			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Content = new StreamContent(ms);

			return response;
		}
	}
}
