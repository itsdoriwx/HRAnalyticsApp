using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;

namespace HRAnalyticsApp.Services
{
    public class JsonBinFileReader : IFileLoader
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public async IAsyncEnumerable<T> LoadStreamAsync<T>(string jsonBinUrl, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!jsonBinUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var localReader = new JsonFileReader();
                await foreach (var item in localReader.LoadStreamAsync<T>(jsonBinUrl, cancellationToken))
                {
                    yield return item;
                }
                yield break;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, jsonBinUrl);
            request.Headers.Add("X-Bin-Meta", "false"); // прибраємо обгортку з метаданими

            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var asyncEnumerable = JsonSerializer.DeserializeAsyncEnumerable<T>(
                stream,
                DefaultOptions,
                cancellationToken);

            await foreach (var item in asyncEnumerable.WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (item != null)
                {
                    yield return item;
                }
            }
        }
    }
}