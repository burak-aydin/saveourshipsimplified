using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace saveourship
{
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


            Log.Message("isTechSaved:" + Saveourships_settings.load_tech);
            if (Saveourships_settings.load_tech)
            {
                Scribe_Deep.Look(ref Current.Game.researchManager, false, "researchManager", new object[0]);
            }
            Log.Message("isDrugPoliciesSaved:" + Saveourships_settings.load_drug_policies);
            if (Saveourships_settings.load_drug_policies)
            {
                Scribe_Deep.Look(ref Current.Game.drugPolicyDatabase, false, "drugPolicyDatabase", new object[0]);
            }


            List<Pawn> oldpawns = new List<Pawn>();
            Scribe_Collections.Look<Pawn>(ref oldpawns, "oldpawns", LookMode.Deep);

            List<Building> ship = new List<Building>();
            Scribe_Collections.Look<Building>(ref ship, "buildings", LookMode.Deep);

            Scribe.loader.FinalizeLoading();

            if (ship == null)
            {
                Log.Error("ship is null");
                return;
            }
            if (ship.Count == 0)
            {
                Log.Error("ship count zero");
                return;
            }

            IntVec3 spot = MapGenerator.PlayerStartSpot;
            IntVec3 offset = spot - ship[0].Position;

            IntVec3 shipcoordmin = new IntVec3(ship[0].Position.ToVector3() + offset.ToVector3());
            IntVec3 shipcoordmax = new IntVec3(ship[0].Position.ToVector3() + offset.ToVector3());

            Log.Message("Reading ship contents");
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

                if (building.def == ThingDefOf.Ship_ComputerCore)
                {
                    //we remove the computercore, so the player needs to build new one
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
                CompHibernatable hibernatable = building.TryGetComp<CompHibernatable>();
                if (hibernatable != null)
                {
                    hibernatable = new CompHibernatable();
                    hibernatable.Initialize(new CompProperties());
                    hibernatable.PostSpawnSetup(true);
                    building.InitializeComps();
                }
            }
            Log.Message("Ship contents loaded");

            foreach (Pawn pawn in oldpawns)
            {
                Log.Message("found world pawn:" + pawn.Name);
                pawn.SetFaction(Faction.OfAncients);
                pawn.health.SetDead();
                Current.Game.World.worldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            }

            Log.Message("Setting terrain");
            // set terrain and remove objects in place
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
            }
            catch (Exception e)
            {
                Log.Error("Error while setting terrain: " + e.Message);
            }
            Log.Message("Fixing ship fog and deleting the roof if necessary");
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
            }
            catch (Exception e)
            {
                Log.Error("Error while fixing fog: " + e.Message);
            }
            // add ship to map
            Log.Message("Putting the ship onto the map");
            try
            {
                new ThingMutator<Building>()
                    .For<Building>(x => x.SpawnSetup(map, false))
                    .SetAsHome<Building>()
                    .UnsafeExecute(ship, Handler);
            }
            catch (Exception e)
            {
                Log.Message("Error while putting the ship onto the map: " + e.Message);
            }

            Log.Message("Loading ship completed");
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

}
