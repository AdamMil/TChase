using System;
using GameLib;
using GameLib.Video;

namespace TriangleChase
{

public sealed class App
{ public static void Main(string[] args)
  { if(args==null || args.Length!=2)
    { Console.WriteLine("Usage: mapconv overlayimage output.png");
    }
    Video.Initialize();

    Color[] clr = new Color[]
    { new Color(0, 0, 0),      // passable, in-front
      new Color(0, 255, 0),    // passable, behind
      new Color(0, 64, 0),     // impassable, normal
      new Color(255, 255, 0),  // impassable, base
      new Color(255, 0, 0),    // spawn point
    };
    byte[] map = new byte[]
    { 0,  // passable, in-front
      1,  // passable, behind
      2,  // impassable, normal
      6,  // impassable, base
      8,  // spawn point
    };
    Surface overlay = new Surface(args[0]), output = new Surface(overlay.Width, overlay.Height, 8);
    overlay.Lock();
    output.Lock();
    for(int y=0,i; y<overlay.Height; y++)
      for(int x=0; x<overlay.Width; x++)
      { Color c = overlay.GetPixel(x, y);
        for(i=0; i<clr.Length; i++)
          if(c==clr[i]) { output.PutPixel(x, y, map[i]); break; }
        if(i==clr.Length) Console.WriteLine("Unknown color {0} at {1},{2}", c, x, y);
      }
    output.Unlock();
    overlay.Unlock();
    
    Color[] pal = new Color[256];
    for(int i=0; i<clr.Length; i++) pal[map[i]] = clr[i];
    output.SetPalette(pal);
    output.Save(args[1], ImageType.Png);

    Video.Deinitialize();
  }
}

}