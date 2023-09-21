using CatPunchPunchDP;
using CatPunchPunchDP.Modules;
using CowBoySlug.CowBoy.Ability.RopeUse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static CatPunchPunchDP.Modules.PunchFunc;

namespace CowBoySlug.CatPunch
{
    public class HatPunch : PunchExtend
    {
        public HatPunch() : base("HatPunch") { }

        public override bool ParseObjectType(AbstractPhysicalObject obj)
        {
            return obj.type == CowBoyHatFisob.AbstrCrate;
        }

        public override PunchConfig.ConfigSetting GetConfigSetting()
        {
            return new PunchConfig.ConfigSetting()
            {
                elementName = ItemSymbol.SpriteNameForItem(CowBoyHatFisob.AbstrCrate, 0),
                color = ItemSymbol.ColorForItem(CowBoyHatFisob.AbstrCrate, 0),

                defaultCoolDown = 120,
                coolDownHigh = 500,
                coolDownLow = 40,

                valName ="range",
                defaultFloatVal= 0.1f,
                floatValHigh= 2f,
                floatValLow= 0.05f,
            };
        
        }

        public override PunchFunc GetPunchFunc()
        {
            return new RopePunch();
        }


    }

    public class RopePunch : PunchFunc
    {
        public RopePunch() : base(new PunchType("HatPunch",false))
        {
        }

        public override void Punch(Player player, TargetPackage targetPackage)
        {
        }

        public override void PunchAnimation(Player player, PlayerGraphics playerGraphics, int attackHand, Vector2 PunchVec)
        {
            base.PunchAnimation(player, playerGraphics, attackHand, PunchVec);

            //在房间里面找个玩家扔出去的矛
            foreach (var item in player.room.updateList)
            {
                var spear = item as Spear;
                //给一个被判断为服务器扔出的矛,而且在飞行的矛加上丝线,或加固丝线
                if (spear != null && spear.abstractSpear.ID.spawner != -2 && Vector2.Distance(playerGraphics.hands[attackHand].pos, spear.firstChunk.pos) < 30)
                {

                    //如果有线就跳过
                    foreach (var item2 in UseCowBoyRope.RopeList)
                    {
                        //寻找矛上有没有其他的线
                        if (item2 != null && item2.spear != null && item2.spear == spear)
                        {//如果有线就加固线然后return
                            var umbilical = item2;
                            if (umbilical.points.GetLength(0) > 10)
                            {
                                for (int i = 0; i < umbilical.points.GetLength(0); i++)
                                {
                                    umbilical.points[i, 3].x = 25f;
                                }
                            }
                            return;
                        }
                    }

                    //弄个线上去
                    UseCowBoyRope.SpawnRope(item as Spear, player, Color.Lerp(Color.white, player.ShortCutColor(), 0.9f), player.ShortCutColor());
                    return;
                }
            }


        }


    }
}
