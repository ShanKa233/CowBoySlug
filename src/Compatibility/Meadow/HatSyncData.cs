using CowBoySlug;
using RainMeadow;
using System;
using UnityEngine;

namespace Compatibility.Meadow
{
    /// <summary>
    /// 帽子的佩戴/视觉状态同步:挂在帽子 APO 的在线实体(OnlinePhysicalObject)上,
    /// 佩戴者以 EntityId 形式随实体状态流同步,新加入的玩家自动获得全部佩戴资讯。
    /// 佩戴关系的本地重建(wearers、AbstractHatWearStick、hatList)在 ReadTo 里做,
    /// 远端自建的 stick 是纯本地结构,不依赖 Meadow 的 stick 同步(它不认识自定义 stick)。
    /// </summary>
    public class HatSyncData : OnlineEntity.EntityData
    {
        /// <summary>所有者端创建时传入;远端由状态 ReadTo 惰性绑定</summary>
        public CowBoyHat hat;

        public HatSyncData() { }

        public HatSyncData(CowBoyHat hat)
        {
            this.hat = hat;
        }

        /// <summary>
        /// 把同步数据挂到帽子 APO 的在线实体上(幂等,非在线时跳过)。
        /// 在帽子 realized(构造)与佩戴(WearHat)时调用,保证所有者端的状态流一定带上它。
        /// </summary>
        public static void TryAttach(CowBoyHat hat)
        {
            if (!ModCompat_Helpers.RainMeadow_IsOnline)
                return;
            if (hat?.abstractPhysicalObject?.GetOnlineObject() is OnlinePhysicalObject opo
                && !opo.TryGetData<HatSyncData>(out _))
            {
                opo.AddData(new HatSyncData(hat));
            }
        }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            if (hat == null)
                hat = (entity as OnlinePhysicalObject)?.apo?.realizedObject as CowBoyHat;
            if (hat == null)
                return null;
            return new State(this);
        }

        public class State : EntityDataState
        {
            /// <summary>佩戴者(可空 = 未被佩戴)</summary>
            [OnlineField(nullable = true)]
            public OnlineEntity.EntityId wearerId;

            /// <summary>飞行/佩戴的朝向,远端绘制直接使用</summary>
            [OnlineField]
            public Vector2 rotation;

            /// <summary>装饰的水平角度(0~360)</summary>
            [OnlineFieldHalf]
            public float levelAngle;

            /// <summary>装饰旋转速度</summary>
            [OnlineFieldHalf]
            public float rotationSpeed;

            [OnlineFieldColorRgb]
            public Color mainColor;

            [OnlineFieldColorRgb]
            public Color decorateColor;

            public State() { }

            public State(HatSyncData data)
            {
                var hat = data.hat;
                wearerId =
                    hat.wearers != null
                    && hat.wearers.abstractPhysicalObject.GetOnlineObject()
                        is OnlinePhysicalObject wearerOpo
                        ? wearerOpo.id
                        : null;
                rotation = hat.rotation;
                levelAngle = hat.levelAngle;
                rotationSpeed = hat.rotationSpeed;
                mainColor = hat.mainColor;
                decorateColor = hat.decorateColor;
            }

            public override Type GetDataType()
            {
                return typeof(HatSyncData);
            }

            public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
            {
                var hatData = (HatSyncData)data;
                var hat =
                    hatData.hat
                    ?? (hatData.hat = (onlineEntity as OnlinePhysicalObject)?.apo?.realizedObject as CowBoyHat);
                if (hat == null)
                    return;

                // 视觉状态:远端帽子的姿态/装饰/颜色以权威端为准
                hat.rotation = rotation;
                hat.lastRotation = rotation;
                hat.levelAngle = levelAngle;
                hat.rotationSpeed = rotationSpeed;
                hat.mainColor = mainColor;
                hat.decorateColor = decorateColor;

                // 佩戴关系:与本地不符时重建(变化时才触发,音效只放一次)
                var wearer =
                    (wearerId?.FindEntity() as OnlinePhysicalObject)?.apo?.realizedObject as Player;
                var current = hat.wearers as Player;
                if (wearer != current)
                {
                    if (current != null)
                        Hat.UnwearHatLocal(current, hat);
                    if (wearer != null)
                    {
                        Hat.WearHatLocal(wearer, hat);
                        hat.room?.PlaySound(SoundID.Big_Spider_Spit, hat.firstChunk);
                    }
                }
            }
        }
    }
}
