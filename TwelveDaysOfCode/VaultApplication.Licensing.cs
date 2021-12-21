using MFiles.VAF;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration;
using MFilesAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwelveDaysOfCode
{
    public partial class VaultApplication
    {
		/// <summary>
		/// Configures <see cref="VaultApplicationBase.License"/>.
		/// </summary>
        protected virtual void ConfigureLicenseDecoder()
        {
			try
			{
				// Set up the license decoder.
				var licenseDecoder =
					new LicenseDecoder(LicenseDecoder.EncMode.TwoKey);

				// This is from the key file (MainKey.PublicXml).
				licenseDecoder.MainKey = "<RSAKeyValue><Modulus>u4LjTZZz7moIOAmMjknIGrdI0S6AVNS2rl34SgIyPV+WwlOBlxc1M+Jo5/6ArLru4ux9vNc3ZizegWwz7aXRdpzA5tzMsyIsUf5gRdfap57TLByELiGWic1a1VgDoSMthI+EL3OLs8KtHv4byVc93X9PSw0trxgvhYsV51NGh3k=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

				// This is from the key file (SecondKey.SecretXml)
				licenseDecoder.AltKey = "<RSAKeyValue><Modulus>0Rov6L7j0W2mpQfedHZcBUkq0q3wRq0apeImrX3It1kKxlGEnQ/0fmYwQZoLc9HgdhGKV/PQDQLrc2U/Qd1nK8D2Ui7F1Fm2cFdEl/VK9GTPZFzn/bXuECTynMwE6BBoDQwDa67Q+PoZjSTkFd5jo+8IZSCrvXZD5Le5bcN0eDE=</Modulus><Exponent>AQAB</Exponent><P>19p14INIchsQMkUog7C4pOmD0/+5daRrDZqYmDctelmtaL/dxgRZT5F2/nlCBstYM04hAsb50rVCs4dMAZlL7w==</P><Q>9/5I2LafzxYviUbe4zozMQ0uRgtyvH8nE1O/99Pr1XihMbQ5yH2/vMMhshnejz/euPMlwAnQk7yCEIuUF4Ld3w==</Q><DP>MkbnR/ksSa+2EQ98xVfHWlot45Zf+1/ls5B71JCdni7/LjPqkzH2H4txXQqfb3ezvpeHJt9z1zlzJN/xuzmarw==</DP><DQ>uYPt9sBXOFF+ahEsN4uYM/+KODfkMwJjtt+V4c0UxPKik04hU8xOHOVUValohnzfHjg2azxsXbhNDBd+R0BMvQ==</DQ><InverseQ>y2MeUFV3R/VbKLbxpP78Kp80ViGGIv7yeNXNb3dCwPR5+NmIpAda/36f7pGo7OgkGo1tVqyuAneuGQWKx8PFgA==</InverseQ><D>oZqVkFGHjKIr+rucJ3IaKFOl7vFTE6xRPgcMUWU3LMx6UU9LKH/eO5oKjYjadQatbVKdEuBx2Lx679I+E09jnbylWQOtwigpN6oB+Gawvax4uTyHoL00UDQWgA3CQRICKQ9m8GOP0ezXHP6QWyEwhf9xmHx87QE7KkqKXE5ctSk=</D></RSAKeyValue>";
				this.License =
					 new LicenseManagerBase<LicenseContentBase>(licenseDecoder);

			}
			catch (Exception ex)
			{
				this.Logger.Warn(ex);
				SysUtils.ReportErrorToEventLog(this.EventSourceIdentifier, ex.Message);
			}
		}

		/// <inheritdoc />
        public override void SetApplicationLicense(string license)
        {
			// Use the base implementation (to load the licence).
            base.SetApplicationLicense(license);

			// Evaluate the license validity.
			this.License.Evaluate(this.PermanentVault, false);

			// Output the license status.
			switch (this.License.LicenseStatus)
			{
				case MFApplicationLicenseStatus.MFApplicationLicenseStatusTrial:
					{
						this.Logger.Warn("Application is running in a trial mode.");
						SysUtils.ReportToEventLog
						(
							"Application is running in a trial mode.",
							EventLogEntryType.Warning
						);
						break;
					}
				case MFApplicationLicenseStatus.MFApplicationLicenseStatusValid:
					{
						this.Logger.Info("Application is licensed.");
						SysUtils.ReportInfoToEventLog("Application is licensed.");
						break;
					}
				default:
					{
						this.Logger.Fatal($"Application is in an unexpected state: {this.License.LicenseStatus}.");
						SysUtils.ReportToEventLog
						(
							$"Application is in an unexpected state: {this.License.LicenseStatus}.",
							EventLogEntryType.Error
						);
						break;
					}
			}
		}
	}
}
