
using CowBoySLug;
using RWCustom;
using SlugBase.DataTypes;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using CowBoySlug.CowBoy.Ability.RopeUse;

namespace CowBoySlug
{
    public class CowBoyModule
    {
        int timeToRemoveFood = 900;//减少食物的时间
        public int stopTime = 0;//静止不动的时间
        public int changeHand = 0;//换手了没


        public Color scarfColor = Color.yellow;
        public readonly SlugcatStats.Name name;
        public readonly SlugBaseCharacter character;

        public Player self;
        public CowBoyModule(Player player)
        {
            this.self = player;
            stopTime = 0;
            changeHand = 0;
            timeToRemoveFood = 1200;


        }


        public void UseFood()
        {
            if (self.playerState.permanentDamageTracking > 1000)
            {
                self.Die();
            }
            if (self.FoodInStomach <= 0) { self.playerState.permanentDamageTracking++; }
            if (timeToRemoveFood <= 0 && self.playerState.foodInStomach > 0)
            {
                self.SubtractFood(1);
                timeToRemoveFood = 1800;
            }
            if (timeToRemoveFood == 1800 && self.playerState.foodInStomach <= 0)
            {
                self.Stun(180);
            }
        }
        public void BackToNormal()
        {
            
            if (self.playerState.foodInStomach > 0)
            {
                self.playerState.permanentDamageTracking = 0;
            }
            if (timeToRemoveFood > 0)
            {
                timeToRemoveFood--;
            }
            if (stopTime > 0 && !(self.input[0].x == 0 && self.input[0].y == 0))
            {
                stopTime -= 3;
            }
            if (changeHand > 0)
            {
                changeHand--;
            }
            
        }
        public void NotMove()
        {
            var self = this.self;
            bool flag = self.input[0].x == 0 && self.input[0].y == 0;
            bool range = stopTime < 30;
            if (flag && range)
            {
                stopTime++;
            }
        }
        public void ChangeHand()
        {
            var self = this.self;
            bool range = changeHand < 5;
            if (range)
            {
                changeHand += 5;
            }
        }


        public void RockMake(Rock rock)
        {
            //检查玩家有没有做出正确的操作
            bool triga1 = self.input[0].x != 0 || self.input[0].y != 0;
            bool triga2 = stopTime > 15 && self.switchHandsCounter > 0;
            if (triga1 && triga2 && rock != null)
            {
                if (PlayerHook.rockModule.TryGetValue(rock, out var value))
                {
                    PlayerHook.rockModule.Remove(rock);
                }
                PlayerHook.rockModule.Add(rock, new SuperRockModule(rock));
            }



        }//确认是否属于超级投掷出去的石头并打上标记



        
    }



}
