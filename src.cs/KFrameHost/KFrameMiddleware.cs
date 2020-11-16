using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;

namespace KFrame
{
  /// <summary>
  /// Class KFrameMiddleware.
  /// </summary>
  public class KFrameMiddleware
  {
    readonly RequestDelegate _next;
    readonly KFrameOptions _options;
    readonly IMemoryCache _cache;
    readonly KFrameRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="KFrameMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next.</param>
    /// <param name="options">The options.</param>
    /// <param name="cache">The cache.</param>
    public KFrameMiddleware(RequestDelegate next, KFrameOptions options, IMemoryCache cache, IKFrameSource[] sources)
    {
      _next = next;
      _options = options;
      _cache = cache;
      _repository = new KFrameRepository(_cache, _options, sources);
    }

    /// <summary>
    /// invoke as an asynchronous operation.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>Task.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
      var req = context.Request; var res = context.Response;
      if ((!string.IsNullOrEmpty(_options.RequestPath) && req.Path.StartsWithSegments(_options.RequestPath, StringComparison.OrdinalIgnoreCase, out var remaining)) ||
        (!string.IsNullOrEmpty(_options.RequestSPath) && req.Path.StartsWithSegments(_options.RequestSPath, StringComparison.OrdinalIgnoreCase, out remaining)))
      {
        KFrameTrace trace = null;
        if (remaining.StartsWithSegments("/i", StringComparison.OrdinalIgnoreCase, out var remaining2)) trace = await IFrameAsync(req, res, remaining2);
        else if (remaining.StartsWithSegments("/p", StringComparison.OrdinalIgnoreCase, out remaining2)) trace = await PFrameAsync(req, res, remaining2);
        else if (remaining.StartsWithSegments("/clear", StringComparison.OrdinalIgnoreCase, out remaining2)) trace = await ClearAsync(req, res, remaining2);
        else if (remaining.StartsWithSegments("/install", StringComparison.OrdinalIgnoreCase, out remaining2)) trace = await InstallAsync(req, res, remaining2);
        else if (remaining.StartsWithSegments("/uninstall", StringComparison.OrdinalIgnoreCase, out remaining2)) trace = await UninstallAsync(req, res, remaining2);
        else if (remaining.StartsWithSegments("/reinstall", StringComparison.OrdinalIgnoreCase, out remaining2)) trace = await ReinstallAsync(req, res, remaining2);
        else await _next(context);
        if (_options.Log != null)
          trace?.Log(_options.Log);
        return;
      }
      await _next(context);
    }

    async Task<KFrameTrace> IFrameAsync(HttpRequest req, HttpResponse res, string remaining)
    {
      var trace = new KFrameTrace(KFrameTrace.TraceMethod.IFrame);
      res.Clear();
      var etag = req.Headers["If-None-Match"];
      if (!string.IsNullOrEmpty(etag) && etag == "\"iframe\"")
      {
        res.StatusCode = trace.StatusCode = (int)HttpStatusCode.NotModified;
        trace.FromETag = etag.ToString();
        return trace;
      }
      var result = (List<object>)(await _repository.GetIFrameAsync(trace));
      trace.IFrames = result.Select(x => (long)((dynamic)x).frame).ToArray();
      res.StatusCode = (int)HttpStatusCode.OK;
      res.Headers.Add("Access-Control-Allow-Origin", "*");
      res.ContentType = "application/json";
      var typedHeaders = res.GetTypedHeaders();
      typedHeaders.CacheControl = new CacheControlHeaderValue { Public = true, MaxAge = trace.MaxAge = KFrameTiming.IFrameCacheMaxAge() };
      typedHeaders.Expires = trace.Expires = KFrameTiming.IFrameCacheExpires();
      typedHeaders.ETag = new EntityTagHeaderValue("\"iframe\"");
      trace.ETag = "\"iframe\"";
      var json = JsonSerializer.Serialize(result);
      await res.WriteAsync(json);
      trace.ContentLength = res.ContentLength ?? json.Length;
      return trace;
    }

