using Shouldly;
using Xunit;

namespace KFrame.Tests
{
  public class DefaultTest
  {
    const string CustomKframeUrl = "https://assist.degdigital.com/@frame";

    public DefaultTest() { KFrame<object>.Config.kframeUrl = CustomKframeUrl; }

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
    public void Frame()
    {
      // should get frame
      {
        var frame = KFrame<object>.GetFrame();
        frame.ShouldNotBeNull();
      }
    }
  }
}
