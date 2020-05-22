using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ExposureNotification.Backend.Network;
using ExposureNotification.Backend.DeviceVerification;

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
					if (!await Verify.VerifyDevice(diagnosis.DeviceVerificationPayload, diagnosis.Platform.Equals("android") ? Verify.DevicePlatform.Android : Verify.DevicePlatform.iOS))
						return new BadRequestResult();
				}

				await Startup.Database.SubmitPositiveDiagnosisAsync(diagnosis);
			}

			return new OkResult();
		}
	}
}
