using KFrame;
using System;

namespace KFrameConsole
{
  class Program
  {
    const string CustomKframeUrl = "https://assist.degdigital.com/@frame";

    static Program() => ((KFrameDefaultConfig)KFrameManager.Config).KframeUrl = CustomKframeUrl;

    static void Main(string[] args)
    {
      var frame = KFrameManager.GetFrame();

      Console.WriteLine("Hello World!");
    }
  }
}
