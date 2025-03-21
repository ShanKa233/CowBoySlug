using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CowBoySlug
{
    public static class CowBoyModule
    {
        public static ConditionalWeakTable<Player, ExPlayer> modules = new ConditionalWeakTable<Player, ExPlayer>();
        public static string CowboySlugID = "CowBoySLug";

        public static ExPlayer GetCowBoyData(this Player player) => modules.GetValue(player, (_) => new ExPlayer(player));

        public static bool IsCowBoys(this Player player) => player.slugcatStats.name.value == CowboySlugID;
        public static bool IsCowBoys(this Player player, out ExPlayer exPlayer)
        {
            if (player.IsCowBoys())
            {
                exPlayer = modules.GetValue(player, (_) => new ExPlayer(player));
                return true;
            }
            exPlayer = null;
            return false;
        }
    }

    public class ExPlayer
    {
        public Player player;
        public ScarfModule scarf
        {
            get
            {
                if (_scarf == null &&
                (player.IsCowBoys() || (Scarf.HaveScarf.TryGet(player, out var haveScarf) && haveScarf)))
                {
                    _scarf = new ScarfModule(player);
                    _scarf.ribbon = new GenericBodyPart[2];
                    for (int i = 0; i < _scarf.ribbon.Length; i++)
                    {
                        _scarf.ribbon[i] = new GenericBodyPart(
                            player.graphicsModule,
                            1,      // 重量
                            0.8f,   // 弹性
                            0.3f,   // 阻力
                            player.mainBodyChunk  // 连接到玩家的主体
                        );
                    }
                }
                return _scarf;
            }
            set => _scarf = value;
        }
        private ScarfModule _scarf;

        public List<CowBoyHat> hatList = new List<CowBoyHat>();
        public bool HaveHat => hatList.Count > 0;

        // 新增的方法
        public void StackHat(CowBoyHat hat)
        {
            if (hat != null && !hatList.Contains(hat))
            {
                hatList.Add(hat);

            }
        }

        public void UnstackHat(CowBoyHat hat)
        {
            if (hat != null && hatList.Contains(hat))
            {
                hatList.Remove(hat);
            }
        }

        public ExPlayer(Player player)
        {
            this.player = player;
            stopTime = 0;
            changeHand = 0;
            timeToRemoveFood = 1200;

            //如果有围巾就增加用于显示围巾的变量
            // if (player.IsCowBoys() || (Scarf.HaveScarf.TryGet(player, out var haveScarf) && haveScarf))
            // {

            //     scarf = new ScarfModule(player);
            //     // 初始化围巾的两个部分（上下两条飘带）
            //     scarf.ribbon = new GenericBodyPart[2];
            //     for (int i = 0; i < scarf.ribbon.Length; i++)
            //     {
            //         scarf.ribbon[i] = new GenericBodyPart(
            //             player.graphicsModule,
            //             1,      // 重量
            //             0.8f,   // 弹性
            //             0.3f,   // 阻力
            //             player.mainBodyChunk  // 连接到玩家的主体
            //         );
            //     }
            // }

        }

        int timeToRemoveFood = 900; //减少食物的时间
        public int stopTime = 0; //静止不动的时间
        public int changeHand = 0; //换手了没

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
            var self = player;
            bool flag = self.input[0].x == 0 && self.input[0].y == 0;
            bool range = stopTime < 30;
            if (flag && range)
            {
                stopTime++;
            }
        }

        public void ChangeHand()
        {
            var self = player;
            bool range = changeHand < 5;
            if (range)
            {
                changeHand += 5;
            }
        }
    }
}
