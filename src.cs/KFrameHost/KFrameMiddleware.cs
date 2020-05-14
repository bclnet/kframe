using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
      if (req.Path.StartsWithSegments(_options.RequestPath, StringComparison.OrdinalIgnoreCase, out var remaining))
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
      var trace = new KFrameTrace(KFrameTrace.Type.IFrame);
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
      var json = JsonConvert.SerializeObject((object)result);
      await res.WriteAsync(json);
      trace.ContentLength = res.ContentLength ?? json.Length;
      return trace;
    }

    async Task<KFrameTrace> PFrameAsync(HttpRequest req, HttpResponse res, string remaining)
    {
      var trace = new KFrameTrace(KFrameTrace.Type.PFrame);
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
      var json = JsonConvert.SerializeObject(result.Result);
      await res.WriteAsync(json);
      trace.ContentLength = res.ContentLength ?? json.Length;
      return trace;
    }

    async Task<KFrameTrace> ClearAsync(HttpRequest req, HttpResponse res, string remaining)
    {
      var trace = new KFrameTrace(KFrameTrace.Type.Clear);
      await res.WriteAsync(await _repository.ClearAsync(remaining, trace));
      return trace;
    }

    async Task<KFrameTrace> InstallAsync(HttpRequest req, HttpResponse res, string remaining)
    {
      var trace = new KFrameTrace(KFrameTrace.Type.Clear);
      await res.WriteAsync(await _repository.InstallAsync(remaining, trace));
      return trace;
    }

    async Task<KFrameTrace> UninstallAsync(HttpRequest req, HttpResponse res, string remaining)
    {
      var trace = new KFrameTrace(KFrameTrace.Type.Clear);
      await res.WriteAsync(await _repository.UninstallAsync(remaining, trace));
      return trace;
    }

    async Task<KFrameTrace> ReinstallAsync(HttpRequest req, HttpResponse res, string remaining)
    {
      var trace = new KFrameTrace(KFrameTrace.Type.Clear);
      await res.WriteAsync(await _repository.ReinstallAsync(remaining, trace));
      return trace;
    }
  }
}
