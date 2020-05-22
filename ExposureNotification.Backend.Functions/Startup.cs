using System;
using ExposureNotification.Backend.Database;
using ExposureNotification.Backend.Signing;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(ExposureNotification.Backend.Functions.Startup))]

namespace ExposureNotification.Backend.Functions
{
	public class Startup : FunctionsStartup
	{
		const string inMemoryDatabaseName = "ChangeInProduction";
		readonly static char[] separators = new[] { ';', ',', ':' };

		internal static ExposureNotificationStorage Database { get; private set; }

		public override void Configure(IFunctionsHostBuilder builder)
		{
			SqlServerConnectionString = GetEnv("SqlServerConnectionString");
			BlobStorageConnectionString = GetEnv("BlobStorageConnectionString");
			BlobStorageContainerNamePrefix = GetEnv("BlobStorageContainerNamePrefix", string.Empty);
			DeleteKeysFromDbAfterBatching = GetEnv("DeleteKeysFromDbAfterBatching", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
			ExposureKeyRegions = GetEnv("ExposureKeyRegions", DbTemporaryExposureKey.DefaultRegion).Split(separators);

			builder.Services.AddTransient<ISigner, Signer>();

			Database = new ExposureNotificationStorage(
				builder =>
				{
					if (string.IsNullOrEmpty(SqlServerConnectionString))
						builder.UseInMemoryDatabase(inMemoryDatabaseName)
							.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
					else
						builder.UseSqlServer(SqlServerConnectionString);
				},
				initialize =>
				{
					initialize.Database.EnsureCreated();
				});
		}

		internal static string SqlServerConnectionString { get; private set; }

		internal static string BlobStorageConnectionString { get; private set; }

		internal static string BlobStorageContainerNamePrefix { get; private set; }

		internal static string[] ExposureKeyRegions { get; private set; }

		internal static bool DeleteKeysFromDbAfterBatching { get; private set; }

		static string GetEnv(string name, string nullValue = null)
			=> Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process) ?? nullValue;
	}
}
