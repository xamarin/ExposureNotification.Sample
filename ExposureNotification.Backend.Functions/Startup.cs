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
			SqlServerConnectionString =
				Environment.GetEnvironmentVariable("SqlServerConnectionString", EnvironmentVariableTarget.Process);
			BlobStorageConnectionString =
				Environment.GetEnvironmentVariable("BlobStorageConnectionString", EnvironmentVariableTarget.Process);
			BlobStorageContainerNamePrefix =
				Environment.GetEnvironmentVariable("BlobStorageContainerNamePrefix", EnvironmentVariableTarget.Process) ?? string.Empty;
			DeleteKeysFromDbAfterBatching =
				(Environment.GetEnvironmentVariable("DeleteKeysFromDbAfterBatching", EnvironmentVariableTarget.Process) ?? string.Empty).Equals("true", StringComparison.OrdinalIgnoreCase);

			var regions =
				Environment.GetEnvironmentVariable("ExposureKeyRegions", EnvironmentVariableTarget.Process)
					?? DbTemporaryExposureKey.DefaultRegion;

			ExposureKeyRegions = regions.Split(new[] { ';', ',', ':' });

			Database = new ExposureNotificationStorage(
				builder =>
				{
					if (string.IsNullOrEmpty(SqlServerConnectionString))
						builder.UseInMemoryDatabase("ChangeInProduction");
					else
						builder.UseSqlServer(SqlServerConnectionString);
				},
				initialize =>
				{
					//if (string.IsNullOrEmpty(SqlServerConnectionString))
						initialize.Database.EnsureCreated();
				});
		}

		internal static string SqlServerConnectionString { get; private set; }
		internal static string BlobStorageConnectionString { get; private set; }
		internal static string BlobStorageContainerNamePrefix { get; private set; }

		internal static string[] ExposureKeyRegions { get; private set; }

		internal static bool DeleteKeysFromDbAfterBatching { get; private set; }
	}
}
