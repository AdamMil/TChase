using System;
using GameLib.Video;
using GameLib.Input;
using GameLib.Events;

namespace TriangleChase
{

public sealed class App
{ public const int  Version = 10;     // divide by 100 to get version #. ie, 150 = version 1.5
  public const byte ProtoVersion = 1; // network protocol version

  public static void Main()
  { Events.Initialize();

    Server = new Server();
    Server.Start("testmap.xml");

    Video.Initialize();
    Input.Initialize(false);
    Video.SetMode(400, 300, 32);//, SurfaceFlag.Fullscreen);
    Globals.InitGraphics();
    Client = new Client();
    Client.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, Server.DefaultPort), "Adam", Team.Green);

    try
    { bool quit = false;
      do
      { Event e;
        while((e=Events.NextEvent(0))!=null) if(!ProcessEvent(e)) quit=true;
        if(Server!=null) Server.DoTicks();
        if(Client!=null && Client.DoTicks())
        { Client.Render(Video.DisplaySurface);
          Video.Flip();
        }
      } while(!quit);
    }
    finally
    { if(Client!=null) Client.Disconnect();
      if(Server!=null) Server.Stop();
      if(Client!=null)
      { Input.Deinitialize();
        Video.Deinitialize();
      }
      Events.Deinitialize();
    }
  }

  public static bool ProcessEvent(Event e)
  { if(e is RepaintEvent) Video.Flip();
    else if(e is QuitEvent) return false;
    else if(e is ExceptionEvent) throw ((ExceptionEvent)e).Exception;
    return true;
  }

  public static Server Server;
  public static Client Client;
}

}