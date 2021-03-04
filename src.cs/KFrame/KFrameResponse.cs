using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KFrame
{
  /// <summary>
  /// _del_
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
    public Dictionary<string, JsonElement> Data { get; set; }
  }
}
