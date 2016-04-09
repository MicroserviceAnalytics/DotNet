﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using HubAnalytics.Core.Model;
using Newtonsoft.Json;

// ReSharper disable ConvertToAutoProperty

namespace HubAnalytics.Core.Implementation
{
    class RemoteClientConfiguration : IClientConfiguration
    {
        private const string SettingsRelativePath = "v1/settings";

        private readonly string _propertyId;
        private readonly string _key;
        private readonly string _apiRoot;
        private readonly string _correlationIdKey;
        private readonly bool _enableCorrelation;
        private readonly string _httpStopwatchKey;
        private readonly bool _isRemoteUpdateEnabled;
        private readonly bool _stripHttpQueryParams;
        private readonly string[] _httpRequestHeaderWhitelist;
        private readonly string[] _httpResponseHeaderWhitelist;
        private readonly Uri _endpoint;
        private readonly string _sessionIdKey;
        private readonly string _userIdKey;
        private readonly string[] _excludedVerbs;
        private readonly bool _isUserIdCreationEnabled;
        private readonly bool _isSessionIdCreationEnabled;
        private readonly string _tailCorrelationCookieName;
        private readonly string _trackingUserCookieName;
        private readonly string _trackingSessionCookieName;

        private TimeSpan _uploadInterval;
        private bool _isCaptureHttpEnabled;
        private bool _isCaptureErrorsEnabled;
        private bool _isCaptureSqlEnabled;
        private bool _isCaptureCustomMetricEnabled;
        private bool _isCaptureLogsEnabled;
        private bool _isUserTrackingEnabled;
        private bool _isSessionTrackingEnabled;        

        public RemoteClientConfiguration(IClientConfiguration initialConfiguration)
        {
            _propertyId = initialConfiguration.PropertyId;
            _key = initialConfiguration.Key;
            
            _apiRoot = initialConfiguration.ApiRoot;
            _correlationIdKey = initialConfiguration.CorrelationIdKey;
            _enableCorrelation = initialConfiguration.EnableCorrelation;
            _httpStopwatchKey = initialConfiguration.HttpStopwatchKey;
            _isRemoteUpdateEnabled = initialConfiguration.IsRemoteUpdateEnabled;
            _stripHttpQueryParams = initialConfiguration.StripHttpQueryParams;
            _httpRequestHeaderWhitelist = initialConfiguration.HttpRequestHeaderWhitelist;
            _httpResponseHeaderWhitelist = initialConfiguration.HttpResponseHeaderWhitelist;
            _sessionIdKey = initialConfiguration.SessionIdKey;
            _userIdKey = initialConfiguration.UserIdKey;
            _excludedVerbs = initialConfiguration.ExcludedVerbs;
            _isUserIdCreationEnabled = initialConfiguration.IsUserIdCreationEnabled;
            _isSessionIdCreationEnabled = initialConfiguration.IsSessionIdCreationEnabled;
            _tailCorrelationCookieName = initialConfiguration.TailCorrelationCookieName;
            _trackingUserCookieName = initialConfiguration.TrackingUserCookieName;
            _trackingSessionCookieName = initialConfiguration.TrackingSessionCookieName;

            _uploadInterval = initialConfiguration.UploadInterval;
            _isCaptureHttpEnabled = initialConfiguration.IsCaptureHttpEnabled;
            _isCaptureErrorsEnabled = initialConfiguration.IsCaptureErrorsEnabled;
            _isCaptureSqlEnabled = initialConfiguration.IsCaptureSqlEnabled;
            _isCaptureCustomMetricEnabled = initialConfiguration.IsCaptureCustomMetricEnabled;
            _isCaptureLogsEnabled = initialConfiguration.IsCaptureLogsEnabled;
            _isUserTrackingEnabled = initialConfiguration.IsUserTrackingEnabled;
            _isSessionTrackingEnabled = initialConfiguration.IsSessionTrackingEnabled;
            
            _endpoint = new Uri(_apiRoot.EndsWith("/") ? $"{_apiRoot}{SettingsRelativePath}" : $"{_apiRoot}/{SettingsRelativePath}");

            Task.Run(async () =>
            {
                await BackgroundUpdate();
            });
        }

        public string PropertyId => _propertyId;

        public string Key => _key;

        public string ApiRoot => _apiRoot;

        public string CorrelationIdKey => _correlationIdKey;

        public bool EnableCorrelation => _enableCorrelation;

        public string HttpStopwatchKey => _httpStopwatchKey;

        public string SessionIdKey => _sessionIdKey;

        public string UserIdKey => _userIdKey;

        public bool IsRemoteUpdateEnabled => _isRemoteUpdateEnabled;

        public bool StripHttpQueryParams => _stripHttpQueryParams;

        public string[] HttpRequestHeaderWhitelist => _httpRequestHeaderWhitelist;

        public string[] HttpResponseHeaderWhitelist => _httpResponseHeaderWhitelist;

        public string[] ExcludedVerbs => _excludedVerbs;

        public TimeSpan UploadInterval => _uploadInterval;

        public bool IsCaptureHttpEnabled => _isCaptureHttpEnabled;

        public bool IsCaptureErrorsEnabled => _isCaptureErrorsEnabled;

        public bool IsCaptureSqlEnabled => _isCaptureSqlEnabled;

        public bool IsCaptureCustomMetricEnabled => _isCaptureCustomMetricEnabled;

        public bool IsCaptureLogsEnabled => _isCaptureLogsEnabled;
        public bool IsUserTrackingEnabled => _isUserTrackingEnabled;
        public bool IsSessionTrackingEnabled => _isSessionTrackingEnabled;
        public bool IsUserIdCreationEnabled => _isUserIdCreationEnabled;
        public bool IsSessionIdCreationEnabled => _isSessionIdCreationEnabled;
        public string TrackingSessionCookieName => _trackingSessionCookieName;
        public string TrackingUserCookieName => _trackingUserCookieName;
        public string TailCorrelationCookieName => _tailCorrelationCookieName;


        private async Task BackgroundUpdate()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                try
                {
                    HttpClient client = new HttpClient();
                    HttpRequestMessage message = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = _endpoint
                    };
                    message.Headers.Add("af-property-id", _propertyId);
                    message.Headers.Add("af-collection-key", _key);

                    using (HttpResponseMessage response = await client.SendAsync(message))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string result = await response.Content.ReadAsStringAsync();
                            ApplicationCaptureSettings settings = JsonConvert.DeserializeObject<ApplicationCaptureSettings>(result);
                            _uploadInterval = TimeSpan.FromMilliseconds(settings.UploadIntervalMs);
                            _isCaptureHttpEnabled = settings.IsCaptureHttpEnabled;
                            _isCaptureErrorsEnabled = settings.IsCaptureErrorsEnabled;
                            _isCaptureSqlEnabled = settings.IsCaptureSqlEnabled;
                            _isCaptureCustomMetricEnabled = settings.IsCaptureCustomMetricsEnabled;
                            _isCaptureLogsEnabled = settings.IsCaptureLogsEnabled;
                            _isUserTrackingEnabled = settings.IsUserTrackingEnabled;
                            _isSessionTrackingEnabled = settings.IsSessionTrackingEnabled;
                        }
                    }
                }
                catch (Exception)
                {
                    // do nothing
                }
            }
        }
    }
}