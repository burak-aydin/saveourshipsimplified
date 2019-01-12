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


    [StaticConstructorOnStartup]
    public static class MyDetours
    {
        static MyDetours()
        {

        }

        private static void saveShip(List<Building> list)
        {
            string str1 = Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships");
            string name = Faction.OfPlayer.Name;


            string str2 = Path.Combine(str1, name + ".rwship");

            bool zero = true;
            System.IO.Directory.CreateDirectory(str1);
            while (System.IO.Directory.Exists(str2))
            {
                if (zero)
                {
                    name += "1";
                    str2 = Path.Combine(str1, name + ".rwship");
                    zero = false;
                }
                else
                {

                    int i;
                    if (Int32.TryParse(name.Last().ToString(), out i))
                    {
                        name = name.Substring(0, name.Length - 1);
                        name += ++i;
                        str2 = Path.Combine(str1, name + ".rwship");
                    }
                    else
                    {
                        Log.Error("name parsing error");
                    }


                }
            }

            SafeSaver.Save(str2, "RWShip", (Action)(() =>
            {
                ScribeMetaHeaderUtility.WriteMetaHeader();
                Scribe_Defs.Look<FactionDef>(ref Faction.OfPlayer.def, "playerFactionDef");


               // Scribe_Deep.Look<List<Building>>(ref list, "ship",false, new UnityEngine.Object[0]);   
                                                                               
                Scribe_Collections.Look<Building>(ref list, "buildings", LookMode.Deep);

                /*
                int i = 0;
                foreach(Building building in list)
                {
                    if(building.def == ThingDefOf.Ship_CryptosleepCasket)
                    {
                        Building_CryptosleepCasket casket = building as Building_CryptosleepCasket;
                        Pawn pawn = casket.ContainedThing as Pawn;
                        Scribe_Deep.Look<Pawn>(ref pawn, "pawn" + i.ToString());
                        i++;
                    }
                }
                */

                Scribe_Deep.Look<ResearchManager>(ref Current.Game.researchManager, false, "researchManager", new object[0]);
                Scribe_Deep.Look<TaleManager>(ref Current.Game.taleManager, false, "taleManager", new object[0]);
                Scribe_Deep.Look<UniqueIDsManager>(ref Current.Game.uniqueIDsManager, false, "uniqueIDsManager", new object[0]);
                Scribe_Deep.Look<TickManager>(ref Current.Game.tickManager, false, "tickManager", new object[0]);
                Scribe_Deep.Look<DrugPolicyDatabase>(ref Current.Game.drugPolicyDatabase, false, "drugPolicyDatabase", new object[0]);
                Scribe_Deep.Look<OutfitDatabase>(ref Current.Game.outfitDatabase, false, "outfitDatabase", new object[0]);
                Scribe_Deep.Look<PlayLog>(ref Current.Game.playLog, false, "playLog", new object[0]);
                Scribe_Deep.Look<WorldPawns>(ref Current.Game.World.worldPawns, false, "worldPawns", new object[0]);

                


            }));

        }



        static FieldInfo pht_root = typeof(ShipCountdown).GetField("shipRoot", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        public static void countdownend()
        {
            GameVictoryUtility.ShowCredits("victory");
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

            saveShip(list);
            /*
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
            */
            //string victoryText = "GameOverShipLaunched".Translate(stringBuilder.ToString(), GameVictoryUtility.PawnsLeftBehind());
            Log.Message("launched");

        }

    }
       
    public class ScenLand : ScenPart
    {
     
        public string shipFactionName;
        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);

            loadShip(map);

           
         //   GameDataSaveLoader.LoadGame("name.rws");            
        }

        public override void GenerateIntoMap(Map map)
        {
        }
        public override void PostWorldGenerate()
        {
            Find.GameInitData.startingPawnCount = 0;

            

        }

        private void Handler(Exception e) => Log.Error("Error during landing: " + e.Message);

        private void loadShip(Map map)
        {
            Log.Message("Loading ship");
            string file = Path.Combine(Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships"), "New Arrivals" + ".rwship");
            if (!File.Exists(file))
            {
                Log.Error("File Doesnt exist");
                return;
            }


            Scribe.loader.InitLoading(file);

            Scribe_Deep.Look(ref Current.Game.uniqueIDsManager, false, "uniqueIDsManager", new object[0]);
            Scribe_Deep.Look(ref Current.Game.tickManager, false, "tickManager", new object[0]);
            Scribe_Deep.Look(ref Current.Game.drugPolicyDatabase, false, "drugPolicyDatabase", new object[0]);
            Scribe_Deep.Look(ref Current.Game.outfitDatabase, false, "outfitDatabase", new object[0]);
            Scribe_Deep.Look(ref Current.Game.World.worldPawns, false, "worldPawns", new object[0]);
            
            List<Building> ship = new List<Building>();

            //Scribe_Deep.Look(ref ship, false, "ship", new object[0]);
            Scribe_Collections.Look<Building>(ref ship, "buildings", LookMode.Deep);


            if (ship == null){
                Log.Error("ship null");
                return;
            }
            if(ship.Count == 0)
            {
                Log.Error("ship count zero");
                return;
            }


         
            foreach (Building building in ship)
            {
                if (building.def == ThingDefOf.Ship_CryptosleepCasket)
                {
                    Building_CryptosleepCasket cask = building as Building_CryptosleepCasket;
                    if (cask.HasAnyContents)
                    {
                        Pawn pawn = cask.ContainedThing as Pawn;
                        PawnComponentsUtility.AddComponentsForSpawn(pawn);

                        List<Hediff> hediffs = new List<Hediff>(pawn.health.hediffSet.hediffs);
                        pawn.health.Reset();
                        foreach(Hediff h in hediffs)
                        {                         
                            pawn.health.AddHediff(h);
                        }
                        
                        Log.Message("third");
                    }
                }
                
                /*
                CompHibernatable hibernatable = building.TryGetComp<CompHibernatable>();               
                if (hibernatable != null)
                {
                    hibernatable.State = HibernatableStateDefOf.Hibernating;
                    hibernatable.Startup();
                }
                */
               
            }




            IntVec3 spot = MapGenerator.PlayerStartSpot;

            string v1 = "Landing";
            bool v2 = true;
            LongEventHandler.QueueLongEvent(() =>
            {



            }, v1 + "_Spawn", v2, Handler);

            new ThingMutator<Building>()
                .For<Building>(x => x.SpawnSetup(map, false))
                .SetAsHome<Building>()
                .QueueAsLongEvent(ship, v1 + "_Spawn", v2, Handler)
                .Then(() =>
                {
                    ship.Where(t => t is Building).Select(t => new WeakReference(t));

                    GameDataSaveLoader.SaveGame("name.rws");
                    Verse.GenScene.GoToMainMenu();
                    GameDataSaveLoader.LoadGame("name.rws");

                }, v1 + "_Spawn", Handler);

           

            Scribe_Deep.Look(ref Current.Game.researchManager, false, "researchManager", new object[0]);
            Scribe_Deep.Look(ref Current.Game.taleManager, false, "taleManager", new object[0]);
            Scribe_Deep.Look(ref Current.Game.playLog, false, "playLog", new object[0]);

            string actualFactionName = "";
            Scribe_Deep.Look(ref actualFactionName, "playerFactionDef");
            Faction.OfPlayer.Name = actualFactionName;
            shipFactionName = actualFactionName;
            Log.Message("Loading complete");

            

        }

        public override string Summary(Scenario scen)
        {
            return ScenSummaryList.SummaryWithList(scen, "LandShip", Translator.Translate(nameof(ScenLand)));
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


