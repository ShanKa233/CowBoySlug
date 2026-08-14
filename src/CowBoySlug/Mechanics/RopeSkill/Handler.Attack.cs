using System;
using RWCustom;
using UnityEngine;

namespace CowBoySlug.Mechanics.RopeSkill
{
    /// <summary>
    /// 攻击模式(甩矛):召回流程内另一只手按拾取时,把矛甩向玩家方向造成伤害。
    /// 连打取消(MashCancel)在分发器 CallBackSpear_Local 里检查,保持整帧 return 语义。
    /// </summary>
    public partial class Handler
    {
        #region 攻击模式(甩矛)

        /// <summary>
        /// 攻击甩矛:矛加速甩向玩家方向,伤害加成略微递减。
        /// 调用处的按键/距离/连打取消判定已做完,这里只做动作。
        /// </summary>
        private static void AttackSpear(
            Player player,
            Spear spear,
            Simulator umbilical,
            Vector2 spearToEndPointDir
        )
        {
            // 控制手和绳子
            player.HandData().Pulling(20, umbilical, player.FreeHand());
            spear.ChangeMode(Weapon.Mode.Thrown);
            spear.spearDamageBonus *= 0.9f;
            spear.thrownBy = player;
            spear.throwDir = new IntVector2(
                Convert.ToInt32(spearToEndPointDir.x),
                Convert.ToInt32(spearToEndPointDir.y)
            );

            spear.rotation = spear.throwDir.ToVector2();
            spear.firstChunk.pos -= spearToEndPointDir;
            spear.firstChunk.vel += spear.throwDir.ToVector2() * 50 * spear.spearDamageBonus;
        }

        #endregion
    }
}
