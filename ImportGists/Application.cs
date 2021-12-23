using MFiles.Extensibility.Applications;
using MFiles.Extensibility.ExternalRepositories;
using MFiles.Extensibility.Framework.Applications;
using MFiles.Extensibility.Framework.ExternalObjectTypes;
using MFiles.VAF.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace ImportGists
{
    /// <summary>
    /// The main application.
    /// </summary>
    public class Application : ExternalObjectTypeApplication
    {
        /// <summary>
        /// The supported factory service.
        /// </summary>
        private DataSource factory = null;

        /// <summary>
        /// The services the application offers.
        /// </summary>
        public override IEnumerable<IService> Services => new IService[] { this.factory };

        /// <summary>
        /// Mandatory method for the vault applications to implement which gets called during the initialization. The
        /// application is supposed to create the supported services in this call.
        /// </summary>
        protected override void CreateServices(IOperationContext operationContext)
        {
            // Create the supported services.
            this.factory = new DataSource(operationContext, this);

            // Enable TLS.
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }
    }
}
