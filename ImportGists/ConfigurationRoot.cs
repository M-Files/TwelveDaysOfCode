using MFiles.VAF.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace ImportGists
{
    /// <summary>
    /// Configuration.
    /// </summary>
    [DataContract]
    [JsonConfEditor]
    public class ConfigurationRoot
    {

        [DataMember]
        [JsonConfEditor
        (
            Label = "Remote Address",
            DefaultValue = "https://api.github.com/gists"
        )]
        public string RemoteAddress { get; set; } = "https://api.github.com/gists";

        [DataMember]
        [JsonConfEditor
        (
            Label = "User Agent",
            DefaultValue = "External Object Type Data Source"
        )]
        public string UserAgent { get; set; } = "External Object Type Data Source";


    }
}
