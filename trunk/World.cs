using System;
using System.Drawing;
using System.IO;
using System.Collections;
using GameLib.Events;
using GameLib.Video;
using GameLib.Input;
using GameLib.Collections;
using GameLib.Mathematics;

namespace TriangleChase
{

public enum Explosion
{ Tiny, Small, Medium, Large, Huge, Armageddon
}
public enum State
{ InPlay, BaseMenu, MaybeQuit
}
public enum Team
{ Green, Blue, Red
}
public class World
{ public World() { keyHandler = new KeyPressHandler(OnKey); }

  public void Load(string foreground, string background, string overlay)
  { fore = new Surface(Globals.DataPath+foreground).CloneDisplay(false);
    back = new Surface(Globals.DataPath+background).CloneDisplay(false);
    over = new Surface(Globals.DataPath+overlay);
    fore.SetColorKey(Colors.Transparent);
    tick = 0;

    objects.Clear();
    guns.Clear();
    specials.Clear();
    netGame = false;
  }

  public int nObjects;
  
  public Player Me
  { get { return me; }
    set { me = (Player)players[players.IndexOf(value)]; if(focus==null) focus=me.Ship; } // throws exception if not found
  }
  public GameObject Focus
  { get { return focus; }
    set { if(value==null && me!=null) focus=me.Ship; else focus=value; }
  }
  public bool Net { get { return netGame; } }
  public int NumGuns     { get { return guns.Count; } }
  public int NumSpecials { get { return specials.Count; } }

  public uint TPS
  { get { return tps; }
    set { timeInc=1000/value; tps=value; }
  }

  public uint Tick { get { return tick; } }
  public State State
  { get { return state; }
    set
    { SetKeyHandler(value==State.BaseMenu || value==State.MaybeQuit);
      lastState = state;
      state = value;
    }
  }
  public VectorF Gravity = new VectorF(0, 1f/30);

  public bool HitMap(int x, int y)  { return x<0 || y<0 || x>=over.Width || y>=over.Height || (over.GetPixelRaw(x, y)&2)!=0; }
  public bool HitBase(int x, int y) { return x>=0 && y>=0 && x<over.Width && y<over.Height && (over.GetPixelRaw(x, y)&4)!=0; }

  public void AddPlayer(Player p)
  { players.Add(p);
  }
  public void RemPlayer(Player p)
  { players.Remove(p);
    if(p==me) me=null;
  }
  public void AddObject(GameObject o) { objects.Append(o); nObjects++; }

  public void Start()
  { nextTime = GameLib.Timing.Ticks;
    State    = State.InPlay;
    if(!netGame) foreach(Player p in players) p.Ship.Spawn(FindSpawnPoint());
  }

  public void Render(Surface display)
  { int camX = focus.RX, camY = focus.RY;
    int w = display.Width/2, h=display.Height/2;
    Rectangle frect = new Rectangle(camX-w, camY-h, camX+w, camY+h), brect = new Rectangle();

    if(frect.X<0) { frect.Width -= frect.X; frect.X=0; }
    else if(frect.Width>fore.Width) { frect.X -= frect.Width-fore.Width; frect.Width=fore.Width; }
    if(frect.Y<0) { frect.Height -= frect.Y; frect.Y=0; }
    else if(frect.Height>fore.Height) { frect.Y -= frect.Height-fore.Height; frect.Height=fore.Height; }

    if(back!=null)
    { w = fore.Width-display.Width;
      h = back.Width-display.Width;
      if(w<0) w=0;
      if(h<0) h=0;
      brect.X = w>0 && h>0 ? frect.X*h/w : 0;
      brect.Width = brect.X+display.Width;
      
      w = fore.Height-display.Height;
      h = back.Height-display.Height;
      if(w<0) w=0;
      if(h<0) h=0;
      brect.Y = w>0 && h>0 ? frect.Top*h/w : 0;
      brect.Height = brect.Y+display.Height;
      
      back.Blit(display, brect, 0, 0);
    }
    else display.Fill();
    fore.Blit(display, frect, 0, 0);

    display.Lock();
    foreach(GameObject o in objects) if(!o.Remove) o.Draw(display, frect.X, frect.Y);
    foreach(Player p in players) p.Ship.Draw(display, frect.X, frect.Y);

    if(me!=null)
    { int bx=4, by=3, bw=40, bh=2, hw=bw*me.Ship.Health/me.Ship.MaxHealth, fw=bw*me.Ship.Fuel/me.Ship.MaxFuel,
          gw=me.Ship.Gun==null ? 0 : bw*me.Ship.Gun.Ammo/me.Ship.Gun.MaxAmmo,
          sw=me.Ship.Special==null ? 0 : bw*me.Ship.Special.Ammo/me.Ship.Special.MaxAmmo;
      if(hw>0) Primitives.FilledBox(display, bx, by, bx+hw, by+bh, Colors.LtRed);
      if(fw>0) Primitives.FilledBox(display, bx, by+bh+2,   bx+fw, by+bh*2+2, Colors.LtBlue);
      if(gw>0) Primitives.FilledBox(display, bx, by+bh*2+4, bx+gw, by+bh*3+4, Colors.Green);
      if(sw>0) Primitives.FilledBox(display, bx, by+bh*3+6, bx+sw, by+bh*4+6, Colors.LtGrey);
    }
    display.Unlock();
    
    { int x=display.Width-5, y=0;
      foreach(Player p in players)
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
    }
  }

