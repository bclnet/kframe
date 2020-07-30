using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KFrame
{
  public class KFrameManager
  {
    static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    static readonly IHttp _http = Default.Http();

    public static IKFrameConfig Config { get; set; } = new KFrameDefaultConfig();

    public static bool IsLoaded { get; } = Frame != null;
    public static IDictionary<string, FrameObject> Frame { get; private set; }
    public static DateTime FrameDate { get; private set; }
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
      var kframeUrl = Config.KframeUrl;
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

    public static IDictionary<string, FrameObject> CheckFrame(IDictionary<string, FrameObject> frame, TimeSpan expires)
    {
      if (FrameDate + expires <= DateTime.UtcNow)
        return Frame;
      Frame = default;
      return GetFrame();
    }
  }
}
