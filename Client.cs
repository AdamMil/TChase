using System;
using System.Collections.Generic;
using GameLib.Events;
using GameLib.Input;
using GameLib.Video;
using GameLib.Network;
using AdamMil.Mathematics.Geometry;
using GameLib;

namespace TriangleChase
{

public enum State { Connecting, Loading, ConnFailed, InPlay, BaseMenu, MaybeQuit, Disconnected }

public class Client : NetBase
{ public Client()
  {
    client = new GameLib.Network.Client() { UseThread = false };
    client.Disconnected += client_Disconnected;
    client.MessageReceived += client_MessageReceived;
    client.MessageConverter.RegisterTypes(messageTypes);
  }
  
  public State State
  { get { return state; }
    set
    { SetKeyHandler(value!=State.InPlay);
      if(value==State.BaseMenu) menuGun=true;
      lastState = state;
      state = value;
    }
  }

  public bool IsConnected { get { return client.IsConnected; } }

  public void Connect(System.Net.IPEndPoint server, string name, Team team)
  { Disconnect();
    lastState = State.InPlay;
    state     = State.Connecting;
    messages.Clear();

    client.Connect(server);
    client.Send(new LoginMessage(name, team), SendFlag.Reliable);
  }

  public void Disconnect()
  { client.Disconnect();
    world.Unload();
    me = null;
    ClearWeapons();
  }
  
  public bool DoTicks()
  {
    client.Poll();
    DoInput();
    return world.Loaded ? world.DoTicks() : true;
  }

  public void Send(Message msg, SendFlag flags) { Send(msg, flags, 0); }
  public void Send(Message msg, SendFlag flags, int timeoutMs) { client.Send(msg, flags, timeoutMs); }

  public void Render(Surface display)
  { if(world.Loaded) world.Render(display);
    else display.Fill();

    if(me!=null)
    { int bx=4, by=3, bw=40, bh=2, hw=bw*me.Ship.Health/me.Ship.MaxHealth, fw=bw*me.Ship.Fuel/me.Ship.MaxFuel,
          gw=me.Ship.Gun==null ? 0 : bw*me.Ship.Gun.Ammo/me.Ship.Gun.MaxAmmo,
          sw=me.Ship.Special==null ? 0 : bw*me.Ship.Special.Ammo/me.Ship.Special.MaxAmmo;
      if(hw>0) Shapes.FilledBox(display, bx, by, bx+hw, by+bh, Colors.LtRed);
      if(fw>0) Shapes.FilledBox(display, bx, by+bh+2,   bx+fw, by+bh*2+2, Colors.LtBlue);
      if(gw>0) Shapes.FilledBox(display, bx, by+bh*2+4, bx+gw, by+bh*3+4, Colors.Green);
      if(sw>0) Shapes.FilledBox(display, bx, by+bh*3+6, bx+sw, by+bh*4+6, Colors.LtGrey);
    }

    { int x=display.Width-5, y=0;
      foreach(Player p in world.Players)
        if(p!=me)
        { int wid = Globals.Font.CalculateSize(p.Name).Width;
          Globals.Font.Color = p.Ship.Color;
          Globals.Font.Render(display, p.Name, x-wid, y);
          y += Globals.Font.LineHeight;
        }
    }

    lock(messages)
    {
      LinkedListNode<TextMessage> node = messages.First;
      if(node!=null)
      { int i=0, y=0;
        bool shift = world.Tick>lastMsgTime+(node.Value).Text.Length*world.TPS/15; // assume 15 cps reading
        do
        {
          Globals.Font.Color = node.Value.Color;
          Globals.Font.Center(display, node.Value.Text, y);
          y += Globals.Font.LineHeight;
          node = node.Next;
        }
        while(node!=null && ++i<3);
        if(shift) messages.Remove(messages.First);
      }
    }

    switch(State)
    { case State.InPlay:
        Globals.Font.Color = Colors.White;
        if(me.Ship.Dead) Globals.Font.Center(display, "Press [space] to respawn!");
        else if(me.Ship.OnBase && me.Ship.Health>0) Globals.Font.Center(display, "Press [enter] to dock", (display.Height-Globals.Font.LineHeight)/2 - 50);
        break;
      case State.BaseMenu:
        { int x=10, y=120, wid = Globals.Font.CalculateSize("Special: ").Width;
          Globals.Font.Color = menuGun ? Colors.White : Colors.LtGrey;
          Globals.Font.Render(display, "Gun:", x, y);
          Globals.Font.Render(display, me.Ship.Gun.Name, x+wid, y);
          Globals.Font.Color = !menuGun ? Colors.White : Colors.LtGrey;
          Globals.Font.Render(display, String.Format("Special: {0}", me.Ship.Special.Name), x, y+Globals.Font.LineHeight);
        }
        break;
      case State.Connecting:
        Globals.Font.Color = Colors.White;
        Globals.Font.Center(display, "Connecting... press [escape] to abort");
        break;
      case State.Loading:
        Globals.Font.Color = Colors.White;
        Globals.Font.Center(display, "Loading map... press [escape] to abort");
        break;
      case State.ConnFailed:
        string msg = "Failed, ";
        switch(failReason)
        { case LoginReturnMessage.Status.BadProtocol: case LoginReturnMessage.Status.BadVersion:
            msg += String.Format("invalid version. Server is running version {0}", serverVersion/100f);
            break;
          case LoginReturnMessage.Status.Banned: msg += "this client has been banned."; break;
          case LoginReturnMessage.Status.TooManyUsers: msg += "the server is full."; break;
          default: msg += "unknown reason."; break;
        }
        Globals.Font.Color = Colors.White;
        Globals.Font.Center(display, msg+" Press any key.");
        break;
      case State.MaybeQuit:
        Globals.Font.Color = Colors.White;
        Globals.Font.Center(display, "Press [escape] again to disconnect");
        break;
      case State.Disconnected:
        Globals.Font.Color = Colors.White;
        Globals.Font.Center(display, "Connection lost. Press any key to quit");
        break;
    }
  }

