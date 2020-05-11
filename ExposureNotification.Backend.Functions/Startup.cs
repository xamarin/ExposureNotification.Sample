using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xamarin.ExposureNotifications;

[assembly: FunctionsStartup(typeof(ExposureNotification.Backend.Functions.Startup))]

namespace ExposureNotification.Backend.Functions
{
	public class Startup : FunctionsStartup
	{
		internal static ExposureNotificationStorage Database;

		public override void Configure(IFunctionsHostBuilder builder)
		{
			Database = new ExposureNotificationStorage(
				builder => builder.UseInMemoryDatabase("ChangeInProduction"),
				initialize => initialize.Database.EnsureCreated());

			BlobStorageConnectionString =
				Environment.GetEnvironmentVariable("BlobStorageConnectionString", EnvironmentVariableTarget.Process);
			BlobStorageContainerNamePrefix =
				Environment.GetEnvironmentVariable("BlobStorageContainerNamePrefix", EnvironmentVariableTarget.Process) ?? string.Empty;

			var regions =
				Environment.GetEnvironmentVariable("ExposureKeyRegions", EnvironmentVariableTarget.Process)
					?? DbTemporaryExposureKey.DefaultRegion;

			ExposureKeyRegions = regions.Split(new[] { ';', ',', ':' });
		}

		internal static string BlobStorageConnectionString { get; private set; }
		internal static string BlobStorageContainerNamePrefix { get; private set; }

		internal static string[] ExposureKeyRegions { get; private set; }
	}
}
