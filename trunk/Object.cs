using System;
using System.Drawing;
using GameLib.Mathematics;
using GameLib.Video;

namespace TriangleChase
{

public abstract class GameObject
{ public float X  { get { return Pos.X; } }
  public float Y  { get { return Pos.Y; } }
  public int   RX { get { return (int)Math.Round(Pos.X); } }
  public int   RY { get { return (int)Math.Round(Pos.Y); } }
  public float XV { get { return Vel.X; } }
  public float YV { get { return Vel.Y; } }

  public float Momentum { get { return Vel.LengthSqr*Weight; } }

  public void AddVelocity(float xv, float yv) { Vel.X+=xv; Vel.Y+=yv; }
  public void AddVelocity(VectorF v) { Vel+=v; }
  public void Move() { Pos+=Vel; }
  public void Rotate(int angle) { Angle += angle; }

  public abstract bool IsHitMap  { get; }
  public abstract bool IsHitBase { get; }

  public virtual  void HitMap() { Remove=true; }
  public virtual  void HitShip(Ship ship) { Impact(ship); Remove=true; }
  public virtual  void HitObject(GameObject obj) { }
  public virtual  void CalcVel() { AddVelocity(App.World.Gravity); }
  public abstract bool Intersects(GameObject obj);
  public virtual  void Think() { }
  public abstract void Draw(Surface dest, int cx, int cy);

  public virtual float CalcDamage(Ship ship, VectorF vel) { return vel.Length*Weight; }

  public void Impact(Ship ship) { Impact(ship, false, 0); }
  public void Impact(Ship ship, bool land) { Impact(ship, land, 0); }
  public void Impact(Ship ship, bool land, int damageAdd)
  { VectorF vel = (Vel-ship.Vel);
    int damage = (int)(CalcDamage(ship, vel)+0.5)+damageAdd;
    ship.Vel += vel*((float)Weight/ship.Weight);
    if(damage>0)
    { ship.Health -= damage;
      ship.Flash = 2;
    }
    else if(land && !ship.Resting)
    { ship.Resting = true;
      ship.OnBase  = IsHitBase;
    }
  }

  public VectorF Pos, Vel;
  public int     Angle, Weight, Age;
  public bool    Remove, NoGrav, CanHitObjs;
  
  protected void CenterBlit(Surface dest, Surface src, int cx, int cy) { src.Blit(dest, RX-cx-src.Width/2, RY-cy-src.Height/2); }
}

public abstract class PointObject : GameObject
{ public PointObject() { }
  public PointObject(int x, int y, float xv, float yv) { Pos=new VectorF(x, y); Vel=new VectorF(xv, yv); }
  public PointObject(VectorF pos, VectorF vel) { Pos=pos; Vel=vel; }
  
  public override bool IsHitMap  { get { return App.World.HitMap(RX, RY); } }
  public override bool IsHitBase { get { return App.World.HitBase(RX, RY); } }
  
  public override bool Intersects(GameObject o)
  { if(o is PointObject) return RX==o.RX && RY==o.RY;
    else if(o is SphereObject)
    { float xd=o.X-X, yd=o.Y-Y, dist=xd*xd+yd*yd;
      return dist <= ((SphereObject)o).RadiusSqr;
    }
    throw new ArgumentException("Unhandled object type");
  }
}

public abstract class SphereObject : GameObject
{ public SphereObject() { }
  public SphereObject(int radius) { Radius=radius; }
  public SphereObject(int x, int y, int radius) { Pos.X=x; Pos.Y=y; Radius=radius; }
  public SphereObject(int x, int y, int radius, float xv, float yv)
  { Pos.X=x; Pos.Y=y; Radius=radius; Vel.X=xv; Vel.Y=yv;
  }
  public SphereObject(VectorF pos, int radius, VectorF vel) { Pos=pos; Radius=radius; Vel=vel; }

  public int Radius { get { return radius; } set { radius=value; RadiusSqr=value*value; } }

  public override bool IsHitMap
  { get
    { int x=RX, y=RY, rad=radius-1;
      return App.World.HitMap(x, y)     || App.World.HitMap(x-rad, y) || App.World.HitMap(x+rad, y) ||
             App.World.HitMap(x, y-rad) || App.World.HitMap(x, y+rad);
    }
  }
  public override bool IsHitBase
  { get
    { int x=RX, y=RY, rad=radius-1;
      return App.World.HitBase(x, y)     || App.World.HitBase(x-rad, y) || App.World.HitBase(x+rad, y) ||
             App.World.HitBase(x, y-rad) || App.World.HitBase(x, y+rad);
    }
  }

