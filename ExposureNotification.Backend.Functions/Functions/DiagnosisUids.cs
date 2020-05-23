using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ExposureNotification.Backend.Functions
{
	public static class DiagnosisUids
	{
		[FunctionName("DiagnosisUids")]
		public static async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Function, "put", "delete", Route = null)] HttpRequest req)
		{
			var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

			if (req.Method.Equals("put", StringComparison.OrdinalIgnoreCase))
			{
				var diagnosisUids = JsonConvert.DeserializeObject<IEnumerable<string>>(requestBody);

				await Startup.Database.AddDiagnosisUidsAsync(diagnosisUids);
			}
			else if (req.Method.Equals("delete", StringComparison.OrdinalIgnoreCase))
			{
				var diagnosisUids = JsonConvert.DeserializeObject<IEnumerable<string>>(requestBody);

				await Startup.Database.RemoveDiagnosisUidsAsync(diagnosisUids);
			}

			return new OkResult();
		}
	}
}
