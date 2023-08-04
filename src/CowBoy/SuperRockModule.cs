using CowBoySLug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CowBoySlug
{
    public class SuperRockModule
    {
        public bool isSuperRock = true;
        public Rock self;
        public Color rockColor = new Color(103/255f, 5/255f, 4/255f);
        public bool canMoreFast = true;


        public void RockPowerUp()
        {
            var rock = self;
            if (rock != null && PlayerHook.rockModule.TryGetValue(rock, out var superRock) && superRock.isSuperRock)
            {
                
                var player = rock.thrownBy as Player;
                if (player != null)
                {
                    rock.firstChunk.vel = new Vector2(player.input[0].x, player.input[0].y) * 250;
                    player.mushroomCounter = 2;
                    player.mushroomEffect = 0.5f;
                }
                canMoreFast= false;
            }

        }//如果是超级石头时加速石头
        public SuperRockModule(Rock self)
        {
            this.canMoreFast = true;
            this.self = self;
            this.rockColor = Color.Lerp(self.color, rockColor, 0.2f);
        }
    }
}