  public bool DoTicks()
  { uint time = GameLib.Timing.Ticks;
    bool did  = false;
    if(netGame) CheckNetwork();
    while(time>=nextTime)
    { DoInput();
      if(netGame || state==State.InPlay || state==State.BaseMenu) Advance();
      nextTime += timeInc;
      if(netGame) CheckNetwork();
      did = true;
    }
    return did;
  }
  
  public void DoInput()
  { switch(State)
    { case TriangleChase.State.InPlay:
        if(me.Ship.Dead && (Keyboard.PressedRel(Key.Return) || Keyboard.PressedRel(Key.Space)))
          me.Ship.Spawn(FindSpawnPoint());
        if(me.Ship.Health>0)
        { if(Keyboard.Pressed(Key.Left))
          { me.Ship.Rotate(-me.Ship.TurnAcc++);
            if(me.Ship.TurnAcc>me.Ship.MaxTurn) me.Ship.TurnAcc=me.Ship.MaxTurn;
          }
          if(Keyboard.Pressed(Key.Right))
          { me.Ship.Rotate(me.Ship.TurnAcc++);
            if(me.Ship.TurnAcc>me.Ship.MaxTurn) me.Ship.TurnAcc=me.Ship.MaxTurn;
          }
          if(Keyboard.Pressed(Key.Up)) me.Ship.Accelerate();
          if(Keyboard.Pressed(Key.LCtrl)) me.Ship.Fire();
          if(Keyboard.Pressed(Key.Space)) me.Ship.UseSpecial();
          
          if(me.Ship.OnBase && Keyboard.PressedRel(Key.Return)) State = State.BaseMenu;
        }
        if(Keyboard.PressedRel(Key.Escape)) State = State.MaybeQuit;
        break;

      case TriangleChase.State.BaseMenu:
        while(keys.Count>0)
        { KeyboardEvent kb = (KeyboardEvent)keys.Dequeue();
          if(!kb.Down) continue;
          if(kb.Key==Key.Up || kb.Key==Key.Down) menuGun = !menuGun;
          else if(kb.Key==Key.Left)
          { if(menuGun) me.Ship.Gun = PrevWeapon(me.Ship.Gun, guns);
            else me.Ship.Special = PrevWeapon(me.Ship.Special, specials);
          }
          else if(kb.Key==Key.Right)
          { if(menuGun) me.Ship.Gun = NextWeapon(me.Ship.Gun, guns);
            else me.Ship.Special = NextWeapon(me.Ship.Special, specials);
          }
          else if(kb.Key==Key.Escape || kb.Key==Key.Return) { State=lastState; break; }
        }
        break;
      
      case TriangleChase.State.MaybeQuit:
        while(keys.Count>0)
        { KeyboardEvent kb = (KeyboardEvent)keys.Dequeue();
          if(!kb.Down) continue;
          if(kb.Key==Key.Escape) GameLib.Events.Events.PushEvent(new GameLib.Events.QuitEvent());
          else State=lastState;
          break;
        }
        break;
    }
  }
  
