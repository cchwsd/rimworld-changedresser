using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using System;
using System.Linq;
using ChangeDresser.UI;

namespace ChangeDresser
{
    public class WorldComp : WorldComponent
    {
        public static LinkedList<Building_Dresser> DressersToUse { get; private set; }

        public static Dictionary<Pawn, PawnOutfitTracker> PawnOutfits { get; private set; }

        public static Dictionary<Pawn, PawnOutfitTracker> PlayFunctionPawnOutfits
        {
            get
            {
                return PawnOutfits
                    .Where(p => p.Key.Faction == Faction.OfPlayer && !p.Key.Dead && !p.Key.Destroyed &&
                                !KidnapUtility.IsKidnapped(p.Key)).ToDictionary(p => p.Key, p => p.Value);
            }
            private set { PlayFunctionPawnOutfits = value; }
        }
        
        public static Dictionary<Pawn, PawnOutfitTracker> DisfunctionPawnOutfits
        {
            get
            {
                return PawnOutfits
                    .Where(p => p.Key.Faction != Faction.OfPlayer || p.Key.Dead || p.Key.Destroyed ||
                                KidnapUtility.IsKidnapped(p.Key)).ToDictionary(p => p.Key, p => p.Value);
            }
            private set { PlayFunctionPawnOutfits = value; }
        }

        public static PawnTableDef AssginOutfit
        {
            get
            {
                PawnTableDef assignOutfit = new PawnTableDef();
                assignOutfit.minWidth = (int)AssignOutfitUI.WindowsWidth - (18 * 2);
                assignOutfit.workerClass = typeof(PawnTable_PlayerPawns);
                assignOutfit.defName = "AssignOutfit";
                assignOutfit.columns = new List<PawnColumnDef>();

                PawnColumnDef pawnColumnDef = new PawnColumnDef();
                pawnColumnDef.defName = "LabelShortWithIcon";
                pawnColumnDef.label = "Name".Translate();
                pawnColumnDef.workerClass = typeof(PawnColumnWorker_LabelWithCustomHead);
                pawnColumnDef.sortable = true;
                pawnColumnDef.useLabelShort = true;
                pawnColumnDef.showIcon = true;
                assignOutfit.columns.Add(pawnColumnDef);

                bool moveWorkTypeLabelDown = false;
                foreach (var outfit in Current.Game.outfitDatabase.AllOutfits)
                {
                    moveWorkTypeLabelDown = !moveWorkTypeLabelDown;
                    PawnColumnDef def = new PawnColumnDef_AssignOutfit();
                    def.defName = "Outfit_" + outfit.label;
                    def.workerClass = typeof(PawnColumnWorker_AssignOutfit);
                    def.sortable = true;
                    def.paintable = true;
                    def.moveWorkTypeLabelDown = moveWorkTypeLabelDown;
                    ((PawnColumnDef_AssignOutfit)def).apparelPolicy = outfit;
                    assignOutfit.columns.Add(def);
                }

                // Log.Warning(assignOutfit.columns.Count.ToString());
                return assignOutfit;
            }
        }

        public static List<ApparelPolicy> OutfitsForBattle { get; private set; }

        public static OutfitType GetOutfitType(ApparelPolicy outfit)
        {
            return OutfitsForBattle.Contains(outfit) ? OutfitType.Battle : OutfitType.Civilian;
        }

        public static ApparelMapTracker ApparelMapTracker = new ApparelMapTracker();

        private static int nextDresserOutfitId = 0;

        public static int NextDresserOutfitId
        {
            get
            {
                int id = nextDresserOutfitId;
                ++nextDresserOutfitId;
                return id;
            }
        }

        static WorldComp()
        {
            DressersToUse = new LinkedList<Building_Dresser>();
        }

        public WorldComp(World world) : base(world)
        {
            if (DressersToUse != null)
            {
                DressersToUse.Clear();
            }
            else
            {
                DressersToUse = new LinkedList<Building_Dresser>();
            }

            if (PawnOutfits != null)
            {
                PawnOutfits.Clear();
            }
            else
            {
                PawnOutfits = new Dictionary<Pawn, PawnOutfitTracker>();
            }

            if (OutfitsForBattle != null)
            {
                OutfitsForBattle.Clear();
            }
            else
            {
                OutfitsForBattle = new List<ApparelPolicy>();
            }
        }
        
        
        public static bool TrySpawn(Thing toSpawn, IntVec3 dest, Map map, bool makeForbidden = false)
        {
            try
            {
                if (!toSpawn.Spawned)
                {
                    GenThing.TryDropAndSetForbidden(toSpawn, dest, map, ThingPlaceMode.Direct, out Thing t, makeForbidden);
                    
                }
                if (!toSpawn.Spawned)
                {
                    GenPlace.TryPlaceThing(toSpawn, dest, map, ThingPlaceMode.Direct);
                }
                if (!toSpawn.Spawned)
                {
                    GenPlace.TryPlaceThing(toSpawn, dest, map, ThingPlaceMode.Near);
                }

                toSpawn.Position = dest;

                return toSpawn.Spawned;
            }
            catch (Exception e)
            {
                Log.Warning(
                    "ChangeDresser:BuildingUtil.DropApparel\n" +
                    e.GetType().Name + " " + e.Message + "\n" +
                    e.StackTrace);
            }
            return false;
        }
        
