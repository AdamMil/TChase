using System;
using System.Drawing;
using GameLib.Mathematics;
using GameLib.Video;

namespace TriangleChase
{

public interface Ammo
{ Ship Owner { get; }
}

public abstract class PointAmmo : PointObject, Ammo
{ public PointAmmo(Ship owner, VectorF pos, VectorF vel) { this.owner=owner; Pos=pos; Vel=vel; }
  public Ship Owner { get { return owner; } }
  protected Ship owner; // TODO: move this to object level?
}

public abstract class SphereAmmo : SphereObject, Ammo
{ public SphereAmmo(Ship owner, VectorF pos, VectorF vel, int radius) { this.owner=owner; Pos=pos; Vel=vel; Radius=radius; }
  public Ship Owner { get { return owner; } }
  protected Ship owner; // TODO: move this to object level?
}

public class Bullet : PointAmmo
{ public Bullet(Ship owner, VectorF pos, VectorF vel) : base(owner, pos, vel) { Color=Color.White; Weight=1; }
  public Bullet(Ship owner, VectorF pos, VectorF vel, Color color) : base(owner, pos, vel) { Color=color; Weight=1; }
  public override float CalcDamage(Ship ship, VectorF vec) { return base.CalcDamage(ship, vec)+2.5f; }
  public override void Draw(Surface dest, int cx, int cy) { dest.PutPixel(RX-cx, RY-cy, Color); }

  public Color Color;
}

public class SpriteAmmo : SphereAmmo
{ public SpriteAmmo(Ship owner, VectorF pos, VectorF vel, int radius, Sprite sprite) : base(owner, pos, vel, radius)
  { this.sprite=sprite;
  }
  public override void Draw(Surface dest, int cx, int cy) { CenterBlit(dest, Globals.Sprite(sprite), cx, cy); }
  protected Sprite sprite;
}

public class CannonBall : SpriteAmmo
{ public CannonBall(Ship owner, VectorF pos, VectorF vel) : base(owner, pos, vel, 3, Sprite.CannonBall) { Weight=25; }
  public override float CalcDamage(Ship ship, VectorF vec) { return base.CalcDamage(ship, vec)+10; }
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
{ public Grenade(Ship owner, VectorF pos, VectorF vel) : base(owner, pos, vel, 2, Sprite.Grenade)
  { Weight=5;
    RadiusSqr=8;
  }
  public override void HitMap()
  { world.Explode(owner, Explosion.Tiny, Pos);
    base.HitMap();
  }
  public override float CalcDamage(Ship ship, VectorF vec) { return base.CalcDamage(ship, vec)+20; }
  public override void Think()
  { if(Age>world.TPS/2)
    { Remove=true;
      for(int a=0; a<256; a+=Globals.Random.Next(8))
      { VectorF v = Globals.Vector(a);
        for(int n=0; n<2; n++)
        { int cn = Globals.Random.Next(3); if(cn>2) cn=2;
          world.AddObject(new Bullet(owner, Pos+v*((float)Globals.Random.NextDouble()*4),
                                     Vel+v*((float)Globals.Random.NextDouble()/2), owner.ColorMap[cn]));
        }
      }
    }
  }
}

} // namespace TriangleChase