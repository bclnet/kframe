using Contoso.Extensions.Caching.MemoryStream;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;

namespace KFrame
{
  /// <summary>
  /// Default
  /// </summary>
  public static class Default
  {
    /// <summary>
    /// HTTPs the specified serializer options.
    /// </summary>
    /// <param name="serializerOptions">The serializer options.</param>
    /// <returns></returns>
    public static IHttp Http(JsonSerializerOptions serializerOptions = null) => new Http(new HttpCachedClient(StreamCache), serializerOptions ?? SerializerOptions);
    /// <summary>
    /// HTTPs the specified handler.
    /// </summary>
    /// <param name="handler">The handler.</param>
    /// <param name="serializerOptions">The serializer options.</param>
    /// <returns></returns>
    public static IHttp Http(HttpMessageHandler handler, JsonSerializerOptions serializerOptions = null) => new Http(new HttpCachedClient(StreamCache, handler), serializerOptions ?? SerializerOptions);
    /// <summary>
    /// HTTPs the specified handler.
    /// </summary>
    /// <param name="handler">The handler.</param>
    /// <param name="disposeHandler">if set to <c>true</c> [dispose handler].</param>
    /// <param name="serializerOptions">The serializer options.</param>
    /// <returns></returns>
    public static IHttp Http(HttpMessageHandler handler, bool disposeHandler, JsonSerializerOptions serializerOptions = null) => new Http(new HttpCachedClient(StreamCache, handler, disposeHandler), serializerOptions ?? SerializerOptions);

    /// <summary>
    /// Gets the stream cache.
    /// </summary>
    /// <value>
    /// The stream cache.
    /// </value>
    static MemoryStreamCache StreamCache => new MemoryStreamCache(new OptionsWrapper<MemoryStreamCacheOptions>(new MemoryStreamCacheOptions()));

    /// <summary>
    /// Gets the serializer options.
    /// </summary>
    /// <value>
    /// The serializer options.
    /// </value>
    static JsonSerializerOptions SerializerOptions => new JsonSerializerOptions { };
  }
}
