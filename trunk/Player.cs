using System;
using System.Drawing;
using GameLib.Network;

namespace TriangleChase
{

public class Player
{ public Player(string name, Team team, ServerPlayer link) { Name=name; Team=team; Link=link; }

  public Team Team
  { get { return team; }
    set
    { switch(value)
      { case Team.Green: Ship.ColorMap = new Color[] { Colors.DkGreen, Colors.Green, Colors.LtGreen }; break;
        case Team.Blue:  Ship.ColorMap = new Color[] { Colors.DkBlue, Colors.Blue, Colors.LtBlue }; break;
        case Team.Red:   Ship.ColorMap = new Color[] { Colors.DkRed, Colors.Red, Colors.LtRed }; break;
      }
      team = value;
    }
  }

  public Ship   Ship = new Ship();
  public string Name;
  public ServerPlayer Link;
  public InputMessage.Key Inputs;

  Team   team;
}

} // namespace TriangleChase