using MFiles.VAF.AppTasks;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration;
using MFiles.VAF.Configuration.JsonAdaptor;
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
    internal class MigrateContractsModule
        : SimpleModuleBase<MigrateContractsConfiguration>
    {
        public MigrateContractsModule()
            : base((c) => c?.MigrateContractsConfiguration)
        {
            this.Name = "Migrate contracts to successor";
        }

        /// <summary>
        /// The task type for migrating all contracts from one person to another.
        /// </summary>
        public const string MigrateContractsToSuccessorTaskType = "MigrateContractsToSuccessorTaskType";

        /// <summary>
        /// Processes items of type <see cref="MigrateContractsToSuccessorTaskType" /> on queue <see cref="TaskQueueID" />
        /// </summary>
        [TaskProcessor(VaultApplication.TaskQueueID, MigrateContractsToSuccessorTaskType, TransactionMode = TransactionMode.Unsafe)]
        [ShowOnDashboard("Migrate contracts to successor")]
        public void MigrateContractsToSuccessor(ITaskProcessingJob<MigrateContractsToSuccessorTaskDirective> job)
        {
            // Sanity.
            if (false == (this.Configuration?.Enabled ?? false))
            {
                this.Logger.Info("Migration of contracts to successor is disabled in configuration; re-queuing task");
                throw new AppTaskException(TaskProcessingJobResult.Requeue);
            }
            if (false == (this.Configuration.ContractOwnerProperty?.IsResolved ?? false))
            {
                this.Logger.Fatal($"Contract owner property is not configured.");
                throw new AppTaskException(TaskProcessingJobResult.Requeue);
            }

            // This has transaction mode = unsafe, so it does not have a timeout.
            // If it is stopped midway through (e.g. the vault goes offline) then it will
            // be picked up again and executed.  This means that as long as we build the
            // code in such a way that it supports resuming, we're good to go.

            // Load the person who is leaving.
            ObjVerEx personLeaving = null;
            {
                if (false == job.Directive.PersonLeaving.TryGetObjID(out var personLeavingObjID))
                {
                    this.Logger.Fatal($"Could not load person leaving from directive:\r\n:{Newtonsoft.Json.JsonConvert.SerializeObject(job.Directive)}");
                    throw new AppTaskException(TaskProcessingJobResult.Fatal);
                }
                try
                {
                    personLeaving = new ObjVerEx(job.Vault, job.Vault.ObjectOperations.GetLatestObjectVersionAndProperties(personLeavingObjID, false, true));
                }
                catch
                {
                    this.Logger.Fatal($"Could not load person leaving ({personLeavingObjID.ToJSON()}) object from vault.");
                    throw new AppTaskException(TaskProcessingJobResult.Fatal);
                }
            }

            // Load the successor.
            ObjVerEx successor = null;
            {
                if (false == job.Directive.Successor.TryGetObjID(out var successorObjID))
                {
                    this.Logger.Fatal($"Could not load successor from directive:\r\n:{Newtonsoft.Json.JsonConvert.SerializeObject(job.Directive)}");
                    throw new AppTaskException(TaskProcessingJobResult.Fatal);
                }
                try
                {
                    successor = new ObjVerEx(job.Vault, job.Vault.ObjectOperations.GetLatestObjectVersionAndProperties(successorObjID, false, true));
                }
                catch
                {
                    this.Logger.Fatal($"Could not load successor ({successorObjID.ToJSON()}) object from vault.");
                    throw new AppTaskException(TaskProcessingJobResult.Fatal);
                }
            }

            // Find all contracts that are assigned to the person leaving.
            var searchBuilder = new MFSearchBuilder(job.Vault, this.Configuration?.DocumentsToUpdate?.ToApiObject() ?? new SearchConditions());
            searchBuilder.Property(this.Configuration.ContractOwnerProperty.ID, personLeaving.ID);

            // Get the transaction runner; we'll run each one in a transaction.
            var transactionRunner = this.VaultApplication?.GetTransactionRunner()
                ?? throw new AppTaskException(TaskProcessingJobResult.Fatal, "Transaction runner could not be loaded.");

            // Iterate over each contract and update it.
            // This runs a segmented search so will find everything, even in a very large vault.
            // It might be slow, though!
            var itemsUpdated = 0;
            searchBuilder.ForEach((ov) =>
            {
                // Throw if needed.
                job.ThrowIfJobAbortRequested();
                job.ThrowIfTaskCancellationRequested();

                // We will update each one in a single transaction.
                // Not the most efficient, so we may re-visit this.
                transactionRunner.Run((transactionalVault) =>
                {
                    // Load the object with the transactional vault.
                    var objVerEx = new ObjVerEx(transactionalVault, ov);

                    // Try and update it.
                    try
                    {
                        // Try and check it out.
                        var b = objVerEx.StartRequireCheckedOut();

                        // Update the required property.
                        var pv = objVerEx.GetProperty(this.Configuration.ContractOwnerProperty.ID);
                        if (pv.TypedValue.DataType == MFDataType.MFDatatypeLookup)
                            pv.Value.SetValue(MFDataType.MFDatatypeLookup, successor.ID);
                        else if (pv.TypedValue.DataType == MFDataType.MFDatatypeMultiSelectLookup)
                        {
                            pv.RemoveLookup(personLeaving.ID);
                            pv.AddLookup(successor.ID);
                        }
                        else
                        {
                            // Configuration is broken.  Don't lose that this needs to happen.
                            this.Logger.Fatal($"Contract owner property is not of the expected types (is {pv.TypedValue.DataType}, but only lookup/mslu supported).");
                            throw new AppTaskException(TaskProcessingJobResult.Requeue);
                        }

                        // Update the object.
                        objVerEx.VersionComment = $"Owner changed due to {successor.Title}, as {personLeaving.Title} was marked as left.";
                        objVerEx.SaveProperties();

                        // Check it in.
                        objVerEx.EndRequireCheckedOut(b);
                    }
                    catch
                    {
                        // Assign it to the successor as it cannot be updated.
                        var propertyValuesBuilder = new MFPropertyValuesBuilder(transactionalVault);
                        propertyValuesBuilder.SetClass((int)MFBuiltInObjectClass.MFBuiltInObjectClassGenericAssignment);
                        propertyValuesBuilder.SetTitle($"Contract could not be assigned to {successor.Title}");
                        propertyValuesBuilder.AddLookup
                        (
                            transactionalVault.ObjectTypeOperations.GetObjectType(ov.ObjVer.Type).DefaultPropertyDef,
                            ov.ObjVer.ID
                        );
                        propertyValuesBuilder.AddLookup
                        (
                            (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefAssignedTo,
                            successor.GetLookupID(this.Configuration.MFilesUser.ID)
                        );
                        transactionalVault.ObjectOperations.CreateNewObjectExQuick
                        (
                            (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment,
                            propertyValuesBuilder.Values
                        );
                    }

                    itemsUpdated++;
                    job.Update(details: $"Updated {itemsUpdated}");

                    // Log?
                    if (itemsUpdated % 20 == 0)
                        this.Logger.Info($"Migrated {itemsUpdated} documents from {personLeaving.Title} to {successor.Title}.");
                });
            });

            // Log that we're done.
            this.Logger.Info($"Migrated {itemsUpdated} documents from {personLeaving.Title} to {successor.Title}.");

        }

        /// <summary>
        /// Handles the <see cref="MFEventHandlerType.MFEventHandlerBeforeCheckInChangesFinalize" /> event.
        /// </summary>
        /// <param name="env">The vault/object environment.</param>
        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeCheckInChangesFinalize)]
        public void MigrateContractsToSuccessor(EventHandlerEnvironment env)
        {
            // Sanity.
            if (false == (this.Configuration?.Enabled ?? false))
            {
                this.Logger.Info("Migration of contracts is disabled in configuration; skipping checks.");
                return;
            }

            // Does the object match the trigger?
            if (false == (this.Configuration?.Trigger?.Condition?.IsMatch(env.ObjVerEx, true, env.CurrentUserID) ?? false))
            {
                this.Logger.Info($"Object {env.ObjVer.ToJSON()} does not match trigger to migrate contracts.");
                return;
            }

            // More sanity.
            if (false == (this.Configuration.EmployeeObjectType?.IsResolved ?? false))
            {
                this.Logger.Fatal($"Employee object type is not configured.");
                throw new InvalidOperationException("Employee object type  is not configured.");
            }
            if (false == (this.Configuration.SuccessorProperty?.IsResolved ?? false))
            {
                this.Logger.Fatal($"Successor property is not configured.");
                throw new InvalidOperationException("Successor property is not configured.");
            }
            if (false == (this.Configuration.MFilesUser?.IsResolved ?? false))
            {
                this.Logger.Fatal($"M-Files user property is not configured.");
                throw new InvalidOperationException("M-Files user property is not configured.");
            }

            // Read the value of the successor property.
            var successorId = env.ObjVerEx.GetLookupID(this.Configuration.SuccessorProperty.ID);
            if (successorId == -1)
            {
                throw new InvalidOperationException("Successor is not configured.");
            }
            if (successorId == env.ObjVerEx.ID)
            {
                throw new InvalidOperationException("Successor cannot be the person who is leaving.");
            }
            var successor = new ObjVerEx(env.Vault, env.ObjVerEx.Type, successorId, -1);
            successor.EnsureLoaded();

            // Make sure that the successor has an M-Files user!
            if (-1 == successor.GetLookupID(this.Configuration.MFilesUser.ID))
            {
                throw new InvalidOperationException("Successor must have an M-Files user.");
            }

            // TODO: Probably needs some additional logic to make sure that the successor has not left!

            // Add the task to the queue.
            this.Logger.Info($"Adding task to queue to migrate documents from {env.ObjVerEx.Title} to {successor.Title}");
            this.VaultApplication.TaskManager.AddTask
            (
                env.Vault,
                VaultApplication.TaskQueueID,
                MigrateContractsToSuccessorTaskType,
                new MigrateContractsToSuccessorTaskDirective()
                {
                    DisplayName = $"Migrate documents from {env.ObjVerEx.Title} to {successor.Title}",
                    PersonLeaving = new ObjIDTaskDirective()
                    {
                        ObjectTypeID = this.Configuration.EmployeeObjectType.ID,
                        ObjectID = env.ObjVer.ID
                    },
                    Successor = new ObjIDTaskDirective()
                    {
                        ObjectTypeID = this.Configuration.EmployeeObjectType.ID,
                        ObjectID = successorId
                    }
                }
                );
        }
    }

    [DataContract]
    public class MigrateContractsToSuccessorTaskDirective
        : TaskDirectiveWithDisplayName
    {
        [DataMember]
        public ObjIDTaskDirective PersonLeaving { get; set; }


        [DataMember]
        public ObjIDTaskDirective Successor { get; set; }
    }

    [DataContract]
    public class MigrateContractsConfiguration
        : ConfigurationBase
    {

        // We will use the trigger class from a previous task here too.
        [DataMember(Order = 1)]
        [JsonConfEditor
        (
            HelpText = "An object must match all of the conditions the trigger to cause the documents to be re-assigned."
        )]
        public Trigger Trigger { get; set; }

        [DataMember(Order = 2)]
        [JsonConfEditor(Label = "Documents to update")]
        public SearchConditionsJA DocumentsToUpdate { get; set; }

        [DataMember(Order = 3)]
        [MFObjType]
        [JsonConfEditor(Label = "Employee")]
        public MFIdentifier EmployeeObjectType { get; set; } = "OT.Employee";

        [DataMember(Order = 4)]
        [MFPropertyDef(Datatypes = new[] { MFDataType.MFDatatypeLookup, MFDataType.MFDatatypeMultiSelectLookup })]
        [JsonConfEditor(Label = "Contract owner")]
        public MFIdentifier ContractOwnerProperty { get; set; } = "PD.ContractOwner";

        [DataMember(Order = 5)]
        [MFPropertyDef(Datatypes = new[] { MFDataType.MFDatatypeLookup, MFDataType.MFDatatypeMultiSelectLookup })]
        [JsonConfEditor(Label = "Successor")]
        public MFIdentifier SuccessorProperty { get; set; } = "PD.Successor";

        [DataMember(Order = 5)]
        [MFPropertyDef(Datatypes = new[] { MFDataType.MFDatatypeLookup })]
        [JsonConfEditor(Label = "M-Files user")]
        public MFIdentifier MFilesUser { get; set; } = "PD.MfilesUser";
    }
}
