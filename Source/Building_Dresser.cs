using ChangeDresser.UI.Util;
using RimWorld;
// using SaveStorageSettingsUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ChangeDresser
{
    public class Building_Dresser : Building_Storage //, IStoreSettingsParent
    {
        public const long THIRTY_SECONDS = TimeSpan.TicksPerMinute / 2;
        
        public readonly JobDef storeApparelJobDef = DefDatabase<JobDef>.GetNamed("StoreApparel", true);
        

        public static JobDef WEAR_APPAREL_FROM_DRESSER_JOB_DEF { get; private set; }

        public const StoragePriority DefaultStoragePriority = StoragePriority.Low;

        public bool AllowAdds { get; set; }

        private Map CurrentMap { get; set; }

        private bool includeInTradeDeals = true;

        public bool IncludeInTradeDeals
        {
            get { return this.includeInTradeDeals; }
        }

        private List<Thing> forceAddedApparel = null;

        public bool UseDresserToDressFrom = true;

        private StoragePriority storagePriority = DefaultStoragePriority;

        public string Name = "";

        public Building_Dresser()
        {
            this.AllowAdds = true;
        }
        
        public override string Label => (this.Name == "") ? base.Label : this.Name;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.CurrentMap = map;
            WorldComp.AddDresser(this);

            if (settings == null)
            {
                base.settings = new StorageSettings(this);
                base.settings.CopyFrom(this.def.building.defaultStorageSettings);
                base.settings.filter.SetDisallowAll();
            }

            foreach (Building_RepairChangeDresser r in
                     BuildingUtil.FindThingsOfTypeNextTo<Building_RepairChangeDresser>(base.Map, base.Position,
                         Settings.RepairAttachmentDistance))
            {
#if DEBUG_REPAIR
                Log.Warning("Adding Dresser " + this.Label + " to " + r.Label);
#endif
                r.AddDresser(this);
            }

            this.storagePriority = base.settings.Priority;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            try
            {
                this.Dispose();
                base.Destroy(mode);
            }
            catch (Exception e)
            {
                Log.Error(
                    "ChangeDresser:Building_Dresser.Destroy\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            try
            {
                this.Dispose();
                base.DeSpawn(mode);
            }
            catch (Exception e)
            {
                Log.Error(
                    "ChangeDresser:Building_Dresser.DeSpawn\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        private void Dispose()
        {
            try
            {
                this.Empty<Apparel>();
            }
            catch (Exception e)
            {
                Log.Error(
                    "ChangeDresser:Building_Dresser.Dispose\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }

            WorldComp.RemoveDesser(this);
            foreach (Building_RepairChangeDresser r in
                     BuildingUtil.FindThingsOfTypeNextTo<Building_RepairChangeDresser>(this.CurrentMap, base.Position,
                         Settings.RepairAttachmentDistance))
            {
#if DEBUG_REPAIR
                Log.Warning("Removing Dresser " + this.Label + " to " + r.Label);
#endif
                r.RemoveDresser(this);
            }
        }

        private bool DropThing(Thing t, bool makeForbidden = true)
        {
            return BuildingUtil.DropThing(t, this, this.CurrentMap, makeForbidden);
        }

        private void DropApparel<T>(IEnumerable<T> things, bool makeForbidden = true) where T : Thing
        {
            try
            {
                if (things != null)
                {
                    foreach (T t in things)
                    {
                        this.DropThing(t, makeForbidden);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    "ChangeDresser:Building_Dresser.DropApparel\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
        }

        public void Empty<T>(List<T> removed = null) where T : Thing
        {
            try
            {
                this.AllowAdds = false;
            }
            finally
            {
                this.AllowAdds = true;
            }
        }

        internal void ReclaimApparel(bool force = false)
        {
            if (base.Map == null)
                return;
#if DEBUG
            List<Apparel> ll =
 new List<Apparel>(BuildingUtil.FindThingsOfTypeNextTo<Apparel>(base.Map, base.Position, 1));
            Log.Warning("Apparel found: " + ll.Count);
#endif
            try
            {
                List<Thing> l = BuildingUtil.FindThingsNextTo(base.Map, base.Position, 1);
                if (l.Count > 0)
                {
                    foreach (Thing t in l)
                    {
                        try
                        {
                            if (t is Apparel)
                            {
                                if (!WorldComp.StoreApparel((Apparel)t) &&
                                    force &&
                                    t.Spawned)
                                {
                                    Messages.Message("Unable to store apparel: " + t.Label + ". No available storage space.", MessageTypeDefOf.CautionInput, false);
                                }
                            }
                        }
                        catch
                        {
                            // Ignore
                        }
                    }

                    l.Clear();
                    l = null;
                }
            }
            catch
            {
                // Ignore
            }
        }


//         public void HandleThingsOnTop()
//         {
// #if TRADE_DEBUG
//             Log.Warning("Start ChangeDresser.HandleThingsOnTop for " + this.Label + " Spawned: " + this.Spawned);
// #endif
//             if (this.Spawned)
//             {
//                 foreach (Thing t in base.Map.thingGrid.ThingsAt(this.Position))
//                 {
// #if DEBUG
//                     Log.Warning("ChangeDresser.HandleThingsOnTop - Thing " + t.Label + " Type: " + t.GetType().Name);
// #endif
//                     if (t != null && t != this && !(t is Blueprint) && !(t is Building))
//                     {
//                         if (t is Apparel)
//                         {
//                             this.AddApparel((Apparel)t);
//                         }
//                         else
//                         {
//                             IntVec3 p = t.Position;
//                             p.x = p.x + 1;
//                             t.Position = p;
//                             Log.Warning("Moving " + t.Label);
//                         }
//                     }
//                 }
//             }
// #if TRADE_DEBUG
//             Log.Warning("End ChangeDresser.HandleThingsOnTop");
// #endif
//         }

        // public override void Notify_ReceivedThing(Thing newItem)
        // {
        //     if (!this.AllowAdds ||
        //         !(newItem is Apparel))
        //     {
        //         DropThing(newItem);
        //         return;
        //     }
        //
        //     Apparel a = (Apparel)newItem;
        //     base.Notify_ReceivedThing(a);
        //     if (!this.StoredApparel.Contains(a))
        //     {
        //         if (newItem.Spawned)
        //         {
        //             newItem.DeSpawn();
        //         }
        //
        //         this.StoredApparel.AddApparel(a);
        //     }
        // }

        private List<Apparel> tempApparelList = null;

        public override void ExposeData()
        {
#if DEBUG
            Log.Warning(Environment.NewLine + "Start Building_Dresser.ExposeData mode: " + Scribe.mode);
#endif
            base.ExposeData();

            //bool useInLookup = this.UseInApparelLookup;
            //Scribe_Values.Look(ref useInLookup, "useInApparelLookup", false, false);
            //this.UseInApparelLookup = useInLookup;

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (this.forceAddedApparel == null)
                    this.forceAddedApparel = new List<Thing>();
            }

#if DEBUG
            Log.Warning(" Scribe_Collections.Look tempApparelList");
#endif
            Scribe_Collections.Look(ref this.tempApparelList, "apparel", false, LookMode.Deep, new object[0]);
            Scribe_Values.Look(ref this.includeInTradeDeals, "includeInTradeDeals", true);
            Scribe_Collections.Look(ref this.forceAddedApparel, "forceAddedApparel", false, LookMode.Deep,
                new object[0]);
            Scribe_Values.Look(ref this.UseDresserToDressFrom, "useDresserToDressFrom", true, false);
            Scribe_Values.Look(ref this.Name, "name", "", false);
#if DEBUG
            if (this.tempApparelList != null)
                Log.Warning(" tempApparelList Count: " + this.tempApparelList.Count);
            else
                Log.Warning(" StempApparelList is null");
#endif
            if (this.tempApparelList != null &&
                Scribe.mode == LoadSaveMode.PostLoadInit)
            {
#if DEBUG
                Log.Warning(" tempApparelList != null && PostLoadInit");
#endif
                foreach (Apparel apparel in this.tempApparelList)
                {
                    if (apparel != null && !apparel.Destroyed && apparel.HitPoints > 0.01)
                    {
                        BuildingUtil.DropThing(apparel, this, this.Map, false);
                    }
                }
            }

            if (this.tempApparelList != null &&
                (Scribe.mode == LoadSaveMode.Saving ||
                 Scribe.mode == LoadSaveMode.PostLoadInit))
            {
#if DEBUG
                StringBuilder sb = new StringBuilder(" Saving or PostLoadInit - Count: " + this.StoredApparel.Count);
                foreach (Apparel a in this.StoredApparel.Apparel)
                {
                    sb.Append(", ");
                    sb.Append(a.LabelShort);
                }
                Log.Warning(sb.ToString());
#endif
                this.tempApparelList.Clear();
                this.tempApparelList = null;

                if (this.forceAddedApparel != null && this.forceAddedApparel.Count == 0)
                    this.forceAddedApparel = null;
            }

#if DEBUG
            Log.Message("End Building_Dresser.ExposeData" + Environment.NewLine);
#endif
        }

        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder(base.GetInspectString());
            if (sb.Length > 0)
                sb.Append(Environment.NewLine);
            sb.Append("ChangeDresser.StoragePriority".Translate());
            sb.Append(": ");
            sb.Append(("StoragePriority" + base.settings.Priority).Translate());
            sb.Append(Environment.NewLine);
            sb.Append("ChangeDresser.ApparelCount".Translate());
            sb.Append(": ");
            sb.Append(this.Count);
            sb.Append(Environment.NewLine);
            sb.Append("ChangeDresser.IncludeInTradeDeals".Translate());
            sb.Append(": ");
            sb.Append(this.includeInTradeDeals.ToString());
            return sb.ToString();
        }

        public List<Apparel> Apparel
        {
            get
            {
                List<Thing> things = new List<Thing>();
                this.Map.listerThings.GetAllThings(
                    in things,
                    ThingRequestGroup.Apparel,
                    validator: thing => thing.IsInAnyStorage(),
                    lookInHaulSources: true
                );
                return things.OfType<Apparel>().ToList();
            }
        }

        public int Count => this.Apparel.Count;

        /// <summary>
        /// DO NOT CHANGE THIS METHOD'S SIGNATURE. IT WILL BREAK MENDING PATCH MOD
        /// </summary>
        [Obsolete("No StoredApparel Allowed.", true)]
        public void Remove(Apparel a, bool forbidden = true)
        {
            // TODO: fix reparing
        }
        
        //private long lastAutoCollect = 0;
        public override void TickLong()
        {
            if (this.Spawned && base.Map != null)
            {
                // Fix for an issue where apparel will appear on top of the dresser even though it's already stored inside
                // this.HandleThingsOnTop();
            }

            /*if (!this.AreStorageSettingsEqual())
            {
                try
                {
                    this.AllowAdds = false;

                    WorldComp.SortDressersToUse();
                    this.UpdatePreviousStorageFilter();

                    List<Apparel> removed = this.StoredApparel.RemoveFilteredApparel(this.settings);
                    foreach (Apparel a in removed)
                    {
                        if (!WorldComp.AddApparel(a))
                        {
                            this.DropThing(a, false);
                        }
                    }
                }
                finally
                {
                    this.AllowAdds = true;
                }
            }*/

            if (this.forceAddedApparel != null && this.forceAddedApparel.Count > 0)
            {
                foreach (Thing t in this.forceAddedApparel)
                {
                    try
                    {
                        this.DropThing(t, false);
                    }
                    catch
                    {
                    }
                }

                this.forceAddedApparel.Clear();
                this.forceAddedApparel = null;
            }

            /*long now = DateTime.Now.Millisecond;
            if (now - this.lastAutoCollect > THIRTY_SECONDS)
            {
                this.lastAutoCollect = now;
                this.ReclaimApparel();
            }*/

            if (this.storagePriority != base.settings.Priority)
            {
                this.storagePriority = base.settings.Priority;
                WorldComp.SortDressersToUse();
            }
        }

        #region Float Menu Options

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn pawn)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            if (pawn.apparel?.LockedApparel?.Count == 0)
            {
                list.Add(new FloatMenuOption(
                    "ChangeDresser.StoreApparel".Translate(),
                    delegate
                    {
                        Job job = new Job(storeApparelJobDef, this);
                        pawn.jobs.TryTakeOrderedJob(job);
                    }));
            }

            return list;
        }
        
        public override IEnumerable<Gizmo> GetGizmos()
        {
            IEnumerable<Gizmo> enumerables = base.GetGizmos();

            List<Gizmo> l;
            if (enumerables != null)
                l = new List<Gizmo>(enumerables);
            else
                l = new List<Gizmo>(1);

            int groupKey = this.GetType().Name.GetHashCode();

            // l.Add(new Command_Action
            // {
            //     icon = ContentFinder<Texture2D>.Get("UI/Commands/RenameZone", true),
            //     defaultLabel = "CommandRenameZoneLabel".Translate(),
            //     action = delegate { Find.WindowStack.Add(new Dialog_Rename(this)); },
            // });

            Command_Action a = new Command_Action();
            a.icon = WidgetUtil.manageapparelTexture;
            a.defaultDesc = "ChangeDresser.ManageApparelDesc".Translate();
            a.defaultLabel = "ChangeDresser.ManageApparel".Translate();
            a.activateSound = SoundDef.Named("Click");
            a.action = delegate { Find.WindowStack.Add(new UI.StorageUI(this, null)); };
            a.groupKey = groupKey;
            ++groupKey;
            l.Add(a);

            a = new Command_Action();
            a.icon = WidgetUtil.assignweaponsTexture;
            a.defaultDesc = "ChangeDresser.AssignOutfitsDesc".Translate();
            a.defaultLabel = "ChangeDresser.AssignOutfits".Translate();
            a.activateSound = SoundDef.Named("Click");
            a.action = delegate { Find.WindowStack.Add(new UI.AssignOutfitUI(this)); };
            a.groupKey = groupKey;
            ++groupKey;
            l.Add(a);

            a = new Command_Action();
            a.icon = WidgetUtil.customapparelTexture;
            a.defaultDesc = "ChangeDresser.CustomOutfitsDesc".Translate();
            a.defaultLabel = "ChangeDresser.CustomOutfits".Translate();
            a.activateSound = SoundDef.Named("Click");
            a.action = delegate { Find.WindowStack.Add(new UI.CustomOutfitUI(this)); };
            a.groupKey = groupKey;
            ++groupKey;
            l.Add(a);

            // a = new Command_Action();
            // a.icon = WidgetUtil.emptyTexture;
            // a.defaultDesc = "ChangeDresser.EmptyDesc".Translate();
            // a.defaultLabel = "ChangeDresser.Empty".Translate();
            // a.activateSound = SoundDef.Named("Click");
            // a.action =
            //     delegate { this.Empty<Apparel>(); };
            // a.groupKey = groupKey;
            // ++groupKey;
            // l.Add(a);

            a = new Command_Action();
            a.icon = WidgetUtil.collectTexture;
            a.defaultDesc = "ChangeDresser.CollectDesc".Translate();
            a.defaultLabel = "ChangeDresser.Collect".Translate();
            a.activateSound = SoundDef.Named("Click");
            a.action =
                delegate { this.ReclaimApparel(); };
            a.groupKey = groupKey;
            ++groupKey;
            l.Add(a);

            // a = new Command_Action();
            // if (this.includeInTradeDeals)
            // {
            //     a.icon = WidgetUtil.yesSellTexture;
            // }
            // else
            // {
            //     a.icon = WidgetUtil.noSellTexture;
            // }

            // a.defaultDesc = "ChangeDresser.IncludeInTradeDealsDesc".Translate();
            // a.defaultLabel = "ChangeDresser.IncludeInTradeDeals".Translate();
            // a.activateSound = SoundDef.Named("Click");
            // a.action =
            //     delegate { this.includeInTradeDeals = !this.includeInTradeDeals; };
            // a.groupKey = groupKey;
            // ++groupKey;
            // l.Add(a);

            // a = new Command_Action();
            // if (this.UseDresserToDressFrom)
            // {
            //     a.icon = WidgetUtil.yesDressFromTexture;
            // }
            // else
            // {
            //     a.icon = WidgetUtil.noDressFromTexture;
            // }
            //
            // a.defaultDesc = "ChangeDresser.UseDresserToDressFromDesc".Translate();
            // a.defaultLabel = "ChangeDresser.UseDresserToDressFrom".Translate();
            // a.activateSound = SoundDef.Named("Click");
            // a.action =
            //     delegate { this.UseDresserToDressFrom = !this.UseDresserToDressFrom; };
            // a.groupKey = groupKey;
            // ++groupKey;
            // l.Add(a);

            // return SaveStorageSettingsGizmoUtil.AddSaveLoadGizmos(l, SaveTypeEnum.Apparel_Management, this.settings.filter);
            return l;
        }

        #endregion

/*#region ThingFilters
        private ThingFilter previousStorageFilters = new ThingFilter();
        private FieldInfo AllowedDefsFI = typeof(ThingFilter).GetField("allowedDefs", BindingFlags.Instance | BindingFlags.NonPublic);
        protected bool AreStorageSettingsEqual()
        {
            ThingFilter currentFilters = base.settings.filter;
            if (currentFilters.AllowedDefCount != this.previousStorageFilters.AllowedDefCount ||
                currentFilters.AllowedQualityLevels != this.previousStorageFilters.AllowedQualityLevels ||
                currentFilters.AllowedHitPointsPercents != this.previousStorageFilters.AllowedHitPointsPercents)
            {
                return false;
            }

            HashSet<ThingDef> currentAllowed = AllowedDefsFI.GetValue(currentFilters) as HashSet<ThingDef>;
            foreach (ThingDef previousAllowed in AllowedDefsFI.GetValue(this.previousStorageFilters) as HashSet<ThingDef>)
            {
                if (!currentAllowed.Contains(previousAllowed))
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdatePreviousStorageFilter()
        {
            ThingFilter currentFilters = base.settings.filter;

            this.previousStorageFilters.AllowedHitPointsPercents = currentFilters.AllowedHitPointsPercents;
            this.previousStorageFilters.AllowedQualityLevels = currentFilters.AllowedQualityLevels;

            HashSet<ThingDef> previousAllowed = AllowedDefsFI.GetValue(this.previousStorageFilters) as HashSet<ThingDef>;
            previousAllowed.Clear();
            previousAllowed.AddRange(AllowedDefsFI.GetValue(currentFilters) as HashSet<ThingDef>);
        }
        #endregion*/
    }
}