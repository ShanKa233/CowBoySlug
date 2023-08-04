using CowBoySLug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CowBoySlug
{
    static class WhiteDropWorm
    {
        public static ConditionalWeakTable<DropBug, WhiteDropModule> modules = new ConditionalWeakTable<DropBug, WhiteDropModule>();
        public  static void Hook()
        {

            On.DropBug.ctor += WhiteDropBug_ctor;
            On.DropBugGraphics.DrawSprites += DropBugGraphics_DrawSprites;
            On.DropBugGraphics.Update += DropBugGraphics_Update;
        }

        private static void DropBugGraphics_Update(On.DropBugGraphics.orig_Update orig, DropBugGraphics self)
        {
            orig.Invoke(self);
            if (modules.TryGetValue(self.bug,out var whiteDropModule))
            {
                float f = 1f - self.bug.State.health;
                if (self.bug.dead)
                {
                    whiteDropModule.whiteCamoColorAmount = Mathf.Lerp(whiteDropModule.whiteCamoColorAmount, 0.3f, 0.01f);
                }
                else
                {
                    if ((self.bug.State.health < 0.6f && Random.value * 1.5f < self.bug.State.health && Random.value < 1f / (self.bug.Stunned ? 10f : 40f)))
                    {
                        if (Random.value < 0.2f)
                        {
                            whiteDropModule.whiteCamoColorAmount = 1f;
                        }
                        if (Random.value < 0.5f)
                        {
                            whiteDropModule.whiteCamoColor = Color.Lerp(whiteDropModule.whiteCamoColor, new Color(Random.value, Random.value, Random.value), Mathf.Pow(f, 0.2f) * Mathf.Pow(Random.value, 0.1f));
                        }
                        if (Random.value < 0.33333334f)
                        {
                            whiteDropModule.whitePickUpColor = new Color(Random.value, Random.value, Random.value);
                        }
                    }
                    else if (Vector2.Distance(self.bug.mainBodyChunk.pos, self.bug.mainBodyChunk.lastPos)==0)
                    {
                        whiteDropModule.whiteCamoColorAmount = Mathf.Clamp(Mathf.Lerp(whiteDropModule.whiteCamoColorAmount, whiteDropModule.whiteCamoColorAmountDrag, 0.1f * Random.value), 0.15f, 1f);
                        whiteDropModule.whiteCamoColor = Color.Lerp(whiteDropModule.whiteCamoColor, whiteDropModule.whitePickUpColor, 0.1f);
                    }
                    else
                    {
                        whiteDropModule.whiteCamoColor = Color.Lerp(whiteDropModule.whiteCamoColor, self.bug.ShortCutColor(), 0.1f);
                    }
                }
            }
        }

        private static void DropBugGraphics_DrawSprites(On.DropBugGraphics.orig_DrawSprites orig, DropBugGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (modules.TryGetValue(self.bug,out var whiteDropModule))
            {

                  if (Vector2.Distance(self.bug.mainBodyChunk.pos, self.bug.mainBodyChunk.lastPos) == 0)
                {
                    whiteDropModule.whitePickUpColor = rCam.PixelColorAtCoordinate(self.bug.mainBodyChunk.pos);
                }
                foreach (var item in sLeaser.sprites)
                {

                    item.color = Color.Lerp(new Color(0.6f, 0.6f, 0.6f), whiteDropModule.whiteCamoColor, whiteDropModule.whiteCamoColorAmount);

                }
            }
        }

        private static void WhiteDropBug_ctor(On.DropBug.orig_ctor orig, DropBug self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature, world);
            if (world.game.session.characterStats.name.value == "CowBoySLug"&&Plugin.menu.whiteDrop.Value)
            {
                if (Random.value>0.8f)
                {
                    modules.Add(self, new WhiteDropModule());
                }
            }
            
        }













    }
}
