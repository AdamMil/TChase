using System;
using GameLib.Mathematics;

namespace TriangleChase
{

public class Vectors
{ Vectors() { }
  static Vectors() { for(int i=0; i<256; i++) vects[i] = new VectorF(0, -1).RotatedZ(i); }
  public static VectorF Get(int angle) { return vects[angle&0xFF]; }
  
  static VectorF[] vects = new VectorF[256];
}

} // namespace TriangleChase