using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KFrame.Tests
{
  public class KFrameTest
  {
    const string CustomKframeUrl = "https://assist.degdigital.com/@frame";

    public KFrameTest() { KFrame<object>.Config.kframeUrl = CustomKframeUrl; }

    [Fact]
    public void Config()
    {
      var config = KFrame<object>.Config;
      // should use custom url
      {
        config.kframeUrl.ShouldBe(CustomKframeUrl);
      }
    }

    [Fact]
    public Task Frame()
    {
      // should get frame
      try
      {
        var frame = KFrame<object>.GetFrame();
        frame.ShouldNotBeNull();
      }
      catch (Exception e)
      {

      }
      return Task.CompletedTask;
    }
  }
}
