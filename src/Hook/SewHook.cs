
using CowBoySLug;
using Mono.Cecil;
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
    internal class SewHook
    {
        public static ConditionalWeakTable<Creature, SewModule> modules = new ConditionalWeakTable<Creature, SewModule>();

        public static void Hook()
        {
            On.Creature.Violence += SewCreature_Violence;
            On.Lizard.Violence += SewLizard_Violence;
            On.Creature.Update += SewCreature_Update;
        }

        private static void SewCreature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
        {
            orig.Invoke(self, eu);
            if (modules.TryGetValue(self, out var sewModule))
            {
                sewModule.RopeFlash();
            }
        }

        private static void SewLizard_Violence(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            orig.Invoke(self, source, directionAndMomentum,hitChunk,onAppendagePos,type,damage,stunBonus);
            if (!self.State.dead||hitChunk==self.firstChunk|| source == null)
            {
                //确认生物死了没
                return;
            }
            //确认生物是否有被缝纫过了,如果没有就加入缝纫列表
            if (!modules.TryGetValue(self, out var sewModule))
            {
                Spear spear = source.owner as Spear;
                if (spear != null && spear.thrownBy as Player != null && CowBoySlug.Mechanics.RopeSkill.UserData.modules.TryGetValue(spear.thrownBy as Player, out var module))
                {
                    modules.Add(self, new SewModule(self, module.ropeColor));
                }
            }
            //如果被缝纫过了就减少剩余缝纫次数
            else if (modules.TryGetValue(self, out sewModule))
            {
                sewModule.Sewing();
            }
        }

        private static void SewCreature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            orig.Invoke(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            if (!self.State.dead||source==null)
            {
                //确认生物死了没
                return;
            }
            //确认生物是否有被缝纫过了,如果没有就加入缝纫列表
            if (!modules.TryGetValue(self,out var sewModule)) 
            {
                Spear spear = source.owner as Spear;
                if (spear != null && spear.thrownBy as Player != null && Mechanics.RopeSkill.UserData.modules.TryGetValue(spear.thrownBy as Player, out var module))
                {
                    modules.Add(self, new SewModule(self, module.ropeColor));
                }
            }
            //如果被缝纫过了就减少剩余缝纫次数
            else if(modules.TryGetValue(self, out sewModule))
            {
                sewModule.Sewing();
            }
            

        }
    }


    public class SewModule
    {
        public float needSewIndex;//还需要多少下来缝纫
        public float maxSewIndex;//需要多少下来缝纫
        public Color makeItemColor;//做出来是什么颜色
        public Color makeItemColor2;//做出来是什么颜色2
        public Creature owner;


        public Color ropeColor;//线的颜色

        public SewModule(Creature self, Color color)
        {
            this.owner = self;
            this.maxSewIndex = 10+Convert.ToInt32(self.firstChunk.mass*4);
            this.needSewIndex = maxSewIndex-5;


            this.makeItemColor = self.ShortCutColor();
            this.makeItemColor2 = Color.Lerp(self.ShortCutColor(), UnityEngine.Random.ColorHSV(), UnityEngine.Random.Range(0, 0.5f));

            //缝线颜色
            this.ropeColor = color;

        }

        public void RopeFlash()
        {
            if(Random.Range(0,100)*maxSewIndex/needSewIndex>100)
            {
                owner.room.AddObject(new Spark(owner.firstChunk.pos, new Vector2(Random.Range(-5, 5), Random.Range(-5, 5)), ropeColor, null, 20, 80));
            }
            
            needSewIndex += 0.01f;

            if (needSewIndex>maxSewIndex)
            {
                SewHook.modules.Remove(owner);
            }
        }
        public void Sewing()
        {
            if (needSewIndex<=0)
            {
                for (int i = 0; i < 50; i++)
                {
                    owner.room.AddObject(new Spark(owner.firstChunk.pos, new Vector2(Random.Range(-15, 15), Random.Range(-15, 15)), makeItemColor, null, 20, 80));
                }
                var newHat = new CowBoyHat(new CowBoyHatAbstract(owner.room.world, owner.abstractCreature.pos, owner.room.game.GetNewID()));
                newHat.mainColor = this.makeItemColor;
                newHat.decorateColor = this.makeItemColor2;
                newHat.PlaceInRoom(owner.room);
                //owner.room.AddObject(newHat);
                SewHook.modules.Remove(owner);
                owner.Destroy();
                return;
            }
            needSewIndex--;



            for (int i = 0; i < 10; i++)
            {
                owner.room.AddObject(new Spark(owner.firstChunk.pos, new Vector2(Random.Range(-5, 5), Random.Range(-5, 5)), ropeColor, null, 20, 80));
            }

            for (int i = 0; i < owner.bodyChunks.Length; i++)
            {
                if (owner.bodyChunks[i].mass>0.5)
                {
                    owner.bodyChunks[i].mass *= 0.9f;
                }
            }

        }









    }




}
