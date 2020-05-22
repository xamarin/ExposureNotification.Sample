using System;
using System.IO;
using ExposureNotification.Backend.DeviceVerification;
using ExposureNotification.Backend.Network;
using Newtonsoft.Json;
using Xunit;

namespace ExposureNotification.Backend.Functions.Tests
{
	public class AndroidVerifyTests
	{
		static readonly SelfDiagnosisSubmission actualSubmission = 
			JsonConvert.DeserializeObject<SelfDiagnosisSubmission>(File.ReadAllText("TestAssets/submission.json"));

		const string cleartext =
			"com.companyname.ExposureNotification.app|" +
			"A8sh1Z5hB7hFKejbzwclnA==.2649983.3.2,Qm8HISNU/bdI+9o1/4ZHZw==.2650127.0.6|" +
			"DEFAULT|" +
			"POSITIVE_TEST_123456";

		const string sha256 = "pnpQ6KkSqZ+y0yJIxKc9eqNXIM8olKZ5/rV0mxvIDWg=";

		static readonly SelfDiagnosisSubmission submission = new SelfDiagnosisSubmission
		{
			AppPackageName = "com.companyname.ExposureNotification.app",
			Platform = "android",
			Regions = new[] { "default" },
			VerificationPayload = "POSITIVE_TEST_123456",
			Keys =
			{
				new ExposureKey { Key = "Qm8HISNU/bdI+9o1/4ZHZw==", RollingStart = 2650127, RollingDuration = 0, TransmissionRisk = 6 },
				new ExposureKey { Key = "A8sh1Z5hB7hFKejbzwclnA==", RollingStart = 2649983, RollingDuration = 3, TransmissionRisk = 2 },
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

		[Fact]
		public void VerifyPayloadTest()
		{
			var claims = AndroidVerify.VerifyPayload(actualSubmission.DeviceVerificationPayload);
			var nonce = submission.GetAndroidNonce();

			Assert.Equal(Convert.ToBase64String(nonce), Convert.ToBase64String(claims.Nonce));
		}
	}
}
