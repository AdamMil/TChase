using System;
using System.Drawing;
using System.Collections;
using GameLib.Video;
using GameLib.Collections;
using GameLib.Mathematics;

namespace TriangleChase
{

public enum Explosion : byte { Tiny, Small, Medium, Large, Huge, Armageddon }
public enum Team : byte { Unspecified, Green, Blue, Red }
public enum State { InPlay, BaseMenu, MaybeQuit, Disconnected }

public class World
{ public bool IsServer;
  public GameObject Focus;

  public uint TPS
  { get { return tps; }
    set { timeInc=1000/value; tps=value; }
  }
  public uint Tick { get { return tick; } }
  public VectorF Gravity = new VectorF(0, 1f/30);

  public ArrayList Players { get { return players; } }
  
  public void Load(string mapname, bool server)
  { System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
    xml.Load(mapname);
    Load(xml, server);
  }
  public void Load(System.Xml.XmlDocument xml, bool server)
  { Unload();
    fore = new Surface(Globals.MapPath+xml.DocumentElement.GetAttribute("fore")).CloneDisplay(false);
    back = new Surface(Globals.MapPath+xml.DocumentElement.GetAttribute("back")).CloneDisplay(false);
    over = new Surface(Globals.MapPath+xml.DocumentElement.GetAttribute("overlay"));
    fore.SetColorKey(Colors.Transparent);
    
    tick = 0;
    objects.Clear(); objIndex.Clear();
    IsServer = server;
  }
  
  public void Unload()
  { fore.Dispose();
    back.Dispose();
    over.Dispose();
    fore = back = null;
  }

  public bool HitMap(int x, int y)  { return x<0 || y<0 || x>=over.Width || y>=over.Height || (over.GetPixelRaw(x, y)&2)!=0; }
  public bool HitBase(int x, int y) { return x>=0 && y>=0 && x<over.Width && y<over.Height && (over.GetPixelRaw(x, y)&4)!=0; }
  
  public void AddPlayer(Player p) { players.Add(p); }
  public void RemPlayer(Player p) { players.Remove(p); }

  public uint AddObject(GameObject o)
  { AddObject(o, nextObj++);
    return o.ID;
  }
  public void AddObject(GameObject o, uint id)
  { o.ID         = id;
    objIndex[id] = objects.Append(o);
    o.World      = this;
    switch(o.NetPolicy)
    { default: throw new ArgumentException(String.Format("Object {0} has unknown net policy", o));
    }
  }
  public void RemoveObject(GameObject o) { RemoveObject(o.ID); }
  public void RemoveObject(uint id)
  { if(!objIndex.Contains(id)) return;
    LinkedList.Node node = (LinkedList.Node)objIndex[id];
    objIndex.Remove(node);
    objects.Remove(node);
  }

  public void Start()
  { nextTime = GameLib.Timing.Ticks;
    nextObj  = 1;
    updated  = false;
  }

  public void Render(Surface display)
  { int camX = Focus.RX, camY = Focus.RY;
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
    display.Unlock();
  }

  public bool DoTicks()
  { uint time = GameLib.Timing.Ticks;
    bool did  = updated;
    while(time>=nextTime)
    { Advance();
      nextTime += timeInc;
      did = true;
    }
    updated = false;
    return did;
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

  void Advance()
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
        GameObject o = (GameObject)cur.Data;
        if(o.Remove && o.NetPolicy!=NetPolicy.RemoteAll) RemoveObject(o.ID);
        cur = next;
      }
    }

    tick++;
  }

  VectorF FindSpawnPoint() { return new VectorF(60, 50); }

  class ExpData
  { public ExpData(int[] lows, int[] highs, int radius) { Lows=lows; Highs=highs; Radius=radius; }
    public int[] Lows, Highs;
    public int Radius;
  }

  ArrayList  players  = new ArrayList();
  LinkedList objects  = new LinkedList();
  Hashtable  objIndex = new Hashtable();
  Surface    fore, back, over;
  uint  tick, nextTime, timeInc=33, tps=30, nextObj;
  bool  updated;

  static readonly ExpData[] expData = new ExpData[6]
  { new ExpData(new int[] { 0,   1,  0, 0 }, new int[] { 2,   1,  1,  0 }, 4),
    new ExpData(new int[] { 3,   1,  1, 0 }, new int[] { 6,   3,  3,  0 }, 10),
    new ExpData(new int[] { 10,  4,  2, 1 }, new int[] { 18,  6,  5,  2 }, 20),
    new ExpData(new int[] { 18,  7,  4, 2 }, new int[] { 25, 11,  8,  5 }, 14),
    new ExpData(new int[] { 25, 10,  8, 4 }, new int[] { 40, 18, 14,  8 }, 50),
    new ExpData(new int[] { 50, 20, 16, 8 }, new int[] {150, 36, 28, 16 }, 70),
  };
}

} // namespace TriangleChase
