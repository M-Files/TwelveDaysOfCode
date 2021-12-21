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
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;

namespace TwelveDaysOfCode
{
    internal class EnsurePrerequisitesModule
        : SimpleModuleBase<EnsureObjectPrerequisitesConfiguration>
    {
        public EnsurePrerequisitesModule()
            : base((c) => c?.EnsureObjectPrerequisitesConfiguration)
        {
            this.Name = "Ensure prerequisite objects";
        }
        /// <summary>
        /// Checks that the object has any configured prerequisites as per configuration.
        /// </summary>
        /// <param name="env">The vault/object environment.</param>
        [EventHandler(MFEventHandlerType.MFEventHandlerBeforeCheckInChangesFinalize)]
        public void EnsureObjectPrerequisites(EventHandlerEnvironment env)
        {
            // Sanity.
            if (false == (this.Configuration?.Enabled ?? false))
            {
                this.Logger.Info("Object prerequisite checking skipped; disabled in configuration.");
                return;
            }

            // Are there any rules for this object?
            var matchingRules = this.Configuration? 
                .Rules?
                .Where(r => r.Triggers?.Any(c => c?.Condition?.IsMatch(env.ObjVerEx, true, env.CurrentUserID) ?? false) ?? false)?
                .ToList()
                ?? new List<PrerequisiteRule>();
            if (matchingRules.Count == 0)
            {
                this.Logger.Info($"Object prerequisite checking for object {env.ObjVer.ToJSON()}; no matching rules.");
                return;
            }

            // We have one or more rules to execute.  Let's action them!
            foreach (var rule in matchingRules)
            {
                // Run each condition separately.
                foreach(var condition in rule.Conditions.Where(c => c?.Search != null))
                {
                    // Get the search as an API object.
                    var searchConditions = condition.Search.ToApiObject(env.Vault);

                    // Build up the search conditions.
                    var searchBuilder = new MFSearchBuilder(env.Vault, searchConditions);
                    searchBuilder.ReferencesWithAnyProperty(env.ObjVer.ObjID); // Must reference this object.

                    // Find the number of items.
                    var count = searchBuilder.FindCount();

                    // Did we get what we expected?
                    var passedCondition = true;
                    switch (condition.ConditionType)
                    {
                        case MFConditionType.MFConditionTypeEqual:
                            passedCondition = count == condition.Value;
                            break;
                        case MFConditionType.MFConditionTypeNotEqual:
                            passedCondition = count != condition.Value;
                            break;
                        case MFConditionType.MFConditionTypeGreaterThan:
                            passedCondition = count > condition.Value;
                            break;
                        case MFConditionType.MFConditionTypeGreaterThanOrEqual:
                            passedCondition = count >= condition.Value;
                            break;
                        case MFConditionType.MFConditionTypeLessThan:
                            passedCondition = count < condition.Value;
                            break;
                        case MFConditionType.MFConditionTypeLessThanOrEqual:
                            passedCondition = count <= condition.Value;
                            break;
                        default:
                            throw new InvalidOperationException($"A rule is configured that uses the condition type of {condition.ConditionType}, but this is not supported for numerical comparisons.");
                    }
                    
                    // If we did not pass then throw the exception.
                    if (false == passedCondition)
                        throw new InvalidOperationException(condition.ExceptionMessage);

                }

            }
        }


    }

    [DataContract]
    public class EnsureObjectPrerequisitesConfiguration
        : ConfigurationBase
    {
        [DataMember(Order = 1)]
        public List<PrerequisiteRule> Rules { get; set; }
            = new List<PrerequisiteRule>();

    }
    [DataContract]
    public class PrerequisiteRule
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }

        // We will use the trigger class from yesterday's task here too.
        [DataMember(Order = 2)]
        [JsonConfEditor
        (
            HelpText = "An object must match all of the conditions in one or more triggers to cause the configured conditions to be checked.  i.e. if there are three triggers then the object must match ALL of the conditions in trigger one, or ALL of the conditions in trigger two, or ALL of the conditions in trigger three."
        )]
        public List<Trigger> Triggers { get; set; }
            = new List<Trigger>();

        [DataMember(Order = 3)]
        [JsonConfEditor
        (
            HelpText = "Conditions are used to express what state supporting objects should be in, to allow the object to be saved.  For example: a condition that states 'Class = Contract and Is Signed = true, at least 1' would mean that there must be at least one object with those properties that refers to the current object to allow the object to be saved."
        )]
        public List<Condition> Conditions { get; set; }
            = new List<Condition>();

    }
    [DataContract]
    public class Condition
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }

        [DataMember(Order = 2)]
        public SearchConditionsJA Search { get; set; }
            = new SearchConditionsJA();

        [DataMember(Order = 3)]
        [JsonConfEditor(DefaultValue = MFConditionType.MFConditionTypeEqual)]
        public MFConditionType ConditionType { get; set; }
            = MFConditionType.MFConditionTypeEqual;

        [DataMember(Order = 4)]
        [JsonConfEditor(DefaultValue = 1)]
        public int Value { get; set; } = 1;

        [DataMember(Order = 5, IsRequired = true)]
        [JsonConfEditor
        (
            IsRequired = true,
            Label = "Exception message",
            HelpText = "Message shown if the object does not meet the specified conditions."
        )]
        public string ExceptionMessage { get; set; }
    }
}