using System;
using System.Collections;
using GameLib.Events;
using GameLib.Input;
using GameLib.Video;
using GameLib.Network;

namespace TriangleChase
{

public class Client
{ public Client()
  { world  = new World();
    client = new GameLib.Network.Client();
    client.Disconnected += new DisconnectHandler(client_Disconnected);
    client.MessageReceived += new ClientReceivedHandler(client_MessageReceived);
    keyHandler = new KeyPressHandler(OnKey);
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

  public void Connect(System.Net.IPEndPoint server)
  { Disconnect();
    client.Connect(server);
    // load map
    // load weapons
    // get players
    lastState = state = State.InPlay;
  }

  public void Disconnect()
  { client.Disconnect();
    world.Unload();
    ClearWeapons();
  }
  
  public bool DoTicks() { return world.DoTicks(); }

  public void Send(Message msg, SendFlag flags) { client.Send(msg, flags, 0); }
  public void Send(Message msg, SendFlag flags, uint timeoutMs) { client.Send(msg, flags, timeoutMs); }

  public void Render(Surface display)
  { world.Render(display);

    if(me!=null)
    { int bx=4, by=3, bw=40, bh=2, hw=bw*me.Ship.Health/me.Ship.MaxHealth, fw=bw*me.Ship.Fuel/me.Ship.MaxFuel,
          gw=me.Ship.Gun==null ? 0 : bw*me.Ship.Gun.Ammo/me.Ship.Gun.MaxAmmo,
          sw=me.Ship.Special==null ? 0 : bw*me.Ship.Special.Ammo/me.Ship.Special.MaxAmmo;
      if(hw>0) Primitives.FilledBox(display, bx, by, bx+hw, by+bh, Colors.LtRed);
      if(fw>0) Primitives.FilledBox(display, bx, by+bh+2,   bx+fw, by+bh*2+2, Colors.LtBlue);
      if(gw>0) Primitives.FilledBox(display, bx, by+bh*2+4, bx+gw, by+bh*3+4, Colors.Green);
      if(sw>0) Primitives.FilledBox(display, bx, by+bh*3+6, bx+sw, by+bh*4+6, Colors.LtGrey);
    }

    { int x=display.Width-5, y=0;
      foreach(Player p in world.Players)
        if(p!=me)
        { int wid = Globals.Font.CalculateSize(p.Name).Width;
          Globals.Font.Color = p.Ship.Color;
          Globals.Font.Render(display, p.Name, x-wid, y);
          y += Globals.Font.LineSkip;
        }
    }

    switch(State)
    { case State.InPlay:
        Globals.Font.Color = Colors.White;
        if(me.Ship.Dead) Globals.Font.Center(display, "Press [space] to respawn!");
        else if(me.Ship.OnBase && me.Ship.Health>0) Globals.Font.Center(display, "Press [enter] to dock", -50);
        break;
      case State.BaseMenu:
        { int x=10, y=120, wid = Globals.Font.CalculateSize("Special: ").Width;
          Globals.Font.Color = menuGun ? Colors.White : Colors.LtGrey;
          Globals.Font.Render(display, "Gun:", x, y);
          Globals.Font.Render(display, me.Ship.Gun.Name, x+wid, y);
          Globals.Font.Color = !menuGun ? Colors.White : Colors.LtGrey;
          Globals.Font.Render(display, String.Format("Special: {0}", me.Ship.Special.Name), x, y+Globals.Font.LineSkip);
        }
        break;
      case State.MaybeQuit:
        Globals.Font.Color = Colors.White;
        Globals.Font.Center(display, "Press [escape] again to quit");
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
        if(me.Ship.Dead && (Keyboard.PressedRel(Key.Return) || Keyboard.PressedRel(Key.Space)))
          Send(new SpawnMeMessage(), SendFlag.ReliableSequential);
        if(me.Ship.Health>0)
        { InputMessage.Key state = InputMessage.Key.None;
          if(Keyboard.Pressed(Key.Left))  state |= InputMessage.Key.Left;
          if(Keyboard.Pressed(Key.Right)) state |= InputMessage.Key.Right;
          if(Keyboard.Pressed(Key.Up))    state |= InputMessage.Key.Accel;
          if(Keyboard.Pressed(Key.LCtrl)) state |= InputMessage.Key.Fire;
          if(Keyboard.Pressed(Key.Space)) state |= InputMessage.Key.Special;
          me.Ship.ApplyKeys(state);
          if(state != keyState) Send(new InputMessage(keyState=state), SendFlag.ReliableSequential);

          if(me.Ship.OnBase && Keyboard.PressedRel(Key.Return)) State = State.BaseMenu;
        }
        if(Keyboard.PressedRel(Key.Escape)) State = State.MaybeQuit;
        break;

      case TriangleChase.State.BaseMenu:
        while(keys.Count>0)
        { KeyboardEvent kb = (KeyboardEvent)keys.Dequeue();
          if(kb.Key==Key.Up || kb.Key==Key.Down) menuGun = !menuGun;
          else if(kb.Key==Key.Left)
          { if(menuGun) me.Ship.Gun = PrevWeapon(me.Ship.Gun, guns);
            else me.Ship.Special = PrevWeapon(me.Ship.Special, specials);
          }
          else if(kb.Key==Key.Right)
          { if(menuGun) me.Ship.Gun = NextWeapon(me.Ship.Gun, guns);
            else me.Ship.Special = NextWeapon(me.Ship.Special, specials);
          }
          else if(kb.Key==Key.Escape || kb.Key==Key.Return)
          { Send(new UpdateWeapsMessage(me.Ship.Gun.Ammo, me.Ship.Special.Ammo, FindGun(me.Ship.Gun),
                                        FindSpecial(me.Ship.Special)),
                 SendFlag.ReliableSequential|SendFlag.HighPriority);
            State=lastState; break;
          }
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

  void SetKeyHandler(bool handler)
  { if(handler)
    { if(keyHandler==null)
      { keyHandler = new KeyPressHandler(OnKey);
        Keyboard.KeyPress += keyHandler;
      }
    }
    else if(keyHandler!=null)
    { Keyboard.KeyPress -= keyHandler;
      keyHandler = null;
      keys.Clear();
    }
  }
  
  void OnKey(KeyboardEvent e) { if(e.Down) { keys.Enqueue(e); Keyboard.Release(e.Key); } }

  void ClearWeapons()          { guns.Clear(); specials.Clear(); }
  void AddGun(Type gun)        { guns.Add(gun); }
  void AddSpecial(Type gun)    { specials.Add(gun); }
  byte FindGun(Weapon gun)     { return (byte)guns.IndexOf(gun.GetType()); }
  byte FindSpecial(Weapon gun) { return (byte)specials.IndexOf(gun.GetType()); }

  Weapon MakeGun(Ship ship, int index) { return MakeWeapon(ship, index, guns); }
  Weapon MakeSpecial(Ship ship, int index) { return MakeWeapon(ship, index, specials); }
  Weapon PrevWeapon(Weapon weap, ArrayList weaps) { return NextWeapon(weap, weaps, -1); }
  Weapon NextWeapon(Weapon weap, ArrayList weaps) { return NextWeapon(weap, weaps, 1); }
  Weapon NextWeapon(Weapon weap, ArrayList weaps, int offset)
  { int index = weaps.IndexOf(weap.GetType())+offset;
    if(index<0) index=weaps.Count-1;
    else if(index>=weaps.Count) index=0;
    Weapon newWeap = MakeWeapon(me.Ship, index, weaps);
    newWeap.Ammo = newWeap.Ammo*weap.Ammo/(weap.MaxAmmo*2);
    return newWeap;
  }
  Weapon MakeWeapon(Ship ship, int index, ArrayList weaps)
  { return (Weapon)((Type)weaps[index]).GetConstructor(new Type[] { typeof(Ship) }).Invoke(new object[] { ship });
  }

  void client_Disconnected(object sender) { State = State.Disconnected; }

  void client_MessageReceived(GameLib.Network.Client client, object msg)
  {
  }

  Queue keys = new Queue();
  InputMessage.Key keyState;
  KeyPressHandler keyHandler;

  GameLib.Network.Client client;
  World world;
  
  Player me;

  ArrayList guns = new ArrayList(), specials = new ArrayList();
  State state, lastState;
  bool  menuGun;
}

} // namespace TriangleChase