using System;
using System.IO;
using System.Threading.Tasks;
using ExposureNotification.Backend.DeviceVerification;
using ExposureNotification.Backend.Network;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace ExposureNotification.Backend.Functions
{
	public class SelfDiagnosis
	{
		[FunctionName("Diagnosis")]
		public async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "selfdiagnosis")] HttpRequest req)
		{
			var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

			if (req.Method.Equals("put", StringComparison.OrdinalIgnoreCase))
			{
				var diagnosis = JsonConvert.DeserializeObject<SelfDiagnosisSubmission>(requestBody);

				// Verification may be disabled for testing
				if (!Startup.DisableDeviceVerification)
				{
					// Verify the device payload (safetynet attestation on android, or device check token on iOS)
					var platform = diagnosis.Platform.Equals("android", StringComparison.OrdinalIgnoreCase)
						? Verify.DevicePlatform.Android
						: Verify.DevicePlatform.iOS;

					if (!await Verify.VerifyDevice(diagnosis, DateTimeOffset.UtcNow, platform))
						return new BadRequestResult();
				}

				await Startup.Database.SubmitPositiveDiagnosisAsync(diagnosis);
			}

			return new OkResult();
		}
	}
}
