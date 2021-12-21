using MFiles.VAF;
using MFiles.VAF.AppTasks;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration;
using MFiles.VAF.Configuration.AdminConfigurations;
using MFiles.VAF.Configuration.Domain.Dashboards;
using MFiles.VAF.Core;
using MFilesAPI;
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
    /// <summary>
    /// The entry point for this Vault Application Framework application.
    /// </summary>
    /// <remarks>Examples and further information available on the developer portal: http://developer.m-files.com/. </remarks>
    public partial class VaultApplication
        : MFiles.VAF.Extensions.ConfigurableVaultApplicationBase<Configuration>
    {

        /// <summary>
        /// Registers a task queue with ID "MFiles.TwelveDaysOfCode".
        /// </summary>
        [TaskQueue]
        public const string TaskQueueID = "MFiles.TwelveDaysOfCode";

        public VaultApplication()
        {
            this.ConfigureLogging();

            // Enable TLS.
            System.Net.ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            // Set up the modules
            this.PopulateModules();
        }

        /// <summary>
        /// The application's current configuration.
        /// </summary>
        public new Configuration Configuration => base.Configuration;

        ~VaultApplication()
        {
            this.DestroyLogging();
        }

    }
}