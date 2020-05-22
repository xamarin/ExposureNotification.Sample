using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Android.Gms.SafetyNet;
using ExposureNotification.App.Droid.Services;
using ExposureNotification.App.Services;
using ExposureNotification.Backend.Network;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Dependency(typeof(SafetyNetAttestor))]

namespace ExposureNotification.App.Droid.Services
{
	partial class SafetyNetAttestor : IDeviceVerifier
	{
		readonly string safetyNetApiKey = "YOUR-KEY-HERE";

		public Task<string> VerifyAsync(SelfDiagnosisSubmission submission) =>
			AttestAsync(submission.Keys, submission.Regions, submission.VerificationPayload);

		public Task<string> AttestAsync(IEnumerable<ExposureKey> keys, IEnumerable<string> regions, string verificationCode)
		{
			var cleartext = GetClearText(keys, regions, verificationCode);
			var nonce = GetSha256(cleartext);

			return GetSafetyNetAttestationAsync(nonce);
		}

		async Task<string> GetSafetyNetAttestationAsync(byte[] nonce)
		{
			using var client = SafetyNetClass.GetClient(Android.App.Application.Context);
			using var response = await client.AttestAsync(nonce, safetyNetApiKey);
			return response.JwsResult;
		}

		static string GetClearText(IEnumerable<ExposureKey> keys, IEnumerable<string> regions, string verificationCode) =>
			string.Join("|", AppInfo.PackageName, GetKeyString(keys), GetRegionString(regions), verificationCode);

		static string GetKeyString(IEnumerable<ExposureKey> keys) =>
			string.Join(",", keys.Select(k => GetKeyString(k)).OrderBy(k => k));

		static string GetKeyString(ExposureKey k) =>
			string.Join(".", k.Key, k.RollingStart, k.RollingDuration);

		static string GetRegionString(IEnumerable<string> regions) =>
			string.Join(",", regions);

		static byte[] GetSha256(string text)
		{
			using var sha = SHA256.Create();
			var textBytes = Encoding.UTF8.GetBytes(text);
			return sha.ComputeHash(textBytes);
		}
	}
}
