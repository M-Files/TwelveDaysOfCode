using MFiles.VAF.AppTasks;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration;
using MFilesAPI.Extensions;
using MFiles.VAF.Extensions;
using MFiles.VAF.Extensions.Dashboards;
using MFilesAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TwelveDaysOfCode
{
    public partial class VaultApplication
    {
        // Finance uploader set in constructor.
        internal IFinanceUploader FinanceUploader { get; set; }

        /// <summary>
        /// The task type used for tasks to upload invoices to the finance system.
        /// </summary>
        public const string UploadToFinanceTaskType = "UploadToFinanceTaskType";

        /// <summary>
        /// Handles the <see cref="MFEventHandlerType.MFEventHandlerBeforeCheckInChangesFinalize" /> event.
        /// </summary>
        /// <param name="env">The vault/object environment.</param>
        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeCheckInChangesFinalize)]
        public void UploadInvoiceToFinance(EventHandlerEnvironment env)
        {
            // Sanity.
            if(false == (this.Configuration?.UploadToFinanceConfiguration?.Enabled ?? false))
            {
                this.Logger.Info("Upload to finance is disabled in configuration; skipping checks.");
                return;
            }
            if(null == this.FinanceUploader)
            {
                this.Logger.Fatal("Finance uploader not configured");
                return;
            }

            // Does the object match the trigger?
            if(false == (this.Configuration?.UploadToFinanceConfiguration?.Trigger?.Condition?.IsMatch(env.ObjVerEx, true, env.CurrentUserID) ?? false))
            {
                this.Logger.Info($"Object {env.ObjVer.ToJSON()} does not match trigger to upload to finance.");
                return;
            }

            // Add the task to the queue.
            this.TaskManager.AddTask(env.Vault, TaskQueueID, UploadToFinanceTaskType, new ObjIDTaskDirective(env.ObjVerEx.ObjID, env.ObjVerEx.Title));
        }

        /// <summary>
        /// Processes items of type <see cref="UploadToFinanceTaskType" /> on queue <see cref="TaskQueueID" />
        /// </summary>
        // Using full transaction mode which may not be best for situations with slower "save" methods, like uploads.
        [TaskProcessor(TaskQueueID, UploadToFinanceTaskType, TransactionMode = TransactionMode.Full)]
        [ShowOnDashboard("Upload invoices to finance")]
        public void UploadToFinanceTaskProcessor(ITaskProcessingJob<ObjIDTaskDirective> job)
        {
            // Sanity.
            if (false == (this.Configuration?.UploadToFinanceConfiguration?.Enabled ?? false))
            {
                this.Logger.Info("Upload to finance is disabled in configuration; re-queuing task");
                throw new AppTaskException(TaskProcessingJobResult.Requeue);
            }
            if (null == this.FinanceUploader)
            {
                this.Logger.Fatal("Finance uploader not configured");
                throw new AppTaskException(TaskProcessingJobResult.Fatal);
            }

            // Attempt to get the latest object version.
            ObjVerEx objVerEx = null;
            {
                ObjID objId;
                if (false == job.Directive.TryGetObjID(out objId))
                {
                    this.Logger.Fatal("Could not load object ID from directive.");
                    throw new AppTaskException(TaskProcessingJobResult.Fatal);
                }

                try
                {
                    objVerEx = new ObjVerEx(job.Vault, job.Vault.ObjectOperations.GetLatestObjectVersionAndProperties(objId, true, true));
                }
                catch
                {
                    // If we cannot get the object from the vault then we have a big problem.
                    this.Logger.Fatal($"Could not load object {objId.ToJSON()} from the vault; fatal.");
                    throw new AppTaskException(TaskProcessingJobResult.Fatal);
                }
            }

            // If the object no longer matches the trigger then stop.
            if (false == (this.Configuration?.UploadToFinanceConfiguration?.Trigger?.Condition?.IsMatch(objVerEx, true) ?? false))
            {
                this.Logger.Info($"Skipping {objVerEx.ObjVer.ToJSON()} as it no longer matches the trigger conditions.");
                return;
            }

            // If we cannot check out the object then we need to wait.
            if(objVerEx.Info.ObjectCheckedOut)
            {
                this.Logger.Info($"Cannot check out {objVerEx.ObjVer.ToJSON()}; attempting to re-queue");
                throw new AppTaskException(TaskProcessingJobResult.Requeue);
            }

            // Check it out.
            bool b;
            try
            {
                b = objVerEx.StartRequireCheckedOut();
            }
            catch
            {
                this.Logger.Info($"Exception checking out {objVerEx.ObjVer.ToJSON()}; attempting to re-queue");
                throw new AppTaskException(TaskProcessingJobResult.Requeue);
            }

            // Output the item.
            try
            {
                this.FinanceUploader.Upload(objVerEx);
            }
            catch(Exception e)
            {
                // In reality not all exceptions are probably fatal, but this will do for some sample code.
                // If we were uploading to a remote system, for example, then transient network errors would
                // need to be handled in a less "final" way.
                this.Logger.Fatal(e, $"Exception uploading data.");
                throw new AppTaskException(TaskProcessingJobResult.Fatal);
            }

            // Apply any properties.
            foreach(var value in this.Configuration?.UploadToFinanceConfiguration?.UpdatedObjectValues ?? new List<UpdatedObjectValues>())
            {
                // Sanity.
                if(0 > value.Property.ID)
                {
                    this.Logger.Fatal($"Updated object value configuration invalid; property ID {value.Property.ID} invalid.");
                    throw new AppTaskException(TaskProcessingJobResult.Fatal);
                }

                // Set the value.
                switch (value.Value.Mode)
                {
                    case TypedValueSettingMode.SetToNULL:
                        {
                            // Set the property to null.
                            objVerEx.SetProperty
                            (
                                value.Property.ID,
                                job.Vault.PropertyDefOperations.GetPropertyDef(value.Property.ID).DataType,
                                null
                            );
                            break;
                        }
                    case TypedValueSettingMode.Static:
                        {
                            // Set the value as configured.
                            var staticValue = value.Value.TypedValue.ToApiObject(job.Vault);
                            objVerEx.SetProperty
                            (
                                value.Property.ID,
                                staticValue.DataType,
                                staticValue.Value
                            );
                            break;
                        }
                    default:
                        {
                            this.Logger.Fatal($"Updated object value configuration invalid; mode {value.Value.Mode} not supported");
                            throw new AppTaskException(TaskProcessingJobResult.Fatal);
                        }
                }
            }
            objVerEx.SaveProperties();

            // Check it in.
            objVerEx.EndRequireCheckedOut(b);
        }


    }

    [DataContract]
    public class UploadToFinanceConfiguration
        : ConfigurationBase
    {
        [DataMember(Order = 1)]
        [JsonConfEditor(DefaultValue = @"C:\Temp\invoice-output\")]
        public string OutputPath { get; set; } = @"C:\Temp\invoice-output\";

        // We will use the trigger class from a previous task here too.
        [DataMember(Order = 2)]
        [JsonConfEditor
        (
            HelpText = "An object must match all of the conditions the trigger to cause the invoice to be output."
        )]
        public Trigger Trigger { get; set; }

        [DataMember(Order = 3)]
        [JsonConfEditor
        (
            Label = "Updated object values",
            HelpText = "Values that will be set on the object after it has been exported; use this to move it to a different workflow state, for example."
        )]
        public List<UpdatedObjectValues> UpdatedObjectValues { get; set; }
            = new List<UpdatedObjectValues>();
    }

    [DataContract]
    public class UpdatedObjectValues
    {
        [MFPropertyDef]
        [DataMember]
        public MFIdentifier Property { get; set; }

        [DataMember]
        [ValueSetter
        (
            AllowedModes = new[] { TypedValueSettingMode.Static, TypedValueSettingMode.SetToNULL },
            PropertyDefReferencePath = ".parent._children{.key == 'Property' }"
        )]
        public TypedValueSetter Value { get; set; }
    }

    public interface IFinanceUploader
    {
        void Upload(ObjVerEx objectToUpload);
    }
    public class SaveToDiskUploader
        : IFinanceUploader
    {
        protected Func<string> OutputPathGenerator { get; }
        public SaveToDiskUploader(Func<string> outputPathGenerator)
        {
            this.OutputPathGenerator = outputPathGenerator
                ?? throw new ArgumentNullException(nameof(outputPathGenerator));
        }
        void IFinanceUploader.Upload(ObjVerEx objectToUpload)
        {
            // Sanity.
            if (null == objectToUpload)
                throw new ArgumentNullException(nameof(objectToUpload));

            // Set up where we will output to.
            var path = this.OutputPathGenerator();
            if (false == System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);
            var guid = Guid.NewGuid();

            try
            {

                // Generate the XML file.
                this.GenerateXmlFile(path, guid, objectToUpload);

                // Generate the PDF file.
                this.GenerateSupportingFiles(path, guid, objectToUpload);

            }
            catch
            {
                // Remove any temporary files.
                foreach (var file in System.IO.Directory.GetFiles(path, $"{guid}.*"))
                {
                    try
                    {
                        System.IO.File.Delete(file);
                    }
                    catch { }
                }

                throw;
            }
        }

        /// <summary>
        /// Creates an XML file containing data for <paramref name="outputPath"/>.
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="guid"></param>
        /// <param name="objectToUpload"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void GenerateXmlFile(string outputPath, Guid guid, ObjVerEx objectToUpload)
        {
            // Sanity.
            if (null == objectToUpload)
                throw new ArgumentNullException(nameof(objectToUpload));

            // Create the file to output to.
            var outputFile = new System.IO.FileInfo(System.IO.Path.Combine(outputPath, $"{guid}.xml"));

            // Build up the XML file.
            using (var stream = outputFile.Open(System.IO.FileMode.CreateNew))
            {
                using (var xmlWriter = new System.Xml.XmlTextWriter(stream, Encoding.UTF8))
                {
                    xmlWriter.WriteStartDocument();

                    // We would write out lots of content here, but let's just do the basics for now.
                    xmlWriter.WriteStartElement("invoice");
                    xmlWriter.WriteAttributeString("title", objectToUpload.Title);
                    xmlWriter.WriteAttributeString("date", DateTime.UtcNow.ToString("u"));
                    xmlWriter.WriteEndElement();


                    xmlWriter.WriteEndDocument();

                    xmlWriter.Flush();
                }
            }
        }

        /// <summary>
        /// Writes any files attached to <paramref name="outputPath"/> to disk.
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="guid"></param>
        /// <param name="objectToUpload"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void GenerateSupportingFiles(string outputPath, Guid guid, ObjVerEx objectToUpload)
        {
            // Sanity.
            if (null == objectToUpload)
                throw new ArgumentNullException(nameof(objectToUpload));

            // Iterate over any supporting files and output them.
            // HACK: The files in the current version may not be committed, but they are in the previous one.
            foreach(var file in objectToUpload.PreviousVersion.Info.Files.Cast<ObjectFile>())
            {
                // Where should it be downloaded to?
                var fileName = System.IO.Path.Combine(outputPath, $"{guid}.{file.GetNameForFileSystem()}");

                // Let's use the extensions library to short-circuit some of this.
                file.Download(objectToUpload.Vault, fileName);
            }
        }
    }
}
