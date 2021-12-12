using MFiles.VAF;
using MFiles.VAF.AppTasks;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration;
using MFiles.VAF.Core;
using MFilesAPI;
using NLog;
using NLog.Targets;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;

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
        }

        ~VaultApplication()
        {
            this.DestroyLogging();
        }

    }
}