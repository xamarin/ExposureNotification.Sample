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
using Microsoft.Extensions.Configuration;
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
			BlobStorageConnectionString = await GetKeyVaultSecret(GetEnv("EN_BlobStorageConnectionString_VaultSecretId"));
			BlobStorageContainerNamePrefix = GetEnv("EN_BlobStorageContainerNamePrefix", string.Empty);
			DbConnectionString = await GetKeyVaultSecret(GetEnv("EN_DbConnectionString_VaultSecretId"));
			DeleteKeysFromDbAfterBatching = GetEnv("EN_DeleteKeysFromDbAfterBatching", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
			DisableDeviceVerification = GetEnv("EN_DisableDeviceVerification", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
			SupportedRegions = GetEnv("EN_SupportedRegions").Split(separators);
			SigningKeyBase64String = await GetKeyVaultSecret(GetEnv("EN_SigningKeyBase64String_VaultSecretId"));

			builder.Services.AddTransient<ISigner, Signer>();

			Database = new ExposureNotificationStorage(
				builder =>
				{
					if (string.IsNullOrEmpty(DbConnectionString))
						builder.UseInMemoryDatabase(inMemoryDatabaseName)
							.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
					else
						builder.UseSqlServer(DbConnectionString);
				},
				initialize =>
				{
					initialize.Database.EnsureCreated();
				});
		}

		// TODO: load this from a DB or config
		internal static Task<List<DbSignerInfo>> GetAllSignerInfosAsync()
			=> Task.FromResult(new List<DbSignerInfo> {
				new DbSignerInfo
				{
					AndroidPackage = "com.xamarin.exposurenotification.sampleapp",
					AppBundleId = "com.xamarin.exposurenotification.sampleapp",
					VerificationKeyId = "ExampleServer_k1",
					VerificationKeyVersion = "1",
					SigningKeyBase64String = SigningKeyBase64String
				}
			});

		// TODO: load this from a DB or config
		internal static DbAuthorizedApp GetAuthorizedApp(Verify.DevicePlatform platform) =>
			platform switch
			{
				Verify.DevicePlatform.Android => new DbAuthorizedApp
				{
					PackageName = "com.xamarin.exposurenotification.sampleapp",
					Platform = "android",
				},
				Verify.DevicePlatform.iOS => new DbAuthorizedApp
				{
					PackageName = "com.xamarin.exposurenotification.sampleapp",
					Platform = "ios",
					DeviceCheckKeyId = "YOURKEYID",
					DeviceCheckTeamId = "YOURTEAMID",
					DeviceCheckPrivateKey = "CONTENTS-OF-P8-FILE-WITH-NO-LINE-BREAKS"
				},
				_ => throw new ArgumentOutOfRangeException(nameof(platform))
			};

		internal static string DbConnectionString { get; private set; }

		internal static string BlobStorageConnectionString { get; private set; }

		internal static string BlobStorageContainerNamePrefix { get; private set; }

		internal static string[] SupportedRegions { get; private set; }

		internal static bool DeleteKeysFromDbAfterBatching { get; private set; }

		internal static bool DisableDeviceVerification { get; private set; }

		internal static string SigningKeyBase64String { get; private set; }

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
