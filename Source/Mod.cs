using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;


using UnityEngine;
using RimWorld;
using Verse;
using RimWorld.Planet;
using System.IO;
using HarmonyLib;


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
                
                listing_Standard.CheckboxLabeled("Save tech" , ref Saveourships_settings.load_tech);
                listing_Standard.CheckboxLabeled("Save drug policies", ref Saveourships_settings.load_drug_policies);
                listing_Standard.TextEntry("Make sure to disable \"save drug policies\" if you remove a mod that adds drugs between games");

    //          listing_Standard.CheckboxLabeled("DEBUG_FORCE_CRASH", ref Saveourships_settings.debugforce_crash);
                

                listing_Standard.End();
                settings.Write();
            }
        }

        public override void WriteSettings()
        {
            base.WriteSettings();             
        }
    }


    [HarmonyPatch(typeof(ShipCountdown), "CountdownEnded")]
    public class ShipCountdown_countdownend
    {

        static FieldInfo pht_root = typeof(ShipCountdown).GetField("shipRoot", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        private static List<String> saveShip(List<Building> list, List<String> errors)
        {
            Log.Message("SAVE SHIP STARTED");
            string saved_name = "ship_file_name";
            try
            {
                foreach (Building building in list)
                {
                    if (building.def == ThingDefOf.Ship_ComputerCore)
                    {
                        Log.Message("getting ship name");
                        Building_CustomShipComputerCore core = building as Building_CustomShipComputerCore;
                        saved_name = core.outputname;
                        Log.Message("ship name : " + saved_name);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Message("CUSTOM_SHIP_COMPUTER_CORE is not valid");
                errors.Add("CUSTOM_SHIP_COMPUTER_CORE is not valid");
                Log.Message(e.Message);
            }

            if (saved_name == "")
            {
                saved_name = "ship_file_name";
            }

            string str1 = Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships");
            str1.Replace('/', '\\');
            Log.Message("checking if folder exists : " + str1);
            if (!System.IO.Directory.Exists(str1))
            {
                Log.Message("creating folder : " + str1);
                System.IO.Directory.CreateDirectory(str1);
                Log.Message("folder created successfully");
            }

            int num = 0;
            string orstr2 = Path.Combine(str1, saved_name);
            Log.Message(orstr2);
            string str2 = orstr2 + ".rwship";
            while (System.IO.File.Exists(str2))
            {
                num++;
                str2 = orstr2 + num.ToString() + ".rwship";
            }

            Log.Message(str2);

            SafeSaver.Save(str2, "RWShip", (Action)(() =>
            {
                Log.Message("safesaver");
                ScribeMetaHeaderUtility.WriteMetaHeader();


                List<Pawn> launchedpawns = new List<Pawn>();
                foreach (Building building in list)
                {
                    if (building.def == ThingDefOf.Ship_CryptosleepCasket)
                    {
                        Building_CryptosleepCasket cask = building as Building_CryptosleepCasket;
                        if (cask.HasAnyContents)
                        {
                            Pawn pawn = cask.ContainedThing as Pawn;
                            launchedpawns.Add(pawn);
                        }
                    }
                }

                //start saving
                Scribe_Collections.Look<Building>(ref list, "buildings", LookMode.Deep);

                Scribe_Deep.Look<ResearchManager>(ref Current.Game.researchManager, false, "researchManager", new object[0]);                
                Scribe_Deep.Look<UniqueIDsManager>(ref Current.Game.uniqueIDsManager, false, "uniqueIDsManager", new object[0]);
                Scribe_Deep.Look<DrugPolicyDatabase>(ref Current.Game.drugPolicyDatabase, false, "drugPolicyDatabase", new object[0]);
                Scribe_Deep.Look<OutfitDatabase>(ref Current.Game.outfitDatabase, false, "outfitDatabase", new object[0]);
                Scribe_Deep.Look<IdeoManager>(ref Current.Game.World.ideoManager, false, "ideo", new object[0]);
            //    Scribe_Deep.Look<TaleManager>(ref Current.Game.taleManager, false, "taleManager", new object[0]);
            //    Scribe_Deep.Look<PlayLog>(ref Current.Game.playLog, false, "playLog", new object[0]);


                int year = GenDate.YearsPassed;
                Log.Message("year:" + year);
                Scribe_Values.Look<int>(ref year, "currentyear", 0);

                List<Pawn> savedpawns = new List<Pawn>();
                List<Pawn> mappawns = Current.Game.CurrentMap.mapPawns.AllPawns.ToList();
                for (int i = 0; i < mappawns.Count; i++)
                {
                    Pawn p = mappawns[i];
                    if (p == null)
                        continue;
                    if (p.Destroyed)
                        continue;
                    if (p.Faction != Faction.OfPlayer)
                        continue;
                    if (launchedpawns.Contains(p))
                        continue;
                    Log.Message("rpawns:" + p.Name);
                    savedpawns.Add(p);
                }
                for (int i = 0; i < Current.Game.World.worldPawns.AllPawnsAliveOrDead.Count(); i++)
                {
                    Pawn efpawn = null;
                    try
                    {
                        Pawn pawn = Current.Game.World.worldPawns.AllPawnsAliveOrDead.ElementAt(i);
                        efpawn = pawn;
                        if (pawn == null)
                        {
                            continue;
                        }
                        if (pawn.Destroyed)
                        {
                            continue;
                        }
                        Log.Message("world pawn:" + pawn.Name);
                        if (pawn.Faction == Faction.OfPlayer)
                        {
                            Log.Message("colonistsaved:" + pawn.Name);
                            savedpawns.Add(pawn);
                            continue;
                        }                        

                        foreach (Pawn colonist in launchedpawns)
                        {
                            bool doo = false;
                            if (
                            pawn.relations.DirectRelationExists(PawnRelationDefOf.Bond, colonist)
                            || pawn.relations.DirectRelationExists(PawnRelationDefOf.Lover, colonist)
                            || pawn.relations.DirectRelationExists(PawnRelationDefOf.Parent, colonist)
                            || pawn.relations.DirectRelationExists(PawnRelationDefOf.Child, colonist)
                            || pawn.relations.DirectRelationExists(PawnRelationDefOf.ExSpouse, colonist)
                            || pawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, colonist)
                            || pawn.relations.DirectRelationExists(PawnRelationDefOf.Fiance, colonist))
                            {
                                doo = true;
                            }
                            if (pawn.relations.FamilyByBlood.Contains(colonist))
                            {
                                doo = true;
                            }
                            if (doo)
                            {
                                Log.Message("relativeof:" + colonist.Name);
                                pawn.SetFaction(Current.Game.World.factionManager.OfPlayer);
                                savedpawns.Add(pawn);
                                break;
                            }
                        }


                    }
                    catch (Exception e)
                    {
                        Log.Error("ERROR AT PAWN");
                        Log.Error(e.Message);
                        errors.Add("ERROR AT PAWN");
                        errors.Add(e.Message);
                        try
                        {
                            Log.Message(efpawn.Name.ToString());
                            errors.Add(efpawn.Name.ToString());
                        }
                        catch (Exception innere)
                        {
                            Log.Error("cannot access its name");
                            errors.Add("cannot access its name");
                            Log.Message("innerebegin");
                            Log.Error(innere.Message);
                            Log.Message("innereend");
                        }

                    }
                }
                Log.Message("Finishing");
                Log.Message("Current.Game.World.worldPawns.AllPawnsAliveOrDead.Count:" + Current.Game.World.worldPawns.AllPawnsAliveOrDead.Count());

                Log.Message("savedpawns saving");
                Scribe_Collections.Look<Pawn>(ref savedpawns, "oldpawns", LookMode.Deep);
                Log.Message("savedpawns saved successfully");


            }));
            return errors;

        }
        private static void output_errors(List<String> errors, bool hard_fail)
        {
            bool debugforcecrash = Saveourships_settings.debugforce_crash;

            String dialogtext = "";
            //output errors if any
            if (errors.Count != 0)
            {
                //Root level exception in Update(): System.MissingMethodException: void Verse.Dialog_MessageBox..ctor
                //(Verse.TaggedString,string,System.Action,string,System.Action,string,bool,System.Action,System.Action)
                
                if (hard_fail)
                {
                    dialogtext += "SAVE IS NOT SUCCESSFUL\nSave our ship encountered an MAJOR ERROR and the ship file doesnt saved.\nPlease get in touch with the developer\nErrors:\n";
                }
                else
                {
                    dialogtext += "SAVE IS SUCCESSFUL but save our ship encountered minor errors\nMore information:\n";
                }
                foreach (string error in errors)
                {
                    dialogtext += error + "\n";
                }
                
            }
            else
            {
                dialogtext += "Ship file saved successfully";                
            }

            Dialog_MessageBox dialog = new Dialog_MessageBox(dialogtext);
            Find.WindowStack.Add(dialog);
        }

        public static bool CountdownEnded()
        {
            Log.Message("CountdownEnded");
            bool hard_fail = false;
            List<String> errors = new List<string>();

            if (pht_root != null)
            {
                Building shipRoot = (Building)pht_root.GetValue(null);

                List<Building> list = null;
                try
                {
                    list = ShipUtility.ShipBuildingsAttachedTo(shipRoot).ToList<Building>();

                    if (list.Count == 0)
                    {
                        throw new Exception("LIST_EMPTY");
                    }
                }
                catch (Exception e)
                {

                    Log.Error(e.Message);
                    errors.Add(e.Message);
                    output_errors(errors, true);
                    GameVictoryUtility.ShowCredits("ERROR");
                    return false;
                }



                Log.Message("creating ending message");

                StringBuilder stringBuilder = new StringBuilder();
                foreach (Building building in list)
                {
                    try
                    {
                        Building_CryptosleepCasket building_CryptosleepCasket = building as Building_CryptosleepCasket;
                        if (building_CryptosleepCasket != null && building_CryptosleepCasket.HasAnyContents)
                        {
                            stringBuilder.AppendLine("   " + building_CryptosleepCasket.ContainedThing.LabelCap);
                            Find.StoryWatcher.statsRecord.colonistsLaunched++;
                            TaleRecorder.RecordTale(TaleDefOf.LaunchedShip, new object[]
                            {
                        building_CryptosleepCasket.ContainedThing
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error("error for a building in the list");
                        Log.Error(e.Message);
                        errors.Add("error for a building in the list");
                        errors.Add(e.Message);
                    }

                }

                Log.Message("ShowCreditsb");
                GameVictoryUtility.ShowCredits(GameVictoryUtility.MakeEndCredits("GameOverShipLaunchedIntro".Translate(), "GameOverShipLaunchedEnding".Translate(), stringBuilder.ToString()));
                Log.Message("ShowCreditsa");

                try
                {
                    List<Building> listcopy = new List<Building>(list);

                    errors = saveShip(listcopy, errors);
                }
                catch (Exception e)
                {
                    hard_fail = true;
                    Log.Message("error while saving");
                    errors.Add("error while saving");
                    errors.Add(e.Message);
                    Log.Error(e.Message);
                }


                foreach (Building building in list)
                {
                    building.Destroy(DestroyMode.Vanish);
                }

                output_errors(errors, hard_fail);
                return false;

            }
            else
            {
                Log.Error("pht_root null");
                errors.Add("pht_root null");
                output_errors(errors, true);
                GameVictoryUtility.ShowCredits("ERROR");
            }


            GameVictoryUtility.ShowCredits(GameVictoryUtility.MakeEndCredits("GameOverShipLaunchedIntro".Translate(), "GameOverShipLaunchedEnding".Translate(), null, "GameOverColonistsEscaped", null), null, false, 5f);

            return false;

        }

    }
                
    public class ScenLand : ScenPart
    {

        public bool saveresearch = true;
        public bool saveworldpawns = true;
        public bool load_first = true;

        public string shipFactionName;
        public override void PostMapGenerate(Map map)
        {
        }

        public override void GenerateIntoMap(Map map)
        {

            // save                     Scribe_Collections.Look<Building>(ref list, "buildings", LookMode.Deep);

            
            if (map.gameConditionManager.ownerMap.IsPlayerHome && !map.gameConditionManager.ownerMap.IsTempIncidentMap)
            {
                if (load_first)
                {
                    loadShip(map);
                    load_first = false;
                    Scribe_Values.Look<bool>(ref load_first, "saveourship_game_start", true, true);
					Find.GameInitData.startingAndOptionalPawns.Clear();
				}
			}
        }
        public override void PostWorldGenerate()
        {
			load_first = true;
        }


		public override void PostIdeoChosen()
		{
			Find.GameInitData.startingPawnCount = 1;
		}



		private void Handler(Exception e) => Log.Error("Error during landing: " + e.Message);

        private void loadShip(Map map)
        {
            Log.Message("Loading ship");
            string file = Path.Combine(Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships"), shipFactionName + ".rwship");
            if (!File.Exists(file))
            {
                Log.Error("File Doesnt exist");
                return;
            }

          //  Find.GameInitData.startedFromEntry = false;
            Scribe.loader.InitLoading(file);
           

            Scribe_Deep.Look(ref Current.Game.uniqueIDsManager, false, "uniqueIDsManager", new object[0]);
            Scribe_Deep.Look(ref Current.Game.outfitDatabase, false, "outfitDatabase", new object[0]);
            Scribe_Deep.Look(ref Current.Game.World.ideoManager, false, "ideo", new object[0]);

            List<Ideo> ideosListForReading = Find.IdeoManager.IdeosListForReading;
            

            int currentyear = 0; 
            Scribe_Values.Look<int>(ref currentyear, "currentyear", 0);            
            
            if (currentyear != 0)
            { 
                currentyear += 2;
                if (currentyear <= int.MaxValue - 3600000)
                {                                
                    Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + currentyear * 3600000);
                }
            }
            

            Log.Message("techsave:" + Saveourships_settings.load_tech);
            if (Saveourships_settings.load_tech)
            {
                Scribe_Deep.Look(ref Current.Game.researchManager, false, "researchManager", new object[0]);
            }
            Log.Message("drugsave:" + Saveourships_settings.load_drug_policies);
            if (Saveourships_settings.load_drug_policies)
            {
                Scribe_Deep.Look(ref Current.Game.drugPolicyDatabase, false, "drugPolicyDatabase", new object[0]);
            }


            List<Pawn> oldpawns = new List<Pawn>();
            Scribe_Collections.Look<Pawn>(ref oldpawns,"oldpawns",LookMode.Deep);
            
            List<Building> ship = new List<Building>();

            Scribe_Collections.Look<Building>(ref ship, "buildings", LookMode.Deep);

            Scribe.loader.FinalizeLoading();
            
            if (ship == null){
                Log.Error("ship null");
                return;
            }
            if(ship.Count == 0)
            {
                Log.Error("ship count zero");
                return;
            }            

            IntVec3 spot = MapGenerator.PlayerStartSpot;
            IntVec3 offset = spot - ship[0].Position;

            IntVec3 shipcoordmin = new IntVec3(ship[0].Position.ToVector3() + offset.ToVector3());
            IntVec3 shipcoordmax = new IntVec3(ship[0].Position.ToVector3() + offset.ToVector3());
            
            Log.Message("Shipfix");
            for (int i = 0; i < ship.Count; i++)
            {
                Building building = ship.ElementAt(i);
                building.Position += offset;
                if (shipcoordmax.x < building.Position.x)
                {
                    shipcoordmax.x = building.Position.x;
                }
                if (shipcoordmax.z < building.Position.z)
                {
                    shipcoordmax.z = building.Position.z;
                }
                if (shipcoordmin.x > building.Position.x)
                {
                    shipcoordmin.x = building.Position.x;
                }
                if (shipcoordmin.z > building.Position.z)
                {
                    shipcoordmin.z = building.Position.z;
                }
                building.SetFaction(Current.Game.World.factionManager.OfPlayer);
                
                if(building.def == ThingDefOf.Ship_ComputerCore)
                {
                    ship.RemoveAt(i);
                    i--;
                    continue;
                }
                if (building.def == ThingDefOf.Ship_CryptosleepCasket)
                {
                    Building_CryptosleepCasket cask = building as Building_CryptosleepCasket;
                    if (cask.HasAnyContents)
                    {
                        Pawn pawn = cask.ContainedThing as Pawn;
                        Log.Message("pawn.name:" + pawn.Name);     
                        pawn.SetFaction(Current.Game.World.factionManager.OfPlayer);
                    }
                }
                Building_ShipReactor building_ShipReactor = new Building_ShipReactor();
                CompHibernatable hibernatable = building.TryGetComp<CompHibernatable>();               
                if (hibernatable != null)
                {
                    hibernatable = new CompHibernatable();
                    hibernatable.Initialize(new CompProperties());
                    hibernatable.PostSpawnSetup(true);
                    building.InitializeComps();
                }
            }
            Log.Message("Shipfixend");
                        
            foreach(Pawn pawn in oldpawns)
            {
                Log.Message("found world pawn:" + pawn.Name);
                pawn.SetFaction(Faction.OfAncients);
                pawn.health.SetDead();
                Current.Game.World.worldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            }
            
            Log.Message("setting terrain");            
            // set terrain and remove walls etc
            try
            {
                shipcoordmax += new IntVec3(3, 0, 3);
                shipcoordmin -= new IntVec3(3, 0, 3);
                for (int i = shipcoordmin.x; i < shipcoordmax.x; i++)
                {
                    for (int z = shipcoordmin.z; z < shipcoordmax.z; z++)
                    {
                        IntVec3 point = new IntVec3(i, 0, z);
                        int cellindex = map.cellIndices.CellToIndex(point);
                        List<Thing> t = map.thingGrid.ThingsAt(point).ToList();
                        foreach (Thing thing in t)
                        {
                            thing.Destroy();
                        }
                        map.terrainGrid.SetTerrain(point, TerrainDefOf.Gravel);
                    }
                }
            }catch(Exception e)
            {
                Log.Error(e.Message);
            }            
            Log.Message("fixing fog");
            // fix fog
            try
            {
                shipcoordmax += new IntVec3(1, 0, 1);
                shipcoordmin -= new IntVec3(1, 0, 1);
                for (int i = shipcoordmin.x; i < shipcoordmax.x; i++)
                {
                    for (int z = shipcoordmin.z; z < shipcoordmax.z; z++)
                    {
                        IntVec3 point = new IntVec3(i, 0, z);
                        bool v = map.fogGrid.IsFogged(point);
                        if (v)
                        {
                            map.fogGrid.Unfog(point);
                        }                        
                        map.roofGrid.SetRoof(point, null);
                    }
                }
            }catch(Exception e)
            {
                Log.Error(e.Message);
            }
            // add ship to map
            Log.Message("loading ship building");
            try
            {
                new ThingMutator<Building>()
                    .For<Building>(x => x.SpawnSetup(map, false))
                    .SetAsHome<Building>()
                    .UnsafeExecute(ship, Handler);
            }catch(Exception e)
            {
                Log.Message(e.Message);
            }

            Log.Message("Loading ship complete");
        }

        public override IEnumerable<string> GetSummaryListEntries(string tag)
        {
            if (tag == "LandShip")
                yield return shipFactionName;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref shipFactionName, "shipFactionName", null, false);
            Scribe_Values.Look<bool>(ref load_first, "saveourship_game_start", true, true);
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            if (!Widgets.ButtonText(listing.GetScenPartRect(this, RowHeight), shipFactionName, true, false, true))
                return;
            List<FloatMenuOption> floatMenuOptionList = new List<FloatMenuOption>();
            List<string> stringList = new List<string>();
            stringList.AddRange(Directory.GetFiles(Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships")));
            foreach (string str in stringList)
            {
                string ship = str;
                floatMenuOptionList.Add(new FloatMenuOption(Path.GetFileNameWithoutExtension(ship), (Action)(() => this.shipFactionName = Path.GetFileNameWithoutExtension(ship)), (MenuOptionPriority)4, null, (Thing)null, 0.0f, (Func<Rect, bool>)null, (WorldObject)null));
            }
            Find.WindowStack.Add(new FloatMenu(floatMenuOptionList));
        }

        public override void Randomize()
        {
            List<string> stringList = new List<string>();
            stringList.AddRange(Directory.GetFiles(Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships")));
            shipFactionName = Path.GetFileNameWithoutExtension(stringList.RandomElement());
        }

        public override bool CanCoexistWith(ScenPart other)
        {
            if (other is ScenLand || other is ScenPart_StartingAnimal || (other is ScenPart_StartingThing_Defined || other is ScenPart_ScatterThingsNearPlayerStart))
                return false;
			return true;
        }

        

    }

    //CUSTOM SHIP PART
    public class Building_CustomShipComputerCore : Building_ShipComputerCore
    {

        public string outputname = "Ship Name";

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            outputname = Faction.OfPlayer.Name;
        }

        // Token: 0x17000569 RID: 1385
        // (get) Token: 0x0600245A RID: 9306 RVA: 0x00114E11 File Offset: 0x00113211
      
        public class Dialog_Rename_Ship : Dialog_Rename
        {

            public Dialog_Rename_Ship(Building_CustomShipComputerCore core)
            {
                this.shipcore = core;
                this.curName = core.outputname;
            }

            public override Vector2 InitialSize
            {
                get
                {
                    return new Vector2(500f, 175f);
                }
            }

            protected override void SetName(string name)
            {
                this.shipcore.outputname = this.curName;
            }
            Building_CustomShipComputerCore shipcore;
        }

        public void Rename()
        {
            Find.WindowStack.Add(new Dialog_Rename_Ship(this));

        }

        // Token: 0x0600245B RID: 9307 RVA: 0x00114E24 File Offset: 0x00113224
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo c in base.GetGizmos())
            {
                yield return c;
            }
            
            Command_Action rename = new Command_Action();
            rename.defaultLabel = "Rename";
            rename.icon = TexButton.Rename;
            rename.defaultDesc = outputname;
            rename.action = new Action(this.Rename);
            yield return rename;
            
            yield break;
        }
        
    }


}


