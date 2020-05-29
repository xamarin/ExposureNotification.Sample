using System;
using System.IO;
using ExposureNotification.Backend.Database;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(ExposureNotification.Backend.Functions.Startup))]

namespace ExposureNotification.Backend.Functions
{
	public class Startup : FunctionsStartup
	{
		const string inMemoryDatabaseName = "ChangeInProduction";
		readonly static char[] separators = new[] { ';', ',', ':' };

		public override void Configure(IFunctionsHostBuilder builder)
		{
			var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder
					.AddConsole()
					.AddAzureWebAppDiagnostics();
			});

			var logger = loggerFactory.CreateLogger("Startup");
			logger.LogInformation("Starting setup...");

			builder.Services.AddLogging();

			// load the basics
			var configBuilder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables();
			var config = configBuilder.Build();

			var vault = config["EN-KeyVaultName"];

			logger.LogInformation("Loaded basic configuration.");

			if (config.GetValue<bool>("EN-SkipKeyVault") == false)
			{
				logger.LogInformation("Adding KeyVault configurations...");

				try
				{
					// use that to load the keyvault
					var azureServiceTokenProvider = new AzureServiceTokenProvider();
					var keyVaultClient = new KeyVaultClient(
						new KeyVaultClient.AuthenticationCallback(
							azureServiceTokenProvider.KeyVaultTokenCallback));
					configBuilder.AddAzureKeyVault(
						$"https://{config["EN-KeyVaultName"]}.vault.azure.net/",
						keyVaultClient,
						new DefaultKeyVaultSecretManager());
					config = configBuilder.Build();
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error adding KeyVault configurations.");
				}
			}

			logger.LogInformation("Reading configuation...");

			var conn = config["EN-DbConnectionString"];

			// now set things up for reals
			builder.Services.Configure<Settings>(settings =>
			{
				settings.BlobStorageConnectionString = config["EN-BlobStorageConnectionString"];
				settings.BlobStorageContainerNamePrefix = config["EN-BlobStorageContainerNamePrefix"] ?? "region-";
				settings.DbConnectionString = conn;
				settings.DeleteKeysFromDbAfterBatching = config.GetValue<bool>("EN-DeleteKeysFromDbAfterBatching");
				settings.DisableDeviceVerification = config.GetValue<bool>("EN-DisableDeviceVerification");
				settings.SigningKeyBase64String = config["EN-SigningKey"];
				settings.VerificationKeyId = config["EN-VerificationKey-Id"];
				settings.VerificationKeyVersion = config["EN-VerificationKey-Version"];
				settings.SupportedRegions = config["EN-SupportedRegions"]?.ToUpperInvariant()?.Split(separators) ?? new string[0];
				settings.AndroidPackageName = config["EN-Android-PackageName"];
				settings.iOSBundleId = config["EN-iOS-BundleId"];
				settings.iOSDeviceCheckTeamId = config["EN-iOS-DeviceCheck-TeamId"];
				settings.iOSDeviceCheckKeyId = config["EN-iOS-DeviceCheck-KeyId"];
				settings.iOSDeviceCheckPrivateKey = config["EN-iOS-DeviceCheck-PrivateKey"];
			});

			logger.LogInformation("Setting up database...");

			// set up the database
			builder.Services.AddDbContext<ExposureNotificationContext>(builder =>
			{
				if (string.IsNullOrEmpty(conn))
					builder.UseInMemoryDatabase(inMemoryDatabaseName)
						.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
				else
					builder.UseSqlServer(conn);
			});

			// set up database "repository"
			builder.Services.AddTransient<ExposureNotificationStorage>();

			logger.LogInformation("All done.");
		}
	}
}
