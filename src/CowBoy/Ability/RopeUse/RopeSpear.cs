using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CowBoySlug.CowBoy.Ability.RopeUse
{
    internal class RopeSpear
    {
        public static void Hook()
        {


            //降低插墙所需的速度
            IL.Spear.Update += Spear_Update;
        }

        private static void Spear_Update(MonoMod.Cil.ILContext il)
        {
            var c =new  ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                i=>i.MatchLdarg(0),
                i=>i.Match(OpCodes.Call),
                i=>i.MatchLdflda<BodyChunk>("vel"),
                i=>i.MatchCall<Vector2>("get_magnitude"),
                i=>i.MatchLdcR4(10)
            ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, Spear, float>>((orig,self) =>
                {
                    if (self.rope().IsRopeSpear)
                    {
                        UnityEngine.Debug.Log("CowBoy Spear modify");
                        return 0;
                    }
                    return 10f;
                });
            }
        }
    }







    public static class ExSpear
    {
        public static ConditionalWeakTable<Spear, RopeData> modules = new ConditionalWeakTable<Spear, RopeData>();

        public static RopeData rope(this Spear spear) => modules.GetValue(spear, (_) => new RopeData(spear));
    }
    /// <summary>
    /// 用于记录与获取和矛所关联的绳子的信息的类
    /// </summary>
    public class RopeData
    {
        Spear spear;
        PhysicalObject owner;
        CowRope rope;

        public void RemoveRope()
        {
            owner = null;
            rope= null; 
        }
        public void GetRope(PhysicalObject owner,CowRope rope )
        {
            this.owner = owner;
            this.rope = rope;

        }
        //检测是否是带绳的矛
        public bool IsRopeSpear => rope != null;

        public RopeData(Spear spear)
        {
            this.spear = spear;
        }
    }


}
