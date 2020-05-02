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
			if (!DateTime.TryParse(req.Query?["since"], out var since))
				since = DateTime.UtcNow.AddDays(-14);

			var keysResponse = await Startup.Database.GetKeysAsync(since);

			return new OkObjectResult(keysResponse);
		}
	}
}
