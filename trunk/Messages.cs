using System;
using System.Runtime.InteropServices;
using GameLib.Network;

namespace TriangleChase
{

public enum MessageType : byte
{ // client to server
  Login, Input, SpawnMe, 
  // server to client
  MapInfo, LoginReturn, Joined, Left, Spawned, AddObject, RemObject, Explosion, UpdatePos, Hit, RemCircles,
  AddObjects, UpdateShips,
  // either way
  UpdateWeaps,
}

[StructLayout(LayoutKind.Sequential, Pack=1)]
public abstract class Message
{ protected Message(MessageType type) { Type=type; }
  public MessageType Type;
}

public class Login : Message
{ public Login(string name, Team team) : base(MessageType.Login)
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

public class InputMessage : Message
{ [Flags] public enum Key : byte { None=0, Left=1, Right=2, Turn=Left|Right, Accel=4, Fire=8, Special=16 }

  public InputMessage(Key keys) : base(MessageType.Input) { Keys=keys; }
  public Key Keys;
}

public class SpawnMeMessage : Message
{ public SpawnMeMessage() : base(MessageType.SpawnMe) { }
}

public class UpdateWeapsMessage : Message
{ public UpdateWeapsMessage(int gunAmmo, int specAmmo, byte gun, byte special) : base(MessageType.UpdateWeaps)
  { GunAmmo=gunAmmo; SpecialAmmo=specAmmo; Gun=gun; Special=special;
  }
  public int  GunAmmo, SpecialAmmo;
  public byte Gun, Special;
}

} // namespace TriangleChase