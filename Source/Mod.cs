using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace saveourship
{
    public class saveourship : Mod
    {

        public static Saveourships_settings settings;

        public saveourship(ModContentPack content) : base(content)
        {
            settings = GetSettings<Saveourships_settings>();


            Harmony HMInstance = new Harmony("SOS_SIMPLIFIED");

#if DEBUG
            Harmony.DEBUG = true;
#endif
            Log.Message("Save our ship simplified loaded");

            MethodInfo original = AccessTools.Method(typeof(ShipCountdown), "CountdownEnded");
            MethodInfo prefix = AccessTools.Method(typeof(ShipCountdown_countdownend), "CountdownEnded");
            HMInstance.Patch(original, new HarmonyMethod(prefix));

        }

        public override string SettingsCategory() => "Save Our Ship Simplified";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            checked
            {
                Listing_Standard listing_Standard = new Listing_Standard();
                listing_Standard.Begin(inRect);

                listing_Standard.CheckboxLabeled("Save tech", ref Saveourships_settings.load_tech);
                listing_Standard.CheckboxLabeled("Save drug policies", ref Saveourships_settings.load_drug_policies);
                listing_Standard.TextEntry("Make sure to disable \"save drug policies\" if you remove a mod that adds drugs between games");

#if DEBUG
                listing_Standard.CheckboxLabeled("DEBUG_FORCE_CRASH", ref Saveourships_settings.debugforce_crash);
#endif

                listing_Standard.End();
                settings.Write();
            }
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }
    }

}


