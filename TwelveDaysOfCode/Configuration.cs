using MFiles.VAF.Configuration;
using MFiles.VAF.Configuration.JsonAdaptor;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TwelveDaysOfCode
{
    [DataContract]
    public class Configuration
        : ConfigurationBase
    {
        [DataMember(Order = 1)]
        [JsonConfEditor(Label = "Shared Link Generation")]
        public SharedLinkGenerationConfiguration SharedLinkGenerationConfiguration { get; set; }
            = new SharedLinkGenerationConfiguration();
    }
    [DataContract]
    public abstract class ConfigurationBase
    {
        [DataMember(Order = 0)]
        [JsonConfEditor(DefaultValue = false)]
        public bool Enabled { get; set; } = false; 
    }
}