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
        public static bool load_tech = true;
        public static bool load_drug_policies = true;

        public static bool debugforce_crash = false;


        public override void ExposeData()
        {
            base.ExposeData();            
            Scribe_Values.Look<bool>(ref load_tech, "saveourship_save_tech", true, true);
            Scribe_Values.Look<bool>(ref load_drug_policies, "saveourship_save_drug", true, true);
            Scribe_Values.Look<bool>(ref debugforce_crash, "saveourship_debug_forcecrash", false, true);

        }
    }
}
