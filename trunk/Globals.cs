using System;
using System.Drawing;
using GameLib.Video;
using GameLib.Fonts;
using GameLib.Mathematics;

namespace TriangleChase
{

public enum Sprite
{ CannonBall, Grenade,
  Explosion1, Explosion2, Explosion3, Explosion4, Explosion5, Explosion6, Explosion7, Explosion8,
  Num
}

public class Globals
{ static Globals()
  { for(int i=0; i<256; i++) vects[i] = new VectorF(0, -1).RotatedZ(i);
    Font.RenderStyle = RenderStyle.Blended;
  }

  public static string  DataPath
  { get { return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)+'/'
+"../../"; } // TODO: fixme
  }
  public static string MapPath { get { return DataPath; } }
  public static string SpritePath { get { return DataPath; } }

  public static Random  Random = new Random();
  public static TrueTypeFont Font;

  public static VectorF Vector(int angle) { return vects[angle&0xFF]; }
  public static Surface Sprite(TriangleChase.Sprite which) { return sprites[(int)which]; }

  public static void InitGraphics()
  { string[] fn = new string[(int)TriangleChase.Sprite.Num]
    { "cannonball.png", "grenade.png",
      "exp1.png", "exp2.png", "exp3.png", "exp4.png", "exp5.png", "exp6.png", "exp7.png", "exp8.png"
    };
    for(int i=0; i<sprites.Length; i++)
    { sprites[i] = new Surface(SpritePath+fn[i]).CloneDisplay(false);
      sprites[i].SetColorKey(Colors.Transparent);
    }
    
    Font = new TrueTypeFont(DataPath+"arial.ttf", 10);
  }

  static VectorF[] vects = new VectorF[256];
  static Surface[] sprites = new Surface[(int)TriangleChase.Sprite.Num];
}

public class Colors
{ Colors() { }
  public static readonly Color Black     = Color.FromArgb(0, 0, 0);
  public static readonly Color DkGrey    = Color.FromArgb(96, 96, 96);
  public static readonly Color LtGrey    = Color.FromArgb(128, 128, 128);
  public static readonly Color White     = Color.FromArgb(255, 255, 255);
  public static readonly Color DkRed     = Color.FromArgb(128, 0, 0);
  public static readonly Color Red       = Color.FromArgb(192, 0, 0);
  public static readonly Color LtRed     = Color.FromArgb(255, 0, 0);
  public static readonly Color DkGreen   = Color.FromArgb(0, 128, 0);
  public static readonly Color Green     = Color.FromArgb(0, 192, 0);
  public static readonly Color LtGreen   = Color.FromArgb(0, 255, 0);
  public static readonly Color DkBlue    = Color.FromArgb(0, 0, 192);
  public static readonly Color Blue      = Color.FromArgb(0, 64, 192);
  public static readonly Color LtBlue    = Color.FromArgb(0, 128, 255);
  public static readonly Color DkMagenta = Color.FromArgb(128, 0, 128);
  public static readonly Color LtMagenta = Color.FromArgb(255, 0, 255);
  public static readonly Color DkCyan    = Color.FromArgb(0, 128, 128);
  public static readonly Color Cyan      = Color.FromArgb(0, 192, 192);
  public static readonly Color LtCyan    = Color.FromArgb(0, 255, 255);
  public static readonly Color Orange    = Color.FromArgb(255, 128, 0);
  public static readonly Color DkYellow  = Color.FromArgb(128, 128, 0);
  public static readonly Color Yellow    = Color.FromArgb(255, 255, 0);
  public static readonly Color Brown     = Color.FromArgb(64, 32, 16);
  public static readonly Color Tan       = Color.FromArgb(128, 64, 32);
  
  public static readonly Color Transparent = Color.FromArgb(255, 0, 255);
}

} // namespace TriangleChase