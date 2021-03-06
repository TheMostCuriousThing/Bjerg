using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Bjerg.DataDragon;
using Microsoft.Extensions.Logging;

namespace Bjerg
{
    public class RiotDataDragonFetcher : IDataDragonFetcher
    {
        public RiotDataDragonFetcher(ILogger<RiotDataDragonFetcher> logger)
        {
            Client = new HttpClient();
            Logger = logger;
        }

        private HttpClient Client { get; }

        private ILogger Logger { get; }

        private JsonSerializerOptions JsonOptions { get; } = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public async Task<DdGlobals?> FetchGlobals(Locale locale, Version version)
        {
            string l = TransformLocaleForUrl(locale);
            string v = TransformVersionForUrl(version);
            var uri = new Uri($"https://dd.b.pvp.net/{v}/core/{l}/data/globals-{l}.json");
            return await FetchAsync<DdGlobals>(uri);
        }

        public async Task<IReadOnlyList<DdCard>?> FetchSetCards(Locale locale, Version version, DdSet set)
        {
            if (set.NameRef is null) { return null; }

            string l = TransformLocaleForUrl(locale);
            string v = TransformVersionForUrl(version);
            string s = set.NameRef.ToLowerInvariant();
            var uri = new Uri($"https://dd.b.pvp.net/{v}/{s}/{l}/data/{s}-{l}.json");
            return await FetchAsync<IReadOnlyList<DdCard>>(uri);
        }

        private static string TransformLocaleForUrl(Locale locale)
        {
            return $"{locale.Language}_{locale.Country.ToLower()}";
        }

        private static string TransformVersionForUrl(Version version)
        {
            return string.Join('_', version.Numbers);
        }

        private async Task<T?> FetchAsync<T>(Uri uri) where T : class
        {
            using HttpResponseMessage response = await Client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                using Stream stream = await response.Content.ReadAsStreamAsync();
                try
                {
                    T? result = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
                    return result;
                }
                catch (JsonException)
                {
                    Logger.LogError($"Could not parse the content at {uri} as type {typeof(T)}.");
                    return null;
                }
            }
            else
            {
                Logger.LogError($"{uri} returned status code {response.StatusCode}.");
                return null;
            }
        }

        #region Disposable

        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Client.Dispose();
                }

                _disposedValue = true;
            }
        }

        ~RiotDataDragonFetcher()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
