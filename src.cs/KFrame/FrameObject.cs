using System.Collections.Generic;

namespace KFrame
{
  public class FrameObject : Dictionary<object, Dictionary<string, object>>
  {
    public FrameObject(Dictionary<object, Dictionary<string, object>> frame) : base(frame) { }
  }

  internal class FrameObjectComparer : IEqualityComparer<FrameObject>
  {
    public static readonly FrameObjectComparer Default = new FrameObjectComparer();
    public bool Equals(FrameObject x, FrameObject y) => false;
    public int GetHashCode(FrameObject obj) => obj.GetHashCode();
  }
}
