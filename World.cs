using System;
using System.Collections.Generic;
using GameLib.Video;
using AdamMil.Mathematics.Geometry;
using Rectangle = System.Drawing.Rectangle;

namespace TriangleChase
{

public enum Explosion : byte { Tiny, Small, Medium, Large, Huge, Armageddon }
public enum Team : byte { Unspecified, Green, Blue, Red }

public class World
{ public bool IsServer;
  public GameObject Focus;

  public uint TPS
  { get { return tps; }
    set { timeInc=1000/value; tps=value; }
  }
  public uint Tick { get { return tick; } }
  public bool Loaded;
  public Vector2 Gravity = new Vector2(0, 1f/30);

  public List<Player> Players { get { return players; } }
  public uint      NextID  { get { return nextObj++; } }
  
  public void Load(string mapname, bool server)
  { System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
    xml.Load(Globals.MapPath+mapname);
    Load(xml, server);
  }
  public void Load(System.Xml.XmlDocument xml, bool server)
  { Unload();
    if(!server)
    {
      string foreName = xml.DocumentElement.GetAttribute("fore"), backName = xml.DocumentElement.GetAttribute("back");
      if(!string.IsNullOrEmpty(foreName))
      {
        fore = new Surface(Globals.MapPath+foreName).CloneDisplay(false);
        fore.SetColorKey(Colors.Transparent);
      }
      if(!string.IsNullOrEmpty(backName)) back = new Surface(Globals.MapPath+backName).CloneDisplay(false);
    }
    over = new Surface(Globals.MapPath+xml.DocumentElement.GetAttribute("map"));

    tick     = 0;
    nextObj  = 1;
    IsServer = server;
    Loaded   = true;
  }
  
  public void Unload()
  { Loaded = false;
    if(fore!=null) fore.Dispose();
    if(back!=null) back.Dispose();
    if(over!=null) over.Dispose();
    fore = back = over = null;
    players.Clear(); objects.Clear(); objIndex.Clear();
  }

  public bool HitMap(int x, int y)  { return x<0 || y<0 || x>=over.Width || y>=over.Height || (over.GetPixelRaw(x, y)&2)!=0; }
  public bool HitBase(int x, int y) { return x>=0 && y>=0 && x<over.Width && y<over.Height && (over.GetPixelRaw(x, y)&4)!=0; }
  
  public void AddPlayer(Player p)
  { p.Ship.World = this;
    players.Add(p);
  }
  public void RemovePlayer(Player p)
  { players.Remove(p);
    objIndex.Remove(p.Ship.ID);
  }

  public uint AddObject(GameObject o)
  { AddObject(o, nextObj++);
    return o.ID;
  }
  public void AddObject(GameObject o, uint id)
  { o.ID    = id;
    o.World = this;
    lock(objects) objIndex[id] = objects.AddLast(o);
  }
  public void RemoveObject(GameObject o) { RemoveObject(o.ID); }
  public void RemoveObject(uint id)
  { 
    LinkedListNode<GameObject> node;
    if(objIndex.TryGetValue(id, out node))
    {
      lock(objects) { objIndex.Remove(id); objects.Remove(node); }
    }
  }
  public GameObject FindObject(uint id)
  { 
    LinkedListNode<GameObject> node;
    return objIndex.TryGetValue(id, out node) ? node.Value : null;
  }
  public Ship FindShip(uint id)
  { lock(players) foreach(Player p in players) if(p.Ship.ID==id) return p.Ship;
    return null;
  }

  public void Start()
  { nextTime = GameLib.Timing.Milliseconds;
    nextObj  = 1;
    updated  = false;
  }

  public void Render(Surface display)
  { int camX = Focus==null ? 0 : Focus.RX, camY = Focus==null ? 0 : Focus.RY;
    int w = display.Width/2, h=display.Height/2;
    Rectangle frect = new Rectangle(camX-w, camY-h, camX+w, camY+h), brect = new Rectangle();

    if(frect.X<0) { frect.Width -= frect.X; frect.X=0; }
    else if(frect.Width>over.Width) { frect.X -= frect.Width-over.Width; frect.Width=over.Width; }
    if(frect.Y<0) { frect.Height -= frect.Y; frect.Y=0; }
    else if(frect.Height>over.Height) { frect.Y -= frect.Height-over.Height; frect.Height=over.Height; }

    if(back!=null)
    { 
      w = over.Width-display.Width;
      h = back.Width-display.Width;
      if(w<0) w=0;
      if(h<0) h=0;
      brect.X = w>0 && h>0 ? frect.X*h/w : 0;
      brect.Width = brect.X+display.Width;
      
      w = over.Height-display.Height;
      h = back.Height-display.Height;
      if(w<0) w=0;
      if(h<0) h=0;
      brect.Y = w>0 && h>0 ? frect.Top*h/w : 0;
      brect.Height = brect.Y+display.Height;
      
      back.Blit(display, brect, 0, 0);
    }
    else display.Fill();

    if(fore != null) fore.Blit(display, frect, 0, 0);

    display.Lock();
    lock(objects) foreach(GameObject o in objects) if(!o.Remove) o.Draw(display, frect.X, frect.Y);
    lock(players) foreach(Player p in players) p.Ship.Draw(display, frect.X, frect.Y);
    display.Unlock();
  }

