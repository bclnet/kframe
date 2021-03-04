using System.Security.Cryptography.X509Certificates;

namespace KFrame
{
  /// <summary>
  /// IKFrameConfig
  /// </summary>
  public interface IKFrameConfig
  {
    /// <summary>
    /// Gets the kframe URL.
    /// </summary>
    /// <value>
    /// The kframe URL.
    /// </value>
    string KframeUrl { get; }
    /// <summary>
    /// Gets the certificate.
    /// </summary>
    /// <value>
    /// The certificate.
    /// </value>
    X509Certificate Certificate { get; }
  }

  /// <summary>
  /// KFrameDefaultConfig
  /// </summary>
  /// <seealso cref="KFrame.IKFrameConfig" />
  public class KFrameDefaultConfig : IKFrameConfig
  {
    /// <summary>
    /// Gets or sets the kframe URL.
    /// </summary>
    /// <value>
    /// The kframe URL.
    /// </value>
    public string KframeUrl { get; set; } = "/@frame";
    /// <summary>
    /// Gets or sets the certificate.
    /// </summary>
    /// <value>
    /// The certificate.
    /// </value>
    public X509Certificate Certificate { get; set; }
  }
}
