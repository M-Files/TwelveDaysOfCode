using MFiles.Extensibility.ExternalObjectTypes;
using MFiles.Extensibility.Framework.ExternalObjectTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImportGists
{
    /// <summary>
    /// An implementation of IExternalObjectTypeConnection.
    /// Parts for reading data.
    /// </summary>
    public partial class DataSourceConnection
    {
        /// <summary>
        /// The binding flags to load properties and fields.
        /// </summary>
        private static System.Reflection.BindingFlags DefaultBindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

        /// <summary>
        /// Gets the available columns from the currently open selection.
        /// </summary>
        /// <returns>Collection of available columns.</returns>
        public override List<ColumnDefinition> GetAvailableColumns()
        {
            // HACK: This is probably not a great idea; if the properties/fields changed in the future then this would break.
            var columns = new List<ColumnDefinition>();

            // Add all the properties.
            foreach (var p in typeof(Gist).GetProperties(DataSourceConnection.DefaultBindingFlags))
                if (typeMappingByType.ContainsKey(p.PropertyType))
                    columns.Add(new ColumnDefinition()
                    {
                        Name = p.Name,
                        Ordinal = columns.Count,
                        Type = typeMappingByType[p.PropertyType]
                    });

            // Add all the fields.
            foreach (var f in typeof(Gist).GetFields(DataSourceConnection.DefaultBindingFlags))
                if (typeMappingByType.ContainsKey(f.FieldType))
                    columns.Add(new ColumnDefinition()
                    {
                        Name = f.Name,
                        Ordinal = columns.Count,
                        Type = typeMappingByType[f.FieldType]
                    });

            return columns;
        }

        /// <summary>
        /// Gets the items as specified by the select statement.
        /// </summary>
        /// <returns>Items from the data source</returns>
        public override IEnumerable<DataItem> GetItems()
        {
            var webClient = new System.Net.WebClient();
            webClient.Headers.Add("User-Agent", this.Config.CustomConfiguration.UserAgent); // Needed for github!
            var gists = JsonConvert.DeserializeObject<List<Gist>>(webClient.DownloadString("https://api.github.com/gists"));

            // Get the data from the gists.
            return gists.Select
            (
                g =>
                {
                    // The dictionary holds all the data.
                    // The key is the index and needs to match with the indexes returned by GetAvailableColumns.
                    var data = new Dictionary<int, object>();

                    // Add data from all the properties.
                    foreach (var p in typeof(Gist).GetProperties(DataSourceConnection.DefaultBindingFlags))
                        if (typeMappingByType.ContainsKey(p.PropertyType))
                            data.Add(data.Count, p.GetValue(g));

                    // Add data from all the fields.
                    foreach (var f in typeof(Gist).GetFields(DataSourceConnection.DefaultBindingFlags))
                        if (typeMappingByType.ContainsKey(f.FieldType))
                            data.Add(data.Count, f.GetValue(g));

                    return new DataItemSimple(data);
                }
            );
        }
    }



    public class File
    {
        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("raw_url")]
        public string RawUrl { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }
    }


    public class Owner
    {
        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("node_id")]
        public string NodeId { get; set; }

        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonProperty("gravatar_id")]
        public string GravatarId { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }

        [JsonProperty("followers_url")]
        public string FollowersUrl { get; set; }

        [JsonProperty("following_url")]
        public string FollowingUrl { get; set; }

        [JsonProperty("gists_url")]
        public string GistsUrl { get; set; }

        [JsonProperty("starred_url")]
        public string StarredUrl { get; set; }

        [JsonProperty("subscriptions_url")]
        public string SubscriptionsUrl { get; set; }

        [JsonProperty("organizations_url")]
        public string OrganizationsUrl { get; set; }

        [JsonProperty("repos_url")]
        public string ReposUrl { get; set; }

        [JsonProperty("events_url")]
        public string EventsUrl { get; set; }

        [JsonProperty("received_events_url")]
        public string ReceivedEventsUrl { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("site_admin")]
        public bool SiteAdmin { get; set; }
    }

    public class Gist
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("forks_url")]
        public string ForksUrl { get; set; }

        [JsonProperty("commits_url")]
        public string CommitsUrl { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("node_id")]
        public string NodeId { get; set; }

        [JsonProperty("git_pull_url")]
        public string GitPullUrl { get; set; }

        [JsonProperty("git_push_url")]
        public string GitPushUrl { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }

        [JsonProperty("files")]
        public Dictionary<string, File> Files { get; set; }

        [JsonProperty("public")]
        public bool Public { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("comments")]
        public int Comments { get; set; }

        [JsonProperty("user")]
        public object User { get; set; }

        [JsonProperty("comments_url")]
        public string CommentsUrl { get; set; }

        [JsonProperty("owner")]
        public Owner Owner { get; set; }

        [JsonProperty("truncated")]
        public bool Truncated { get; set; }
    }


}
