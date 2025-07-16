using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ChangeDresser
{
    public class ApparelMapTracker : IExposable
    {
        private Dictionary<Apparel, ApparelMap> mapLookup = null;

        public void Clear()
        {
            if (this.mapLookup != null)
            {
                this.mapLookup.Clear();
                this.mapLookup = null;
            }
        }
        
        public void Clean()
        {
            var validApparel = new HashSet<Apparel>();

            if (this.mapLookup == null)
            {
                this.mapLookup = new Dictionary<Apparel, ApparelMap>();
            }

            foreach (Pawn p in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists)
            {
                if (p.Faction == Faction.OfPlayer && p.def.race.Humanlike)
                {
                    foreach (Apparel a in p.apparel.WornApparel)
                    {
                        validApparel.Add(a);
                    }
                    // add validApparel From CustomOutfit
                    if (WorldComp.PawnOutfits.TryGetValue(p, out PawnOutfitTracker tracker))
                    {
                        validApparel.AddRange(tracker.CustomApparel);
                    }
                }
            }
            
            var keysToRemove = new List<Apparel>();
            foreach (var key in this.mapLookup.Keys)
            {
                if (!validApparel.Contains(key))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                this.mapLookup.Remove(key);
            }
        }
        
        
        public void AddApparel(Apparel a)
        {
            if (this.mapLookup == null)
                this.mapLookup = new Dictionary<Apparel, ApparelMap>();

            if (a != null && a.Map != null)
                this.mapLookup[a] = new ApparelMap(a, a.Map);
        }

        public Map GetMap(Apparel a)
        {
            return a != null && mapLookup?.TryGetValue(a, out var apparelMap) == true && !apparelMap.Map.Disposed
                ? apparelMap.Map
                : Find.AnyPlayerHomeMap ?? Find.CurrentMap;
        }
        
        public void RemoveApparel(Apparel a)
        {
            if (this.mapLookup != null && a != null)
                this.mapLookup.Remove(a);
        }

        private List<ApparelMap> l = null;

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (this.mapLookup != null)
                {
                    l = new List<ApparelMap>(this.mapLookup.Count);
                    foreach (ApparelMap am in this.mapLookup.Values)
                    {
                        if (am.Apparel != null && !am.Apparel.Destroyed)
                        {
                            this.l.Add(am);
                        }
                    }
                }
                else
                {
                    l = new List<ApparelMap>(0);
                }
            }

            try
            {
                Scribe_Collections.Look(ref this.l, "ApparelMaps", LookMode.Deep, new object[0]);
            }
            catch (System.Exception e)
            {
                Log.Warning(
                    "Unable to persist original apparel maps." + System.Environment.NewLine +
                    e.GetType().Name + " " + e.Message);
                l.Clear();
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.Clear();

                if (this.l != null)
                {
                    if (this.mapLookup == null)
                    {
                        this.mapLookup = new Dictionary<Apparel, ApparelMap>();
                    }

                    foreach (ApparelMap ac in this.l)
                    {
                        try
                        {
                            if (ac != null && ac.Apparel != null && ac.Map != null && !ac.Map.Disposed)
                            {
                                this.mapLookup.Add(ac.Apparel, ac);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Log.Warning(
                                "Unable to persist an original apparel's map." + System.Environment.NewLine +
                                e.GetType().Name + " " + e.Message);
                        }
                    }
                }
            }


            if ((Scribe.mode == LoadSaveMode.Saving ||
                 Scribe.mode == LoadSaveMode.PostLoadInit) && l != null)
            {
                l.Clear();
                l = null;
            }
        }

        private class ApparelMap : IExposable
        {
            public Apparel Apparel = null;
            public Map Map = null;

            public ApparelMap()
            {
            }

            public ApparelMap(Apparel a, Map m)
            {
                this.Apparel = a;
                this.Map = m;
            }

            public void ExposeData()
            {
                Scribe_References.Look(ref this.Map, "map");
                Scribe_References.Look(ref this.Apparel, "apparel");
            }
        }
    }
}