  public override bool Intersects(GameObject o)
  { float xd=o.X-X, yd=o.Y-Y, dist=xd*xd+yd*yd;
    if(o is PointObject) return dist<=RadiusSqr;
    else if(o is SphereObject) return dist <= RadiusSqr+((SphereObject)o).RadiusSqr;
    throw new ArgumentException("Unhandled object type");
  }
  
  public int RadiusSqr;
  protected int radius;
}

public class Spark : PointObject
{ public Spark() { Life=100; Color=Color.Magenta; }
  public Spark(VectorF pos, VectorF vel, int life, Color color) { Pos=pos; Vel=vel; Life=life; Color=color; }
  
  public override void Draw(Surface dest, int cx, int cy) { dest.PutPixel(RX-cx, RY-cy, Color); }
  public override void HitShip(Ship ship) { }
  public override void Think() { if(Age>Life) Remove=true; }

  public Color Color;
  public int   Life;
}

public class FlameSpark : Spark
{ public FlameSpark(VectorF pos, VectorF vel, int life, Color color) : base(pos, vel, life, color) { }
  public override void Think()
  { if(--Life==0) Remove=true;
    else if(Life<7 && Color.Equals(Colors.LtGrey)) Color=Colors.DkGrey;
    else if(Life<9 && Color.Equals(Colors.DkBlue)) Color=Colors.Yellow;
    else if(Life<6 && Color.Equals(Colors.Yellow)) Color=Colors.Orange;
    else if(Life<4 && Color.Equals(Colors.Orange)) Color=Colors.Red;
  }
}

public class Exploder : SphereObject
{ public Exploder(int type)
  { NoGrav = true;
    Type   = type;
    Radius = Types[type].Radius;
    Weight = Types[type].Weight;
  }
  public override void CalcVel() { } // no gravity

  public override void HitMap() { App.World.RemoveCircle(RX, RY, Radius); Vel=new VectorF(); }

  public override void HitShip(Ship ship)
  { if(HitDelay==0 && Activate<=0)
    { Impact(ship, false, Types[Type].Damage);
      HitDelay = (int)(App.World.TPS/2);
    }
  }

  public override void Draw(Surface dest, int cx, int cy)
  { if(Activate>0) return;
    CenterBlit(dest, Globals.Sprite((Sprite)((int)Sprite.Explosion1+Types[Type].Sprites[Stage])), cx, cy);
  }

  public override void Think()
  { if(Age<Activate) return;
    if(Activate>=0) { Age=0; Activate=-1; }
    if(HitDelay>0) HitDelay--;
    if(Age==Types[Type].Ages[Stage]) Stage++;
    if(Stage==Types[Type].Ages.Length)
    { Remove=true;
      App.World.RemoveCircle(RX, RY, Radius);
    }
  }

  public int Type, Stage, Activate, HitDelay;

  protected class ExpType
  { public ExpType(int[] sprites, int[] ages, int radius, int weight, int damage)
    { Sprites=sprites; Ages=ages; Radius=radius; Weight=weight; Damage=damage;
    }
    public int[] Sprites, Ages;
    public int Radius, Weight, Damage;
  }
  protected static readonly ExpType[] Types = new ExpType[3]
  { new ExpType(new int[] { 0, 1 }, new int[] { 3, 6 }, 3, 5, 25),
    new ExpType(new int[] { 0, 2, 3 }, new int[] { 3, 6, 12 }, 4, 10, 40),
    new ExpType(new int[] { 0, 4, 5, 6, 7 }, new int[] { 3, 6, 10, 16, 25 }, 5, 20, 60),
  };
}

public class AfterburnerFlame : GameObject
{ public AfterburnerFlame(Ship ship) { this.ship=ship; }
  public override bool IsHitMap  { get { return false; } }
  public override bool IsHitBase { get { return false; } }
  public override void HitShip(Ship ship) { }
  public override bool Intersects(GameObject obj) { return false; }
  public override void Draw(Surface dest, int cx, int cy)
  { int mul = ship.Size/2;
    VectorF v1 = Globals.Vector(ship.Angle+112)*(mul-3), v2 = Globals.Vector(ship.Angle+144)*(mul-3),
            v3 = Globals.Vector(ship.Angle+128)*(mul+5);
    int  x=ship.RX-cx,   y=ship.RY-cy,
        x1=(int)v1.X+x, y1=(int)v1.Y+y,
        x2=(int)v2.X+x, y2=(int)v2.Y+y,
        x3=(int)v3.X+x, y3=(int)v3.Y+y;
    Primitives.TriangleAA(dest, x1, y1, x2, y2, x3, y3, Color.FromArgb(0, 128, 192));
  }

  protected Ship ship;
}

public class Ship : SphereObject
{ public Ship() { Init(); }
  
