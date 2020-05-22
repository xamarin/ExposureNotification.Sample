using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExposureNotification.Backend.Database;
using ExposureNotification.Backend.DeviceVerification;
using ExposureNotification.Backend.Signing;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
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

		public override async void Configure(IFunctionsHostBuilder builder)
		{
			SqlServerConnectionString = await GetKeyVaultSecret(GetEnv("DbConnectionStringSecretId"));
			BlobStorageConnectionString = await GetKeyVaultSecret(GetEnv("BlobStorageConnectionStringSecretId"));
			BlobStorageContainerNamePrefix = GetEnv("BlobStorageContainerNamePrefix", string.Empty);
			DeleteKeysFromDbAfterBatching = GetEnv("DeleteKeysFromDbAfterBatching", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
			DisableDeviceVerification = GetEnv("DisableDeviceVerification", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
			ExposureKeyRegions = GetEnv("ExposureKeyRegions", DbTemporaryExposureKey.DefaultRegion).Split(separators);
			AppleDeviceCheckKeyId = await GetKeyVaultSecret(GetEnv("AppleDeviceCheckKeyIdSecretId"));
			AppleDeviceCheckTeamId = await GetKeyVaultSecret(GetEnv("AppleDeviceCheckTeamIdSecretId"));
			AppleDeviceCheckP8FileContents = await GetKeyVaultSecret(GetEnv("AppleDeviceCheckP8FileContentsSecretId"));

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

		// TODO: load this from a DB or config
		internal static Task<List<DbSignerInfo>> GetAllSignerInfosAsync()
		{
			var si = new List<DbSignerInfo>
			{
				new DbSignerInfo
				{
					AndroidPackage = "com.xamarin.exposurenotificationsample",
					AppBundleId = "com.xamarin.exposurenotificationsample",
					VerificationKeyId = "ExampleServer_k1",
					VerificationKeyVersion = "1",
				}
			};

			return Task.FromResult(si);
		}

		// TODO: load this from a DB or config
		internal static DbAuthorizedApp GetAuthorizedApp(Verify.DevicePlatform platform) =>
			platform switch
			{
				Verify.DevicePlatform.Android => new DbAuthorizedApp
				{
					PackageName = "com.companyname.ExposureNotification.app",
					Platform = "android",
				},
				Verify.DevicePlatform.iOS => new DbAuthorizedApp
				{
					PackageName = "com.companyname.ExposureNotification.App",
					Platform = "ios",
					DeviceCheckKeyId = AppleDeviceCheckKeyId,
					DeviceCheckTeamId = AppleDeviceCheckTeamId,
					DeviceCheckPrivateKey = AppleDeviceCheckP8FileContents
				},
				_ => throw new ArgumentOutOfRangeException(nameof(platform))
			};

		internal static string SqlServerConnectionString { get; private set; }

		internal static string BlobStorageConnectionString { get; private set; }

		internal static string BlobStorageContainerNamePrefix { get; private set; }

		internal static string[] ExposureKeyRegions { get; private set; }

		internal static bool DeleteKeysFromDbAfterBatching { get; private set; }

		internal static bool DisableDeviceVerification { get; private set; }

		internal static string AppleDeviceCheckKeyId { get; private set; }

		internal static string AppleDeviceCheckTeamId { get; private set; }

		internal static string AppleDeviceCheckP8FileContents { get; private set; }

		static string GetEnv(string name, string nullValue = null)
			=> Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process) ?? nullValue;

		internal static async Task<string> GetKeyVaultSecret(string secretIdentifier)
		{
			// Use a built in token provider which works in azure functions if the service/app identity is granted access to the key vault
			var azureServiceTokenProvider = new AzureServiceTokenProvider();

			// Create a key vault client instance with the token provider
			var keyVault = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

			// Get the secret specified in the app config
			// This is actually am Elliptic Curve certificate (ECDSA) with a P-256 curve
			var keyVaultSecret = await keyVault.GetSecretAsync(secretIdentifier);

			return keyVaultSecret.Value;
		}
	}
}
