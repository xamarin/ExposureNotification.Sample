using System.Threading.Tasks;
using Shared;

namespace Mobile.Services
{
	public interface IDeviceVerifier
	{
		Task<string> VerifyAsync(SelfDiagnosisSubmission submission);
	}
}
