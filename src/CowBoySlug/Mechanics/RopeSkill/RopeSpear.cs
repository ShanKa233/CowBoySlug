using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace CowBoySlug.Mechanics.RopeSkill
{
  /// <summary>
  /// 矛侧的钩子:维护 RopeData 计时器、防止拉回时乱转、同步绳索渲染层级、降低插墙速度需求。
  /// 矛的关联数据见 RopeData(通过 spear.rope() 获取)。
  /// </summary>
  internal class RopeSpear
  {
    public static void Hook()
    {
      //用来更新计时器防止矛一直倒下
      On.Spear.Update += Spear_RopeData_Update;

      //让矛在拉回来的时候不要乱跳
      On.Spear.SetRandomSpin += Spear_SetRandomSpin;

      //改变层级让线在插入生物是可以在矛的图层后面
      On.Weapon.ChangeOverlap += Weapon_ChangeOverlap;
      //降低插墙所需的速度
      IL.Spear.Update += Spear_Update;
    }

    private static void Spear_RopeData_Update(On.Spear.orig_Update orig, Spear self, bool eu)
    {
      orig.Invoke(self, eu);
      self.rope().Update();
    }

    private static void Spear_SetRandomSpin(On.Spear.orig_SetRandomSpin orig, Spear self)
    {
      if (self.rope().cantRotationCount > 0)
        return;
      orig.Invoke(self);
    }

    private static void Weapon_ChangeOverlap(
        On.Weapon.orig_ChangeOverlap orig,
        Weapon self,
        bool newOverlap
    )
    {
      if (self is Spear && (self as Spear).rope().IsRopeSpear)
      {
        var rope = (self as Spear).rope().rope;

        if (self.inFrontOfObjects == (newOverlap ? 1 : 0) || self.room == null)
        {
          return;
        }
        for (int i = 0; i < self.room.game.cameras.Length; i++)
        {
          rope.room.game.cameras[i]
              .MoveObjectToContainer(
                  rope,
                  rope.room.game.cameras[i]
                      .ReturnFContainer(newOverlap ? "Items" : "Background")
              );
        }
        self.inFrontOfObjects = (newOverlap ? 1 : 0);
      }
      orig.Invoke(self, newOverlap);
    }

    private static void Spear_Update(MonoMod.Cil.ILContext il)
    {
      var c = new ILCursor(il);
      if (
          c.TryGotoNext(
              MoveType.After,
              i => i.MatchLdarg(0),
              i => i.Match(OpCodes.Call),
              i => i.MatchLdflda<BodyChunk>("vel"),
              i => i.MatchCall<Vector2>("get_magnitude"),
              i => i.MatchLdcR4(10)
          )
      )
      {
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<float, Spear, float>>(
            (orig, self) =>
            {
              if (self.rope().IsRopeSpear)
              {
                // UnityEngine.Debug.Log("CowBoy Spear modify");
                return 0;
              }
              return 10f;
            }
        );
      }
    }
  }
}