  public void Advance()
  { for(LinkedList.Node node = objects.Head; node!=null; node=node.NextNode)
    { GameObject o = (GameObject)node.Data;
      if(o.Remove) continue;
      o.Think();
      o.CalcVel();
      o.Move();
      o.Age++;
    }
    foreach(Player p in players)
    { p.Ship.Think();
      if(!p.Ship.Dead)
      { p.Ship.CalcVel();
        p.Ship.Move();
        if(p.Ship.IsHitMap) p.Ship.HitMap();
      }
      else if(!netGame && p!=Me) p.Ship.Spawn(FindSpawnPoint());
    }
    
    for(LinkedList.Node node = objects.Head; node!=null; node=node.NextNode)
    { GameObject o = (GameObject)node.Data;
      if(o.Remove) continue;
      if(o.IsHitMap) o.HitMap();
      if(o.CanHitObjs)
        foreach(GameObject oo in objects)
        { if(oo.Remove || oo==o) continue;
          if(o.Intersects(oo)) o.HitObject(oo);
        }
      if(o.Remove) continue;
      foreach(Player p in players)
        if(p.Ship.Health>0 && o.Intersects(p.Ship)) o.HitShip(p.Ship);
    }
    if((tick&0x1F)==0) // GC every 32 ticks
    { LinkedList.Node cur = objects.Head, next;
      while(cur!=null)
      { next = cur.NextNode;
        if(((GameObject)cur.Data).Remove) { objects.Remove(cur); nObjects--; }
        cur = next;
      }
    }

    tick++;
  }
  
  public void Explode(Ship owner, Explosion type, VectorF pos)
  { ExpData data = expData[(int)type];
    int[]    num = new int[4];
    for(int i=0; i<4; i++) num[i] = data.Lows[i]+Globals.Random.Next(data.Highs[i]-data.Lows[i]);
    
    for(int i=0; i<num[0]; i++) // shrapnel
    { VectorF ang = Globals.Vector(Globals.Random.Next());
      AddObject(new Bullet(owner, pos+ang*Globals.Random.Next(data.Radius), ang, owner.Color));
    }

    for(int i=1,e=0; i<4; i++)
      for(int j=0; j<num[i]; j++)
      { Exploder o = new Exploder(i-1);
        if(type==Explosion.Tiny) o.Pos=pos;
        else
        { VectorF ang = Globals.Vector(Globals.Random.Next());
          o.Pos = pos+ang*Globals.Random.Next(8);
          o.Vel = ang*(Globals.Random.Next((int)type+1)*0.5f);
          o.Activate = Globals.Random.Next(e++*2);
        }
        AddObject(o);
      }
  }
  
  public void RemoveCircle(int x, int y, int radius)
  { Primitives.FilledCircle(fore, x, y, radius, Colors.Transparent);
    Primitives.FilledCircle(over, x, y, radius, 255);
  }

  public void AddGun(Type gun) { guns.Add(gun); }
  public void AddSpecial(Type gun) { specials.Add(gun); }
  public Weapon MakeGun(Ship ship, int index) { return MakeWeapon(ship, index, guns); }
  public Weapon MakeSpecial(Ship ship, int index) { return MakeWeapon(ship, index, specials); }

  class ExpData
  { public ExpData(int[] lows, int[] highs, int radius) { Lows=lows; Highs=highs; Radius=radius; }
    public int[] Lows, Highs;
    public int Radius;
  }

  VectorF FindSpawnPoint() { return new VectorF(60, 50); }
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
  
  void OnKey(KeyboardEvent e) { keys.Enqueue(e); Keyboard.Release(e.Key); }

  void CheckNetwork()
  {
  }

  ArrayList  players = new ArrayList(), circles = new ArrayList(), guns = new ArrayList(), specials = new ArrayList();
  LinkedList objects = new LinkedList();
  Queue      keys    = new Queue();
  Player     me;
  GameObject focus;
  Surface    fore, back, over;
  KeyPressHandler keyHandler;
  ExpData[]  expData = new ExpData[6]
  { new ExpData(new int[] { 0,   1,  0, 0 }, new int[] { 2,   1,  1,  0 }, 4),
    new ExpData(new int[] { 3,   1,  1, 0 }, new int[] { 6,   3,  3,  0 }, 10),
    new ExpData(new int[] { 10,  4,  2, 1 }, new int[] { 18,  6,  5,  2 }, 20),
    new ExpData(new int[] { 18,  7,  4, 2 }, new int[] { 25, 11,  8,  5 }, 14),
    new ExpData(new int[] { 25, 10,  8, 4 }, new int[] { 40, 18, 14,  8 }, 50),
    new ExpData(new int[] { 50, 20, 16, 8 }, new int[] {150, 36, 28, 16 }, 70),
  };
  uint  tick, nextTime, timeInc=33, tps=30;
  State state, lastState;
  bool  menuGun=true, netGame;
}

} // namespace TriangleChase
