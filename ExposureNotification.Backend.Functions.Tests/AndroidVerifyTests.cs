using System;
using System.IO;
using ExposureNotification.Backend.DeviceVerification;
using ExposureNotification.Backend.Network;
using Xunit;

namespace ExposureNotification.Backend.Functions.Tests
{
	public class AndroidVerifyTests
	{
		const string appPackageName = "com.google.android.apps.exposurenotification";

		static readonly string payload = File.ReadAllText("TestAssets/test.payload");

		const string cleartext = "com.google.android.apps.exposurenotification|QLIvVheW9p6JiTx4pslesg==.2649312.144.1,UW7pkDKfLbfLs8LveFyY3w==.2649168.144.1|US|POSITIVE_TEST_123456";
		const string sha256 = "VLIju4jpickw6QDy/poqNlVd/0aIsJlNclAIh2jBz1M=";

		static readonly SelfDiagnosisSubmission submission = new SelfDiagnosisSubmission
		{
			AppPackageName = appPackageName,
			DeviceVerificationPayload = payload,
			Platform = "android",
			Regions = new[] { "US" },
			VerificationPayload = "POSITIVE_TEST_123456",
			Keys =
			{
				new ExposureKey { Key = "QLIvVheW9p6JiTx4pslesg==", RollingStart = 2649312, RollingDuration = 144, TransmissionRisk = 1 },
				new ExposureKey { Key = "UW7pkDKfLbfLs8LveFyY3w==", RollingStart = 2649168, RollingDuration = 144, TransmissionRisk = 1 },
			}
		};

		[Fact]
		public void VerifyNonceClearTextTest()
		{
			var nonceClear = submission.GetAndroidNonceClearText();

			Assert.Equal(cleartext, nonceClear);
		}

		[Fact]
		public void VerifyNonceTest()
		{
			var nonce = submission.GetAndroidNonce();

			Assert.Equal(sha256, Convert.ToBase64String(nonce));
		}

		//[Fact]
		//public void VerifyPayloadTest()
		//{
		//	var claims = AndroidVerify.VerifyPayload(payload);
		//	var nonce = submission.GetAndroidNonce();

		//	Assert.Equal(Convert.ToBase64String(nonce), Convert.ToBase64String(claims.Nonce));
		//}
	}
}
