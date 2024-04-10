using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace saveourship
{
    public class Building_CustomShipComputerCore : Building_ShipComputerCore, IRenameable
    {
        public string RenamableLabel { get; set; }
        public string BaseLabel { get; set; }
        public string InspectLabel
        {
            get
            {
                return RenamableLabel;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            BaseLabel = Faction.OfPlayer.Name;
            RenamableLabel = BaseLabel;
        }
        public class Dialog_Rename_Ship : Dialog_Rename<IRenameable>
        {

            public Dialog_Rename_Ship(IRenameable renameable) : base(renameable)
            {
            }

            public override Vector2 InitialSize
            {
                get
                {
                    return new Vector2(500f, 175f);
                }
            }

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
            rename.defaultDesc = RenamableLabel;
            rename.action = new Action(this.Rename);
            yield return rename;

            yield break;
        }

    }

}
