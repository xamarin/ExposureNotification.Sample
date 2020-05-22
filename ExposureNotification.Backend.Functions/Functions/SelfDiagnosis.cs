using System;
using System.IO;
using System.Threading.Tasks;
using ExposureNotification.Backend.Database;
using ExposureNotification.Backend.DeviceVerification;
using ExposureNotification.Backend.Network;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ExposureNotification.Backend.Functions
{
	public class SelfDiagnosis
	{
		readonly ExposureNotificationStorage storage;
		readonly IOptions<Settings> settings;

		public SelfDiagnosis(ExposureNotificationStorage storage, IOptions<Settings> settings)
		{
			this.storage = storage;
			this.settings = settings;
		}

		[FunctionName("Diagnosis")]
		public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "selfdiagnosis")] HttpRequest req)
		{
			var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

			if (req.Method.Equals("put", StringComparison.OrdinalIgnoreCase))
			{
				var diagnosis = JsonConvert.DeserializeObject<SelfDiagnosisSubmission>(requestBody);

				// Verification may be disabled for testing
				if (!settings.Value.DisableDeviceVerification)
				{
					var platform = DbAuthorizedApp.ParsePlatform(diagnosis.Platform);
					var authApp = storage.GetAuthorizedApp(platform);

					// Verify the device payload (safetynet attestation on android, or device check token on iOS)
					if (!await Verify.VerifyDevice(diagnosis, DateTimeOffset.UtcNow, platform, authApp))
						return new BadRequestResult();
				}

				await storage.SubmitPositiveDiagnosisAsync(diagnosis);
			}

			return new OkResult();
		}
	}
}