        public static bool StoreApparel(Apparel apparel)
        {
            if (apparel == null)
                return true;
            var map = ApparelMapTracker.GetMap(apparel);
            if (map == null)
                return false;
            ApparelMapTracker.RemoveApparel(apparel);
            if (StoreUtility.TryFindBestBetterStorageFor(
                    apparel,
                    carrier: map.mapPawns.FreeColonists[0], //TODO: dummy colonist?
                    map: map,
                    currentPriority: StoreUtility.CurrentStoragePriorityOf(apparel),
                    faction: Faction.OfPlayer,
                    out IntVec3 destCell,
                    out IHaulDestination haulDestination,
                    needAccurateResult: true
                ))
            {
                int num;
                switch (haulDestination)
                {
                    case ISlotGroupParent _:
                        TrySpawn(apparel, destCell, map);
                        return true;
                    case Thing thing:
                        TrySpawn(apparel, thing.Position, map);
                        return true;
                        break;
                    default:
                        TrySpawn(apparel, destCell, map);
                        return true;
                }
            }

            return false;
        }
        
        public static void AddDresser(Building_Dresser dresser)
        {
            if (dresser == null || dresser.Map == null)
            {
                Log.Error("Cannot add ChangeDresser that is either null or has a null map.");
                return;
            }

            if (!DressersToUse.Contains(dresser))
            {
                DressersToUse.AddFirst(dresser);
                SortDressersToUse();
            }
        }

        public static IEnumerable<Building_Dresser> GetDressers(Map map)
        {
            if (DressersToUse != null)
            {
                foreach (Building_Dresser d in DressersToUse)
                {
                    if (map == null ||
                        (d.Spawned && d.Map == map))
                    {
                        yield return d;
                    }
                }
            }
        }

        public static void CleanupCustomOutfits()
        {
            foreach (PawnOutfitTracker t in PawnOutfits.Values)
                t.Clean();
        }

        public static bool HasDressers()
        {
            return DressersToUse.Count > 0;
        }

        public static bool HasDressers(Map map)
        {
            foreach (Building_Dresser d in DressersToUse)
            {
                if (d.Spawned && d.Map == map)
                    return true;
            }

            return false;
        }

        public static void RemoveDressers(Map map)
        {
            LinkedListNode<Building_Dresser> n = DressersToUse.First;
            while (n != null)
            {
                var next = n.Next;
                Building_Dresser d = n.Value;
                if (d.Map == null)
                {
                    DressersToUse.Remove(n);
                }

                n = next;
            }
        }

        public static bool RemoveDesser(Building_Dresser dresser)
        {
            if (DressersToUse.Remove(dresser))
            {
                return true;
            }

            return false;
        }

        public static void SortDressersToUse()
        {
            LinkedList<Building_Dresser> l = new LinkedList<Building_Dresser>();
            foreach (Building_Dresser d in DressersToUse)
            {
                bool added = false;
                for (LinkedListNode<Building_Dresser> n = l.First; n != null; n = n.Next)
                {
                    if (d.settings.Priority > n.Value.settings.Priority)
                    {
                        added = true;
                        l.AddBefore(n, d);
                        break;
                    }
                }

                if (!added)
                {
                    l.AddLast(d);
                }
            }

            DressersToUse.Clear();
            DressersToUse = l;
        }

        private List<PawnOutfitTracker> tempPawnOutfits = null;

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.tempPawnOutfits = new List<PawnOutfitTracker>(PawnOutfits.Count);
                foreach (PawnOutfitTracker po in PawnOutfits.Values)
                {
                    if (po != null)
                        this.tempPawnOutfits.Add(po);
                }
            }

            Scribe_Values.Look<int>(ref nextDresserOutfitId, "nextDresserOutfitId", 0);
            Scribe_Collections.Look(ref this.tempPawnOutfits, "pawnOutfits", LookMode.Deep, new object[0]);
            Scribe_Deep.Look(ref ApparelMapTracker, "apparelColorTrack");

            List<ApparelPolicy> ofb = OutfitsForBattle;
            Scribe_Collections.Look(ref ofb, "outfitsForBattle", LookMode.Reference, new object[0]);
            OutfitsForBattle = ofb;

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (PawnOutfits == null)
                {
                    PawnOutfits = new Dictionary<Pawn, PawnOutfitTracker>();
                }

                if (OutfitsForBattle == null)
                {
                    OutfitsForBattle = new List<ApparelPolicy>();
                }

                PawnOutfits.Clear();
                if (this.tempPawnOutfits != null)
                {
                    foreach (PawnOutfitTracker po in this.tempPawnOutfits)
                    {
                        if (po != null && po.Pawn != null && !po.Pawn.Dead)
                        {
                            PawnOutfits.Add(po.Pawn, po);
                        }
                    }
                }

                for (int i = OutfitsForBattle.Count - 1; i >= 0; --i)
                {
                    if (OutfitsForBattle[i] == null)
                    {
                        OutfitsForBattle.RemoveAt(i);
                    }
                }

                if (ApparelMapTracker == null)
                {
                    ApparelMapTracker = new ApparelMapTracker();
                }

                ApparelMapTracker.Clean();
            }

            if (this.tempPawnOutfits != null &&
                (Scribe.mode == LoadSaveMode.Saving ||
                 Scribe.mode == LoadSaveMode.PostLoadInit))
            {
                this.tempPawnOutfits.Clear();
                this.tempPawnOutfits = null;
            }
        }
    }
}