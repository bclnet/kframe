using System.Collections.Generic;

namespace KFrame
{
  /// <summary>
  /// FrameObject
  /// </summary>
  /// <seealso cref="System.Collections.Generic.Dictionary{System.Object, System.Collections.Generic.Dictionary{System.String, System.Object}}" />
  public class FrameObject : Dictionary<object, Dictionary<string, object>>
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="FrameObject"/> class.
    /// </summary>
    /// <param name="frame">The frame.</param>
    public FrameObject(Dictionary<object, Dictionary<string, object>> frame) : base(frame) { }
  }

  internal class FrameObjectComparer : IEqualityComparer<FrameObject>
  {
    public static readonly FrameObjectComparer Default = new FrameObjectComparer();
    public bool Equals(FrameObject x, FrameObject y) => false;
    public int GetHashCode(FrameObject obj) => obj.GetHashCode();
  }
}
