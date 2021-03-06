﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HubAnalytics.Core;
using HubAnalytics.Core.Model;

namespace HubAnalytics.AzureSqlDatabase.Implementation
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    internal class TelemetryProvider : ITelemetryEventProvider
    {
        private readonly TimeSpan _interval;
        private const int MaxConcurrentRetries = 10;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly List<ISqlDatabaseResourceUsage> _usageSets = new List<ISqlDatabaseResourceUsage>();

        public TelemetryProvider(IReadOnlyCollection<AzureSqlDatabase> databases,
            IUsageProvider sqlByHourProvider,
            IUsageProvider sqlByMinuteProvider,
            ITelemetryItemToEventMapper mapper,
            TimeSpan interval)
        {
            _interval = interval;
            _usageSets.AddRange(databases.Select(x => new SqlDatabaseResourceUsage(x, sqlByMinuteProvider, mapper)).ToList());
            _usageSets.AddRange(databases.Select(x => new SqlDatabaseResourceUsage(x, sqlByHourProvider, mapper)).ToList());

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                await BackgroundCollect();
            });
        }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// This method is the reason the telemetry provider needs to be thread safe. It can be called at any time including while the resource
        /// usage collections are being updated
        /// </summary>
        public IReadOnlyCollection<Event> GetEvents(int batchSize)
        {
            List<Event> events = new List<Event>();
            foreach (ISqlDatabaseResourceUsage usageSet in _usageSets)
            {
                events.AddRange(usageSet.GetEvents());
            }

            return events;
        }

        private async Task BackgroundCollect()
        {
            bool shouldContinue = !_cancellationTokenSource.IsCancellationRequested;
            while (shouldContinue)
            {
                foreach (ISqlDatabaseResourceUsage usageSet in _usageSets.ToArray())
                {
                    if (!await usageSet.Update())
                    {
                        if (usageSet.ConcurrentFailures == MaxConcurrentRetries)
                        {
                            System.Diagnostics.Trace.WriteLine($"{MaxConcurrentRetries} of {usageSet.Name}. Cancelling telemetry logging for 15 minutes.", "Error");
                            usageSet.CancelUntil(DateTimeOffset.UtcNow.AddMinutes(15));
                        }
                    }
                }

                if (_usageSets.Count == 0)
                {
                    shouldContinue = false;
                }
                else 
                {
                    await Task.Delay(_interval, _cancellationTokenSource.Token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        shouldContinue = false;
                    }
                }
            }
        }
    }
}
