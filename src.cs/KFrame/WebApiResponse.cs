using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KFrame
{
  class WebApiResponse
  {
    public long frame { get; set; }
    [JsonExtensionData]
    public JObject Data { get; set; }
  }
}
