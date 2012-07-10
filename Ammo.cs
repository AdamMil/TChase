using System;
using AdamMil.Mathematics.Geometry;
using GameLib.Video;
using AdamMil.IO;
using GameLib;

namespace TriangleChase
{

public abstract class PointAmmo : PointObject
{ public PointAmmo() { }
  public PointAmmo(Ship owner, Vector2 pos, Vector2 vel) { this.owner=owner; Pos=pos; Vel=vel; }

  public override void Serialize(BinaryWriter writer, out System.IO.Stream attachedStream)
  {
    base.Serialize(writer, out attachedStream);
    writer.WriteEncoded(owner.ID);
  }
  public override void Deserialize(BinaryReader reader, System.IO.Stream attachedStream)
  {
    base.Deserialize(reader, attachedStream);
    owner = world.FindShip(reader.ReadEncodedUInt32());
  }
  
  protected Ship owner;
}

public abstract class SphereAmmo : SphereObject
{ public SphereAmmo(int radius) { Radius=radius; }
  public SphereAmmo(Ship owner, Vector2 pos, Vector2 vel, int radius)
  { this.owner=owner; Pos=pos; Vel=vel; Radius=radius;
  }

  public override void Serialize(BinaryWriter writer, out System.IO.Stream attachedStream)
  {
    base.Serialize(writer, out attachedStream);
    writer.WriteEncoded(owner.ID);
  }
  public override void Deserialize(BinaryReader reader, System.IO.Stream attachedStream)
  {
    base.Deserialize(reader, attachedStream);
    owner = world.FindShip(reader.ReadEncodedUInt32());
  }
  
  protected Ship owner;
}

public class Bullet : PointAmmo
{ public Bullet() { Color=Color.White; Weight=1; }
  public Bullet(Ship owner, Vector2 pos, Vector2 vel) : base(owner, pos, vel) { Color=Color.White; Weight=1; }
  public Bullet(Ship owner, Vector2 pos, Vector2 vel, Color color) : base(owner, pos, vel) { Color=color; Weight=1; }

  public override float CalcDamage(Ship ship, Vector2 vec) { return base.CalcDamage(ship, vec)+2.5f; }
  public override void Draw(Surface dest, int cx, int cy) { dest.PutPixel(RX-cx, RY-cy, Color); }

  public override void Serialize(BinaryWriter writer, out System.IO.Stream attachedStream)
  {
    base.Serialize(writer, out attachedStream);
    writer.Write(Color.R);
    writer.Write(Color.G);
    writer.Write(Color.B);
  }
  public override void Deserialize(BinaryReader reader, System.IO.Stream attachedStream)
  {
    base.Deserialize(reader, attachedStream);
    Color = new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
  }

  public Color Color;
}

public class SpriteAmmo : SphereAmmo
{ public SpriteAmmo(int radius, Sprite sprite) : base(radius) { this.sprite=sprite; }
  public SpriteAmmo(Ship owner, Vector2 pos, Vector2 vel, int radius, Sprite sprite) : base(owner, pos, vel, radius)
  { this.sprite=sprite;
  }
  public override void Draw(Surface dest, int cx, int cy) { CenterBlit(dest, Globals.Sprite(sprite), cx, cy); }
  protected Sprite sprite;
}

public class CannonBall : SpriteAmmo
{ public CannonBall() : base(3, Sprite.CannonBall) { Weight=25; NetPolicy = NetPolicy.RemoteAll; }
  public CannonBall(Ship owner, Vector2 pos, Vector2 vel) : base(owner, pos, vel, 3, Sprite.CannonBall)
  { Weight=25;
    NetPolicy = NetPolicy.RemoteAll;
  }
  public override float CalcDamage(Ship ship, Vector2 vec) { return base.CalcDamage(ship, vec)+10; }
  public override void HitMap()
  { world.Explode(owner, Explosion.Tiny, Pos);
    base.HitMap();
  }
  public override void HitShip(Ship ship)
  { world.Explode(owner, Explosion.Tiny, Pos);
    base.HitShip(ship);
  }
}

public class Grenade : SpriteAmmo
{ public Grenade() : base(2, Sprite.Grenade) { Weight=5; RadiusSqr=8; }
  public Grenade(Ship owner, Vector2 pos, Vector2 vel) : base(owner, pos, vel, 2, Sprite.Grenade)
  { Weight=5;
    RadiusSqr=8;
  }
  public override void HitMap()
  { world.Explode(owner, Explosion.Tiny, Pos);
    base.HitMap();
  }
  public override float CalcDamage(Ship ship, Vector2 vec) { return base.CalcDamage(ship, vec)+20; }
  public override void Think()
  { if(Age>world.TPS/2)
    { Remove=true;
      if(world.IsServer)
        for(int a=0; a<256; a+=Globals.Random.Next(8))
        { Vector2 v = Globals.Vector(a);
          for(int n=0; n<2; n++)
          { int cn = Globals.Random.Next(3); if(cn>2) cn=2;
            App.Server.AddObject(new Bullet(owner, Pos+v*((float)Globals.Random.NextDouble()*4),
                                            Vel+v*((float)Globals.Random.NextDouble()/2), owner.ColorMap[cn]));
          }
        }
    }
  }
}

} // namespace TriangleChase