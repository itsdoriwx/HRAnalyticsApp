using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HRAnalyticsApp.Services
{
    public class JsonFileReader : IFileLoader
    {
        private readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public async IAsyncEnumerable<T> LoadStreamAsync<T>(
            string filePath,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                yield break;
            }

            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

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