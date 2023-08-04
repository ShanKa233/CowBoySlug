using CowBoySLug;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CowBoySlug
{
    public static class DroneJumpHook
    {
        public static void Hook()
        {
            On.Player.Update += Player_Use_Drone_Jump;
        }

        private static void Player_Use_Drone_Jump(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            
            //如果玩家没有在空中按下跳跃按键就直接return
            if (!(self.input[2].jmp==false &&self.wantToJump>0&&Plugin.menu.jumpDrone.Value))
            {
                return;
            }
            //搜索房间里面有没有无人机
            foreach (var item in self.room.updateList)
            {
                var drone = item as Creature;

                //如果有而且和玩家距离很近
                if (drone != null && drone.GetType().ToString().Contains("Drone") && Custom.DistLess(self.bodyChunks[1].pos, drone.firstChunk.pos, 25) )
                {
                    //检查这个无人机有没有被玩家拿着
                    bool grabbed = false;
                    foreach (var grasp in drone.grabbedBy)
                    {
                        if (grasp.grabber == self)
                        {
                            grabbed = true;
                        }
                    }
                    //如果被拿着就不满足条件跳过这个循环找下一只
                    if (grabbed) { break; }

                    //如果没被拿着而且有重力就跳高,如果无重力就朝玩家想去的方向推进
                    if (self.gravity > 0)
                    {
                        self.mainBodyChunk.vel.y = 25;
                        self.jumpBoost = 5;
                        drone.firstChunk.vel.y -= 10;
                    }
                    else
                    {
                        self.mainBodyChunk.vel += self.mainBodyChunk.Rotation * 5+new Vector2(self.input[0].x, self.input[0].y)*10;
                        drone.firstChunk.vel -= self.mainBodyChunk.Rotation * 10;
                    }

                    //弄点特效
                    for (int i = 0; i < 5; i++)
                    {
                        self.room.AddObject(new Spark(self.bodyChunks[1].pos, -self.bodyChunks[1].Rotation * 3 + Custom.DegToVec(Random.Range(-90, 90))*5, Color.white, null, 20, 30));
                    }
                    return;
                }
            }
        }
    }
}
