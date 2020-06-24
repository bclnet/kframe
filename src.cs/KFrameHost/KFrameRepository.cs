using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KFrame
{
  /// <summary>
  /// Interface IKFrameRepository
  /// </summary>
  public interface IKFrameRepository
  {
    /// <summary>
    /// Installs the asynchronous.
    /// </summary>
    /// <param name="accessCode">The access code.</param>
    /// <param name="trace">The trace.</param>
    /// <returns>Task&lt;System.String&gt;.</returns>
    Task<string> ClearAsync(string accessCode, KFrameTrace trace);
    /// <summary>
    /// Installs the asynchronous.
    /// </summary>
    /// <param name="accessCode">The access code.</param>
    /// <param name="trace">The trace.</param>
    /// <returns>Task&lt;System.String&gt;.</returns>
    Task<string> InstallAsync(string accessCode, KFrameTrace trace);
    /// <summary>
    /// Uninstalls the asynchronous.
    /// </summary>
    /// <param name="accessCode">The access code.</param>
    /// <param name="trace">The trace.</param>
    /// <returns>Task&lt;System.String&gt;.</returns>
    Task<string> UninstallAsync(string accessCode, KFrameTrace trace);
    /// <summary>
    /// Reinstalls the asynchronous.
    /// </summary>
    /// <param name="accessCode">The access code.</param>
    /// <param name="trace">The trace.</param>
    /// <returns>Task&lt;System.String&gt;.</returns>
    Task<string> ReinstallAsync(string accessCode, KFrameTrace trace);
    /// <summary>
    /// Gets the i frame asynchronous.
    /// </summary>
    /// <param name="trace">The trace.</param>
    /// <returns>Task&lt;System.Object&gt;.</returns>
    Task<object> GetIFrameAsync(KFrameTrace trace);
    /// <summary>
    /// Gets the p frame asynchronous.
    /// </summary>
    /// <param name="iframe">The iframe.</param>
    /// <param name="trace">The trace.</param>
    /// <returns>Task&lt;MemoryCacheResult&gt;.</returns>
    Task<MemoryCacheResult> GetPFrameAsync(long iframe, KFrameTrace trace);
    /// <summary>
    /// Determines whether [has i frame] [the specified etag].
    /// </summary>
    /// <param name="etag">The etag.</param>
    /// <returns><c>true</c> if [has i frame] [the specified etag]; otherwise, <c>false</c>.</returns>
    bool HasPFrame(string etag);
  }

  /// <summary>
  /// Class KFrameRepository.
  /// Implements the <see cref="KFrame.IKFrameRepository" />
  /// </summary>
  /// <seealso cref="KFrame.IKFrameRepository" />
  public class KFrameRepository : IKFrameRepository
  {
    public readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
    readonly IMemoryCache _cache;
    IKFrameSource[] _sources;
    List<KFrameNode> _nodes;

    /// <summary>
    /// Initializes a new instance of the <see cref="KFrameRepository" /> class.
    /// </summary>
    /// <param name="cache">The cache.</param>
    /// <param name="options">The options.</param>
    /// <param name="assemblys">The assemblys.</param>
    public KFrameRepository(IMemoryCache cache, KFrameOptions options, IEnumerable<Assembly> assemblys)
        : this(cache, options, FindSourcesFromAssembly(assemblys)) { }
    /// <summary>
    /// Initializes a new instance of the <see cref="KFrameRepository"/> class.
    /// </summary>
    /// <param name="cache">The cache.</param>
    /// <param name="options">The options.</param>
    /// <param name="sources">The sources.</param>
    public KFrameRepository(IMemoryCache cache, KFrameOptions options, IKFrameSource[] sources)
    {
      _cache = cache ?? throw new ArgumentNullException(nameof(cache));
      Options = options ?? throw new ArgumentNullException(nameof(options));
      Sources = sources ?? throw new ArgumentNullException(nameof(sources));
    }

    #region Cache

    /// <summary>
    /// _del_
    /// Implements the <see cref="KFrame.KFrameRepository.IKey" />
    /// </summary>
    /// <seealso cref="KFrame.KFrameRepository.IKey" />
    public class _del_ : Source.IKey
    {
      public object id { get; set; }
      public string t { get; set; }
    }

    readonly static MemoryCacheRegistration IFrame = new MemoryCacheRegistration(nameof(IFrame), new MemoryCacheEntryOptions
    {
      AbsoluteExpiration = KFrameTiming.IFrameAbsoluteExpiration(),
    }, async (tag, values) =>
    {
      var (parent, trace) = ((KFrameRepository, KFrameTrace))tag;
      // build i-frame
      trace.Action = KFrameTrace.TraceAction.BuildIFrame;
      var results = new List<object>();
      foreach (var node in parent.Nodes)
        results.Add(await node.Source.GetIFrameAsync(node.Chapter, node.FrameSources));
      return results;
    }, "KFrame");

    readonly static MemoryCacheRegistration PFrame = new MemoryCacheRegistration(nameof(PFrame), new MemoryCacheEntryOptions
    {
      AbsoluteExpiration = KFrameTiming.PFrameAbsoluteExpiration(),
    }, async (tag, values) =>
    {
      var frame = new DateTime((long)values[0]);
      var results = new List<object>();
      var checks = new List<FrameCheck>();
      var etags = new List<string>();
      var (parent, trace) = ((KFrameRepository, KFrameTrace))tag;
      // lock as late as possible; competing with cache build
      Thread.Sleep(5);
      await parent.Semaphore.WaitAsync();
      try
      {
        // double lock test
        var value = parent._cache.Get(PFrame.GetName(values));
        if (value != null)
          return MemoryCacheResult.CacheResult;
        // build p-frame
        trace.Action = KFrameTrace.TraceAction.BuildPFrame;
        foreach (var node in parent.Nodes)
        {
          var (data, check, etag) = await node.Source.GetPFrameAsync(node.Chapter, node.FrameSources, frame, true);
          results.Add(data);
          checks.Add(check);
          etags.Add(etag);
        }
        return new MemoryCacheResult(results.ToArray())
        {
          Tag = checks,
          ETag = etags.Count != 0 ? $"\"{string.Join(" ", etags)}\"" : null,
        };
      }
      finally { parent.Semaphore.Release(); }
    }, "KFrame")
    {
      PostEvictionCallback = async (key, value, reason, state) =>
      {
        var (parent, trace) = ((KFrameRepository, KFrameTrace))state;
        var iframe = (MemoryCacheResult)parent._cache.Get(IFrame.Name);
        if (iframe == null || !(iframe.Result is List<object> iframeResult) || iframeResult.Count == 0)
          return;
        var frame = (long)((dynamic)iframeResult[0]).frame;
        var pframeKey = PFrame.GetName(frame);
        if (pframeKey != (string)key)
          return;
        // lock as soon as possible; competing with cache build
        await parent.Semaphore.WaitAsync();
        try
        {
          // double lock test
          if (parent._cache.Contains((string)key))
            return;
          if (!(value is MemoryCacheResult result) || !(result.Tag is List<FrameCheck> checks))
            return;
          var i = 0;
          foreach (var node in parent.Nodes)
          {
            var prevCheck = checks[i++];
            var (data, check, etag) = await node.Source.GetPFrameAsync(node.Chapter, node.FrameSources, prevCheck.Frame, false);
            if (check != prevCheck)
              return;
          }
          parent._cache.Set(key, value, new MemoryCacheEntryOptions().SetAbsoluteExpiration(KFrameTiming.PFramePolling()));
        }
        finally { parent.Semaphore.Release(); }
      }
    };


    /// <summary>
    /// Class Check.
    /// </summary>
    public class FrameCheck
    {
      public DateTime Frame;
      public int[] Keys;
      public DateTime MaxDate;

      public override bool Equals(object obj) => obj is FrameCheck b && Keys.SequenceEqual(b.Keys) && MaxDate == b.MaxDate;
      public override int GetHashCode() => Frame.GetHashCode() ^ Keys.GetHashCode() ^ MaxDate.GetHashCode();
      public static bool operator ==(FrameCheck a, FrameCheck b) => a.Equals(b);
      public static bool operator !=(FrameCheck a, FrameCheck b) => !a.Equals(b);
    }

    readonly static MemoryCacheRegistration MergedFrame = new MemoryCacheRegistration(nameof(MergedFrame), 10, (tag, values) =>
    {
      var parent = (KFrameRepository)tag;
      var iframe = (IDictionary<string, object>)parent._cache.Get<dynamic>(IFrame, parent);
      var pframe = (IDictionary<string, object>)parent._cache.GetResult(PFrame, parent, (long)iframe["frame"]).Result;
      var dels = (List<_del_>)pframe["del"];
      var result = (IDictionary<string, object>)new ExpandoObject();
      foreach (var source in parent.Sources)
      {
        var kps = ((IEnumerable<object>)iframe[source.Param.key]).Cast<Source.IKey>().ToList();
        var ips = ((IEnumerable<object>)pframe[source.Param.key]).Cast<Source.IKey>().ToList();
        if (kps.Count == 0 && ips.Count == 0)
          continue;
        var ipsdelsById = dels.Where(x => x.t == source.Param.key).ToDictionary(x => x.id);
        var ipsById = ips.ToDictionary(x => x.id);
        var p = kps.Where(x => !ipsdelsById.ContainsKey(x.id) && !ipsById.ContainsKey(x.id)).Union(ips).ToList();
        result.Add(source.Param.key, p.ToDictionary(x => x.id));
      }
      return (dynamic)result;
    }, "KFrame");

    #endregion

    /// <summary>
    /// Gets or sets the sources.
    /// </summary>
    /// <value>The sources.</value>
    public IKFrameSource[] Sources
    {
      get => _sources;
      set
      {
        _sources = value ?? throw new ArgumentNullException(nameof(value));
        _nodes = null;
      }
    }

    List<KFrameNode> Nodes
    {
      get
      {
        if (_nodes != null)
          return _nodes;
        var nodes = new List<KFrameNode>();
        // db-source
        var dbFrameSources = Sources.OfType<IKFrameDbSource>().ToArray();
        if (dbFrameSources.Length > 0)
        {
          var dbSource = Options.DbSource ?? throw new InvalidOperationException($"{nameof(KFrameOptions.DbSource)} not set");
          nodes.Add(new KFrameNode(dbSource, dbFrameSources));
        }
        // kv-source
        var kvFrameSources = Sources.OfType<IKFrameKvSource>().ToArray();
        if (kvFrameSources.Length > 0)
        {
          var kvSource = Options.KvSource ?? throw new InvalidOperationException($"{nameof(KFrameOptions.KvSource)} not set");
          nodes.Add(new KFrameNode(kvSource, kvFrameSources));
        }
        return _nodes = nodes;
      }
    }

    /// <summary>
    /// Gets or sets the options.
    /// </summary>
    /// <value>The options.</value>
    public KFrameOptions Options { get; set; }

    bool ValidAccessCode(string accessCode, KFrameTrace trace, out string message)
    {
      if (!string.IsNullOrEmpty(Options.AccessToken) && $"/{Options.AccessToken}" != accessCode)
      {
        message = "Invalid Access Token";
        trace.AccessDenied = true;
        return false;
      }
      message = null;
      return true;
    }

    /// <summary>
    /// clear as an asynchronous operation.
    /// </summary>
    /// <param name="accessCode">The access code.</param>
    /// <param name="trace">The trace.</param>
    /// <returns>Task&lt;System.String&gt;.</returns>
    public async Task<string> ClearAsync(string accessCode, KFrameTrace trace)
    {
      if (!ValidAccessCode(accessCode, trace, out var message))
        return message;
      var b = new StringBuilder();
      foreach (var node in Nodes)
        b.Append(await node.Source.ClearAsync(node.Chapter, node.FrameSources));
      _cache.Touch("KFrame");
      return b.ToString();
    }

    /// <summary>
    /// install as an asynchronous operation.
    /// </summary>
    /// <param name="accessCode">The access code.</param>
    /// <param name="trace">The trace.</param>
    /// <returns>Task&lt;System.String&gt;.</returns>
    /// <exception cref="System.InvalidOperationException"></exception>
    /// <exception cref="System.InvalidOperationException"></exception>
    public async Task<string> InstallAsync(string accessCode, KFrameTrace trace)
    {
      if (!ValidAccessCode(accessCode, trace, out var message))
        return message;
      var b = new StringBuilder();
      foreach (var node in Nodes)
        b.Append(await node.Source.InstallAsync(node.Chapter, node.FrameSources));
      return b.ToString();
    }

    /// <summary>
    /// uninstall as an asynchronous operation.
    /// </summary>
    /// <param name="accessCode">The access code.</param>
    /// <returns>Task&lt;System.String&gt;.</returns>
    /// <exception cref="System.InvalidOperationException"></exception>
    /// <exception cref="System.InvalidOperationException"></exception>
    public async Task<string> UninstallAsync(string accessCode, KFrameTrace trace)
    {
      if (!ValidAccessCode(accessCode, trace, out var message))
        return message;
      var b = new StringBuilder();
      foreach (var node in Nodes)
        b.Append(await node.Source.UninstallAsync(node.Chapter, node.FrameSources));
      return b.ToString();
    }

    /// <summary>
    /// reinstall as an asynchronous operation.
    /// </summary>
    /// <param name="accessCode">The access code.</param>
    /// <param name="trace">The trace.</param>
    /// <returns>Task&lt;System.String&gt;.</returns>
    public async Task<string> ReinstallAsync(string accessCode, KFrameTrace trace)
    {
      if (!ValidAccessCode(accessCode, trace, out var message))
        return message;
      var b = new StringBuilder();
      b.Append(await UninstallAsync(accessCode, trace));
      b.Append(await InstallAsync(accessCode, trace));
      return b.ToString();
    }

    /// <summary>
    /// get i frame as an asynchronous operation.
    /// </summary>
    /// <param name="trace">The trace.</param>
    /// <returns>Task&lt;dynamic&gt;.</returns>
    public async Task<dynamic> GetIFrameAsync(KFrameTrace trace) => await _cache.GetAsync<dynamic>(IFrame, (this, trace));

    /// <summary>
    /// get p frame as an asynchronous operation.
    /// </summary>
    /// <param name="iframe">The iframe.</param>
    /// <param name="trace">The trace.</param>
    /// <returns>Task&lt;MemoryCacheResult&gt;.</returns>
    public async Task<MemoryCacheResult> GetPFrameAsync(long iframe, KFrameTrace trace) => await _cache.GetResultAsync(PFrame, (this, trace), iframe);

    /// <summary>
    /// Determines whether [has p frame] [the specified etag].
    /// </summary>
    /// <param name="etag">The etag.</param>
    /// <returns><c>true</c> if [has p frame] [the specified etag]; otherwise, <c>false</c>.</returns>
    public bool HasPFrame(string etag) => _cache.Contains(PFrame, etag);

    /// <summary>
    /// get merged frame as an asynchronous operation.
    /// </summary>
    /// <param name="trace">The trace.</param>
    /// <returns>Task&lt;dynamic&gt;.</returns>
    public async Task<dynamic> GetMergedFrameAsync(KFrameTrace trace) => await _cache.GetAsync<dynamic>(MergedFrame, (this, trace));

    /// <summary>
    /// Finds the sources from assembly.
    /// </summary>
    /// <param name="assemblysToScan">The assemblys to scan.</param>
    /// <param name="condition">The condition.</param>
    /// <returns>IReferenceSource[].</returns>
    public static IKFrameSource[] FindSourcesFromAssembly(IEnumerable<Assembly> assemblysToScan, Predicate<Type> condition) =>
        assemblysToScan.SelectMany(a => a.GetTypes().Where(t => condition(t))
            .Select(t => (IKFrameSource)Activator.CreateInstance(t))).ToArray();

    /// <summary>
    /// Finds the sources from assembly.
    /// </summary>
    /// <param name="assemblysToScan">The assemblys to scan.</param>
    /// <param name="excludes">The excludes.</param>
    /// <returns>IReferenceSource[].</returns>
    public static IKFrameSource[] FindSourcesFromAssembly(IEnumerable<Assembly> assemblysToScan, params Type[] excludes) =>
        FindSourcesFromAssembly(assemblysToScan, x => !x.IsAbstract && !x.IsInterface && typeof(IKFrameSource).IsAssignableFrom(x) && !excludes.Contains(x));
  }
}
