﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using HubAnalytics.AzureSqlDatabase.Implementation;
using HubAnalytics.Core;

namespace HubAnalytics.AzureSqlDatabase
{
    public class AzureSqlDatabaseTelemetryPlugin : IDataCapturePlugin
    {
        public void Initialize(IHubAnalyticsClient hubAnalyticsClient)
        {
            List<AzureSqlDatabase> databases = DatabasesFromConfigurationSection(hubAnalyticsClient.ClientConfiguration.PropertyId);
            if (databases.Count == 0)
            {
                databases.AddRange(from ConnectionStringSettings settings in ConfigurationManager.ConnectionStrings
                    select new AzureSqlDatabase
                    {
                        ConnectionString = settings.ConnectionString, Name = settings.Name
                    });
            }
            TimeSpan interval = TimeSpan.FromMilliseconds(HubAnalyticsAzureSqlDatabaseConfigurationSection.Settings.TelemetryIntervalMs);            
            hubAnalyticsClient.RegisterTelemetryProvider(
                new TelemetryProvider(databases,
                    new SqlByHourProvider(new DataReaderToTelemetryItemMapper()),
                    new SqlByMinuteProvider(new DataReaderToTelemetryItemMapper()),
                    new TelemetryItemToEventMapper(),
                    interval));
        }

        private static List<AzureSqlDatabase> DatabasesFromConfigurationSection(string defaultPropertyId)
        {
            List<AzureSqlDatabase> result = new List<AzureSqlDatabase>();
            if (HubAnalyticsAzureSqlDatabaseConfigurationSection.Settings.AzureSqlDatabases.Count > 0)
            {
                result.AddRange(from AzureSqlDatabaseConfigurationElement element in HubAnalyticsAzureSqlDatabaseConfigurationSection.Settings.AzureSqlDatabases
                    select new AzureSqlDatabase
                    {
                        ConnectionString = element.ConnectionString, Name = element.Name, PropertyId = string.IsNullOrWhiteSpace(element.PropertyId) ? defaultPropertyId : element.PropertyId
                    });
            }
            return result;
        }
    }
}
