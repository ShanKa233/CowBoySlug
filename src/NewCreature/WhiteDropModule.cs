using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CowBoySlug
{
    internal class WhiteDropModule
    {

        public Color whiteCamoColor = new Color(0f, 0f, 0f);
        public Color whitePickUpColor;

        public float whiteCamoColorAmount = -1f;
        public float whiteCamoColorAmountDrag = 1f;


        public float Camouflaged
        {
            get
            {
                if (this.whiteCamoColorAmount == -1f)
                {
                    return 1f;
                }
                return this.whiteCamoColorAmount;
            }
        }
        public WhiteDropModule()
        {
        }
    }
}
