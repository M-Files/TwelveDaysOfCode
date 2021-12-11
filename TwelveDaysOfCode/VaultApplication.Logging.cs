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

namespace TwelveDaysOfCode
{
    /// <summary>
    /// The entry point for this Vault Application Framework application.
    /// </summary>
    /// <remarks>Examples and further information available on the developer portal: http://developer.m-files.com/. </remarks>
    public partial class VaultApplication
        : MFiles.VAF.Extensions.ConfigurableVaultApplicationBase<Configuration>
    {
        private Logger Logger { get; set; }

        public virtual void ConfigureLogging()
        {
            // For now, hard-code the logging target.
            // Note: file system targets are not a good idea for MSM.  Instead maybe application insights?
            var config = new NLog.Config.LoggingConfiguration();
            var target = new FileTarget()
            {
                Name = "File target",
                FileName = "C:\\temp\\active-log.txt",
                Layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss}: ${message}",
                ArchiveFileName = "C:\\temp\\archives\\log.{#####}.txt",
                ArchiveAboveSize = 10240000,
                ArchiveNumbering = ArchiveNumberingMode.Sequence,
                AutoFlush = true,
                FileNameKind = FilePathKind.Absolute,
            };
            config.AddTarget(target);
            config.AddRuleForAllLevels(target);
            NLog.LogManager.Configuration = config;

            // Create our logger.
            this.Logger = NLog.LogManager.GetLogger(this.GetType().FullName);
            this.Logger.Info("Logging configured.");
        }

        public virtual void DestroyLogging()
        {
            NLog.LogManager.Shutdown();
        }

    }
}