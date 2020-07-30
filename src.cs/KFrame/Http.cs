using Contoso.Extensions.Caching.MemoryStream;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
    readonly JsonSerializerOptions _serializerOptions;
    public static string LastErrorContent;

    public Http(HttpClient client, JsonSerializerOptions serializerOptions)
    {
      _client = client;
      _serializerOptions = serializerOptions;
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
        //streamCache.SaveToFile(TestCacheFile);
        return await DeserializeAsync<T>(response).ConfigureAwait(false);
      }
      catch (Exception e)
      {
        throw e;
      }
    }

    async ValueTask<T> DeserializeAsync<T>(HttpResponseMessage response) =>
        await JsonSerializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), _serializerOptions);
  }
}