  public bool DoTicks()
  { uint time = GameLib.Timing.Milliseconds;
    bool did  = updated;
    while(time>=nextTime)
    { Advance();
      nextTime += timeInc;
      did = true;
    }
    updated = false;
    return did;
  }

  public void Explode(Ship owner, Explosion type, Vector2 pos)
  { ExpData data = expData[(int)type];
    int[]    num = new int[4];
    for(int i=0; i<4; i++) num[i] = data.Lows[i]+Globals.Random.Next(data.Highs[i]-data.Lows[i]);
    
    for(int i=0; i<num[0]; i++) // shrapnel
    { Vector2 ang = Globals.Vector(Globals.Random.Next());
      AddObject(new Bullet(owner, pos+ang*Globals.Random.Next(data.Radius), ang, owner.Color));
    }

    for(int i=1,e=0; i<4; i++)
      for(int j=0; j<num[i]; j++)
      { Exploder o = new Exploder(i-1);
        if(type==Explosion.Tiny) o.Pos=pos;
        else
        { Vector2 ang = Globals.Vector(Globals.Random.Next());
          o.Pos = pos+ang*Globals.Random.Next(8);
          o.Vel = ang*(Globals.Random.Next((int)type+1)*0.5f);
          o.Activate = Globals.Random.Next(e++*2);
        }
        AddObject(o);
      }
  }

  public void RemoveCircle(int x, int y, int radius)
  { if(fore!=null) Shapes.FilledCircle(fore, x, y, radius, Colors.Transparent);
    Shapes.FilledCircle(over, x, y, radius, over.ColorKey);
  }

  public Vector2 FindSpawnPoint()
  {
    // TODO: find spawn points once and cache them
    List<Vector2> spawnPoints = new List<Vector2>();
    over.Lock();
    for(int y=0; y<over.Height; y++)
    {
      for(int x=0; x<over.Width; x++)
      {
        if((over.GetPixelRaw(x, y) & 8) != 0) spawnPoints.Add(new Vector2(x, y));
      }
    }
    over.Unlock();
    return spawnPoints[Globals.Random.Next(spawnPoints.Count)];
  }

  void Advance()
  { for(LinkedListNode<GameObject> node = objects.First; node!=null; node=node.Next)
    { GameObject o = node.Value;
      if(o.Remove) continue;
      o.Think();
      o.CalcVel();
      o.Move();
      o.Age++;
    }
    foreach(Player p in players)
    { p.Ship.ApplyKeys(p.Inputs);
      p.Ship.Think();
      if(!p.Ship.Dead)
      { p.Ship.CalcVel();
        p.Ship.Move();
        if(p.Ship.IsHitMap) p.Ship.HitMap();
      }
    }
    
    for(LinkedListNode<GameObject> node = objects.First; node!=null; node=node.Next)
    { GameObject o = node.Value;
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
    { LinkedListNode<GameObject> cur = objects.First, next;
      while(cur!=null)
      { next = cur.Next;
        GameObject o = cur.Value;
        if(o.Remove && o.NetPolicy!=NetPolicy.RemoteAll) RemoveObject(o.ID);
        cur = next;
      }
    }

    tick++;
  }

  class ExpData
  { public ExpData(int[] lows, int[] highs, int radius) { Lows=lows; Highs=highs; Radius=radius; }
    public int[] Lows, Highs;
    public int Radius;
  }

  List<Player> players = new List<Player>();
  LinkedList<GameObject> objects = new LinkedList<GameObject>();
  Dictionary<uint, LinkedListNode<GameObject>> objIndex = new Dictionary<uint, LinkedListNode<GameObject>>();
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
