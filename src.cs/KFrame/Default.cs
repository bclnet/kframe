using Contoso.Extensions.Caching.FileSystem;
using Newtonsoft.Json;
using System.Net.Http;

namespace KFrame
{
  public static class Default
  {
    public static IHttp Http(JsonSerializerSettings jsonSettings = null) => new Http(new HttpCachedClient(FileSystemCache), jsonSettings ?? SerializerSettings);

    static FileSystemCache FileSystemCache => new FileSystemCache(null);

    static JsonSerializerSettings SerializerSettings => new JsonSerializerSettings { };
  }
}
