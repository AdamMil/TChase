using System;
using System.Collections;
using GameLib.Network;

namespace TriangleChase
{

public class Server
{ public const int DefaultPort = 7892;

  public Server()
  { world  = new World();
    server = new GameLib.Network.Server();
    server.MessageReceived += new ServerReceivedHandler(server_MessageReceived);
    server.PlayerDisconnected += new PlayerDisconnectHandler(server_PlayerDisconnected);
  }

  public System.Net.IPEndPoint LocalEndPoint { get { return server.LocalEndPoint; } }
  
  public void Start(string mapfile) { Start(mapfile, DefaultPort); }
  public void Start(string mapfile, int port)
  { Stop();

    System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
    xml.Load(mapfile);
    world.Load(xml, true);

    string max = xml.DocumentElement.GetAttribute("maxPlayers");
    maxPlayers = (max=="" ? -1 : int.Parse(max));
    
    System.Xml.XmlNode disallowed = xml.SelectSingleNode("map/disallow");
    Hashtable badTypes = new Hashtable();
    if(disallowed!=null)
      foreach(System.Xml.XmlNode node in disallowed.ChildNodes) badTypes[node.Value] = true;

    ClearWeapons();
    foreach(Type type in gunTypes)     if(!badTypes.Contains(type.ToString())) AddGun(type);
    foreach(Type type in specialTypes) if(!badTypes.Contains(type.ToString())) AddSpecial(type);

    server.Listen(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));
  }

  public void Stop()
  { server.Close();
    world.Unload();
  }

  public bool DoTicks() { return world.DoTicks(); }

  public void Send(Message msg, SendFlag flags) { server.Send(null, msg, flags, 0); }
  public void Send(Message msg, SendFlag flags, uint timeoutMs) { server.Send(null, msg, flags, timeoutMs); }
  public void Send(Player player, Message msg, SendFlag flags, uint timeoutMs)
  { server.Send(player.Link, msg, flags, timeoutMs);
  }
  
  void ClearWeapons()       { guns.Clear(); specials.Clear(); }
  void AddGun(Type gun)     { guns.Add(gun); }
  void AddSpecial(Type gun) { specials.Add(gun); }
  byte FindGun(Weapon gun)     { return (byte)guns.IndexOf(gun.GetType()); }
  byte FindSpecial(Weapon gun) { return (byte)specials.IndexOf(gun.GetType()); }

  void server_MessageReceived(GameLib.Network.Server sender, ServerPlayer player, object msg)
  {
  }

  void server_PlayerDisconnected(GameLib.Network.Server server, ServerPlayer player)
  {
  }

  ArrayList guns = new ArrayList(), specials = new ArrayList();
  GameLib.Network.Server server;
  World world;
  int maxPlayers;

  static readonly Type[] gunTypes = new Type[]
  { typeof(MachineGun), typeof(DualMachineGun), typeof(FBMachineGun), typeof(WavyMachineGun)
  };
  static readonly Type[] specialTypes = new Type[]
  { typeof(Cannon), typeof(GrenadeLauncher), typeof(Afterburner)
  };
}

} // namespace TriangleChase