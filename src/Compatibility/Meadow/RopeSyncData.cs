using CowBoySlug.Mechanics.RopeSkill;
using RainMeadow;
using System;
using UnityEngine;

namespace Compatibility.Meadow
{
    /// <summary>
    /// 绳矛状态的权威同步:挂在矛 APO 的在线实体(OnlinePhysicalObject)上。
    /// 绳子的存在性/归属/颜色/收紧状态随实体状态流同步:
    /// - 新加入的玩家自动补建绳子(事件 RPC 做不到的"资讯同步")
    /// - 双端绳子最终一致,事件 RPC 只负责即时触发(建绳/断绳特效)
    /// - 远端绳子的收紧视觉(used)由状态驱动,不再依赖本地模拟
    /// </summary>
    public class RopeSyncData : OnlineEntity.EntityData
    {
        public RopeSyncData() { }

        /// <summary>
        /// 把同步数据挂到矛 APO 的在线实体上(幂等,非在线时跳过)。
        /// 在绳矛创建(ThrowSpearWithRope)时调用。
        /// </summary>
        public static void TryAttach(Spear spear)
        {
            if (!ModCompat_Helpers.RainMeadow_IsOnline)
                return;
            if (spear?.abstractPhysicalObject?.GetOnlineObject() is OnlinePhysicalObject opo
                && !opo.TryGetData<RopeSyncData>(out _))
            {
                opo.AddData(new RopeSyncData());
            }
        }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            var spear = (entity as OnlinePhysicalObject)?.apo?.realizedObject as Spear;
            if (spear == null)
                return null;
            return new State(spear);
        }

        public class State : EntityDataState
        {
            /// <summary>矛上是否有绳子</summary>
            [OnlineField]
            public bool hasRope;

            /// <summary>绳子的归属玩家(可空 = 没有绳子)</summary>
            [OnlineField(nullable = true)]
            public OnlineEntity.EntityId ropeOwner;

            [OnlineFieldColorRgb]
            public Color colorStart;

            [OnlineFieldColorRgb]
            public Color colorEnd;

            /// <summary>绳子是否处于收紧中(远端视觉直接使用)</summary>
            [OnlineField]
            public bool used;

            public State() { }

            public State(Spear spear)
            {
                var rd = spear.rope();
                var rope = rd?.rope;
                if (rope != null && rd.owner != null)
                {
                    hasRope = true;
                    ropeOwner = rd.owner.abstractPhysicalObject.GetOnlineObject()?.id;
                    colorStart = rope.colorStart;
                    colorEnd = rope.colorEnd;
                    used = rope.used;
                }
                else
                {
                    hasRope = false;
                }
            }

            public override Type GetDataType()
            {
                return typeof(RopeSyncData);
            }

            public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
            {
                var spear = (onlineEntity as OnlinePhysicalObject)?.apo?.realizedObject as Spear;
                if (spear == null)
                    return;
                var rd = spear.rope();

                if (hasRope)
                {
                    var player =
                        (ropeOwner?.FindEntity() as OnlinePhysicalObject)?.apo?.realizedObject as Player;
                    if (player == null)
                        return; // 归属玩家尚未 realized,等下一个状态

                    if (rd.rope?.player != player)
                    {
                        // 与权威状态不符(本地没有,或归属变了):重建
                        if (rd.rope != null)
                            rd.rope.Destroy();
                        Handler.SpawnRope_Local(player, spear, colorStart, colorEnd);
                    }
                    else if (rd.rope != null && !rd.rope.player.IsLocal())
                    {
                        // 远端绳子:收紧视觉跟权威端走,本地模拟的 used 不作数
                        rd.rope.syncedUsed = used;
                    }
                }
                else if (rd.rope != null)
                {
                    // 权威端绳子已消失,本地也销毁
                    rd.rope.Destroy();
                }
            }
        }
    }
}
