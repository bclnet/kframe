using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KFrame
{
  public class KFrameConfig
  {
    public string kframeUrl { get; set; } = "/@frame";
  }

  public class KFrame<T> where T : class
  {
    static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    static readonly IHttp _http = Default.Http();

    public static KFrameConfig Config { get; } = new KFrameConfig();

    public static bool IsLoaded { get; } = Frame != null;
    public static T Frame { get; private set; }
    public static void ClearFrame() { Frame = default; }

    static async Task<T> LookupFrame(CancellationToken? cancellationToken = null)
    {
      var kframeUrl = Config.kframeUrl;
      var i = await _http.Execute<WebApiResponse>(new HttpRequestMessage(HttpMethod.Get, $"{kframeUrl}/i"), cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
      var p = await _http.Execute<WebApiResponse>(new HttpRequestMessage(HttpMethod.Get, $"{kframeUrl}/p/{i.frame}"), cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
      return default;
    }

    public static async Task<T> GetFrame()
    {
      _lock.EnterUpgradeableReadLock();
      try
      {
        if (Frame != null)
          return Frame;
        _lock.EnterWriteLock();
        try { return (Frame = await LookupFrame()); }
        finally { _lock.ExitWriteLock(); }
      }
      finally { _lock.ExitUpgradeableReadLock(); }
    }
  }
}