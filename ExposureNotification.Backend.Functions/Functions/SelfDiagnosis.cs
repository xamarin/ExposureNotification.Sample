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

namespace ExposureNotification.Backend.Functions
{
	public class SelfDiagnosis
	{
		[FunctionName("Diagnosis")]
		public async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", "delete", Route = "selfdiagnosis")] HttpRequest req)
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
			else if (req.Method.Equals("post", StringComparison.OrdinalIgnoreCase))
			{
				var (diagnosisUid, keys) = JsonConvert.DeserializeObject<(string diagnosisUid, IEnumerable<TemporaryExposureKey> keys)>(requestBody);

				await Startup.Database.SubmitPositiveDiagnosisAsync(diagnosisUid, keys);
			}

			return new OkResult();
		}
	}
}
