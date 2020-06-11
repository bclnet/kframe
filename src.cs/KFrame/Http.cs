using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Contoso.Extensions.Caching.MemoryStream;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;

namespace KFrame
{
  public interface IHttp
  {
    Task<T> Execute<T>(HttpRequestMessage requestMessage, CancellationToken? cancellationToken = null);
  }

  class Http : IHttp
  {
    const string TestCacheFile = @"C:\T_\HttpCache.dat";
    readonly HttpClient _client;
    readonly JsonSerializerSettings _jsonSettings;
    public static string LastErrorContent;

    public Http(HttpClient client, JsonSerializerSettings jsonSettings)
    {
      _client = client;
      _jsonSettings = jsonSettings;
    }

    public async Task<T> Execute<T>(HttpRequestMessage requestMessage, CancellationToken? cancellationToken = null)
    {
      var streamCache = (MemoryStreamCache)((HttpCachedClient)_client).Cache;
      streamCache.LoadFromFile(TestCacheFile);
      try
      {
        var response = await _client.SendAsync(requestMessage, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
          LastErrorContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        streamCache.SaveToFile(TestCacheFile);
        return response.Content.Headers.ContentType.MediaType == "application/json"
            ? await Deserialize<T>(response).ConfigureAwait(false)
            : default;
      }
      catch (Exception e)
      {
        throw e;
      }
    }

    async Task<T> Deserialize<T>(HttpResponseMessage response) =>
        JsonSerializer.Create(_jsonSettings).Deserialize<T>(new JsonTextReader(new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false))));
  }
}
