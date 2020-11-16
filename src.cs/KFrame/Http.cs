using Contoso.Extensions.Caching.MemoryStream;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KFrame
{
  /// <summary>
  /// IHttp
  /// </summary>
  public interface IHttp
  {
    /// <summary>
    /// Executes the specified request message.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="requestMessage">The request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<T> Execute<T>(HttpRequestMessage requestMessage, CancellationToken? cancellationToken = null);
  }

  class Http : IHttp
  {
    //const string TestCacheFile = @"C:\T_\{0}.dat";
    readonly HttpClient _client;
    readonly JsonSerializerOptions _serializerOptions;
    public static string LastErrorContent;

    /// <summary>
    /// Initializes a new instance of the <see cref="Http"/> class.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="serializerOptions">The serializer options.</param>
    public Http(HttpClient client, JsonSerializerOptions serializerOptions)
    {
      _client = client;
      _serializerOptions = serializerOptions;
    }

    /// <summary>
    /// Executes the specified request message.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="requestMessage">The request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task<T> Execute<T>(HttpRequestMessage requestMessage, CancellationToken? cancellationToken = null)
    {
      //var streamCache = (MemoryStreamCache)((HttpCachedClient)_client).Cache;
      //streamCache.LoadFromFile(string.Format(TestCacheFile, requestMessage.RequestUri.LocalPath.Replace("/", "_")));
      try
      {
        var response = await _client.SendAsync(requestMessage, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
          LastErrorContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        //streamCache.SaveToFile(string.Format(TestCacheFile, requestMessage.RequestUri.LocalPath.Replace("/", "_")));
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
