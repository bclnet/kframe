namespace KFrame
{
  public interface IKFrameConfig
  {
    string KframeUrl { get; }
  }

  public class KFrameDefaultConfig : IKFrameConfig
  {
    public string KframeUrl { get; set; } = "/@frame";
  }
}
