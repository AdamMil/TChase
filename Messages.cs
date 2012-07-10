using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GameLib.Network;
using AdamMil.IO;

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
public class MapInfoMessage : Message, INetSerializable
{ public MapInfoMessage() : base(MessageType.MapInfo) { }
  public MapInfoMessage(World world, string mapName, ICollection<Type> gunTypes, ICollection<Type> specialTypes)
    : base(MessageType.MapInfo)
  { GravityX = world.Gravity.X; GravityY = world.Gravity.Y;
    Guns = new Type[gunTypes.Count];
    gunTypes.CopyTo(Guns, 0);
    Specials = new Type[specialTypes.Count];
    specialTypes.CopyTo(Specials, 0);
    MapName  = mapName;
  }
  public double GravityX, GravityY;
  public Type[] Guns, Specials;
  public string MapName;

  public void Serialize(BinaryWriter writer, out System.IO.Stream attachedStream)
  {
    attachedStream = null;
    writer.Write(GravityX);
    writer.Write(GravityY);
    writer.WriteStringWithLength(MapName);
    writer.WriteEncoded(Guns.Length);
    foreach(Type type in Guns) WriteGun(writer, type);
    writer.WriteEncoded(Specials.Length);
    foreach(Type type in Specials) WriteGun(writer, type);
  }

  public unsafe void Deserialize(BinaryReader reader, System.IO.Stream attachedStream)
  {
    GravityX = reader.ReadDouble();
    GravityY = reader.ReadDouble();
    MapName  = reader.ReadStringWithLength();
    Guns     = new Type[reader.ReadEncodedInt32()];
    for(int i=0; i<Guns.Length; i++) Guns[i] = ReadGun(reader);
    Specials = new Type[reader.ReadEncodedInt32()];
    for(int i=0; i<Specials.Length; i++) Specials[i] = ReadGun(reader);
  }

  void WriteGun(BinaryWriter writer, Type type)
  {
    writer.WriteStringWithLength(type.ToString());
  }
  
  Type ReadGun(BinaryReader reader)
  { 
    return System.Type.GetType(reader.ReadStringWithLength());
  }
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public class JoinedMessage : Message
{ public JoinedMessage() : base(MessageType.Joined) { }
  public JoinedMessage(Player p) : base(MessageType.Joined)
  { string name = p.Name.Length>64 ? p.Name.Substring(0, 64) : p.Name;
    X = (float)p.Ship.Pos.X; Y = (float)p.Ship.Pos.Y; XV = (float)p.Ship.Vel.X; YV = (float)p.Ship.Vel.Y; Inputs=p.Inputs;
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
public class AddObjectMessage : Message, INetSerializable
{ public AddObjectMessage() : base(MessageType.AddObject) { }
  public AddObjectMessage(GameObject o) : base(MessageType.AddObject) { Object=o; }
  
  public GameObject Object;

  public void Serialize(BinaryWriter writer, out System.IO.Stream attachedStream)
  {
    writer.Write(App.Server.FindObject(Object));
    Object.Serialize(writer, out attachedStream);
  }
  public void Deserialize(BinaryReader reader, System.IO.Stream attachedStream)
  { 
    Object = App.Client.MakeObject(reader.ReadByte());
    Object.Deserialize(reader, attachedStream);
  }
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public class UpdateShipsMessage : Message, INetSerializable
{ public UpdateShipsMessage() : base(MessageType.UpdateShips) { }
  public UpdateShipsMessage(World world) : base(MessageType.UpdateShips) { this.world=world; }

  public struct Update
  { public uint  ShipID;
    public float X, Y, XV, YV;
    public int   Angle;
    public InputMessage.Key Inputs;
  }
  public Update[] Updates;

  public unsafe void Serialize(BinaryWriter writer, out System.IO.Stream attachedStream)
  {
    attachedStream = null;
    int count = 0;
    foreach(Player p in world.Players) if(p.LoggedIn) count++;
    writer.WriteEncoded(count);
    foreach(Player p in world.Players)
      if(p.LoggedIn)
      {
        writer.WriteEncoded(p.Ship.ID);
        writer.Write((float)p.Ship.Pos.X);
        writer.Write((float)p.Ship.Pos.Y);
        writer.Write((float)p.Ship.Vel.X);
        writer.Write((float)p.Ship.Vel.Y);
        writer.Write((byte)p.Ship.Angle);
        writer.Write((byte)p.Inputs);
      }
  }

  public unsafe void Deserialize(BinaryReader reader, System.IO.Stream attachedStream)
  { 
    Updates = new Update[reader.ReadEncodedInt32()];
    for(int i=0; i<Updates.Length; i++)
    {
      Updates[i].ShipID = reader.ReadEncodedUInt32();
      Updates[i].X      = reader.ReadSingle();
      Updates[i].Y      = reader.ReadSingle();
      Updates[i].XV     = reader.ReadSingle();
      Updates[i].YV     = reader.ReadSingle();
      Updates[i].Angle  = reader.ReadByte();
      Updates[i].Inputs = (InputMessage.Key)reader.ReadByte();
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