using MFiles.Extensibility.Applications;
using MFiles.Extensibility.ExternalObjectTypes;
using MFiles.Extensibility.Framework.Applications;
using MFiles.Extensibility.Framework.ExternalObjectTypes;
using MFiles.VAF.Configuration;
using MFilesAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ImportGists
{
    /// <summary>
    /// The datasource service.
    /// </summary>
    public class DataSource
        : ExternalObjectTypeService<ExternalObjectTypeConfiguration<ConfigurationRoot>>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="operationContext">The operation context. Must not be stored.</param>
        /// <param name="vaultApplication">The hosting vault application for the service.</param>
        public DataSource(
            IOperationContext operationContext,
            VaultApplication vaultApplication
        ) : base(operationContext, vaultApplication)
        {
        }

        /// <summary>
        /// The service display name. The display name may be used e.g. for error tracking purposes.
        /// </summary>
        public override string Name => ApplicationContext.Name;

        /// <summary>
        /// The service's unique identifier.
        /// </summary>
        public override Guid Guid => ApplicationContext.Guid;

        /// <summary>
        /// Open External Object Type Connection.
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <param name="stopToken">Stop token.</param>
        /// <returns>External Object Type Connection</returns>
        public override IExternalObjectTypeConnection OpenConnection(
            ExternalObjectTypeConfiguration<ConfigurationRoot> config,
            CancellationToken stopToken
        )
        {
            // Instantiate our data source connection.
            return new DataSourceConnection(config, stopToken);
        }

        #region Optionals

        /// <summary>
        /// Performs a service specific validation on the new configuration.
        /// </summary>
        /// <param name="operationContext">The operation context. Must not be stored.</param>
        /// <param name="instance">The configuration instance identifier.</param>
        /// <param name="newConfiguration">The new configuration.</param>
        /// <returns>A collection of validation findings.</returns>
        public override IEnumerable<ValidationFinding> RunCustomValidation(
            IOperationContext operationContext,
            string instance,
            ExternalObjectTypeConfiguration<ConfigurationRoot> newConfiguration
        )
        {
            // TODO: Validate the settings in newConfiguration
            // and return any ValidationFindings, as appropriate.
            // Verify our settings sanity.
            //if( newConfiguration?.CustomConfiguration?.Type == null )
            //{
            //	// Add error finding from a missing value.
            //	yield return new ValidationFinding( 
            //			ValidationFindingType.Error,
            //			"Type", "Must be defined." );
            //}

            // Delegate to base and return all those findings as well.
            foreach (ValidationFinding find in base.RunCustomValidation(operationContext, instance, newConfiguration))
            {
                yield return find;
            }
        }

        /// <summary>
        /// User level required to take the plugin into use for object type.
        /// 
        /// SystemAdmin: System Admin always.
        /// VaultAdmin: Vault Admin always.
        /// LocalAdmin: System Admin for local installations, Vault Admin for Ground installations.
        /// </summary>
        /// <remarks>
        /// Default level is system admin, plugin needs to override consciously if local or vault level is possible.
        /// </remarks>
        /// <returns>Account level.</returns>
        public override UserLevel SetupUserLevel => UserLevel.SystemAdmin;

        /// <summary>
        /// Returning the configuration info message.
        /// </summary>
        /// <param name="connection">Configuration instance data of the connection.</param>
        /// <returns>Info message.</returns>
        public override string GetCustomInfoMessage(
            ConfiguredInstanceData<ExternalObjectTypeConfiguration> connection
        )
        {
            // Access the configuration as our own type.
            var configuration = connection.Configuration as ExternalObjectTypeConfiguration<ConfigurationRoot>;
            ConfigurationRoot custom = configuration.CustomConfiguration;

            // TODO: This can be customized using data from the configuration.
            return "Accessing data";
        }
        #endregion
    }
}
