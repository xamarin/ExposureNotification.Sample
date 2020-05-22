using System;
using System.Collections;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

namespace ExposureNotification.Backend.DeviceVerification
{
	public static class AndroidVerify
	{
		public static AttestationStatement VerifyPayload(string signedAttestationStatement)
		{
			// First parse the token and get the embedded keys.
			JwtSecurityToken token;
			try
			{
				token = new JwtSecurityToken(signedAttestationStatement);
			}
			catch (ArgumentException)
			{
				// The token is not in a valid JWS format.
				return null;
			}

			// We just want to validate the authenticity of the certificate.
			var validationParameters = new TokenValidationParameters
			{
				ValidateIssuer = false,
				ValidateAudience = false,
				ValidateLifetime = false,
				ValidateIssuerSigningKey = true,
				IssuerSigningKeys = GetEmbeddedKeys(token)
			};

			// Perform the validation
			var tokenHandler = new JwtSecurityTokenHandler();
			SecurityToken validatedToken;
			try
			{
				tokenHandler.ValidateToken(signedAttestationStatement, validationParameters, out validatedToken);
			}
			catch (ArgumentException)
			{
				// Signature validation failed.
				return null;
			}

			// Verify the hostname
			if (!(validatedToken.SigningKey is X509SecurityKey))
				return null;

			if (GetHostName(validatedToken.SigningKey as X509SecurityKey) != "attest.android.com")
				return null;

			// Parse and use the data JSON.
			var claimsDictionary = token.Claims.ToDictionary(x => x.Type, x => x.Value);
			return new AttestationStatement(claimsDictionary);
		}

		static string GetHostName(X509SecurityKey securityKey)
		{
			try
			{
#if DEBUG
				using var chain = new X509Chain();
				var chainBuilt = chain.Build(securityKey.Certificate);
				if (!chainBuilt)
				{
					var s = string.Empty;
					foreach (var chainStatus in chain.ChainStatus)
					{
						s += $"Chain error: {chainStatus.Status} {chainStatus.StatusInformation}\n";
					}
				}
#endif

				if (!securityKey.Certificate.Verify())
					return null;

				return securityKey.Certificate.GetNameInfo(X509NameType.DnsName, false);
			}
			catch (CryptographicException)
			{
				return null;
			}
		}

		static X509SecurityKey[] GetEmbeddedKeys(JwtSecurityToken token) =>
			(token.Header["x5c"] as IEnumerable)
			.Cast<object>()
			.Select(x => x.ToString())
			.Select(x => new X509SecurityKey(new X509Certificate2(Convert.FromBase64String(x))))
			.ToArray();
	}
}
