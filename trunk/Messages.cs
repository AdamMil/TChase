using System;
using System.Collections;
using System.Runtime.InteropServices;
using GameLib.Network;
using GameLib.IO;

namespace TriangleChase
{

public enum MessageType : byte
{ // client to server
  Login, Loaded, Input, SpawnMe, Chat,
  // server to client
  LoginReturn, MapInfo, Joined, Left, AddObject, RemObject, Explosion, UpdatePos, Hit, RemCircles,
  AddObjects, UpdateShips, UpdateShip,
  // either way
  UpdateWeaps, Chatted,
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public abstract class Message
{ protected Message(MessageType type) { Type=type; }
  public MessageType Type;
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public class LoginMessage : Message
{ public LoginMessage() : base(MessageType.Login) { }
  public LoginMessage(string name, Team team) : base(MessageType.Login)
  { name = name.Length>64 ? name.Substring(0, 64) : name;
    Team    = team;
    NameLen = (byte)name.Length;
    Name    = new byte[64];
    Array.Copy(System.Text.Encoding.ASCII.GetBytes(name), Name, name.Length);
  }
  public Team Team;
  public int  Version  = App.Version;
  public byte Protocol = App.ProtoVersion;
  public byte NameLen;
  [MarshalAs(UnmanagedType.ByValArray, SizeConst=64)] public byte[] Name;
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public class LoadedMessage : Message
{ public LoadedMessage() : base(MessageType.Loaded) { }
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public class InputMessage : Message
{ public InputMessage() : base(MessageType.Input) { }
  [Flags] public enum Key : byte { None=0, Left=1, Right=2, Turn=Left|Right, Accel=4, Fire=8, Special=16 }

  public InputMessage(Key keys) : base(MessageType.Input) { Keys=keys; }
  public Key Keys;
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public class SpawnMeMessage : Message
{ public SpawnMeMessage() : base(MessageType.SpawnMe) { }
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public class LoginReturnMessage : Message
{ public enum Status { Success, BadProtocol, BadVersion, TooManyUsers, Banned };
  public LoginReturnMessage() : base(MessageType.LoginReturn) { }
  public LoginReturnMessage(Status status, Player p) : base(MessageType.LoginReturn)
  { LoginStatus = status;
    if(p!=null) ShipID = p.Ship.ID;
    ServerVersion = App.Version;
  }
  public Status LoginStatus;
  public uint   ShipID;
  public int    ServerVersion;
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public class MapInfoMessage : Message, INetSerializeable
{ public MapInfoMessage() : base(MessageType.MapInfo) { }
  public MapInfoMessage(World world, string mapName, ICollection gunTypes, ICollection specialTypes)
    : base(MessageType.MapInfo)
  { GravityX=world.Gravity.X; GravityY=world.Gravity.Y;
    Guns = new Type[gunTypes.Count];
    gunTypes.CopyTo(Guns, 0);
    Specials = new Type[specialTypes.Count];
    specialTypes.CopyTo(Specials, 0);
    MapName  = mapName;
  }
  public float  GravityX, GravityY;
  public Type[] Guns, Specials;
  public string MapName;

  public unsafe int SizeOf()
  { int length = sizeof(float)*2+MapName.Length+3; // 3 is namelen, gunslen, specialslen
    foreach(Type type in Guns)     { length += type.ToString().Length+1; }
    foreach(Type type in Specials) { length += type.ToString().Length+1; }
    return length;
  }

  public unsafe void SerializeTo(byte[] buf, int index)
  { IOH.WriteFloat(buf, index, GravityX); index += sizeof(float);
    IOH.WriteFloat(buf, index, GravityY); index += sizeof(float);
    byte[] bytes = System.Text.Encoding.ASCII.GetBytes(MapName);
    buf[index++] = (byte)bytes.Length;
    Array.Copy(bytes, 0, buf, index, bytes.Length); index += bytes.Length;
    buf[index++] = (byte)Guns.Length;
    foreach(Type type in Guns) index = WriteGun(type, buf, index);
    buf[index++] = (byte)Specials.Length;
    foreach(Type type in Specials) index = WriteGun(type, buf, index);
  }

  public unsafe void DeserializeFrom(byte[] buf, int index)
  { GravityX = IOH.ReadFloat(buf, index); index += sizeof(float);
    GravityY = IOH.ReadFloat(buf, index); index += sizeof(float);
    int namelen = buf[index++];
    MapName = System.Text.Encoding.ASCII.GetString(buf, index, namelen); index += namelen;
    Guns = new Type[buf[index++]];
    for(int i=0; i<Guns.Length; i++) Guns[i] = ReadGun(buf, ref index);
    Specials = new Type[buf[index++]];
    for(int i=0; i<Specials.Length; i++) Specials[i] = ReadGun(buf, ref index);
  }

  int WriteGun(Type type, byte[] buf, int index)
  { byte[] bytes = System.Text.Encoding.ASCII.GetBytes(type.ToString());
    buf[index++] = (byte)bytes.Length;
    Array.Copy(bytes, 0, buf, index, bytes.Length); index += bytes.Length;
    return index;
  }
  
  Type ReadGun(byte[] buf, ref int index)
  { byte[] name = new byte[buf[index++]];
    Array.Copy(buf, index, name, 0, name.Length); index += name.Length;
    return System.Type.GetType(System.Text.Encoding.ASCII.GetString(name));
  }
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public class JoinedMessage : Message
{ public JoinedMessage() : base(MessageType.Joined) { }
  public JoinedMessage(Player p) : base(MessageType.Joined)
  { string name = p.Name.Length>64 ? p.Name.Substring(0, 64) : p.Name;
    X=p.Ship.Pos.X; Y=p.Ship.Pos.Y; XV=p.Ship.Vel.X; YV=p.Ship.Vel.Y; Inputs=p.Inputs;
    Team    = p.Team;
    ShipID  = p.Ship.ID;
    NameLen = (byte)name.Length;
    NameBuf = new byte[64];
    Array.Copy(System.Text.Encoding.ASCII.GetBytes(name), NameBuf, name.Length);
  }

  public string Name { get { return System.Text.Encoding.ASCII.GetString(NameBuf, 0, NameLen); } }

  public float X, Y, XV, YV;
  public InputMessage.Key Inputs;
  public uint ShipID;
  public Team Team;
  public byte NameLen;
  [MarshalAs(UnmanagedType.ByValArray, SizeConst=64)] public byte[] NameBuf;
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public class LeftMessage : Message
{ public LeftMessage() : base(MessageType.Left) { }
  public LeftMessage(Player p) : base(MessageType.Left) { ShipID = p.Ship.ID; }
  public uint ShipID;
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public class AddObjectMessage : Message, INetSerializeable
{ public AddObjectMessage() : base(MessageType.AddObject) { }
  public AddObjectMessage(GameObject o) : base(MessageType.AddObject) { Object=o; }
  
  public GameObject Object;

  public int SizeOf() { return Object.SizeOf()+1; }
  public void SerializeTo(byte[] buf, int index)
  { buf[index++] = App.Server.FindObject(Object);
    Object.SerializeTo(buf, index);
  }
  public void DeserializeFrom(byte[] buf, int index)
  { Object = App.Client.MakeObject(buf[index++]);
    Object.DeserializeFrom(buf, index);
  }
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public class UpdateShipsMessage : Message, INetSerializeable
{ public UpdateShipsMessage() : base(MessageType.UpdateShips) { }
  public UpdateShipsMessage(World world) : base(MessageType.UpdateShips) { this.world=world; }

  public struct Update
  { public uint  ShipID;
    public float X, Y, XV, YV;
    public int   Angle;
    public InputMessage.Key Inputs;
  }
  public Update[] Updates;

  public unsafe int SizeOf()
  { int count = 0;
    foreach(Player p in world.Players) if(p.LoggedIn) count++;
    return count*(sizeof(float)*4 + sizeof(int)*2 + 1) + 1;
  }

  public unsafe void SerializeTo(byte[] buf, int index)
  { int count = 0;
    foreach(Player p in world.Players) if(p.LoggedIn) count++;
    buf[index++] = (byte)count;
    foreach(Player p in world.Players)
      if(p.LoggedIn)
      { IOH.WriteLE4(buf, index, (int)p.Ship.ID); index += 4;
        IOH.WriteFloat(buf, index, p.Ship.Pos.X); index += sizeof(float);
        IOH.WriteFloat(buf, index, p.Ship.Pos.Y); index += sizeof(float);
        IOH.WriteFloat(buf, index, p.Ship.Vel.X); index += sizeof(float);
        IOH.WriteFloat(buf, index, p.Ship.Vel.Y); index += sizeof(float);
        IOH.WriteLE4(buf, index, p.Ship.Angle); index += 4;
        buf[index++] = (byte)p.Inputs;
      }
  }

  public unsafe void DeserializeFrom(byte[] buf, int index)
  { Updates = new Update[buf[index++]];
    for(int i=0; i<Updates.Length; i++)
    { Updates[i].ShipID = IOH.ReadLE4U(buf, index);  index += 4;
      Updates[i].X      = IOH.ReadFloat(buf, index); index += sizeof(float);
      Updates[i].Y      = IOH.ReadFloat(buf, index); index += sizeof(float);
      Updates[i].XV     = IOH.ReadFloat(buf, index); index += sizeof(float);
      Updates[i].YV     = IOH.ReadFloat(buf, index); index += sizeof(float);
      Updates[i].Angle  = IOH.ReadLE4(buf, index);   index += 4;
      Updates[i].Inputs = (InputMessage.Key)buf[index++];
    }
  }
  
  World world;
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public class UpdateShipMessage : Message
{ public UpdateShipMessage() : base(MessageType.UpdateShip) { }
  public UpdateShipMessage(Player p) : base(MessageType.UpdateShip)
  { Health=p.Ship.Health; Fuel=p.Ship.Fuel; GunAmmo=p.Ship.Gun.Ammo; SpecialAmmo=p.Ship.Special.Ammo;
  }
  public int Health, Fuel, GunAmmo, SpecialAmmo;
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public class UpdateWeapsMessage : Message
{ public UpdateWeapsMessage() : base(MessageType.UpdateWeaps) { }
  public UpdateWeapsMessage(int gunAmmo, int specAmmo, byte gun, byte special) : base(MessageType.UpdateWeaps)
  { GunAmmo=gunAmmo; SpecialAmmo=specAmmo; Gun=gun; Special=special;
  }
  public int  GunAmmo, SpecialAmmo;
  public byte Gun, Special;
}

} // namespace TriangleChase