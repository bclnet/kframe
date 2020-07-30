using Contoso.Extensions.Caching.MemoryStream;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;

namespace KFrame
{
  public static class Default
  {
    public static IHttp Http(JsonSerializerOptions serializerOptions = null) => new Http(new HttpCachedClient(StreamCache), serializerOptions ?? SerializerOptions);

    static MemoryStreamCache StreamCache => new MemoryStreamCache(new OptionsWrapper<MemoryStreamCacheOptions>(new MemoryStreamCacheOptions()));

    static JsonSerializerOptions SerializerOptions => new JsonSerializerOptions { };
  }
}
