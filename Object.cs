using System;
using AdamMil.IO;
using AdamMil.Mathematics.Geometry;
using GameLib;
using GameLib.Video;

namespace TriangleChase
{

public enum NetPolicy { Unknown, Local, LocalProp, RemoteAdd, RemoteAll }

#region GameObject
public abstract class GameObject : GameLib.Network.INetSerializable
{ public double X { get { return Pos.X; } }
  public double Y { get { return Pos.Y; } }
  public int RX { get { return (int)Math.Round(Pos.X); } }
  public int RY { get { return (int)Math.Round(Pos.Y); } }
  public double XV { get { return Vel.X; } }
  public double YV { get { return Vel.Y; } }
  public World World { get { return world; } set { world=value; } }

  public float Momentum { get { return (float)Vel.LengthSqr*Weight; } }

  public abstract bool IsHitMap  { get; }
  public abstract bool IsHitBase { get; }
  
  public virtual void Serialize(BinaryWriter writer, out System.IO.Stream attachedStream)
  {
    attachedStream = null;
    writer.WriteEncoded(ID);
    writer.Write((byte)Angle);
    writer.Write((float)Pos.X);
    writer.Write((float)Pos.Y);
    writer.Write((float)Vel.X);
    writer.Write((float)Vel.Y);
  }
  
  public virtual void Deserialize(BinaryReader reader, System.IO.Stream attachedStream)
  {
    ID    = reader.ReadEncodedUInt32();
    Angle = reader.ReadByte();
    Pos.X = reader.ReadSingle();
    Pos.Y = reader.ReadSingle();
    Vel.X = reader.ReadSingle();
    Vel.Y = reader.ReadSingle();
  }

  public Vector2 Pos, Vel;
  public uint    ID;
  public int     Angle, Weight, Age;
  public NetPolicy NetPolicy;
  public bool    Remove, NoGrav, CanHitObjs;

  public void AddVelocity(float xv, float yv) { Vel.X+=xv; Vel.Y+=yv; }
  public void AddVelocity(Vector2 v) { Vel+=v; }
  public void Move() { Pos+=Vel; }
  public void Rotate(int angle) { Angle += angle; }

  public virtual  void HitMap() { Remove=true; }
  public virtual  void HitShip(Ship ship) { Impact(ship); Remove=true; }
  public virtual  void HitObject(GameObject obj) { }
  public virtual  void CalcVel() { AddVelocity(world.Gravity); }
  public abstract bool Intersects(GameObject obj);
  public virtual  void Think() { }
  public abstract void Draw(Surface dest, int cx, int cy);

  public virtual float CalcDamage(Ship ship, Vector2 vel) { return (float)vel.Length*Weight; }

  public void Impact(Ship ship) { Impact(ship, false, 0); }
  public void Impact(Ship ship, bool land) { Impact(ship, land, 0); }
  public void Impact(Ship ship, bool land, int damageAdd)
  { Vector2 vel = (Vel-ship.Vel);
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

  protected World world;
  
  protected void CenterBlit(Surface dest, Surface src, int cx, int cy) { src.Blit(dest, RX-cx-src.Width/2, RY-cy-src.Height/2); }
}
#endregion

#region PointObject and SphereObject
public abstract class PointObject : GameObject
{ public PointObject() { }
  public PointObject(int x, int y, float xv, float yv) { Pos=new Vector2(x, y); Vel=new Vector2(xv, yv); }
  public PointObject(Vector2 pos, Vector2 vel) { Pos=pos; Vel=vel; }
  
  public override bool IsHitMap  { get { return world.HitMap(RX, RY); } }
  public override bool IsHitBase { get { return world.HitBase(RX, RY); } }
  
