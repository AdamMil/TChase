using System;
using GameLib.Video;
using GameLib.Input;
using GameLib.Events;

namespace TriangleChase
{

public sealed class App
{ public static void Main()
  { Events.Initialize();
    Video.Initialize();
    Input.Initialize(false);

    Video.SetMode(400, 300, 32, SurfaceFlag.Fullscreen);
    Globals.InitSprites();

    World = new World();
    World.Load("fg.png", "bg.png", "overlay.pcx");
    World.AddGun(typeof(MachineGun));
    World.AddGun(typeof(FBMachineGun));
    World.AddGun(typeof(DualMachineGun));
    World.AddGun(typeof(WavyMachineGun));
    World.AddSpecial(typeof(Cannon));
    World.AddSpecial(typeof(GrenadeLauncher));
    World.AddSpecial(typeof(Afterburner));
    
    Player p = new Player("Adam", Team.Green);
    World.AddPlayer(p);
    World.Me = p;

    uint tickStart = GameLib.Timing.Ticks, tick, frames=0;
    float fps=0;
    World.Start();
    while(true)
    { Event e;
      while((e=Events.NextEvent(0))!=null) if(!ProcessEvent(e)) goto Quit;
      /*if(*/World.DoTicks();//)
      { World.Render(Video.DisplaySurface);
        tick = GameLib.Timing.Ticks;
        frames++;
        if(tick-tickStart>1000)
        { //fps = String.Format("{0} fps ({1} frames in {2} ms)", (frames*100000)/(tick-tickStart)/100.0f, frames, tick-tickStart);
          fps = (frames*100000)/(tick-tickStart)/100.0f;
          frames = 0;
          tickStart = tick;
        }
        
        Globals.Font.Render(Video.DisplaySurface, String.Format("{0} fps, {1} objects", fps, World.nObjects), 4, Video.DisplaySurface.Height-Globals.Font.Height);
        Video.Flip();
      }
    }
    Quit:
    Input.Deinitialize();
    Video.Deinitialize();
    Events.Deinitialize();
  }

  public static bool ProcessEvent(Event e)
  { if(e is QuitEvent) return false;
    return true;
  }
  
  public static World World;
}

}