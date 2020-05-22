using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ExposureNotification.Backend.DeviceVerification;
using ExposureNotification.Backend.Functions;

namespace ExposureNotification.Backend.DeviceVerification
{
	internal static class Verify
	{
		public static async Task<bool> VerifyDevice(string token, DevicePlatform platform)
		{
			if (platform == DevicePlatform.Android)
			{
				var s = AndroidVerify.VerifyPayload(token);
				return s.BasicIntegrity;
			}

			return await AppleVerify.VerifyToken(token, Startup.AppleDeviceCheckTeamId, Startup.AppleDeviceCheckKeyId, Startup.AppleDeviceCheckP8FileContents);
		}

		public enum DevicePlatform
		{
			iOS,
			Android
		}
	}
}
