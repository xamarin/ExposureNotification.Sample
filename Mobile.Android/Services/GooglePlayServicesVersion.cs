using Android.Gms.Common;
using Mobile.Droid.Services;
using Mobile.Services;
using Xamarin.Forms;

[assembly: Dependency(typeof(GooglePlayServicesVersion))]

namespace Mobile.Droid.Services
{
	partial class GooglePlayServicesVersion : ISystemImplementationVersion
	{
		public string? GetVersion()
		{
			try
			{
				var pm = Android.App.Application.Context.PackageManager;
				var packageInfo = pm.GetPackageInfo(GoogleApiAvailability.GooglePlayServicesPackage, 0);

				return packageInfo.VersionName;
			}
			catch
			{
				return null;
			}
		}
	}
}
