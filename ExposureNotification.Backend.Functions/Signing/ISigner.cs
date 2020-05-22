using System.Threading.Tasks;
using ExposureNotification.Backend.Database;

namespace ExposureNotification.Backend.Signing
{
	public interface ISigner
	{
		Task<byte[]> GenerateSignatureAsync(byte[] contents, DbSignerInfo signerInfo);
	}
}
