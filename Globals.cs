using System;
using AdamMil.Mathematics.Geometry;
using GameLib;
using GameLib.Fonts;
using GameLib.Video;
using System.IO;

namespace TriangleChase
{

public enum Sprite
{ CannonBall, Grenade,
  Explosion1, Explosion2, Explosion3, Explosion4, Explosion5, Explosion6, Explosion7, Explosion8,
  Num
}

public class Globals
{ static Globals() { for(int i=0; i<256; i++) vects[i] = new Vector2(0, -1).Rotated(i/(Math.PI*128)); }

  public static string DataPath
  {
    get { return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "../../data/"); } // TODO: fixme
  }
  public static string MapPath { get { return DataPath+"maps/"; } }
  public static string SpritePath { get { return DataPath+"sprites/"; } }

  public static Random  Random = new Random();
  public static TrueTypeFont Font;

  public static Vector2 Vector(int angle) { return vects[angle&0xFF]; }
  public static Surface Sprite(TriangleChase.Sprite which) { return sprites[(int)which]; }

  public static void InitGraphics()
  {
    string[] fn = new string[(int)TriangleChase.Sprite.Num]
    {
      "cannonball.png", "grenade.png",
      "exp1.png", "exp2.png", "exp3.png", "exp4.png", "exp5.png", "exp6.png", "exp7.png", "exp8.png"
    };
    for(int i=0; i<sprites.Length; i++)
    { sprites[i] = new Surface(SpritePath+fn[i]).CloneDisplay(false);
      sprites[i].SetColorKey(Colors.Transparent);
    }
    
    Font = new TrueTypeFont(DataPath+"arial.ttf", 10);
    Font.RenderStyle = RenderStyle.Blended;
  }

  static Vector2[] vects = new Vector2[256];
  static Surface[] sprites = new Surface[(int)TriangleChase.Sprite.Num];
}

static class Colors
{
  public static readonly Color Black     = new Color(0, 0, 0);
  public static readonly Color DkGrey    = new Color(96, 96, 96);
  public static readonly Color LtGrey    = new Color(128, 128, 128);
  public static readonly Color White     = new Color(255, 255, 255);
  public static readonly Color DkRed     = new Color(128, 0, 0);
  public static readonly Color Red       = new Color(192, 0, 0);
  public static readonly Color LtRed     = new Color(255, 0, 0);
  public static readonly Color DkGreen   = new Color(0, 128, 0);
  public static readonly Color Green     = new Color(0, 192, 0);
  public static readonly Color LtGreen   = new Color(0, 255, 0);
  public static readonly Color DkBlue    = new Color(0, 0, 192);
  public static readonly Color Blue      = new Color(0, 64, 192);
  public static readonly Color LtBlue    = new Color(0, 128, 255);
  public static readonly Color DkMagenta = new Color(128, 0, 128);
  public static readonly Color LtMagenta = new Color(255, 0, 255);
  public static readonly Color DkCyan    = new Color(0, 128, 128);
  public static readonly Color Cyan      = new Color(0, 192, 192);
  public static readonly Color LtCyan    = new Color(0, 255, 255);
  public static readonly Color Orange    = new Color(255, 128, 0);
  public static readonly Color DkYellow  = new Color(128, 128, 0);
  public static readonly Color Yellow    = new Color(255, 255, 0);
  public static readonly Color Brown     = new Color(64, 32, 16);
  public static readonly Color Tan       = new Color(128, 64, 32);
}

} // namespace TriangleChase