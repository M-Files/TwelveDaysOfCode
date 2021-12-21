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
        /// The modules
        /// </summary>
        internal List<SimpleModuleBase> Modules { get; } = new List<SimpleModuleBase>();

        /// <summary>
        /// Populates <see cref="Modules"/> with data from <see cref="LoadModules"/>.
        /// </summary>
        protected virtual void PopulateModules()
        {
            this.Modules.AddRange(this.LoadModules());
        }

        /// <summary>
        /// Loads all modules.
        /// </summary>
        /// <returns></returns>
        internal virtual IEnumerable<SimpleModuleBase> LoadModules()
        {
            // Load modules from the current assembly.
            return this.LoadModules(this.GetType().Assembly);
        }

        /// <summary>
        /// Loads modules from the supplied <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The assembly to load modules from.</param>
        /// <returns>Any and all modules.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="assembly"/> is null.</exception>
        internal virtual IEnumerable<SimpleModuleBase> LoadModules(System.Reflection.Assembly assembly)
        {
            if(null == assembly)
                throw new ArgumentNullException(nameof(assembly));

            // Load the assemblies.
            foreach (var c in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(SimpleModuleBase).IsAssignableFrom(t)))
            {
                var module = assembly.CreateInstance(c.FullName) as SimpleModuleBase;
                module.VaultApplication = this;
                yield return module;
            }
        }

        /// <inheritdoc />
        /// <remarks>Registers methods from modules.</remarks>
        protected override void LoadHandlerMethods(Vault vault)
        {
            // Use the base implementation.
            base.LoadHandlerMethods(vault);

            // Load module stuff.
            foreach (var module in this.Modules)
            {
                base.RegisterMethodsFromSource(module, vault);
            }
        }

        /// <inheritdoc />
        /// <remarks>Registers task processors from the modules.</remarks>
        protected override void InitializeTaskQueueResolver()
        {
            // Initialise.
            base.InitializeTaskQueueResolver();

            // Include our modules.
            foreach (var module in this.Modules)
                this.TaskQueueResolver.Include(module);
        }

    }
}