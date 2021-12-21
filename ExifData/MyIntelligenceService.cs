using MFiles.Extensibility.Applications;
using MFiles.Extensibility.Framework.Applications;
using MFiles.Extensibility.Framework.IntelligenceServices;
using MFiles.Extensibility.Framework.Terminologies;
using MFiles.Extensibility.IntelligenceServices;
using MFiles.Extensibility.Terminologies;
using System;
using System.Collections.Generic;

namespace ExifData
{
    public class MyIntelligenceService :
        IntelligenceService<IntelligenceServiceConfiguration<MyConfiguration>>
    {
        #region Default implementations (do not typically need to be edited)

        /// <summary>
        /// Name of the service.
        /// </summary>
        public override string Name => this.ApplicationContext.Name;

        /// <summary>
        /// The service's unique identifier.
        /// </summary>
        /// <remarks>This will use the application GUID.  If your application has multiple services then return unique GUIDs for each one.</remarks>
        public override Guid Guid => this.ApplicationContext.Guid;

        /// <summary>
        /// Terminology supported by this intelligence service.
        /// </summary>
        protected TerminologyRaw Terminology = new TerminologyRaw();

        /// <summary>
        /// Creates a new Intelligence Service instance.
        /// </summary>
        /// <param name="operationContext">The context of the operation.</param>
        /// <param name="vaultApplication">The vault application running the service.</param>
        public MyIntelligenceService(
            IOperationContext operationContext,
            VaultApplication vaultApplication
        ) : base(operationContext, vaultApplication)
        {
            // Initialize terminology.
            this.InitializeTerminology();
        }

        /// <summary>
        /// Constructs the provider with a configuration.
        /// 
        /// If the construct method throws an exception, the provider will not be instantiated.
        /// </summary>
        /// <param name="instanceName">The configuration name.</param>
        /// <param name="structureConfig">The metadata structure details for the provider.</param>
        /// <param name="hasConfigurationErrors">True, if the validation found errors from the configuration.</param>
        /// <returns>The constructed provider.</returns>
        public override IIntelligenceProvider Construct(
            string instanceName,
            IntelligenceServiceConfiguration<MyConfiguration> structureConfig,
            bool hasConfigurationErrors
        )
        {
            // Construct a new provider.
            return new MyIntelligenceProvider(instanceName, structureConfig, hasConfigurationErrors);
        }

        /// <summary>
        /// Gets the terminology supported by the specified provider.
        /// <param name="operationContext">The operation context. Must not be stored.</param>
        /// <param name="providerId">The provider instance name.</param>
        /// </summary>
        public override ITerminology GetTerminology(
            IOperationContext operationContext,
            string providerId
        )
        {
            // Return previously initialized terminology.
            return this.Terminology;
        }

        /// <summary>
        /// Returns the default configuration as json string.
        /// </summary>
        /// <param name="operationContext">The context of the operation.</param>
        /// <param name="instance">The configuration instance identifier. Not used here.</param>
        /// <returns>Default configuration as json string.</returns>
        public override string GetDefaultConfigurationString(IOperationContext operationContext, string instance)
        {
            // Try loading the default configuration from the resource.
            try
            {
                // Load and return the default json file (from the assembly resource).
                return System.Text.Encoding.UTF8.GetString(Properties.Resources.defaultConfiguration);
            }
            catch (Exception ex)
            {
                // Let the admin know something went wrong.
                throw new Exception($"Could not load the default configuration.", ex);
            }
        }


        /// <summary>
        /// Initialize <see cref="Terminology"/>.
        /// </summary>
        protected void InitializeTerminology()
        {
            // No-op if the terminology is not empty.
            if (this.Terminology.Thesaurus.IsEmpty == false)
                return;

            // Add the root-level terms as Types.
            this.Terminology.Thesaurus.AddRootTermsAsTypes(Terms.All);
        }

        #endregion
    }
}
