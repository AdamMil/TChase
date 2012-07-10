using System;
using System.Collections.Generic;
using GameLib.Network;

namespace TriangleChase
{

public class Server : NetBase
{ public const int DefaultPort = 7892;

  public Server()
  { server = new GameLib.Network.Server();
    server.MessageReceived += server_MessageReceived;
    server.PlayerDisconnected += server_PlayerDisconnected;
    server.MessageConverter.RegisterTypes(messageTypes);
  }

  public System.Net.IPEndPoint LocalEndPoint { get { return server.LocalEndPoint; } }
  public int MaxPlayers;
  public string MapFile;
  
  public void Load(string mapfile)
  { Unload();
    System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
    xml.Load(Globals.MapPath+mapfile);
    world.Load(xml, true);

    string max = xml.DocumentElement.GetAttribute("maxPlayers");
    MaxPlayers = (max=="" ? 8 : int.Parse(max));

    System.Xml.XmlNode disallowed = xml.SelectSingleNode("map/disallow");
    HashSet<string> badTypes = new HashSet<string>();
    if(disallowed!=null)
      foreach(System.Xml.XmlNode node in disallowed.ChildNodes) badTypes.Add(node.InnerText);

    foreach(Type type in gunTypes)     if(!badTypes.Contains(type.ToString())) AddGun(type);
    foreach(Type type in specialTypes) if(!badTypes.Contains(type.ToString())) AddSpecial(type);
    
    MapFile = mapfile;
  }

  public void Unload()
  { world.Unload();
    ClearWeapons();
  }

  public void Start(string mapfile) { Start(mapfile, DefaultPort); }
  public void Start(string mapfile, int port)
  { Stop();
    Load(mapfile);
    Start(port);
  }
  public void Start(int port) { server.Listen(port); }

  public void Stop()
  {
    server.Deinitialize();
    players.Clear(); guns.Clear(); specials.Clear();
    Unload();
  }

  public void DoTicks()
  { world.DoTicks();
    if((world.Tick&7)==0)
    { lock(players)
      { UpdateShipsMessage usm = new UpdateShipsMessage(world);
        foreach(Player p in players.Values)
          if(p.LoggedIn)
          { 
            Send(p, usm, SendFlag.None, 66);
            Send(p, new UpdateShipMessage(p), SendFlag.None, 66);
          }
      }
    }
  }

  public void Send(Message msg, SendFlag flags) { Send(msg, flags, 0); }
  public void Send(Message msg, SendFlag flags, int timeoutMs) { server.SendToAll(msg, flags, timeoutMs); }
  public void Send(Player player, Message msg, SendFlag flags) { server.Send(player.Link, msg, flags, 0); }
  public void Send(Player player, Message msg, SendFlag flags, int timeoutMs)
  {
    server.Send(player.Link, msg, flags, timeoutMs);
  }
  public void SendExcept(Player player, Message msg, SendFlag flags) { SendExcept(player, msg, flags, 0); }
  public void SendExcept(Player player, Message msg, SendFlag flags, int timeoutMs)
  { 
    List<ServerPlayer> list = new List<ServerPlayer>(players.Count);
    foreach(Player p in players.Values) if(p.LoggedIn && p!=player) list.Add(p.Link);
    if(list.Count>0) server.Send(list, msg, flags, timeoutMs);
  }

  public void AddObject(GameObject o)
  { world.AddObject(o);
    Send(new AddObjectMessage(o), SendFlag.Reliable);
  }
  
  // TODO: add object removal support (automatic and manual)
  // TODO: reset weapons on spawn, transmit this

  void server_MessageReceived(GameLib.Network.Server sender, ServerPlayer player, object msg)
  { MessageType mtype = ((Message)msg).Type;
    Player      p = null;

    if(mtype != MessageType.Login)
    {
      if(!players.TryGetValue(player, out p)) { server.DropPlayer(player); return; }
    }

    switch(mtype)
    { case MessageType.Login:
      { LoginMessage m = (LoginMessage)msg;
        LoginReturnMessage.Status status;
        if(m.Protocol!=App.ProtoVersion) status = LoginReturnMessage.Status.BadProtocol;
        else if(m.Version!=App.Version) status = LoginReturnMessage.Status.BadVersion;
        else if(world.Players.Count>=MaxPlayers) status = LoginReturnMessage.Status.TooManyUsers;
        else
        { status = LoginReturnMessage.Status.Success;
          p = new Player(System.Text.Encoding.ASCII.GetString(m.Name, 0, m.NameLen), m.Team, player);
          p.Ship.ID = world.NextID;
          lock(players) players[player] = p;
        }
        server.Send(player, new LoginReturnMessage(status, p), SendFlag.Reliable);
        if(status==LoginReturnMessage.Status.Success)
          Send(p, new MapInfoMessage(world, MapFile, guns, specials), SendFlag.Reliable);
        else server.DropPlayerDelayed(player, 5000);
        break;
      }

      case MessageType.Loaded:
        p.Ship.Spawn(world.FindSpawnPoint());
        p.Ship.Gun = MakeGun(p.Ship, 0);
        p.Ship.Special = MakeSpecial(p.Ship, 0);
        world.AddPlayer(p);
        p.LoggedIn = true;
        Send(new JoinedMessage(p), SendFlag.Reliable);
        foreach(Player op in players.Values)
          if(op!=p) Send(p, new JoinedMessage(op), SendFlag.Reliable);
        break;
      
      case MessageType.Input:
      { InputMessage m = (InputMessage)msg;
        p.Inputs = m.Keys;
        break;
      }
      
      case MessageType.SpawnMe: p.Ship.Spawn(world.FindSpawnPoint()); break;
      
      case MessageType.UpdateWeaps:
      { UpdateWeapsMessage m = (UpdateWeapsMessage)msg;
        p.Ship.Gun           = MakeGun(p.Ship, m.Gun);
        p.Ship.Special       = MakeSpecial(p.Ship, m.Special);
        if(m.GunAmmo>p.Ship.Gun.MaxAmmo || m.SpecialAmmo>p.Ship.Special.MaxAmmo)
        { server.DropPlayer(player);
          return;
        }
        p.Ship.Gun.Ammo     = m.GunAmmo;
        p.Ship.Special.Ammo = m.SpecialAmmo;
        break;
      }

      default: server.DropPlayer(player); break;
    }
  }

  void server_PlayerDisconnected(GameLib.Network.Server server, ServerPlayer player)
  { Player p;
    if(players.TryGetValue(player, out p))
    { lock(players) players.Remove(player);
      lock(world) world.RemovePlayer(p);
      Send(new LeftMessage(p), SendFlag.Reliable);
    }
  }

  Dictionary<ServerPlayer, Player> players = new Dictionary<ServerPlayer, Player>();
  GameLib.Network.Server server;
}

} // namespace TriangleChase