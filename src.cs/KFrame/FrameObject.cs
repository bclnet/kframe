using System.Collections.Generic;

namespace KFrame
{
  public class FrameObject : Dictionary<object, Dictionary<string, object>>
  {
    public FrameObject(Dictionary<object, Dictionary<string, object>> frame) : base(frame) { }
  }
}