    async Task<KFrameTrace> PFrameAsync(HttpRequest req, HttpResponse res, string remaining)
    {
      var trace = new KFrameTrace(KFrameTrace.TraceMethod.PFrame);
      res.Clear();
      if (string.IsNullOrEmpty(remaining) || remaining[0] != '/')
      {
        res.StatusCode = trace.StatusCode = (int)HttpStatusCode.NotFound;
        return trace;
      }
      if (!long.TryParse(remaining.Substring(1), out var iframe))
      {
        res.StatusCode = trace.StatusCode = (int)HttpStatusCode.NotFound;
        return trace;
      }
      trace.IFrames = new[] { iframe };
      var etag = req.Headers["If-None-Match"];
      if (!string.IsNullOrEmpty(etag) && _repository.HasPFrame(etag))
      {
        res.StatusCode = trace.StatusCode = (int)HttpStatusCode.NotModified;
        trace.FromETag = etag.ToString();
        return trace;
      }
      var result = await _repository.GetPFrameAsync(iframe, trace);
      res.StatusCode = (int)HttpStatusCode.OK;
      res.ContentType = "application/json";
      res.Headers.Add("Access-Control-Allow-Origin", "*");
      var typedHeaders = res.GetTypedHeaders();
      typedHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
      //typedHeaders.Expires = DateTime.Today.ToUniversalTime().AddDays(1);
      typedHeaders.ETag = new EntityTagHeaderValue(result.ETag);
      trace.ETag = result.ETag;
      var json = JsonSerializer.Serialize(result.Result);
      await res.WriteAsync(json);
      trace.ContentLength = res.ContentLength ?? json.Length;
      return trace;
    }

    async Task<KFrameTrace> ClearAsync(HttpRequest req, HttpResponse res, string remaining)
    {
      var trace = new KFrameTrace(KFrameTrace.TraceMethod.Clear);
      await res.WriteAsync(await _repository.ClearAsync(remaining, trace));
      return trace;
    }

    async Task<KFrameTrace> InstallAsync(HttpRequest req, HttpResponse res, string remaining)
    {
      var trace = new KFrameTrace(KFrameTrace.TraceMethod.Clear);
      await res.WriteAsync(await _repository.InstallAsync(remaining, trace));
      return trace;
    }

    async Task<KFrameTrace> UninstallAsync(HttpRequest req, HttpResponse res, string remaining)
    {
      var trace = new KFrameTrace(KFrameTrace.TraceMethod.Clear);
      await res.WriteAsync(await _repository.UninstallAsync(remaining, trace));
      return trace;
    }

    async Task<KFrameTrace> ReinstallAsync(HttpRequest req, HttpResponse res, string remaining)
    {
      var trace = new KFrameTrace(KFrameTrace.TraceMethod.Clear);
      await res.WriteAsync(await _repository.ReinstallAsync(remaining, trace));
      return trace;
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

    /// <summary>
    /// Compares the certificate.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns></returns>
    public static bool CompareCertificate(X509Certificate left, X509Certificate right) =>
        left.Subject == right.Subject &&
        left.Issuer == right.Issuer &&
        (left is X509Certificate2 left2) && (right is X509Certificate2 right2) &&
        left2.Thumbprint == right2.Thumbprint;

    /// <summary>
    /// Verifies the certificate.
    /// </summary>
    /// <param name="caCertificate">The ca certificate.</param>
    /// <param name="certificate">The certificate.</param>
    /// <returns></returns>
    public static bool VerifyCertificate(X509Certificate2 caCertificate, X509Certificate2 certificate)
    {
      var trustedChain = new X509Chain();
      trustedChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
      trustedChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority | X509VerificationFlags.IgnoreNotTimeValid; //X509VerificationFlags.NoFlag;
      trustedChain.ChainPolicy.ExtraStore.Add(caCertificate);
      if (!trustedChain.Build(certificate))
        return false;
      var valid = trustedChain.ChainElements
          .Cast<X509ChainElement>()
          .Any(x => x.Certificate.Thumbprint == caCertificate.Thumbprint);
      return valid;
    }
  }
}