  public override bool Intersects(GameObject o)
  { 
    if(o is PointObject) return RX==o.RX && RY==o.RY;
    else if(o is SphereObject)
    {
      float xd=(float)o.X-(float)X, yd=(float)o.Y-(float)Y, dist=xd*xd+yd*yd;
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
  public SphereObject(Vector2 pos, int radius, Vector2 vel) { Pos=pos; Radius=radius; Vel=vel; }

  public int Radius { get { return radius; } set { radius=value; RadiusSqr=value*value; } }

  public override bool IsHitMap
  { get
    { int x=RX, y=RY, rad=radius-1;
      return world.HitMap(x, y)     || world.HitMap(x-rad, y) || world.HitMap(x+rad, y) ||
             world.HitMap(x, y-rad) || world.HitMap(x, y+rad);
    }
  }
  public override bool IsHitBase
  { get
    { int x=RX, y=RY, rad=radius-1;
      return world.HitBase(x, y)     || world.HitBase(x-rad, y) || world.HitBase(x+rad, y) ||
             world.HitBase(x, y-rad) || world.HitBase(x, y+rad);
    }
  }

  public override bool Intersects(GameObject o)
  {
    float xd=(float)o.X-(float)X, yd=(float)o.Y-(float)Y, dist=xd*xd+yd*yd;
    if(o is PointObject) return dist<=RadiusSqr;
    else if(o is SphereObject) return dist <= RadiusSqr+((SphereObject)o).RadiusSqr;
    throw new ArgumentException("Unhandled object type");
  }
  
  public int RadiusSqr;

  public override void Serialize(BinaryWriter writer, out System.IO.Stream attachedStream)
  {
    base.Serialize(writer, out attachedStream);
    writer.WriteEncoded((uint)Radius);
    writer.WriteEncoded((uint)RadiusSqr);
  }
  public override void Deserialize(BinaryReader reader, System.IO.Stream attachedStream)
  {
    base.Deserialize(reader, attachedStream);
    radius    = (int)reader.ReadEncodedUInt32();
    RadiusSqr = (int)reader.ReadEncodedUInt32();
  }

  protected int radius;
}
#endregion

#region Spark and FlameSpark
public class Spark : PointObject
{ public Spark() { Life=100; Color=Color.Magenta; }
  public Spark(Vector2 pos, Vector2 vel, int life, Color color) { Pos=pos; Vel=vel; Life=life; Color=color; }
  
  public override void Draw(Surface dest, int cx, int cy) { dest.PutPixel(RX-cx, RY-cy, Color); }
  public override void HitShip(Ship ship) { }
  public override void Think() { if(Age>Life) Remove=true; }

  public Color Color;
  public int   Life;
}

public class FlameSpark : Spark
{ public FlameSpark(Vector2 pos, Vector2 vel, int life, Color color) : base(pos, vel, life, color)
  { NetPolicy = NetPolicy.Local;
  }
  public override void Think()
  { if(--Life==0) Remove=true;
    else if(Life<7 && Color.Equals(Colors.LtGrey)) Color=Colors.DkGrey;
    else if(Life<9 && Color.Equals(Colors.DkBlue)) Color=Colors.Yellow;
    else if(Life<6 && Color.Equals(Colors.Yellow)) Color=Colors.Orange;
    else if(Life<4 && Color.Equals(Colors.Orange)) Color=Colors.Red;
  }
}
#endregion

#region Exploder
public class Exploder : SphereObject
{ public Exploder(int type)
  { NoGrav = true;
    Type   = type;
    Radius = Types[type].Radius;
    Weight = Types[type].Weight;
  }
  public override void CalcVel() { } // no gravity

  public override void HitMap() { world.RemoveCircle(RX, RY, Radius); Vel=new Vector2(); }

  public override void HitShip(Ship ship)
  { if(HitDelay==0 && Activate<=0)
    { Impact(ship, false, Types[Type].Damage);
      HitDelay = (int)(world.TPS/2);
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
      world.RemoveCircle(RX, RY, Radius);
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
#endregion

#region ShipAttachments
public abstract class ShipAttachment : GameObject
{ protected ShipAttachment() { }
  protected ShipAttachment(Ship ship) { this.ship=ship; }

  public override void Serialize(BinaryWriter writer, out System.IO.Stream attachedStream)
  {
    base.Serialize(writer, out attachedStream);
    writer.WriteEncoded(ship.ID);
  }

  public override void Deserialize(BinaryReader reader, System.IO.Stream attachedStream)
  {
    base.Deserialize(reader, attachedStream);
    ship = world.FindShip(reader.ReadEncodedUInt32());
  }

  protected Ship ship;
}

public class AfterburnerFlame : ShipAttachment
{ public AfterburnerFlame() { NetPolicy=NetPolicy.LocalProp; }
  public AfterburnerFlame(Ship ship) : base(ship) { NetPolicy=NetPolicy.LocalProp; }

  public override bool IsHitMap  { get { return false; } }
  public override bool IsHitBase { get { return false; } }
  public override void HitShip(Ship ship) { }
  public override bool Intersects(GameObject obj) { return false; }
  public override void Draw(Surface dest, int cx, int cy)
  { int mul = ship.Size/2;
    Vector2 v1 = Globals.Vector(ship.Angle+112)*(mul-3), v2 = Globals.Vector(ship.Angle+144)*(mul-3),
            v3 = Globals.Vector(ship.Angle+128)*(mul+5);
    int  x=ship.RX-cx,   y=ship.RY-cy,
        x1=(int)v1.X+x, y1=(int)v1.Y+y,
        x2=(int)v2.X+x, y2=(int)v2.Y+y,
        x3=(int)v3.X+x, y3=(int)v3.Y+y;
    Shapes.TriangleAA(dest, x1, y1, x2, y2, x3, y3, new Color(0, 128, 192));
  }
}
#endregion

#region Ship
public class Ship : SphereObject
{ public Ship() { Init(); NetPolicy=NetPolicy.RemoteAll; }

  public Vector2 Vector { get { return Globals.Vector(Angle); } }
  public Color   Color  { get { return ColorMap[2]; } }

  public override void Draw(Surface dest, int cx, int cy)
  { if(Dead) return;
    int    mul=Size/2;
    const float DegsTo256 = 256/360f;
    Vector2 v1=Vector*mul, v2=Globals.Vector((int)Math.Round(Angle+140*DegsTo256))*mul,
            v3=Globals.Vector((int)Math.Round(Angle+220*DegsTo256))*mul, v4=Globals.Vector(Angle+128)*(mul-3);
    int  x=RX-cx,        y=RY-cy,
        x1=(int)v1.X+x, y1=(int)v1.Y+y,
        x2=(int)v2.X+x, y2=(int)v2.Y+y,
        x3=(int)v3.X+x, y3=(int)v3.Y+y,
        x4=(int)v4.X+x, y4=(int)v4.Y+y;
    Color c;
    if(Flash>0) { c=Colors.White; Flash--; }
    else c=Color;
    Shapes.FilledTriangle(dest, x1, y1, x2, y2, x4, y4, c);
    Shapes.FilledTriangle(dest, x1, y1, x3, y3, x4, y4, c);
    Shapes.TriangleAA(dest, x1, y1, x2, y2, x4, y4, c);
    Shapes.TriangleAA(dest, x1, y1, x3, y3, x4, y4, c);
  }

  public override float CalcDamage(Ship ship, Vector2 vel)
  { return ship==this ? Momentum/10 : base.CalcDamage(ship, vel);
  }

  public override void HitMap()
  { Impact(this, true);
    Vel = -Vel/5;
    if(Math.Abs(XV)<0.1f) Vel.X=0;
    if(Math.Abs(YV)<0.1f) Vel.Y=0;
    Pos = OldPos;
  }

  public override void Think()
  { if(Dead) return;
    if(Health<-world.TPS*3)
    { world.Explode(this, OnBase ? Explosion.Medium : Explosion.Huge, Pos);
      Dead = true;
    }

    if(Gun!=null) Gun.Think();
    if(Special!=null) Special.Think();
    if(Health<=0) Health--;
    else if(OnBase && world.IsServer)
    { if(Resting)
        if((sbyte)Angle<0) Angle++;
        else if((sbyte)Angle>0) Angle--;
      if(++Health>MaxHealth) Health=MaxHealth;
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
    if(!world.IsServer) AddFlame();
    AddVelocity(Vector*AccelMult);
    Resting = OnBase = false;
  }
  
  public void Fire() { if(Gun!=null) Gun.Fire(); }
  public void UseSpecial() { if(Special!=null) Special.Fire(); }
  
  public void Spawn(Vector2 pos)
  { Init();
    Pos=pos;
    Vel=new Vector2();
  }

  public void ApplyKeys(InputMessage.Key keys)
  { if((keys&InputMessage.Key.Turn)!=0)
    { if(++TurnAcc>MaxTurn) TurnAcc=MaxTurn;
      if((keys&InputMessage.Key.Left)!=0)  Angle -= TurnAcc;
      if((keys&InputMessage.Key.Right)!=0) Angle += TurnAcc;
    }
    else TurnAcc=0;

    if((keys&InputMessage.Key.Accel)!=0)   Accelerate();
    if((keys&InputMessage.Key.Fire)!=0)    Fire();
    if((keys&InputMessage.Key.Special)!=0) UseSpecial();
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
    Size    = 16;
    Radius  = 6; RadiusSqr = 36;
    Flash   = 0;
    Resting = OnBase = Dead = false;
  }

  protected void AddFlame()
  { world.AddObject(new FlameSpark(Pos-Vector*(Size/2-3), Vel-Globals.Vector(Angle+Globals.Random.Next(129)-64)*.1f,
                                   Globals.Random.Next(4)+9, Health<MaxHealth/4 ? Colors.LtGrey : Colors.DkBlue));
  }
  
  protected void Reload(Weapon weap)
  { if(weap!=null && weap.Ammo<weap.MaxAmmo && (weap.FillDelay==0 || world.Tick%(weap.FillDelay+1)==0))
    { weap.Ammo += weap.FillCount;
      if(weap.Ammo>weap.MaxAmmo) weap.Ammo=weap.MaxAmmo;
    }
  }

  protected Vector2 OldPos;
}
#endregion

} // namespace TriangleChase