  public void DoInput()
  { switch(State)
    { case TriangleChase.State.InPlay:
        if(me.Ship.Dead && (Keyboard.PressedRel(Key.Enter) || Keyboard.PressedRel(Key.Space)))
          Send(new SpawnMeMessage(), SendFlag.Reliable);
        if(me.Ship.Health>0)
        { InputMessage.Key state = InputMessage.Key.None;
          if(Keyboard.Pressed(Key.Left))     state |= InputMessage.Key.Left;
          if(Keyboard.Pressed(Key.Right))    state |= InputMessage.Key.Right;
          if(Keyboard.Pressed(Key.Up))       state |= InputMessage.Key.Accel;
          if(Keyboard.Pressed(Key.LeftCtrl)) state |= InputMessage.Key.Fire;
          if(Keyboard.Pressed(Key.Space))    state |= InputMessage.Key.Special;
          if(state != me.Inputs && State==State.InPlay)
          { me.Inputs = state;
            Send(new InputMessage(me.Inputs=state), SendFlag.Reliable|SendFlag.HighPriority);
          }
          if(me.Ship.OnBase && Keyboard.PressedRel(Key.Enter)) State = State.BaseMenu;
        }
        if(Keyboard.PressedRel(Key.Escape)) State = State.MaybeQuit;
        break;

      case TriangleChase.State.BaseMenu:
        while(keys.Count>0)
        { KeyboardEvent kb = keys.Dequeue();
          bool changed = false;
          if(kb.Key==Key.Up || kb.Key==Key.Down) menuGun = !menuGun;
          else if(kb.Key==Key.Left)
          { if(menuGun) me.Ship.Gun = PrevWeapon(me.Ship.Gun, guns);
            else me.Ship.Special = PrevWeapon(me.Ship.Special, specials);
            changed = true;
          }
          else if(kb.Key==Key.Right)
          { if(menuGun) me.Ship.Gun = NextWeapon(me.Ship.Gun, guns);
            else me.Ship.Special = NextWeapon(me.Ship.Special, specials);
            changed = true;
          }
          else if(kb.Key==Key.Escape || kb.Key==Key.Enter)
          { changed = true;
            State=lastState;
          }
          if(changed)
            Send(new UpdateWeapsMessage(me.Ship.Gun.Ammo, me.Ship.Special.Ammo, FindGun(me.Ship.Gun),
                                        FindSpecial(me.Ship.Special)),
                 SendFlag.Reliable);
          if(State==lastState) break;
        }
        break;
      
      case TriangleChase.State.Connecting: case TriangleChase.State.Loading:
        while(keys.Count>0)
        { KeyboardEvent kb = keys.Dequeue();
          if(kb.Key==Key.Escape) Disconnect();
        }
        break;

      case TriangleChase.State.ConnFailed:
        if(keys.Count>0)
        { keys.Dequeue();
          Disconnect();
        }
        break;

      case TriangleChase.State.MaybeQuit:
        if(keys.Count>0)
        { KeyboardEvent kb = (KeyboardEvent)keys.Dequeue();
          if(kb.Key==Key.Escape) Disconnect();
          else State=lastState;
        }
        break;

      case TriangleChase.State.Disconnected:
        if(keys.Count>0) Events.PushEvent(new QuitEvent());
        break;
    }
  }

  struct TextMessage
  { public TextMessage(Color color, string text) { Color=color; Text=text; }
    public string Text;
    public Color Color;
  }

  void Start()
  { world.Start();
    lastMsgTime = world.Tick;
  }
  
  void SetKeyHandler(bool handler)
  { if(handler)
    { if(keyHandler==null)
      { keyHandler = OnKey;
        Keyboard.KeyEvent += keyHandler;
      }
    }
    else if(keyHandler!=null)
    { Keyboard.KeyEvent -= keyHandler;
      keyHandler = null;
      keys.Clear();
    }
  }
  
