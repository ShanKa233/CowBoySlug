using System.Runtime.CompilerServices;

namespace CowBoySlug.Mechanics.RopeSkill
{
    /// <summary>
    /// 矛的扩展方法:给每根矛挂一个 RopeData(条件弱表,CWT)
    /// </summary>
    public static class RopeSpearExtension
    {
        private static readonly ConditionalWeakTable<Spear, RopeData> ropeTable = new ConditionalWeakTable<Spear, RopeData>();

        public static RopeData rope(this Spear spear)
        {
            return ropeTable.GetValue(spear, (s) => new RopeData(s));
        }
    }

    /// <summary>
    /// 用于记录与获取和矛所关联的绳子的信息的类
    /// </summary>
    public class RopeData
    {
        public Spear spear;
        public Player owner;
        public Simulator rope;

        public int brokenCount = 0;
        public int cantRotationCount = 0;

        public void Update()
        {
            if (cantRotationCount > 0)
            {
                cantRotationCount--;
            }

            if (cantRotationCount > 10)
            {
                cantRotationCount = 10;
            }

            if (brokenCount > 0)
            {
                brokenCount--;
            }

            if (brokenCount > 80)
            {
                brokenCount = 0;
                RemoveRope();
            }
        }

        public void RemoveRope()
        {
            owner = null;
            rope = null;
        }

        public void GetRope(Player owner, Simulator rope)
        {
            this.owner = owner;
            this.rope = rope;
        }

        //检测是否是带绳的矛
        public bool IsRopeSpear => rope != null && owner != null && owner.room == rope.room;

        public RopeData(Spear spear)
        {
            this.spear = spear;
        }
    }
}
