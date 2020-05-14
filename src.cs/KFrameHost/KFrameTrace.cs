using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace KFrame
{
  public class KFrameTrace
  {
    public enum Type { IFrame, PFrame, Clear, Install, Uninstall, Reinstall }

    public KFrameTrace(Type method) => Method = method;

    public Type Method;
    public bool AccessDenied;
    public bool Rebuild;
    public string FromETag;
    public int StatusCode;
    public string ETag;
    public long[] IFrames;
    public long? ContentLength;
    public TimeSpan? MaxAge;
    public DateTimeOffset? Expires;

    public string ContentSize
    {
      get
      {
        if (ContentLength == null)
          return null;
        var value = ContentLength.Value;
        var radix = (int)Math.Floor((decimal)(value.ToString().Length - 1) / 3);
        if (radix > 0) return $"{Math.Round(value / (decimal)(1 << 10 * radix), 2)} {"  KBMBGB".Substring(radix << 1, 2)}";
        if (value == 1) return "1 byte";
        return $"{value} bytes";
      }
    }

    public void Log(ILogger log)
    {
      if (AccessDenied)
      {
        log.LogWarning($"{Method} Access Denied");
        return;
      }
      switch (Method)
      {
        case Type.IFrame:
        case Type.PFrame:
          var b = new StringBuilder($"{Method} Returned:\n");
          if (StatusCode != 0)
          {
            if (FromETag != null) b.AppendLine($"from-etag: {FromETag}");
            b.AppendLine($"return: {StatusCode}");
            log.LogInformation(b.ToString());
            return;
          }
          b.AppendLine($"iframes: {string.Join(",", IFrames)}");
          if (Rebuild) b.AppendLine($"*rebuild*");
          if (ETag != null) b.AppendLine($"etag: {ETag}");
          if (MaxAge != null) b.AppendLine($"max-age: {MaxAge}");
          if (Expires != null) b.AppendLine($"expires: {Expires}");
          if (ContentLength != null) b.AppendLine($"size: {ContentSize}");
          log.LogInformation(b.ToString());
          return;
        case Type.Clear: log.LogInformation($"KFrame Cleared"); return;
        case Type.Install: log.LogInformation($"KFrame Installed"); return;
        case Type.Uninstall: log.LogInformation($"KFrame Uninstalled"); return;
        case Type.Reinstall: log.LogInformation($"KFrame Reinstalled"); return;
        default: log.LogWarning($"{Method}"); return;
      }
    }
  }
}
