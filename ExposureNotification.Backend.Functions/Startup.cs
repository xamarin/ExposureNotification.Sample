using System.IO;
using ExposureNotification.Backend.Database;
using ExposureNotification.Backend.Signing;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(ExposureNotification.Backend.Functions.Startup))]

namespace ExposureNotification.Backend.Functions
{
	public class Startup : FunctionsStartup
	{
		const string inMemoryDatabaseName = "ChangeInProduction";
		readonly static char[] separators = new[] { ';', ',', ':' };

		public override void Configure(IFunctionsHostBuilder builder)
		{
			// load the basics
			var configBuilder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables();
			var config = configBuilder.Build();

			if (config.GetValue<bool>("EN:SkipKeyVault") == false)
			{
				// use that to load the keyvault
				var azureServiceTokenProvider = new AzureServiceTokenProvider();
				var keyVaultClient = new KeyVaultClient(
					new KeyVaultClient.AuthenticationCallback(
						azureServiceTokenProvider.KeyVaultTokenCallback));
				configBuilder.AddAzureKeyVault(
					$"https://{config["EN:KeyVaultName"]}.vault.azure.net/",
					keyVaultClient,
					new DefaultKeyVaultSecretManager());
				config = configBuilder.Build();
			}

			var enConfig = config.GetSection("EN");

			var conn = enConfig["DbConnectionString"];

			// now set things up for reals
			builder.Services.Configure<Settings>(settings =>
			{
				settings.BlobStorageConnectionString = enConfig["BlobStorageConnectionString"];
				settings.BlobStorageContainerNamePrefix = enConfig["BlobStorageContainerNamePrefix"];
				settings.DbConnectionString = conn;
				settings.DeleteKeysFromDbAfterBatching = enConfig.GetValue<bool>("DeleteKeysFromDbAfterBatching");
				settings.DisableDeviceVerification = enConfig.GetValue<bool>("DisableDeviceVerification");
				settings.SigningKeyBase64String = enConfig["SigningKey"];
				settings.SupportedRegions = enConfig["SupportedRegions"].Split(separators);
			});

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
			builder.Services.AddTransient<ISigner, Signer>();
		}
	}
}
