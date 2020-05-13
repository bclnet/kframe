using Newtonsoft.Json;
using System.Net.Http;

namespace KFrame
{
  public static class Default
  {
    public static IHttp Http(JsonSerializerSettings jsonSettings = null) => new Http(new HttpClient(), jsonSettings ?? SerializerSettings());

    static JsonSerializerSettings SerializerSettings() => new JsonSerializerSettings { };
  }
}
