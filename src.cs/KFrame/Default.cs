using Contoso.Extensions.Caching.MemoryStream;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http;

namespace KFrame
{
  public static class Default
  {
    public static IHttp Http(JsonSerializerSettings jsonSettings = null) => new Http(new HttpCachedClient(StreamCache), jsonSettings ?? SerializerSettings);

    static MemoryStreamCache StreamCache => new MemoryStreamCache(new OptionsWrapper<MemoryStreamCacheOptions>(new MemoryStreamCacheOptions()));

    static JsonSerializerSettings SerializerSettings => new JsonSerializerSettings { };
  }
}
