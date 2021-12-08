using MFiles.VAF;
using MFiles.VAF.AppTasks;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration;
using MFiles.VAF.Core;
using MFilesAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using MFiles.VAF.Extensions;

namespace TwelveDaysOfCode
{
    /// <summary>
    /// The entry point for this Vault Application Framework application.
    /// </summary>
    /// <remarks>Examples and further information available on the developer portal: http://developer.m-files.com/. </remarks>
    public partial class VaultApplication
        : MFiles.VAF.Extensions.ConfigurableVaultApplicationBase<Configuration>
    {
        /// <summary>
        /// Handles the <see cref="MFEventHandlerType.MFEventHandlerBeforeCheckInChangesFinalize" /> event.
        /// </summary>
        /// <param name="env">The vault/object environment.</param>
        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeCheckInChangesFinalize)]
        public void CreateSharedLinkHandler(EventHandlerEnvironment env)
        {
            // Don't work if this functionality is disabled.
            if (false == (this.Configuration?.Enabled ?? false)
                || false == (this.Configuration?.SharedLinkGenerationConfiguration?.Enabled ?? false))
                return;

            // If it does not have a state, or isn't changing state, then die.
            if (-1 == env.ObjVerEx.State || false == env.ObjVerEx.IsEnteringState)
                return;

            // Is there a trigger for our current state?  If not then die.
            var trigger = this
                    .Configuration?
                    .SharedLinkGenerationConfiguration?
                    .Triggers?
                    .Where(t => t.TriggerState.IsResolved)
                    .FirstOrDefault(t => t.TriggerState.ID == env.ObjVerEx.State);
            if (null == trigger)
                return;

            // Create the shared link.
            var sharedLink = "";
            {
                // We cannot link to this specific version as it's not committed, so link to the previous one.
                var objectVersionAndProperties = env
                    .Vault
                    .ObjectOperations
                    .GetLatestObjectVersionAndProperties(env.ObjVer.ObjID, false, true);
                var sli = new SharedLinkInfo()
                {
                    ObjVer = objectVersionAndProperties.ObjVer,
                    FileVer = objectVersionAndProperties.VersionData.Files[1].FileVer
                };

                // If we have a description then set that.
                if (trigger.Description.IsResolved && env.ObjVerEx.HasValue(trigger.Description.ID))
                    sli.Description = env.ObjVerEx.GetPropertyText(trigger.Description.ID);

                // If we have an expiry date then set that.
                if (trigger.LinkExpiryDate.IsResolved && env.ObjVerEx.HasValue(trigger.LinkExpiryDate.ID))
                    sli.ExpirationTime = env.ObjVerEx.GetPropertyAsDateTime(trigger.LinkExpiryDate.ID)?.ToTimestamp();

                // By default M-Files doesn't allow linking to specific versions.
                if (false == this.Configuration.SharedLinkGenerationConfiguration.UseVersionDependentLinks)
                    sli.FileVer.Version = -1; 

                // Create the shared link in the vault.
                sli = env.Vault.SharedLinkOperations.CreateSharedLink(sli);

                // Create the (usable) link.
                {
                    sharedLink = new Uri
                    (
                        new Uri(UrlHelper.GetBaseUrlForWebAccess(env.Vault)),
                        $"/SharedLinks.aspx?accesskey={sli.AccessKey}&vaultguid={env.Vault.GetGUID()}"
                    ).ToString();
                }
            }

            // Update the current object.
            if (trigger.SharedLinkTarget.IsResolved)
            {
                env.ObjVerEx.SetProperty
                (
                    trigger.SharedLinkTarget.ID, 
                    env.Vault.PropertyDefOperations.GetPropertyDef(trigger.SharedLinkTarget.ID).DataType,
                    sharedLink
                );
            }
            env.ObjVerEx.SaveProperties();

            // Ensure the object history is correct.
            env.ObjVerEx.SetModifiedBy(env.CurrentUserID);
        }


    }

    [DataContract]
    public class SharedLinkGenerationConfiguration
        : ConfigurationBase
    {
        [DataMember]
        [JsonConfEditor(DefaultValue = false)]
        public bool UseVersionDependentLinks { get; set; } = false;

        [DataMember]
        public List<SharedLinkTrigger> Triggers { get; set; }
            = new List<SharedLinkTrigger>();

        [DataContract]
        public class SharedLinkTrigger
        {
            [DataMember]
            [MFState]
            [JsonConfEditor]
            public MFIdentifier TriggerState { get; set; }

            [DataMember]
            [MFPropertyDef(Datatypes = new[] { MFDataType.MFDatatypeDate })]
            [JsonConfEditor(DefaultValue = "PD.LinkExpiryDate")]
            public MFIdentifier LinkExpiryDate { get; set; } = "PD.LinkExpiryDate";

            [DataMember]
            [MFPropertyDef(Datatypes = new[] { MFDataType.MFDatatypeText, MFDataType.MFDatatypeMultiLineText })]
            [JsonConfEditor(DefaultValue = "PD.Url")]
            public MFIdentifier SharedLinkTarget { get; set; } = "PD.Url";

            [DataMember]
            [MFPropertyDef(Datatypes = new[] { MFDataType.MFDatatypeText, MFDataType.MFDatatypeMultiLineText })]
            [JsonConfEditor(DefaultValue = "PD.Description")]
            public MFIdentifier Description { get; set; } = "PD.Description";
        }
    }
}