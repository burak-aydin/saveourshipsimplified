using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using System.Reflection;
using RimWorld.Planet;
using Verse.Sound;
using UnityEngine;

namespace saveourship
{



    public class Saveourships_settings : ModSettings
    {        
        public static bool save_tech = true;        

        public override void ExposeData()
        {
            base.ExposeData();            
            Scribe_Values.Look<bool>(ref save_tech, "saveourship_save_tech", true, true);            
        }
    }
}
