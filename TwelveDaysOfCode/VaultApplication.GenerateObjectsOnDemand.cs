using MFiles.VAF;
using MFiles.VAF.AppTasks;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration;
using MFiles.VAF.Configuration.AdminConfigurations;
using MFiles.VAF.Configuration.JsonAdaptor;
using MFiles.VAF.Configuration.JsonEditor;
using MFiles.VAF.Core;
using MFiles.VAF.Extensions;
using MFilesAPI;
using Newtonsoft.Json;
using NLog;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;

namespace TwelveDaysOfCode
{
    public partial class VaultApplication
        : MFiles.VAF.Extensions.ConfigurableVaultApplicationBase<Configuration>
    {

        /// <summary>
        /// Handles the <see cref="MFEventHandlerType.MFEventHandlerBeforeCheckInChangesFinalize" /> event.
        /// </summary>
        /// <param name="env">The vault/object environment.</param>
        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeCheckInChangesFinalize)]
        public void GenerateDocumentsOnDemand(EventHandlerEnvironment env)
        {
            // Sanity.
            if (false == (this.Configuration?.GenerateDocumentsOnDemandConfiguration?.Enabled ?? false))
            {
                this.Logger.Info("Documentation generation skipped; disabled in configuration.");
                return;
            }

            // Are there any rules for this object?
            var matchingRules = this.Configuration? 
                .GenerateDocumentsOnDemandConfiguration?
                .Rules?
                .Where(r => r.Triggers?.Any(c => c?.Condition?.IsMatch(env.ObjVerEx, true, env.CurrentUserID) ?? false) ?? false)?
                .ToList()
                ?? new List<GenerateDocumentsRule>();
            if (matchingRules.Count == 0)
            {
                this.Logger.Info($"Documentation generation skipped for object {env.ObjVer.ToJSON()}; no matching rules.");
                return;
            }

            // We have one or more rules to execute.  Let's action them!
            foreach (var rule in matchingRules)
            {
                foreach (var action in rule.Actions)
                {
                    // Sanity.
                    if (action.Type == GenerateDocumentActionTypes.Undefined)
                    {
                        this.Logger.Info($"Documentation generation for object {env.ObjVer.ToJSON()} skipped; action type is undefined.");
                        continue;
                    }
                    this.Logger.Info($"Documentation generation for object {env.ObjVer.ToJSON()} starting; action type is {action.Type}.");

                    // Try and load the source object.
                    var sourceObject = action.LoadConfiguredObject(env.Vault);
                    if (null == sourceObject)
                    {
                        this.Logger.Fatal($"Could not load configured source object; documentation generation failed.");
                        throw new InvalidOperationException($"Could not find a source object in the vault to copy; the configuration is invalid:\r\n{JsonConvert.SerializeObject(action)}");
                    }

                    // Copy it!
                    {
                        // Remove the "is template" property.
                        var additionalProperties = new List<ObjectCopyOptions.PropertyValueInstruction>();
                        additionalProperties.Add(new ObjectCopyOptions.PropertyValueInstruction()
                        {
                            InstructionType = ObjectCopyOptions.PropertyValueInstructionType.RemovePropertyValue,
                            PropertyValue = new PropertyValue()
                            {
                                PropertyDef = (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefIsTemplate
                            }
                        });

                        // Add the relationship property, if appropriate.
                        if(action.CreateRelationshipToTriggerObject)
                        {
                            // Set the value pointing at the current object.
                            var typedValue = new TypedValue();
                            typedValue.SetValue
                            (
                                MFDataType.MFDatatypeMultiSelectLookup, 
                                new int[] { env.ObjVer.ID }
                            );

                            // Load the property definition to use for the relationship.
                            var defaultPropertyDef = env.Vault.ObjectTypeOperations.GetObjectType(env.ObjVer.Type).DefaultPropertyDef;

                            this.Logger.Info($"Adding property value for property {defaultPropertyDef} pointing at object with ID {env.ObjVer.ID}.");

                            // Add the instruction to create a property pointing at the current project.
                            additionalProperties.Add(new ObjectCopyOptions.PropertyValueInstruction()
                            {
                                InstructionType = ObjectCopyOptions.PropertyValueInstructionType.AddValueToProperty,
                                PropertyValue = new PropertyValue()
                                {
                                    PropertyDef = defaultPropertyDef,
                                    TypedValue = typedValue
                                }
                            });

                        }

                        // Create the actual object.
                        this.Logger.Info($"Creating the new object..");
                        var createdObject = sourceObject.CreateCopy
                        (
                            new ObjectCopyOptions()
                            {
                                CheckInComments = "Generated automatically",
                                CreatedByUserId = env.CurrentUserID,
                                Properties = additionalProperties
                            }
                        );
                        this.Logger.Info($"New object created: {env.ObjVer.ToJSON()}..");
                    }
                }
            }
        }


    }

    [DataContract]
    public class GenerateDocumentsOnDemandConfiguration
        : ConfigurationBase
    {

        [DataMember(Order = 1)]
        public List<GenerateDocumentsRule> Rules { get; set; }
            = new List<GenerateDocumentsRule>();

    }
    [DataContract]
    public class GenerateDocumentsRule
    {

        [DataMember]
        [JsonConfEditor
        (
            HelpText = "An object must match all of the conditions in one or more triggers to cause the configured actions to be run.  i.e. if there are three triggers then the object must match ALL of the conditions in trigger one, or ALL of the conditions in trigger two, or ALL of the conditions in trigger three."
        )]
        public List<Trigger> Triggers { get; set; }
            = new List<Trigger>();

        [DataMember]
        public List<GenerateDocumentAction> Actions { get; set; }
            = new List<GenerateDocumentAction>();

    }
    [DataContract]
    public class Trigger
    {
        [DataMember]
        public SearchConditionsJA Condition { get; set; }
            = new SearchConditionsJA();
    }

    [DataContract]
    // In theory there could be many types of generation configuration.
    // This implementation supports only two types (by hard-coded ID and type,
    // or selection of specific document).
    // Other implementations may use relationships from the object, or a search,
    // or... anything.
    public class GenerateDocumentAction
    {
        [DataMember(Order = 1)]
        [JsonConfEditor(DefaultValue = GenerateDocumentActionTypes.Undefined)]
        public GenerateDocumentActionTypes Type { get; set; }
            = GenerateDocumentActionTypes.Undefined;

        [DataMember(Order = 2)]
        [JsonConfEditor
        (
            DefaultValue = true,
            Label = "Create relationship",
            HelpText = "Create a relationship from the generated object to the object that triggered its generation?"
        )]
        public bool CreateRelationshipToTriggerObject { get; set; }
            = true;

            [DataMember(Order = 3)]
        [JsonConfEditor
        (
            Label = "Document Template",
            TypeEditor = "options",
            Hidden = true,
            ShowWhen = ".parent._children{ .key == 'Type' && .value == 'SelectTemplateDocument' }"
        )]
        [ValueOptions(typeof(TemplateDocumentStableValueOptionsProvider))]
        public MFIdentifier DocumentTemplate { get; set; }

        [DataMember(Order = 4)]
        [MFObjType(AllowEmpty = true)]
        [JsonConfEditor
        (
            IsRequired = false,
            Label = "Object Type",
            Hidden = true,
            ShowWhen = ".parent._children{ .key == 'Type' && .value == 'EnterDocumentIDAndType' }",
            DefaultValue = null
        )]
        public MFIdentifier ObjectType { get; set; }

        [DataMember(Order = 5)]
        [JsonConfEditor
        (
            Label = "Object ID",
            Hidden = true,
            ShowWhen = ".parent._children{ .key == 'Type' && .value == 'EnterDocumentIDAndType' }"
        )]
        public int ObjectID { get; set; }

        /// <summary>
        /// Returns the object selected in this configuration.
        /// </summary>
        /// <param name="vault">The vault reference to load the object from.</param>
        /// <returns>The object, or null if the configuration is invalid.</returns>
        /// <exception cref="NotImplementedException">Thrown if <see cref="Type"/> is not handled.</exception>
        public ObjVerEx LoadConfiguredObject(Vault vault)
        {
            switch (this.Type)
            {
                case GenerateDocumentActionTypes.Undefined:
                    return null;
                case GenerateDocumentActionTypes.SelectTemplateDocument:
                    { 
                        // Deal with nothing configured/selected.
                        if (null == this.DocumentTemplate)
                            return null;

                        // Return the selected object.
                        try
                        {
                            var objVerEx = new ObjVerEx(vault, (int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument, this.DocumentTemplate.ID, -1);
                            objVerEx.EnsureLoaded();
                            return objVerEx;
                        }
                        catch { return null; }
                    }

                case GenerateDocumentActionTypes.EnterDocumentIDAndType:
                    {
                        // Deal with a bad configuration.
                        if (this.ObjectID <= 0)
                            return null;
                        if (null == this.ObjectType || false == this.ObjectType.IsResolved)
                            return null;

                        // Return the selected object.
                        try
                        {
                            var objVerEx = new ObjVerEx(vault, this.ObjectType.ID, this.ObjectID, -1);
                            objVerEx.EnsureLoaded();
                            return objVerEx;
                        }
                        catch { return null; }
                    }

                default:
                    throw new NotImplementedException($"Type {this.Type} was not handled.");
            }
        }
    }
    public enum GenerateDocumentActionTypes
    {
        [JsonConfEditor(Label = "Undefined; will not run")]
        Undefined = 0,
        [JsonConfEditor(Label = "Select document template from list")]
        SelectTemplateDocument = 1,
        [JsonConfEditor(Label = "Enter object ID and type")]
        EnterDocumentIDAndType = 2
    }

    /// <summary>
    /// Provides a list of not-deleted documents with "Is Template = true".
    /// </summary>
    public class TemplateDocumentStableValueOptionsProvider
        : IStableValueOptionsProvider
    {
        IEnumerable<ValueOption> IStableValueOptionsProvider.GetOptions(IConfigurationRequestContext context)
        {
            // Search for templates of type document.
            var searchBuilder = new MFSearchBuilder(context.Vault);
            searchBuilder.Deleted(false);
            searchBuilder.VisibleTo(context.CurrentUserID);
            searchBuilder.ObjType((int)MFBuiltInObjectType.MFBuiltInObjectTypeDocument);
            searchBuilder.Property((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefIsTemplate, true);

            // Return value options for each document.
            return searchBuilder
                .FindEx()
                .Select(d => new ValueOption()
                {
                    Label = $"{d.Title} (ID: {d.ID})",
                    Value = d.ID
                });
        }
    }
}