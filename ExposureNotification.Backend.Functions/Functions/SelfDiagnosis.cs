using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using Xamarin.ExposureNotifications;
using Newtonsoft.Json.Linq;

namespace ExposureNotification.Backend.Functions
{
	public class SelfDiagnosis
	{
		[FunctionName("Diagnosis")]
		public async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "selfdiagnosis")] HttpRequest req)
		{
			var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

			// Verify the diagnosis uid
			if (req.Method.Equals("post", StringComparison.OrdinalIgnoreCase))
			{
				var j = JObject.Parse(requestBody);

				var diagnosisUid = j.Value<string>("diagnosisUid");

				if (!await Startup.Database.CheckIfDiagnosisUidExistsAsync(diagnosisUid))
					return new ForbidResult();
			}
			// Submit a self diagnosis after verifying
			else if (req.Method.Equals("put", StringComparison.OrdinalIgnoreCase))
			{
				var diagnosis = JsonConvert.DeserializeObject<ExposureNotificationStorage.SelfDiagnosisSubmissionRequest>(requestBody);

				await Startup.Database.SubmitPositiveDiagnosisAsync(diagnosis);
			}

			return new OkResult();
		}
	}
}
