using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ExposureNotification.Backend.Functions
{
	public class Keys
	{
		[FunctionName("Keys")]
		public async Task<IActionResult> Run(
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
	}
}
