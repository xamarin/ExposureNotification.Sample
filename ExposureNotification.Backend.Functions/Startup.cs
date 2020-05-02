using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

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
		}
	}
}
