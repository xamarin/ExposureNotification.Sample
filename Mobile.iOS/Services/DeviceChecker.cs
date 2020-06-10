using System;
using System.Threading.Tasks;
using Mobile.iOS.Services;
using Mobile.Services;
using Shared;
using Xamarin.Forms;

[assembly: Dependency(typeof(DeviceChecker))]

namespace Mobile.iOS.Services
{
	public class DeviceChecker : IDeviceVerifier
	{
		public async Task<string> VerifyAsync(SelfDiagnosisSubmission submission)
		{
			var token = await DeviceCheck.DCDevice.CurrentDevice.GenerateTokenAsync();
			return Convert.ToBase64String(token.ToArray());
		}
	}
}