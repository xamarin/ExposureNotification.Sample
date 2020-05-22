using System;
using System.Threading.Tasks;
using ExposureNotification.Backend.Functions;
using ExposureNotification.Backend.Network;

namespace ExposureNotification.Backend.DeviceVerification
{
	static class Verify
	{
		public static Task<bool> VerifyDevice(SelfDiagnosisSubmission submission, DateTimeOffset requestTime, DevicePlatform platform)
		{
			var auth = Startup.GetAuthorizedApp(platform);

			return platform switch
			{
				DevicePlatform.Android => AndroidVerify.VerifyToken(submission.DeviceVerificationPayload, submission.GetAndroidNonce(), requestTime, auth),
				DevicePlatform.iOS => AppleVerify.VerifyToken(submission.DeviceVerificationPayload, requestTime, auth),
				_ => Task.FromResult(false),
			};
		}

		public enum DevicePlatform
		{
			iOS,
			Android
		}
	}
}
