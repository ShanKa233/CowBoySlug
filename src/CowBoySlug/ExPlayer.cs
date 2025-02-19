using System.Runtime.CompilerServices;
using UnityEngine;

namespace CowBoySlug.CowBoySlugMod
{
  public static class CowBoy
  {
    public static ConditionalWeakTable<Player, ExPlayer> modules = new ConditionalWeakTable<Player, ExPlayer>();
    public static SlugcatStats.Name Name = new SlugcatStats.Name("CowBoySLug", true);


    public static ExPlayer GetCowBoyData(this Player player) => modules.GetValue(player, (Player _) => new ExPlayer(player));


    public static bool IsCowBoys(this Player player) => player.slugcatStats.name == Name;
    public static bool IsCowBoys(this Player player, out ExPlayer exPlayer)
    {
      exPlayer = null;
      if (player.IsCowBoys())
      {
        exPlayer = player.GetCowBoyData();
        return true;
      }
      return false;
    }



  }
  public class ExPlayer
  {
    public Player player;

    public ExPlayer(Player player)
    {

      this.player = player;

      stopTime = 0;
      changeHand = 0;
      timeToRemoveFood = 1200;
    }

    int timeToRemoveFood = 900;//减少食物的时间
    public int stopTime = 0;//静止不动的时间
    public int changeHand = 0;//换手了没


    public Color scarfColor = Color.yellow;

    public void UseFood()
    {
      if (player.playerState.permanentDamageTracking > 1000)
      {
        player.Die();
      }
      if (player.FoodInStomach <= 0) { player.playerState.permanentDamageTracking++; }
      if (timeToRemoveFood <= 0 && player.playerState.foodInStomach > 0)
      {
        player.SubtractFood(1);
        timeToRemoveFood = 1800;
      }
      if (timeToRemoveFood == 1800 && player.playerState.foodInStomach <= 0)
      {
        player.Stun(180);
      }
    }
    public void Update()
    {

      if (player.playerState.foodInStomach > 0)
      {
        player.playerState.permanentDamageTracking = 0;
      }
      if (timeToRemoveFood > 0)
      {
        timeToRemoveFood--;
      }
      if (stopTime > 0 && !(player.input[0].x == 0 && player.input[0].y == 0))
      {
        stopTime -= 3;
      }
      if (changeHand > 0)
      {
        changeHand--;
      }

      NotMove();
    }
    public void NotMove()
    {
      var self = this.player;
      bool flag = self.input[0].x == 0 && self.input[0].y == 0;
      bool range = stopTime < 30;
      if (flag && range)
      {
        stopTime++;
      }
    }
    public void ChangeHand()
    {
      var self = this.player;
      bool range = changeHand < 5;
      if (range)
      {
        changeHand += 5;
      }
    }

  }

}
