using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace saveourship
{
    [HarmonyPatch(typeof(ShipCountdown), "CountdownEnded")]
    public class ShipCountdown_countdownend
    {

        private static List<String> saveShip(List<Building> list, List<String> errors)
        {
            Log.Message("saving ship");
            string saved_name = "default";
            try
            {
                bool foundCore = false;
                foreach (Building building in list)
                {
                    if (building.def == ThingDefOf.Ship_ComputerCore)
                    {
                        foundCore = true;
                        Log.Message("getting the ship name");
                        Building_CustomShipComputerCore core = building as Building_CustomShipComputerCore;
                        saved_name = core.RenamableLabel;
                        Log.Message("ship name : " + saved_name);
                    }
                }
                if (!foundCore)
                {
                    Log.Message("no ship core found for naming");
                    errors.Add("no ship core found for naming");
                }
            }
            catch (Exception e)
            {
                Log.Message("CUSTOM_SHIP_COMPUTER_CORE is not valid: " + e.Message);
                errors.Add("CUSTOM_SHIP_COMPUTER_CORE is not valid: " + e.Message);
            }

            if (saved_name == "")
            {
                saved_name = "default";
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
                Log.Message("safesaver saving");
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
                        Log.Error("exception at pawn: " + e.Message);
                        errors.Add("exception at pawn: " + e.Message);
                        try
                        {
                            Log.Message(efpawn.Name.ToString());
                            errors.Add(efpawn.Name.ToString());
                        }
                        catch (Exception innerException)
                        {
                            Log.Error("cannot access pawn name:" + innerException.Message);
                            errors.Add("cannot access pawn name:" + innerException.Message);
                        }

                    }
                }
                Log.Message("Finishing");
                Log.Message("Current.Game.World.worldPawns.AllPawnsAliveOrDead.Count:" + Current.Game.World.worldPawns.AllPawnsAliveOrDead.Count());

                Log.Message("saving pawns");
                Scribe_Collections.Look<Pawn>(ref savedpawns, "oldpawns", LookMode.Deep);
                Log.Message("pawns saved successfully");
            }));
            return errors;

        }

        public static bool CountdownEnded()
        {
            bool hard_fail = false;
            List<String> errors = new List<string>();
            FieldInfo shipRootField = typeof(ShipCountdown).GetField("shipRoot", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            if (shipRootField != null)
            {
                Building shipRoot = (Building)shipRootField.GetValue(null);

                List<Building> list = null;
                try
                {
                    list = ShipUtility.ShipBuildingsAttachedTo(shipRoot).ToList<Building>();

                    if (list.Count == 0)
                    {
                        throw new Exception("ShipBuildingsAttachedTo returned empty");
                    }
                }
                catch (Exception e)
                {

                    Log.Error(e.Message);
                    errors.Add(e.Message);
                    displayResultDialogbox(errors, true);
                    GameVictoryUtility.ShowCredits("Error while getting ship parts", SongDefOf.EndCreditsSong);
                    return false;
                }


                Log.Message("Creating the ending message");

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
                }

                GameVictoryUtility.ShowCredits(GameVictoryUtility.MakeEndCredits("GameOverShipLaunchedIntro".Translate(), "GameOverShipLaunchedEnding".Translate(), stringBuilder.ToString(), "GameOverColonistsEscaped", null), SongDefOf.EndCreditsSong, false, 5f);
                try
                {
                    List<Building> listcopy = new List<Building>(list);
                    errors = saveShip(listcopy, errors);
                }
                catch (Exception e)
                {
                    hard_fail = true;
                    Log.Error("error while saving: " + e.Message);
                    errors.Add("error while saving: " + e.Message);
                }

                foreach (Building building in list)
                {
                    building.Destroy(DestroyMode.Vanish);
                }

                displayResultDialogbox(errors, hard_fail);
                return false;
            }
            Log.Error("Ship root is null, cannot save");
            return true;
        }


        private static void displayResultDialogbox(List<String> errors, bool hard_fail)
        {
            String dialogtext = "";
            //output errors if any
            if (errors.Count != 0)
            {
                if (hard_fail)
                {
                    dialogtext += @"SAVE IS NOT SUCCESSFUL
Save our ship encountered an MAJOR ERROR and the ship file wasn't saved.
Please get in touch with the developer.
Errors:
";
                }
                else
                {
                    dialogtext += @"SAVE IS SUCCESSFUL but save our ship encountered errors
Errors:
";
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

    }

}
