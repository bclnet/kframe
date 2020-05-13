using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KFrame
{
  public interface IHttp
  {
    Task<T> Execute<T>(HttpRequestMessage requestMessage, CancellationToken? cancellationToken = null);
  }

  class Http : IHttp
  {
    readonly HttpClient _client;
    readonly JsonSerializerSettings _jsonSettings;

    public Http(HttpClient client, JsonSerializerSettings jsonSettings)
    {
      _client = client;
      _jsonSettings = jsonSettings;
    }

    public async Task<T> Execute<T>(HttpRequestMessage requestMessage, CancellationToken? cancellationToken = null)
    {
      var response = await _client.SendAsync(requestMessage, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
      response.EnsureSuccessStatusCode();
      return response.Content.Headers.ContentType.MediaType == "application/json"
          ? await Deserialize<T>(response).ConfigureAwait(false)
          : default;
    }

    async Task<T> Deserialize<T>(HttpResponseMessage response) =>
        JsonSerializer.Create(_jsonSettings).Deserialize<T>(new JsonTextReader(new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false))));
  }
}
