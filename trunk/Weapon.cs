using System;
using GameLib.Mathematics;

namespace TriangleChase
{

public abstract class Weapon
{ public Weapon(Ship ship) { this.ship=ship; }

  public abstract string Name { get; }
  public abstract void Fire();
  public virtual  void Think() { if(Reload>0) Reload--; }

  public int Ammo, MaxAmmo, Reload, FillDelay, FillCount;

  protected Ship ship;
}

public class MachineGun : Weapon
{ public MachineGun(Ship ship) : base(ship) { Ammo=MaxAmmo=400; FillCount=2; }
  
  public override string Name { get { return "Machine Gun"; } }

  public override void Fire()
  { if(Reload==0 && Ammo>0)
    { if(ship.World.IsServer)
        ship.World.AddObject(new Bullet(ship, ship.Pos+ship.Vector*(ship.Size/2+1), ship.Vel+ship.Vector*3));
      Ammo--; Reload=2;
    }
  }
}

public class DualMachineGun : Weapon
{ public DualMachineGun(Ship ship) : base(ship) { Ammo=MaxAmmo=400; FillCount=2; }
  
  public override string Name { get { return "Dual Gun"; } }

  public override void Fire()
  { if(Reload==0 && Ammo>0)
    { VectorF vel = ship.Vel+ship.Vector*3;
      int wid = ship.Size/2-3, hei = -ship.Size/2-1;
      if(ship.World.IsServer)
      { ship.World.AddObject(new Bullet(ship, ship.Pos+new VectorF(-wid, hei).RotatedZ(ship.Angle), vel));
        ship.World.AddObject(new Bullet(ship, ship.Pos+new VectorF( wid, hei).RotatedZ(ship.Angle), vel));
      }
      if((Ammo-=2)<0) Ammo=0;
      Reload = 3;
    }
  }
}

public class FBMachineGun : Weapon
{ public FBMachineGun(Ship ship) : base(ship) { Ammo=MaxAmmo=400; FillCount=2; }
  
  public override string Name { get { return "2-Way Gun"; } }

  public override void Fire()
  { if(Reload==0 && Ammo>0)
    { VectorF vel=ship.Vector*2.5f, pos=ship.Vector*(ship.Size/2+1);
      if(ship.World.IsServer)
      { ship.World.AddObject(new Bullet(ship, ship.Pos+pos, ship.Vel+vel));
        ship.World.AddObject(new Bullet(ship, ship.Pos-pos, ship.Vel-vel));
      }
      if((Ammo-=2)<0) Ammo=0;
      Reload = 2;
    }
  }

  protected int count;
}

public class WavyMachineGun : Weapon
{ public WavyMachineGun(Ship ship) : base(ship) { Ammo=MaxAmmo=300; FillCount=2; }
  
  public override string Name { get { return "Wavy Gun"; } }

  public override void Fire()
  { if(Reload==0 && Ammo>0)
    { int mul = ship.Size/2;
      VectorF pos = new VectorF((float)(mul*SineTable.Sin(count++<<5)), -mul-1).RotatedZ(ship.Angle);
      if(ship.World.IsServer)
        ship.World.AddObject(new Bullet(ship, ship.Pos+pos, ship.Vel+ship.Vector*3));
      Ammo--;
      Reload = 2;
    }
  }

  protected int count;
}

public class Cannon : Weapon
{ public Cannon(Ship ship) : base(ship) { Ammo=MaxAmmo=12; FillDelay=18; FillCount=1; }
  public override string Name { get { return "Cannon"; } }
  public override void Fire()
  { if(Reload==0 && Ammo>0)
    { if(ship.World.IsServer)
        ship.World.AddObject(new CannonBall(ship, ship.Pos+ship.Vector*(ship.Size/2+1), ship.Vel+ship.Vector*3));
      Ammo--;
      Reload = 30;
      ship.Vel -= ship.Vector*0.6f; // recoil
    }
  }
}

public class GrenadeLauncher : Weapon
{ public GrenadeLauncher(Ship ship) : base(ship) { Ammo=MaxAmmo=8; FillDelay=25; FillCount=1; }
  public override string Name { get { return "Grenade Launcher"; } }
  public override void Fire()
  { if(Reload==0 && Ammo>0)
    { if(ship.World.IsServer)
        ship.World.AddObject(new Grenade(ship, ship.Pos+ship.Vector*(ship.Size/2+1), ship.Vel+ship.Vector*2));
      Ammo--;
      Reload = 40;
    }
  }
}

public class Afterburner : Weapon
{ public Afterburner(Ship ship) : base(ship) { Ammo=MaxAmmo=150; FillDelay=2; FillCount=5; }
  public override string Name { get { return "Afterburner"; } }
  public override void Fire()
  { if(Ammo>0)
    { ship.AddVelocity(ship.Vector*0.5f);
      ship.Resting = ship.OnBase = false;
      Ammo--;
    }
    fired = ship.World.Tick;
  }
  public override void Think()
  { bool fire = Ammo>0 && fired==ship.World.Tick;
    if(fire!=firing)
    { if(fire) ship.World.AddObject(obj=new AfterburnerFlame(ship));
      else obj.Remove=true;
      firing = fire;
    }
    base.Think();
  }
  GameObject obj;
  uint fired;
  bool firing;
}

} // namespace TriangleChase