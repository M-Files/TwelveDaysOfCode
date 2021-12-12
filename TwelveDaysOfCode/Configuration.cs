using MFiles.VAF.Configuration;
using MFiles.VAF.Configuration.JsonAdaptor;
using MFiles.VAF.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using static TwelveDaysOfCode.VaultApplication;

namespace TwelveDaysOfCode
{
    [DataContract]
    public class Configuration
        : ConfigurationBase
    {

        [DataMember(Order = 1)]
        [JsonConfEditor(Label = "SMTP configuration")]
        public MFiles.VAF.Extensions.Email.VAFSmtpConfiguration SmtpConfiguration { get; set; }
            = new MFiles.VAF.Extensions.Email.VAFSmtpConfiguration();

        [DataMember(Order = 2)]
        [JsonConfEditor(Label = "Shared link generation")]
        public SharedLinkGenerationConfiguration SharedLinkGenerationConfiguration { get; set; }
            = new SharedLinkGenerationConfiguration();

        [DataMember(Order = 3)]
        [JsonConfEditor(Label = "Import gists")]
        public ImportGistConfiguration ImportGistConfiguration { get; set; }
            = new ImportGistConfiguration();
    }
    [DataContract]
    public abstract class ConfigurationBase
    {
        [DataMember(Order = 0)]
        [JsonConfEditor(DefaultValue = false)]
        public bool Enabled { get; set; } = false; 
    }
}