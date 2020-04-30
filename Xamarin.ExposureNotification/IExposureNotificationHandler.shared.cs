using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.ExposureNotifications
{
	public interface IExposureNotificationHandler
	{
		Configuration Configuration { get; }

		// App should send keys to the backend server
		Task RequestDiagnosisKeys();

		// Might be exposed, check and alert user if necessary
		Task ExposureStateUpdated();
	}
}
