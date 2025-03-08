using CowBoySLug;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CowBoySlug.Menu
{
    class RemixMenu : OptionInterface
    {
        public RemixMenu()
        {
            foodMod = config.Bind("CnowBoy_Food_Bool_Checkbox", true);
            whiteDrop = config.Bind("CowBoy_WhiteDrop_Bool_Checkbox", true);
        }
        public readonly Configurable<bool> foodMod;
        public readonly Configurable<bool> whiteDrop;

        public override void Initialize()
        {
            var opTab1 = new OpTab(this, "CowBoy Game Setting");
            Tabs = new[] { opTab1 }; // Add the tabs into your list of tabs. If there is only a single tab, it will not show the flap on the side because there is not need to.


            //int sizeTest = 100;
            UIelement[] UIArrayElements = new UIelement[] // Labels in a fixed box size + alignment
            {
                new OpLabel(60, 503, "[CowBoy Food System]"),
                new OpCheckBox(foodMod, 30, 500),


                new OpLabel(60, 473, "[White DropWorm]"),
                new OpCheckBox(whiteDrop, 30, 470),

            };
            opTab1.AddItems(UIArrayElements);
        }


    }
}
