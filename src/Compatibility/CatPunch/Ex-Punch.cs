using CatPunchPunchDP;
using CatPunchPunchDP.Modules;
using CowBoySlug;
using CowBoySlug.Mechanics.RopeSkill;
using Fisobs.Core;
using UnityEngine;

namespace Compatibility.CatPunch
{
    /// <summary>
    /// 帽子击打扩展 — 让 CowBoy 的帽子可被猫拳击打
    /// </summary>
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

                defaultCoolDown = 20,
                coolDownHigh = 400,
                coolDownLow = 20,

                valName = "range",
                defaultFloatVal = 80f,
                floatValHigh = 400f,
                floatValLow = 30f,
            };
        }

        public override PunchFunc GetPunchFunc()
        {
            return new RopePunch();
        }
    }

    /// <summary>
    /// 绳索击打功能 — 空气挥拳时给手边最近飞行中的矛系上绳子
    /// </summary>
    public class RopePunch : PunchFunc
    {
        public RopePunch() : base(new PunchType("HatPunch", false))
        {
        }

        public override void Punch(Player player, TargetPackage targetPackage)
        {
            // 不需要命中效果，逻辑在 PunchAnimation 中
        }

        /// <summary>
        /// 挥拳动画期间：找手边最近的玩家扔出的矛，系绳或加固
        /// </summary>
        public override void PunchAnimation(Player player, PlayerGraphics playerGraphics, int attackHand, Vector2 punchVec)
        {
            base.PunchAnimation(player, playerGraphics, attackHand, punchVec);

            var handPos = playerGraphics.hands[attackHand].pos;
            Spear nearestSpear = null;
            float nearestDist = PunchConfig.GetFloatValConfig(punchType).Value;

            foreach (var item in player.room.updateList)
            {
                if (item is not Spear spear
                    || spear.abstractSpear.ID.spawner == -2)
                {
                    continue;
                }

                float dist = Vector2.Distance(handPos, spear.firstChunk.pos);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestSpear = spear;
                }
            }

            if (nearestSpear == null)
            {
                return;
            }

            if (nearestSpear.rope().IsRopeSpear)
            {
                // 加固已有绳子
                var simulator = nearestSpear.rope().rope;
                if (simulator != null && simulator.points.GetLength(0) > 10)
                {
                    for (int i = 0; i < simulator.points.GetLength(0); i++)
                    {
                        simulator.points[i, 3].x = 25f;
                    }
                }
            }
            else
            {
                // 生成新绳子
                Handler.SpawnRope(
                    player,
                    nearestSpear,
                    Color.Lerp(Color.white, player.ShortCutColor(), 0.9f),
                    player.ShortCutColor());
            }
        }
    }
}
