using System;
using System.Threading.Tasks;
using ExposureNotification.Backend.Database;
using ExposureNotification.Backend.Network;

namespace ExposureNotification.Backend.DeviceVerification
{
	static class Verify
	{
		public static Task<bool> VerifyDevice(SelfDiagnosisSubmission submission, DateTimeOffset requestTime, DbAuthorizedApp.DevicePlatform platform, DbAuthorizedApp auth) =>
			platform switch
			{
				DbAuthorizedApp.DevicePlatform.Android => AndroidVerify.VerifyToken(submission.DeviceVerificationPayload, submission.GetAndroidNonce(), requestTime, auth),
				DbAuthorizedApp.DevicePlatform.iOS => AppleVerify.VerifyToken(submission.DeviceVerificationPayload, requestTime, auth),
				_ => Task.FromResult(false),
			};
	}
}
