using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FrameObject = System.Collections.Generic.Dictionary<object, System.Collections.Generic.Dictionary<string, object>>;

namespace KFrame
{
  public class KFrameConfig
  {
    public string kframeUrl { get; set; } = "/@frame";
  }

  public class KFrameManager
  {
    static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    static readonly IHttp _http = Default.Http();

    public static KFrameConfig Config { get; } = new KFrameConfig();

    public static bool IsLoaded { get; } = Frame != null;
    public static IDictionary<string, FrameObject> Frame { get; private set; }
    public static void ClearFrame() { Frame = default; }

    class FrameObjectComparer : IEqualityComparer<FrameObject>
    {
      public static readonly FrameObjectComparer Default = new FrameObjectComparer();
      public bool Equals(FrameObject x, FrameObject y) => false;
      public int GetHashCode(FrameObject obj) => obj.GetHashCode();
    }

    static async Task<IDictionary<string, FrameObject>> LookupFrame(CancellationToken? cancellationToken = null)
    {
      var data = new Dictionary<string, FrameObject>();
      var kframeUrl = Config.kframeUrl;
      var i = await _http.Execute<KFrameResponse[]>(new HttpRequestMessage(HttpMethod.Get, $"{kframeUrl}/i"), cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
      if (i == null || i.Length == 0 || i[0].frame == 0)
        throw new InvalidOperationException("Empty response");
      var p = await _http.Execute<KFrameResponse[]>(new HttpRequestMessage(HttpMethod.Get, $"{kframeUrl}/p/{i[0].frame}"), cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
      for (var idx = 0; idx < p.Length; idx++)
      {
        var ix = i[idx];
        var px = p[idx];
        foreach (var kv in ix.Data)
        {
          var k = kv.Key;
          var iv = kv.Value is JArray ia ? ia.Select(x => x is JObject io ? io.ToObject<Dictionary<string, object>>() : null).ToDictionary(x => x["id"]) : null;
          var pv = px.Data[k] is JArray pa ? pa.Select(x => x is JObject po ? po.ToObject<Dictionary<string, object>>() : null).ToDictionary(x => x["id"]) : null;
          if (iv == null || pv == null)
            continue;
          foreach (var x in px.del.Where(x => x.t == k)) iv.Remove(x.id);
          data[k] = iv.Where(x => !pv.ContainsKey(x.Key)).Concat(pv).ToDictionary(x => x.Key, x => x.Value);
        }
      }
      return data;
    }

    public static IDictionary<string, FrameObject> GetFrame()
    {
      _lock.EnterUpgradeableReadLock();
      try
      {
        if (Frame != null)
          return Frame;
        _lock.EnterWriteLock();
        try { return Frame = Task.Run(() => LookupFrame()).ConfigureAwait(false).GetAwaiter().GetResult(); }
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
  }
}
