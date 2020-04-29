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
using ExposureNotification.App;

namespace ExposureNotification.Backend.Functions
{
	public class Diagnosis
	{
		public Diagnosis(IExposureNotificationStorage storage)
			=> this.storage = storage;

		readonly IExposureNotificationStorage storage;

		[FunctionName("Diagnosis")]
		public async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", "delete", Route = "diagnosis")] HttpRequest req,
			ILogger log)
		{
			var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

			if (req.Method.Equals("put", StringComparison.OrdinalIgnoreCase))
			{
				var diagnosisUids = JsonConvert.DeserializeObject<IEnumerable<string>>(requestBody);

				await storage.AddDiagnosisUidsAsync(diagnosisUids);
			}
			else if (req.Method.Equals("delete", StringComparison.OrdinalIgnoreCase))
			{
				var diagnosisUids = JsonConvert.DeserializeObject<IEnumerable<string>>(requestBody);

				await storage.RemoveDiagnosisUidsAsync(diagnosisUids);
			}
			else if (req.Method.Equals("post", StringComparison.OrdinalIgnoreCase))
			{
				var submission = JsonConvert.DeserializeObject<DiagnosisSubmission>(requestBody);

				await storage.SubmitPositiveDiagnosisAsync(submission.DiagnosisUid, submission.TemporaryExposureKeys);
			}

			return new OkResult();
		}
	}
}
