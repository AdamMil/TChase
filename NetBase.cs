using System;
using System.Collections;

namespace TriangleChase
{

public class NetBase
{ protected NetBase() { world = new World(); }

  public byte FindObject(GameObject obj) // TODO: support runtime registration of types
  { for(int i=0; i<objectTypes.Length; i++) if(obj.GetType()==objectTypes[i]) return (byte)i;
    throw new ArgumentException(String.Format("Object type {0} not registered", obj));
  }
  public GameObject MakeObject(byte index)
  { Type type = objectTypes[index];
    GameObject o = (GameObject)type.GetConstructor(Type.EmptyTypes).Invoke(null);
    o.World = world;
    return o;
  }

  protected void ClearWeapons()       { guns.Clear(); specials.Clear(); }
  protected void AddGun(Type gun)     { guns.Add(gun); }
  protected void AddSpecial(Type gun) { specials.Add(gun); }
  protected byte FindGun(Weapon gun)     { return (byte)guns.IndexOf(gun.GetType()); }
  protected byte FindSpecial(Weapon gun) { return (byte)specials.IndexOf(gun.GetType()); }
  protected Weapon MakeGun(Ship ship, int index) { return MakeWeapon(ship, index, guns); }
  protected Weapon MakeSpecial(Ship ship, int index) { return MakeWeapon(ship, index, specials); }
  protected Weapon MakeWeapon(Ship ship, int index, ArrayList weaps)
  { return (Weapon)((Type)weaps[index]).GetConstructor(new Type[] { typeof(Ship) }).Invoke(new object[] { ship });
  }
  
  protected ArrayList guns = new ArrayList(), specials = new ArrayList();
  protected static readonly Type[] gunTypes = new Type[]
  { typeof(MachineGun), typeof(DualMachineGun), typeof(FBMachineGun), typeof(WavyMachineGun)
  };
  protected static readonly Type[] specialTypes = new Type[]
  { typeof(Cannon), typeof(GrenadeLauncher), typeof(Afterburner)
  };
  protected static readonly Type[] messageTypes = new Type[]
  { typeof(LoginMessage), typeof(LoadedMessage), typeof(InputMessage), typeof(SpawnMeMessage),
    typeof(LoginReturnMessage), typeof(MapInfoMessage), typeof(JoinedMessage), typeof(LeftMessage),
    typeof(AddObjectMessage), typeof(UpdateShipsMessage), typeof(UpdateShipMessage),
    typeof(UpdateWeapsMessage)
  };
  protected static readonly Type[] objectTypes = new Type[]
  { typeof(Bullet), typeof(CannonBall), typeof(Grenade), typeof(AfterburnerFlame)
  };
  
  protected World world;
}

} // namespace TriangleChases