using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace KFrame
{
  /// <summary>
  /// Class _del_.
  /// </summary>
  class _del_
  {
    public object id { get; set; }
    public string t { get; set; }
  }

  class KFrameResponse
  {
    public long frame { get; set; }
    public List<_del_> del { get; set; }
    [JsonExtensionData]
    public JObject Data { get; set; }
  }
}
