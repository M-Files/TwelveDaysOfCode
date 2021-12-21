using MFiles.VAF;
using MFiles.VAF.AppTasks;
using MFiles.VAF.Common;
using MFiles.VAF.Configuration;
using MFiles.VAF.Configuration.AdminConfigurations;
using MFiles.VAF.Configuration.Domain.Dashboards;
using MFiles.VAF.Configuration.JsonAdaptor;
using MFiles.VAF.Core;
using MFiles.VAF.Extensions;
using MFiles.VAF.Extensions.Dashboards;
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
        public override string GetDashboardContent(IConfigurationRequestContext context)
        {
            // Create a new dashboard.
            var dashboard = new StatusDashboard();

            // Add the logo.
            dashboard.AddContent(new DashboardCustomContent($"<div style=\"background-repeat: no-repeat; background-size: contain; background-image: url({DashboardHelper.ImageFileToDataUri("logo.png")}); height: 83px; width: 768px; margin: 16px;\" title=\"M-Files 12 Days Of Code Challenge\"></div>"));

            // Add an overview of the application.
            {
                var panel = new DashboardPanel()
                {
                    Title = "Twelve Days Of Code Application",
                    InnerContent = new DashboardCustomContentEx($@"
					<table>
						<tbody>
							<tr>
								<td style='font-size: 12px; padding: 2px 3px;'>Version:</td>
								<td style='font-size: 12px; padding: 2px 3px;'>{ ApplicationDefinition.Version }</td>
							</tr>
							<tr>
								<td style='font-size: 12px; padding: 2px 3px;'>Publisher:</td>
								<td style='font-size: 12px; padding: 2px 3px;'>{ ApplicationDefinition.Publisher }</td>
							</tr>
							<tr>
								<td style='font-size: 12px; padding: 2px 3px;'>Description:</td>
								<td style='font-size: 12px; padding: 2px 3px;'>{ ApplicationDefinition.Description }</td>
							</tr>
						</tbody>
					</table>")
                };
                dashboard.AddContent(panel);
            }

            // Get module data.
            if(this.Modules.Count > 0)
            { 
                var modulesDashboardPanel = new DashboardPanel()
                {
                    Title = "Available modules"
                };
                var moduleList = new DashboardList();
                foreach (var module in this.Modules)
                {
                    moduleList.Items.Add(new DashboardListItem()
                    {
                        Title = module.Name ?? module.GetType().Name
                    });
                }
                modulesDashboardPanel.InnerContent = moduleList;
                dashboard.AddContent(modulesDashboardPanel);
            }

            // Important statistics?
            if(this.Configuration?.DashboardStatisticsConfiguration?.Enabled ?? false)
            {
                var items = new List<string>();
                foreach(var stat in this.Configuration?.DashboardStatisticsConfiguration?.Statistics ?? new List<Statistic>())
                {
                    // Sanity.
                    if (null == stat)
                        continue;
                    if (string.IsNullOrWhiteSpace(stat.Name))
                        continue;
                    if (null == stat.Search || 0 == stat.Search.Count)
                        continue;

                    // Execute the search.
                    var resultsCount = new MFSearchBuilder(context.Vault, stat.Search.ToApiObject(context.Vault)).FindCount();

                    // Create the item.
                    if (string.IsNullOrWhiteSpace(stat.FormatString))
                    {
                        // Just use the name.
                        items.Add($"{stat.Name}: {resultsCount}");
                    }
                    else
                    {
                        // Use the format string.
                        items.Add(string.Format(stat.FormatString, resultsCount));
                    }
                }

                // If we have any items then add them to the dashboard.
                if (items.Count > 0)
                {
                    var panel = new DashboardPanel()
                    {
                        Title = "Vault statistics",
                        InnerContent = new DashboardCustomContent($"<ul>{string.Join("", items.Select(i => $"<li>{System.Security.SecurityElement.Escape(i)}</li>"))}</ul>")
                    };
                    dashboard.AddContent(panel);
                }
            }

            // Lastly, add the async stuff.
            var asyncStuff = base.GetAsynchronousOperationDashboardContent();
            if (null != asyncStuff)
                dashboard.AddContent(asyncStuff);

            // Return our new dashboard.
            return dashboard.ToString();
        }

    }

    [DataContract]
    public class DashboardStatisticsConfiguration
        : ConfigurationBase
    {
        [DataMember(Order = 1)]
        [JsonConfEditor
        (
            HelpText = "A list of statistics (names and numbers) shown on the dashboard."
        )]
        public List<Statistic> Statistics { get; set; } = new List<Statistic>();
    }

    [DataContract]
    public class Statistic
    {
        [DataMember(IsRequired = true, Order = 0)]
        public string Name { get; set; }

        [DataMember(IsRequired = true, Order = 1)]
        public SearchConditionsJA Search { get; set; }

        [DataMember(Order = 2)]
        [JsonConfEditor
        (
            Label = "Format string",
            HelpText = "Leave empty to just have the name displayed, a colon, then the number.  Otherwise include a .NET format string here, where {0} will be replaced with the number of results, e.g. '{0} unapproved invoices'.",
            DefaultValue = null
        )]
        public string FormatString { get; set; }
    }
}