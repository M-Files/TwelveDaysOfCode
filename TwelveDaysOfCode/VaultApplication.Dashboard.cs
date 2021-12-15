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
            dashboard.AddContent(new DashboardCustomContent($"<div style=\"background-image: url({DashboardHelper.ImageFileToDataUri("logo.png")}); height: 87px; width: 801px;\" title=\"M-Files 12 Days Of Code Challenge\"></div>"));

            // Important statistics?
            if(this.Configuration?.DashboardStatisticsConfiguration?.Enabled ?? false)
            {
                var table = new DashboardTable();
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

                    // Create the row.
                    var row = new DashboardTableRow();
                    if (string.IsNullOrWhiteSpace(stat.FormatString))
                    {
                        // Just use the name.
                        row.AddCell($"{stat.Name}: {resultsCount}");
                    }
                    else
                    {
                        // Use the format string.
                        row.AddCell(string.Format(stat.FormatString, resultsCount));
                    }

                    // Add the row.
                    table.Rows.Add(row);
                }

                // If we have any items then add them to the dashboard.
                if (table.Rows.Count > 0)
                {
                    var panel = new DashboardPanel()
                    {
                        Title = "Vault statistics",
                        InnerContent = table
                    };
                    dashboard.AddContent(panel);
                }
            }

            // If there's some base content then add that.
            var baseContent = base.GetDashboardContent(context);
            if (false == string.IsNullOrWhiteSpace(baseContent))
                dashboard.AddContent(new DashboardCustomContent(baseContent));

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