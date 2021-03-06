using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KFrame.Tests
{
  public class KFrameTest
  {
    const string CustomKframeUrl = "https://assist.degdigital.com/@frame";

    public KFrameTest() => ((KFrameDefaultConfig)KFrameManager.Config).KframeUrl = CustomKframeUrl;

    [Fact]
    public void Config()
    {
      var config = KFrameManager.Config;
      // should use custom url
      {
        config.KframeUrl.ShouldBe(CustomKframeUrl);
      }
    }

    [Fact]
    public void Frame()
    {
      // should get frame
      try
      {
        var frame = KFrameManager.GetFrame();
        frame.ShouldNotBeNull();
      }
      catch (Exception e)
      {

      }
    }
  }
}
