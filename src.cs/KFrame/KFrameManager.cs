using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KFrame
{
  /// <summary>
  /// KFrameManager
  /// </summary>
  public class KFrameManager
  {
    static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    static readonly IHttp _defaultHttp = Default.Http();
    static readonly ConcurrentDictionary<X509Certificate, IHttp> _http = new ConcurrentDictionary<X509Certificate, IHttp>();

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    /// <value>
    /// The configuration.
    /// </value>
    public static IKFrameConfig Config { get; set; } = new KFrameDefaultConfig();

    /// <summary>
    /// Gets a value indicating whether this instance is loaded.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is loaded; otherwise, <c>false</c>.
    /// </value>
    public static bool IsLoaded { get; } = Frame != null;
    /// <summary>
    /// Gets the frame.
    /// </summary>
    /// <value>
    /// The frame.
    /// </value>
    public static IDictionary<string, FrameObject> Frame { get; private set; }
    /// <summary>
    /// Gets the frame date.
    /// </summary>
    /// <value>
    /// The frame date.
    /// </value>
    public static DateTime FrameDate { get; private set; }
    /// <summary>
    /// Clears the frame.
    /// </summary>
    public static void ClearFrame() { Frame = default; }

    static async Task<IDictionary<string, FrameObject>> LookupFrame(CancellationToken? cancellationToken = null)
    {
      var data = new Dictionary<string, FrameObject>();
      var kframeUrl = Config.KframeUrl;
      var certificate = Config.Certificate;
      
      var http = certificate == null ? _defaultHttp : _http.GetOrAdd(certificate, cert =>
      {
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(cert);
        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        handler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
        return Default.Http(handler);
      });
      var i = await http.Execute<KFrameResponse[]>(new HttpRequestMessage(HttpMethod.Get, $"{kframeUrl}/i"), cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
      if (i == null || i.Length == 0 || i[0].frame == 0)
        throw new InvalidOperationException("Empty response");
      var p = await http.Execute<KFrameResponse[]>(new HttpRequestMessage(HttpMethod.Get, $"{kframeUrl}/p/{i[0].frame}"), cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
      for (var idx = 0; idx < p.Length; idx++)
      {
        var ix = i[idx];
        var px = p[idx];
        foreach (var kv in ix.Data)
        {
          var k = kv.Key;
          var iv = kv.Value is JsonElement ia && ia.ValueKind == JsonValueKind.Array ? UnboxJson(ia.EnumerateArray()) : null;
          var pv = px.Data[k] is JsonElement pa && pa.ValueKind == JsonValueKind.Array ? UnboxJson(pa.EnumerateArray()) : null;
          if (iv == null || pv == null)
            continue;
          foreach (var x in px.del.Where(x => x.t == k)) iv.Remove(x.id);
          data[k] = new FrameObject(iv.Where(x => !pv.ContainsKey(x.Key)).Concat(pv).ToDictionary(x => x.Key, x => x.Value));
        }
      }
      return data;
    }

    static Dictionary<object, Dictionary<string, object>> UnboxJson(JsonElement.ArrayEnumerator value) => value
        .Select(x => x.ValueKind == JsonValueKind.Object ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(x.GetRawText()) : null)?
        .ToDictionary(x => UnboxJson(x["id"]), x => x.ToDictionary(y => y.Key, y => UnboxJson(y.Value)));

    static object UnboxJson(JsonElement value)
      => value.ValueKind == JsonValueKind.String ? value.GetString()
      : value.ValueKind == JsonValueKind.Number ? value.GetInt64()
      : (object)value;

    /// <summary>
    /// Gets the frame.
    /// </summary>
    /// <returns></returns>
    public static IDictionary<string, FrameObject> GetFrame()
    {
      _lock.EnterUpgradeableReadLock();
      try
      {
        if (Frame != null)
          return Frame;
        _lock.EnterWriteLock();
        try
        {
          Frame = Task.Run(() => LookupFrame()).ConfigureAwait(false).GetAwaiter().GetResult();
          FrameDate = DateTime.UtcNow;
          return Frame;
        }
        catch (Exception e)
        {
          Console.WriteLine(e);
          Console.WriteLine(Http.LastErrorContent);
          throw;
        }
        finally { _lock.ExitWriteLock(); }
      }
      finally { _lock.ExitUpgradeableReadLock(); }
    }

    /// <summary>
    /// Checks the frame.
    /// </summary>
    /// <param name="frame">The frame.</param>
    /// <param name="expires">The expires.</param>
    /// <returns></returns>
    public static IDictionary<string, FrameObject> CheckFrame(IDictionary<string, FrameObject> frame, TimeSpan expires)
    {
      if (FrameDate + expires <= DateTime.UtcNow)
        return Frame;
      Frame = default;
      return GetFrame();
    }

    /// <summary>
    /// Finds the certificate.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="findType">Type of the find.</param>
    /// <param name="storeName">Name of the store.</param>
    /// <param name="location">The location.</param>
    /// <returns></returns>
    public static X509Certificate FindCertificate(object value, X509FindType findType = X509FindType.FindBySubjectName, string storeName = null, StoreLocation location = StoreLocation.CurrentUser)
    {
      if (value == null || (value is string valueAsString && valueAsString.Length == 0))
        return null;
      using (var store = new X509Store(storeName ?? "MY", location))
      {
        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
        return store.Certificates.Find(findType, value, false).Cast<X509Certificate2>()
            .Where(x => x.NotBefore <= DateTime.Now)
            .OrderBy(x => x.NotAfter)
            .FirstOrDefault();
      }
    }
  }
}