  void AddMessage(string message) { AddMessage(Colors.White, message); }
  void AddMessage(string format, params object[] args) { AddMessage(Colors.White, String.Format(format, args)); }
  void AddMessage(Color color, string message) { lock(messages) messages.AddLast(new TextMessage(color, message)); }
  void AddMessage(Color color, string format, params object[] args) { AddMessage(color, String.Format(format, args)); }

  void OnKey(KeyboardEvent e) { if(e.Down) { keys.Enqueue(e); Keyboard.Press(e.Key, false); } }

  Weapon PrevWeapon(Weapon weap, List<Type> weaps) { return NextWeapon(weap, weaps, -1); }
  Weapon NextWeapon(Weapon weap, List<Type> weaps) { return NextWeapon(weap, weaps, 1); }
  Weapon NextWeapon(Weapon weap, List<Type> weaps, int offset)
  { int index = weaps.IndexOf(weap.GetType())+offset;
    if(index<0) index=weaps.Count-1;
    else if(index>=weaps.Count) index=0;
    Weapon newWeap = MakeWeapon(me.Ship, index, weaps);
    newWeap.Ammo = newWeap.Ammo*weap.Ammo/(weap.MaxAmmo*2);
    return newWeap;
  }

  void client_Disconnected(object sender) { State = State.Disconnected; }

  void client_MessageReceived(GameLib.Network.Client client, object msg)
  {
    switch(((Message)msg).Type)
    { case MessageType.UpdateShips:
      { 
        UpdateShipsMessage m = (UpdateShipsMessage)msg;
        foreach(UpdateShipsMessage.Update u in m.Updates)
        {
          Ship ship  = world.FindShip(u.ShipID);
          ship.Pos   = new Vector2(u.X,  u.Y);
          ship.Vel   = new Vector2(u.XV, u.YV);
          ship.Angle = u.Angle;
          foreach(Player p in world.Players) if(p.Ship==ship) { p.Inputs=u.Inputs; break; }
        }
        break;
      }

      case MessageType.UpdateShip:
      { UpdateShipMessage m = (UpdateShipMessage)msg;
        if(me!=null)
        { me.Ship.Health       = m.Health;
          me.Ship.Fuel         = m.Fuel;
          me.Ship.Gun.Ammo     = m.GunAmmo;
          me.Ship.Special.Ammo = m.SpecialAmmo;
          me.Ship.Dead         = m.Health<=0;  // TODO: doesn't work for other ships, make a 'Flags' member of 'm'
        }
        break;
      }

      case MessageType.AddObject: world.AddObject(((AddObjectMessage)msg).Object); break;

      case MessageType.LoginReturn:
        if(state==State.Connecting)
        { LoginReturnMessage m = (LoginReturnMessage)msg;
          if(m.LoginStatus==LoginReturnMessage.Status.Success)
          { myID  = m.ShipID;
            State = State.Loading;
          }
          else
          { failReason    = m.LoginStatus;
            serverVersion = m.ServerVersion;
            State         = State.ConnFailed;
          }
        }
        break;

      case MessageType.MapInfo:
        if(state==State.Loading)
        { MapInfoMessage m = (MapInfoMessage)msg;

          world.Load(m.MapName, false);
          world.Gravity = new Vector2(m.GravityX, m.GravityY);

          ClearWeapons();
          foreach(Type type in m.Guns)     AddGun(type);
          foreach(Type type in m.Specials) AddSpecial(type);

          Start();
          Send(new LoadedMessage(), SendFlag.Reliable);
        }
        break;

      case MessageType.Joined:
      { JoinedMessage m = (JoinedMessage)msg;
        Player p   = new Player(m.Name, m.Team);
        p.Ship.ID  = m.ShipID;
        p.Ship.Pos = new Vector2(m.X,  m.Y);
        p.Ship.Vel = new Vector2(m.XV, m.YV);
        p.Inputs   = m.Inputs;
        p.Ship.Gun = MakeGun(p.Ship, 0);
        p.Ship.Special = MakeSpecial(p.Ship, 0);
        world.AddPlayer(p);
        if(p.Ship.ID==myID)
        { world.Focus = p.Ship;
          me = p;
          State = State.InPlay;
        }
        else AddMessage(String.Format("{0} has joined the {1} team!", p.Name, p.Team));
        break;
      }
      default: Disconnect(); break;
    }
  }

  LoginReturnMessage.Status failReason;
  int  serverVersion;
  uint myID, lastMsgTime;

  Queue<KeyboardEvent> keys = new Queue<KeyboardEvent>();
  KeyEventHandler keyHandler;

  GameLib.Network.Client client;
  
  Player me;

  LinkedList<TextMessage> messages = new LinkedList<TextMessage>();
  State state, lastState;
  bool  menuGun;
}

} // namespace TriangleChase