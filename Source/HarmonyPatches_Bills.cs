using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace ChangeDresser
{
    partial class HarmonyPatches
    {
        struct StoredApparel
        {
            public readonly Building_Dresser Dresser;
            public readonly Apparel Apparel;
            public StoredApparel(Building_Dresser dresser, Apparel apparel)
            {
                this.Dresser = dresser;
                this.Apparel = apparel;
            }
        }

        struct ApparelToUse
        {
            public readonly List<StoredApparel> Apparel;
            public readonly int Count;
            public ApparelToUse(List<StoredApparel> apparel, int count)
            {
                this.Apparel = apparel;
                this.Count = count;
            }
        }

        class NeededIngrediants
        {
            public readonly ThingFilter Filter;
            public int Count;
            public readonly Dictionary<Def, List<StoredApparel>> FoundThings;

            public NeededIngrediants(ThingFilter filter, int count)
            {
                this.Filter = filter;
                this.Count = count;
                this.FoundThings = new Dictionary<Def, List<StoredApparel>>();
            }
            public void Add(StoredApparel things)
            {
                List<StoredApparel> l;
                if (!this.FoundThings.TryGetValue(things.Apparel.def, out l))
                {
                    l = new List<StoredApparel>();
                    this.FoundThings.Add(things.Apparel.def, l);
                }
                l.Add(things);
            }
            public void Clear()
            {
                this.FoundThings.Clear();
            }
            public bool CountReached()
            {
                foreach (List<StoredApparel> l in this.FoundThings.Values)
                {
                    if (this.CountReached(l))
                        return true;
                }
                return false;
            }
            private bool CountReached(List<StoredApparel> l)
            {
                int count = this.Count;
                foreach (StoredApparel st in l)
                {
                    count -= st.Apparel.stackCount;
                }
                return count <= 0;
            }
            public List<StoredApparel> GetFoundThings()
            {
                foreach (List<StoredApparel> l in this.FoundThings.Values)
                {
                    if (this.CountReached(l))
                    {
#if DEBUG
                        Log.Warning("Count [" + Count + "] reached with: " + l[0].Apparel.def.label);
#endif
                        return l;
                    }
                }
                return null;
            }
        }

    }
}