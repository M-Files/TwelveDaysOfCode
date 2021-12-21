using MFiles.VAF.AppTasks;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration;
using MFiles.VAF.Extensions;
using MFiles.VAF.Extensions.Dashboards;
using MFilesAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TwelveDaysOfCode
{
    internal class ImportGistsModule
        : SimpleModuleBase<ImportGistConfiguration>
    {
        public ImportGistsModule()
            : base((c) => c?.ImportGistConfiguration)
        {
            this.Name = "Import gists";
        }

        public const string ImportGistTaskType = "Import";

        /// <summary>
        /// Retrieves gists and schedules them for import.
        /// </summary>
        [TaskProcessor(VaultApplication.TaskQueueID, ImportGistTaskType, TransactionMode = TransactionMode.Full)]
        [ShowOnDashboard("Import gists", ShowRunCommand = true)]
        public void GistImporter(ITaskProcessingJob<TaskDirective> job)
        {
            // Sanity.
            if (false == (this.Configuration?.Enabled ?? false))
            {
                this.Logger.Info("Gist import skipped; disabled in configuration.");
                return;
            }
            if (false == (this.Configuration?.TargetObjectType?.IsResolved ?? false))
            {
                this.Logger.Info("Gist import skipped as target object type could not be resolved.");
            }
            if (false == (this.Configuration?.TargetClass?.IsResolved ?? false))
            {
                this.Logger.Info("Gist import skipped as target class could not be resolved.");
            }
            var targetObjectType = job.Vault.ObjectTypeOperations.GetObjectType(this.Configuration.TargetObjectType);
            var webClient = new System.Net.WebClient();
            webClient.Headers.Add("User-Agent", "Vault application"); // Needed for github!
            var temporaryFiles = new List<string>();
            var urlDataType = MFDataType.MFDatatypeText;
            if (this.Configuration.TargetURLProperty.IsResolved)
            {
                urlDataType = job.Vault.PropertyDefOperations.GetPropertyDef(this.Configuration.TargetURLProperty.ID).DataType;
            }

            try
            {
                // Download the gists and parse.
                var gists = JsonConvert.DeserializeObject<List<Gist>>(webClient.DownloadString("https://api.github.com/gists"));

                // Create a search builder to remove any gists that are now not in the source.
                var gistSearchBuilder = new MFSearchBuilder(job.Vault);
                gistSearchBuilder.ObjType(this.Configuration.TargetObjectType.ID);

                // Iterate over the gists and add them to the queue.
                foreach(var gist in gists)
                {
                    // Does this gist exist?
                    ObjectVersionAndProperties existingGist = null;
                    try
                    {
                        // Use this method as it's faster than a search for failures.
                        var valueListItem = job.Vault.ValueListItemOperations.GetValueListItemByDisplayIDEx
                        (
                            this.Configuration.TargetObjectType.ID,
                            gist.Id
                        );
                        if (valueListItem != null)
                        {
                            var objID = new ObjID();
                            objID.SetIDs(valueListItem.ValueListID, valueListItem.ID);
                            existingGist = job.Vault.ObjectOperations.GetLatestObjectVersionAndProperties(objID, true, true);

                            // If the gist is checked out then leave it.
                            if(existingGist.VersionData.ObjectCheckedOut)
                            {
                                this.Logger.Warn($"Could not remove {existingGist.ObjVer.ToJSON()} because it's checked out to someone.");
                                // Skip this one.
                                continue;
                            }
                        };
                    }
                    catch { existingGist = null; }

                    // Set up the properties.
                    var propertyValues = new MFPropertyValuesBuilder(job.Vault, existingGist?.Properties ?? new PropertyValues());
                    propertyValues.SetClass(this.Configuration.TargetClass.ID);
                    propertyValues.SetTitle(gist.NodeId, this.Configuration.TargetClass.ID);
                    if (this.Configuration.TargetURLProperty.IsResolved)
                        propertyValues.Add(this.Configuration.TargetURLProperty.ID, urlDataType, gist.Url);

                    // Files?
                    var sourceObjectFiles = new SourceObjectFiles();
                    if(targetObjectType.CanHaveFiles && gist.Files.Count > 0)
                    {
                        foreach(var file in gist.Files)
                        {
                            // Download the raw file to a temporary location.
                            var temporaryFile = SysUtils.GetTempFileName(".tmp");
                            webClient.DownloadFile(file.Value.RawUrl, temporaryFile);
                            temporaryFiles.Add(temporaryFile);

                            // Add the raw file to the source object files.
                            var sourceObjectFile = new SourceObjectFile();
                            sourceObjectFile.Title = file.Key.Contains(".") ? file.Key.Substring(0, file.Key.LastIndexOf(".")) : file.Key;
                            sourceObjectFile.Extension = file.Key.Contains(".") ? file.Key.Substring(file.Key.LastIndexOf(".") + 1) : "";
                            sourceObjectFile.SourceFilePath = temporaryFile;
                            sourceObjectFiles.Add(-1, sourceObjectFile);
                        }
                    }

                    // Upsert.
                    int id = -1;
                    if (existingGist != null)
                    {
                        // Update properties.
                        var objVerEx = new ObjVerEx(job.Vault, existingGist);
                        var b = objVerEx.StartRequireCheckedOut(); ;
                        objVerEx.SaveProperties(propertyValues.Values);

                        // If it is an SFD then set it to false (we are altering the files)
                        if(objVerEx.Info.SingleFile)
                            job.Vault.ObjectOperations.SetSingleFileObject(objVerEx.ObjVer, false);
                        
                        // Remove any existing files.
                        foreach(var file in objVerEx.Info.Files.Cast<ObjectFile>())
                        {
                            job.Vault.ObjectFileOperations.RemoveFile(objVerEx.ObjVer, file.FileVer);
                        }

                        // Add any new files.
                        foreach (var file in sourceObjectFiles.Cast<SourceObjectFile>())
                        {
                            job.Vault.ObjectFileOperations.AddFile(objVerEx.ObjVer, file.Title, file.Extension, file.SourceFilePath);
                        }

                        // If it is now an SFD then set the flag.
                        if (sourceObjectFiles.Count == 1
                                && this.Configuration.TargetObjectType.ID == (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument)
                        {
                            job.Vault.ObjectOperations.SetSingleFileObject(objVerEx.ObjVer, true);
                        }

                        // Save all the changes.
                        objVerEx.EndRequireCheckedOut(b);
                        id = objVerEx.ID;
                        this.Logger.Info($"Updated gist with external ID {gist.Id} ({existingGist.ObjVer.ToJSON()})");
                    }
                    else
                    {
                        // Create.
                        existingGist = job.Vault.ObjectOperations.CreateNewObjectEx
                        (
                            this.Configuration.TargetObjectType.ID,
                            propertyValues.Values,
                            sourceObjectFiles,
                            SFD: sourceObjectFiles.Count == 1
                                && this.Configuration.TargetObjectType.ID == (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument
                        );
                        id = existingGist.ObjVer.ID;
                        this.Logger.Info($"Created gist with external ID {gist.Id} ({existingGist.ObjVer.ToJSON()})");

                        // Set the display ID.
                        job.Vault.ObjectOperations.SetExternalID(existingGist.ObjVer.ObjID, gist.Id);
                    }

                    // Ignore this gist.
                    gistSearchBuilder.NotObject(id);
                }

                // Remove any gists that were not in the source.
                foreach (var existingGist in gistSearchBuilder.Find(sort: false, maxResults: 0, searchTimeoutInSeconds: 0).Cast<ObjectVersion>())
                {
                    try
                    {
                        job.Vault.ObjectOperations.DestroyObject(existingGist.ObjVer.ObjID, true, -1);
                        this.Logger.Info($"Destroyed gist {existingGist.ObjVer.ToJSON()}");
                    }
                    catch (Exception e)
                    {
                        this.Logger.Warn(e, $"Could not remove {existingGist.ObjVer.ToJSON()}");
                    }
                }
            }
            catch(Exception e)
            {
                SysUtils.ReportErrorMessageToEventLog("Exception downloading gists", e);
                throw;
            }
            finally
            {
                foreach(var file in temporaryFiles)
                {
                    try
                    {
                        System.IO.File.Delete(file);
                    }
                    catch(Exception e)
                    {
                        this.Logger.Warn(e, $"Could not clean temporary file: {file}.");
                    }
                }
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
    public class ImportGistConfiguration
        : ConfigurationBase
    {
        [DataMember(Order = 2)]
        [JsonConfEditor(Label = "Schedule")]
        [RecurringOperationConfiguration(VaultApplication.TaskQueueID, ImportGistsModule.ImportGistTaskType)]
        public Frequency ImportGistSchedule { get; set; } = TimeSpan.FromHours(1);

        [DataMember(Order = 3)]
        [MFObjType]
        [JsonConfEditor(Label = "Target object type")]
        public MFIdentifier TargetObjectType { get; set; } = "OT.Gist";

        [DataMember(Order = 4)]
        [MFClass]
        [JsonConfEditor(Label = "Target class")]
        public MFIdentifier TargetClass { get; set; } = "CL.Gist";

        [DataMember(Order = 4)]
        [MFPropertyDef(Datatypes = new[] { MFDataType.MFDatatypeText, MFDataType.MFDatatypeMultiLineText })]
        [JsonConfEditor(Label = "URL property")]
        public MFIdentifier TargetURLProperty { get; set; } = "PD.Url";
    }
}