  public VectorF Vector { get { return Globals.Vector(Angle); } }
  public Color   Color  { get { return ColorMap[2]; } }

  public override void Draw(Surface dest, int cx, int cy)
  { if(Dead) return;
    int    mul=Size/2;
    VectorF v1=Vector*mul, v2=Globals.Vector((int)Math.Round(Angle+140*MathConst.DegsTo256))*mul,
            v3=Globals.Vector((int)Math.Round(Angle+220*MathConst.DegsTo256))*mul, v4=Globals.Vector(Angle+128)*(mul-3);
    int  x=RX-cx,        y=RY-cy,
        x1=(int)v1.X+x, y1=(int)v1.Y+y,
        x2=(int)v2.X+x, y2=(int)v2.Y+y,
        x3=(int)v3.X+x, y3=(int)v3.Y+y,
        x4=(int)v4.X+x, y4=(int)v4.Y+y;
    Color c;
    if(Flash>0) { c=Colors.White; Flash--; }
    else c=Color;
    Primitives.FilledTriangle(dest, x1, y1, x2, y2, x4, y4, c);
    Primitives.FilledTriangle(dest, x1, y1, x3, y3, x4, y4, c);
    Primitives.TriangleAA(dest, x1, y1, x2, y2, x4, y4, c);
    Primitives.TriangleAA(dest, x1, y1, x3, y3, x4, y4, c);
  }

  public override float CalcDamage(Ship ship, VectorF vel)
  { return ship==this ? Momentum/8 : base.CalcDamage(ship, vel);
  }

public override void CalcVel() { if(this==App.World.Me.Ship) base.CalcVel(); }

  public override void HitMap()
  { Impact(this, true);
    Vel = -Vel/5;
    if(Math.Abs(XV)<0.1f) Vel.X=0;
    if(Math.Abs(YV)<0.1f) Vel.Y=0;
    Pos = OldPos;
  }

  public override void Think()
  { if(Dead) return;
    if(Health<-App.World.TPS*3)
    { App.World.Explode(this, OnBase ? Explosion.Medium : Explosion.Huge, Pos);
      Dead = true;
    }

    if(Gun!=null) Gun.Think();
    if(Special!=null) Special.Think();
    if(Health<=0) Health--;
    else if(OnBase)
    { if(Resting)
        if((sbyte)Angle<0) Angle++;
        else if((sbyte)Angle>0) Angle--;
      if(Health++>MaxHealth) Health=MaxHealth;
      Reload(Gun);
      Reload(Special);
      if((Fuel+=2)>MaxFuel) Fuel=MaxFuel;
    }
    OldPos=Pos;
  }
  
  public void Accelerate()
  { if(Fuel>0) Fuel--;
    else if(Health>0) Health--;
    else return;
    AddFlame();
    AddVelocity(Vector*AccelMult);
    Resting = OnBase = false;
  }
  
  public void Fire() { if(Gun!=null) Gun.Fire(); }
  public void UseSpecial() { if(Special!=null) Special.Fire(); }
  
  public void Spawn(VectorF pos)
  { Init();
    Pos=pos;
    Vel=new VectorF();
    Gun=App.World.MakeGun(this, 0);
    Special=App.World.MakeSpecial(this, 0);
  }

  public Weapon Gun, Special;
  public Color[] ColorMap = new Color[3];
  public int Health, Fuel, MaxHealth, MaxFuel, Flash, TurnAcc, MaxTurn, Size;
  public bool Resting, OnBase, Dead;

  protected const float AccelMult=0.084f;

  protected void Init()
  { Angle   = 0;
    Fuel    = MaxFuel   = 1500;
    Health  = MaxHealth = 500;
    Weight  = 20;
    MaxTurn = 3;
    Size    = 12;
    Radius  = 4; RadiusSqr = 36;
    Flash   = 0;
    Resting = OnBase = Dead = false;
  }

  protected void AddFlame()
  { App.World.AddObject(new FlameSpark(Pos-Vector*(Size/2-3), Vel+Globals.Vector(Angle+Globals.Random.Next(29)+114),
                                       Globals.Random.Next(4)+9, Health<MaxHealth/4 ? Colors.LtGrey : Colors.DkBlue));
  }
  
  protected void Reload(Weapon weap)
  { if(weap!=null && weap.Ammo<weap.MaxAmmo && (weap.FillDelay==0 || App.World.Tick%(weap.FillDelay+1)==0))
    { weap.Ammo += weap.FillCount;
      if(weap.Ammo>weap.MaxAmmo) weap.Ammo=weap.MaxAmmo;
    }
  }

  protected VectorF OldPos;
}

} // namespace TriangleChase