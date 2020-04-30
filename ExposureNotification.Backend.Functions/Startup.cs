using ExposureNotification.Core;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

[assembly: FunctionsStartup(typeof(ExposureNotification.Backend.Functions.Startup))]

namespace ExposureNotification.Backend.Functions
{
	public class Startup : FunctionsStartup
	{
		public override void Configure(IFunctionsHostBuilder builder)
		{
			var executionContextOptions = builder.Services.BuildServiceProvider().GetService<IOptions<ExecutionContextOptions>>().Value;
			var currentDirectory = executionContextOptions.AppDirectory;

			var pfx = Environment.GetEnvironmentVariable("CERT_PFX_FILE");

			// Load our certificate's public key
			var certificate = new X509Certificate2(Path.Combine(currentDirectory, pfx), string.Empty, X509KeyStorageFlags.MachineKeySet);
			
			var encoder = new DefaultTemporaryExposureKeyEncoder(certificate);

			var storage = new ExposureNotificationStorage(encoder,
				builder => builder.UseInMemoryDatabase("ChangeInProduction"),
				initialize => initialize.Database.EnsureCreated());

			builder.Services.AddSingleton<IExposureNotificationStorage>(storage);
		}
	}
}
