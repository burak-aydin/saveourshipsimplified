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

namespace saveourship
{

    
    public class saveourship : Mod
    {
        public static Saveourships_settings settings;

        public saveourship(ModContentPack content) : base(content)
        {
            settings = GetSettings<Saveourships_settings>();
        }

        public override string SettingsCategory() => "Save Our Ship Simplified";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            checked
            {
                Listing_Standard listing_Standard = new Listing_Standard();
                listing_Standard.Begin(inRect);
                
                listing_Standard.CheckboxLabeled("Save tech" , ref Saveourships_settings.save_tech);
                listing_Standard.End();
                settings.Write();
            }
        }

        public override void WriteSettings()
        {
            base.WriteSettings(); 
            
        }



    }


    [StaticConstructorOnStartup]
    public static class MyDetours
    {
        static MyDetours()
        {

        }

        private static void saveShip(List<Building> list)
        {
            string str1 = Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships");
            str1.Replace('/', '\\');
            string name = Faction.OfPlayer.Name;
            if (!System.IO.Directory.Exists(str1))
            {
                System.IO.Directory.CreateDirectory(str1);
            }


            int num = 0;
            string orstr2 = Path.Combine(str1, name);
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
                ScribeMetaHeaderUtility.WriteMetaHeader();
                Scribe_Defs.Look<FactionDef>(ref Faction.OfPlayer.def, "playerFactionDef");

            // Scribe_Deep.Look<List<Building>>(ref list, "ship",false, new UnityEngine.Object[0]);   

            Scribe_Collections.Look<Building>(ref list, "buildings", LookMode.Deep);

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

                Scribe_Deep.Look<ResearchManager>(ref Current.Game.researchManager, false, "researchManager", new object[0]);
                Scribe_Deep.Look<TaleManager>(ref Current.Game.taleManager, false, "taleManager", new object[0]);
                Scribe_Deep.Look<UniqueIDsManager>(ref Current.Game.uniqueIDsManager, false, "uniqueIDsManager", new object[0]);
                Scribe_Deep.Look<DrugPolicyDatabase>(ref Current.Game.drugPolicyDatabase, false, "drugPolicyDatabase", new object[0]);
                Scribe_Deep.Look<OutfitDatabase>(ref Current.Game.outfitDatabase, false, "outfitDatabase", new object[0]);
                Scribe_Deep.Look<PlayLog>(ref Current.Game.playLog, false, "playLog", new object[0]);

                int year = GenDate.Year((long)Find.TickManager.TicksAbs, 0.0f);
                Scribe_Values.Look<int>(ref year, "currentyear", 0);

                List<Pawn> savedpawns = new List<Pawn>();
                List<Pawn> mappawns = Current.Game.CurrentMap.mapPawns.AllPawns.ToList();
                for (int i = 0; i < mappawns.Count; i++)
                {
                    Pawn p = mappawns[i];
                    if (p == null)
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
                    Pawn pawn = Current.Game.World.worldPawns.AllPawnsAliveOrDead.ElementAt(i);
                    if (pawn == null)
                    {
                        continue;
                    }
                    Log.Message("wpn:" + pawn.Name);
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
                        || pawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, colonist))
                        {
                            doo = true;
                        }
                        if (pawn.relations.FamilyByBlood.Contains(colonist))
                        {
                            doo = true;
                        }
                        if (doo)
                        {
                            pawn.SetFaction(Faction.OfPlayer);
                            Log.Message("relativeof:" + colonist.Name);
                            savedpawns.Add(pawn);
                            break;
                        }
                    }
                }

                Log.Message("Current.Game.World.worldPawns.AllPawnsAliveOrDead.Count:" + Current.Game.World.worldPawns.AllPawnsAliveOrDead.Count());

                Scribe_Collections.Look<Pawn>(ref savedpawns, "oldpawns", LookMode.Deep);

            }));

        }
               
        static FieldInfo pht_root = typeof(ShipCountdown).GetField("shipRoot", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        public static void countdownend()
        {
            

            if (pht_root == null)
            {
                Log.Error("pht_root null");
                return;
            }
            Building shiproot = (Building)pht_root.GetValue(null);

            List<Building> list = ShipUtility.ShipBuildingsAttachedTo(shiproot).ToList<Building>();


            if (list.Count == 0)
            {
                Log.Error("list null");
                return;
            }
           


            StringBuilder stringBuilder = new StringBuilder();
            foreach (Building building in list)
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
                building.Destroy(DestroyMode.Vanish);
                Log.Message("111");
            }
            string victoryText = "GameOverShipLaunched".Translate(stringBuilder.ToString(), GameVictoryUtility.PawnsLeftBehind());
            GameVictoryUtility.ShowCredits(victoryText);


            try
            {

                saveShip(list);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

        }
            }
       
    public class ScenLand : ScenPart
    {

        public bool saveresearch = true;
        public bool saveworldpawns = true;
        public bool canGenerateship = false;

        public string shipFactionName;
        public override void PostMapGenerate(Map map)
        {
            if (canGenerateship)
            {

                if (map.gameConditionManager.ownerMap.IsPlayerHome && !map.gameConditionManager.ownerMap.IsTempIncidentMap)
                {
                    loadShip(map);
                }
                canGenerateship = false;
            }


        }

        public override void GenerateIntoMap(Map map)
        {
        }
        public override void PostWorldGenerate()
        {
            canGenerateship = true;
            Find.GameInitData.startingPawnCount = 0;   
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
            
            Scribe_Defs.Look(ref Faction.OfPlayer.def, "playerFactionDef");
            string actualFactionName = Faction.OfPlayer.Name;
            //shipFactionName = actualFactionName;

            Scribe_Deep.Look(ref Current.Game.uniqueIDsManager, false, "uniqueIDsManager", new object[0]);
            Scribe_Deep.Look(ref Current.Game.drugPolicyDatabase, false, "drugPolicyDatabase", new object[0]);
            Scribe_Deep.Look(ref Current.Game.outfitDatabase, false, "outfitDatabase", new object[0]);


            int currentyear = 0; 
            Scribe_Values.Look<int>(ref currentyear, "currentyear", 0);
            
            if (currentyear != 0)
            {
                if (currentyear <= int.MaxValue - 3600000)
                {
                    Scribe_Values.Look<int>(ref currentyear, "gameStartAbsTick", 0, false);


                    currentyear -= 5500;
                    currentyear += 20;
                    Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + currentyear * 3600000);
                }
            }

            Log.Message("techsave:" + Saveourships_settings.save_tech);
            if (Saveourships_settings.save_tech)
            {
                Scribe_Deep.Look(ref Current.Game.researchManager, false, "researchManager", new object[0]);
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
                Log.Message("wpawn:" + pawn.Name);
                pawn.SetFaction(Faction.OfAncients);
                pawn.health.SetDead();
                Current.Game.World.worldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            }
            
            Log.Message("setting terrain");
            // set terrain and remove walls etc
            shipcoordmax += new IntVec3(3, 0, 3);
            shipcoordmin -= new IntVec3(3, 0, 3);
            for (int i = shipcoordmin.x; i < shipcoordmax.x; i++)
            {
                for(int z = shipcoordmin.z; z < shipcoordmax.z; z++)
                {
                    IntVec3 point = new IntVec3(i, 0, z);
                    int cellindex = map.cellIndices.CellToIndex(point);
                    List<Thing> t = map.thingGrid.ThingsAt(point).ToList();
                    foreach (Thing thing in t)
                    {
                        thing.Destroy();
                    }
                    map.terrainGrid.SetTerrain(point, TerrainDefOf.Gravel);
                    map.fogGrid.Unfog(point);
                }
            }
            Log.Message("fixing fog");
            // fix fog
            shipcoordmax += new IntVec3(1, 0, 1);
            shipcoordmin -= new IntVec3(1, 0, 1);
            for (int i = shipcoordmin.x; i < shipcoordmax.x; i++)
            {
                for (int z = shipcoordmin.z; z < shipcoordmax.z; z++)
                {
                    IntVec3 point = new IntVec3(i, 0, z);
                    map.fogGrid.Unfog(point);
                    map.roofGrid.SetRoof(point, null);
                }
            }
            // add ship to map
            new ThingMutator<Building>()
                .For<Building>(x => x.SpawnSetup(map, false))
                .SetAsHome<Building>()
                .UnsafeExecute(ship, Handler);
            
            Log.Message("finalizing");
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
                floatMenuOptionList.Add(new FloatMenuOption(Path.GetFileNameWithoutExtension(ship), (Action)(() => this.shipFactionName = Path.GetFileNameWithoutExtension(ship)), (MenuOptionPriority)4, (Action)null, (Thing)null, 0.0f, (Func<Rect, bool>)null, (WorldObject)null));
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
            if (other is ScenPart_ConfigPage_ConfigureStartingPawns)
                Find.Scenario.RemovePart(other);
            if (other is ScenPart_PlayerPawnsArriveMethod)
                Find.Scenario.RemovePart(other);
            return true;
        }

        

    }
        
}


