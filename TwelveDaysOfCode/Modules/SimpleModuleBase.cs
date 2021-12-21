using MFiles.VAF;
using MFiles.VAF.Common;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace TwelveDaysOfCode
{
    internal abstract class SimpleModuleBase
        : MethodSource
    {
        /// <summary>
        /// Gets the logger for the current class.
        /// </summary>
        public ILogger Logger { get; set; } = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The vault application.
        /// </summary>
        public VaultApplication VaultApplication { get; set; }

        /// <summary>
        /// The display name of the module.
        /// </summary>
        public string Name { get; set; }

    }
    /// <summary>
    /// A basic module, for slightly better code structure.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class SimpleModuleBase<T>
        : SimpleModuleBase
        where T : class, new()
    {
        /// <summary>
        /// Gets the current configuration.
        /// </summary>
        public Func<Configuration, T> GetModuleConfiguration { get; }

        /// <summary>
        /// Gets the current configuration.
        /// </summary>
        public T Configuration => this.GetModuleConfiguration(this.VaultApplication?.Configuration);

        /// <summary>
        /// Creates the module.
        /// </summary>
        /// <param name="getConfiguration">Returns the configuration for the module.</param>
        public SimpleModuleBase(Func<Configuration, T> getModuleConfiguration)
        {
            this.GetModuleConfiguration = getModuleConfiguration ?? throw new ArgumentNullException(nameof(getModuleConfiguration));
        }
    }
}