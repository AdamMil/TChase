using System;
using System.Drawing;

namespace TriangleChase
{

public class Player
{ public Player() { Name="UNNAMED"; Team=Team.Green; }
  public Player(string name) { Name=name; Team=Team.Green; }
  public Player(string name, Team team) { Name=name; Team=team; }

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
  Team   team;
}

} // namespace TriangleChase