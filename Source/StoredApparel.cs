using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using System;
using System.Text;
using UnityEngine;

namespace ChangeDresser
{
    public class StoredApparel
    {
        internal Dictionary<ThingDef, LinkedList<Apparel>> StoredApparelLookup =
            new Dictionary<ThingDef, LinkedList<Apparel>>();

        public int Count
        {
            get
            {
                int count = 0;
                foreach (LinkedList<Apparel> ll in this.StoredApparelLookup.Values)
                {
                    count += ll.Count;
                }

                return count;
            }
        }

        public IEnumerable<Apparel> Apparel
        {
            get
            {
                List<Apparel> l = new List<Apparel>(this.Count);
                foreach (LinkedList<Apparel> ll in this.StoredApparelLookup.Values)
                {
                    foreach (Apparel a in ll)
                    {
                        l.Add(a);
                    }
                }

                return l;
            }
        }

        [Obsolete("No StoredApparel Allowed.", true)]
        public void AddApparel(Apparel apparel)
        {
            if (apparel != null)
            {
                if (!this.StoredApparelLookup.TryGetValue(apparel.def, out LinkedList<Apparel> l))
                {
                    l = new LinkedList<Apparel>();
                    this.StoredApparelLookup.Add(apparel.def, l);
                }

                this.AddApparelToLinkedList(apparel, l);
            }
            else
            {
                Log.Error("Tried to add null apparel");
            }
        }

        
        private static readonly SimpleCurve HitPointsPercentScoreFactorCurve = new SimpleCurve()
        {
            {
                new CurvePoint(0.0f, 0.0f),
                true
            },
            {
                new CurvePoint(0.2f, 0.2f),
                true
            },
            {
                new CurvePoint(0.22f, 0.3f),
                true
            },
            {
                new CurvePoint(0.5f, 0.3f),
                true
            },
            {
                new CurvePoint(0.52f, 1f),
                true
            }
        };

        [Obsolete("No StoredApparel Allowed.", true)]
        private static float ApparelScoreRaw(Apparel ap)
        {
            if (ap.def.apparel.blocksVision ||
                ap.def.apparel.slaveApparel)
                return -10f;
            float num1 = 0.1f + ap.def.apparel.scoreOffset + (ap.GetStatValue(StatDefOf.ArmorRating_Sharp) +
                                                              ap.GetStatValue(StatDefOf.ArmorRating_Blunt));
            if (ap.def.useHitPoints)
            {
                float x = (float)ap.HitPoints / (float)ap.MaxHitPoints;
                num1 *= HitPointsPercentScoreFactorCurve.Evaluate(x);
            }

            float num2 = num1 + ap.GetSpecialApparelScoreOffset();
            float num3 = 1f;

            float num4 = num2 * num3;
            if (ap.WornByCorpse)
            {
                num4 -= 0.5f;
                if ((double)num4 > 0.0)
                    num4 *= 0.1f;
            }
            if (ap.Stuff == ThingDefOf.Human.race.leatherDef)
            {
                num4 -= 0.5f;
            }
            return num4;
        }


        [Obsolete("No StoredApparel Allowed.", true)]
        private void AddApparelToLinkedList(Apparel apparel, LinkedList<Apparel> l)
        {
#if DEBUG
            if (!l.Contains(apparel))
            {
                Log.Error("already contains apparel " + apparel.Label);
                return;
            }

#endif
            // Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Pirate);
            // Pawn pawn = null;
            // List<Pawn> pawns = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction;
            // if (pawns != null && pawns.Count > 0)
            //     pawn = pawns[0];
            // else
            //     pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist);

            float score = ApparelScoreRaw(apparel);
            for (LinkedListNode<Apparel> n = l.First; n != null; n = n.Next)
            {
                float nScore = ApparelScoreRaw(apparel);
                if (score >= nScore)
                {
                    l.AddBefore(n, apparel);
                    return;
                }
                else if (score < nScore)
                {
                    l.AddAfter(n, apparel);
                    return;
                }
            }

            l.AddLast(apparel);

            /*
#if TRACE
            Log.Message("Start StoredApparel.AddApparelToLinkedList");
            Log.Warning("Apparel: " + apparel.Label);
            StringBuilder sb = new StringBuilder("LinkedList: ");
            foreach (Apparel a in l)
            {
                sb.Append(a.LabelShort);
                sb.Append(", ");
            }
            Log.Warning(sb.ToString());
#endif
            QualityCategory q;
            if (!apparel.TryGetQuality(out q))
            {
#if TRACE
                Log.Message("AddLast - quality not found");
#endif
                l.AddLast(apparel);
            }
            else
            {
#if TRACE
                Log.Message("HP: " + apparel.HitPoints + " HPMax: " + apparel.MaxHitPoints);
#endif
                int hpPercent = apparel.HitPoints / apparel.MaxHitPoints;
                for (LinkedListNode<Apparel> n = l.First; n != null; n = n.Next)
                {
                    QualityCategory nq;
                    if (!n.Value.TryGetQuality(out nq) ||
                        q > nq ||
                        (q == nq && hpPercent >= (n.Value.HitPoints / n.Value.MaxHitPoints)))
                    {
                        l.AddBefore(n, apparel);
                        return;
                    }
                }
                l.AddLast(apparel);
            }
#if TRACE
            Log.Message("End StoredApparel.AddApparelToLinkedList");
#endif
            */
        }

        [Obsolete("No StoredApparel Allowed.", true)]
        internal int GetApparelCount(ThingDef expectedDef, QualityRange qualityRange, FloatRange hpRange,
            ThingFilter filter)
        {
            LinkedList<Apparel> l;
            if (this.StoredApparelLookup.TryGetValue(expectedDef, out l))
            {
                int count = 0;
                foreach (Apparel a in l)
                {
                    if (this.Allows(a, expectedDef, qualityRange, hpRange, filter))
                    {
                        ++count;
                    }
                }

                return count;
            }

            return 0;
        }

        [Obsolete("No StoredApparel Allowed.", true)]
        private bool Allows(Thing t, ThingDef expectedDef, QualityRange qualityRange, FloatRange hpRange,
            ThingFilter filter)
        {
#if DEBUG || DEBUG_DO_UNTIL_X
            Log.Warning("StoredApparel.Allows Begin [" + t.Label + "]");
#endif
            if (t.def != expectedDef)
            {
#if DEBUG || DEBUG_DO_UNTIL_X
                Log.Warning("StoredApparel.Allows End Def Does Not Match [False]");
#endif
                return false;
            }
#if DEBUG || DEBUG_DO_UNTIL_X
            Log.Message("    def uses HP: " + expectedDef.useHitPoints + " filter: " + hpRange.min + " " + hpRange.max);
#endif
            if (expectedDef.useHitPoints &&
                hpRange != null &&
                hpRange.min != 0f && hpRange.max != 100f)
            {
                float num = (float)t.HitPoints / (float)t.MaxHitPoints;
                num = GenMath.RoundedHundredth(num);
                if (!hpRange.IncludesEpsilon(Mathf.Clamp01(num)))
                {
#if DEBUG || DEBUG_DO_UNTIL_X
                    Log.Warning("StoredApparel.Allows End Hit Points [False - HP]");
#endif
                    return false;
                }
            }
#if DEBUG || DEBUG_DO_UNTIL_X
            Log.Message("    def follows quality: " + t.def.FollowQualityThingFilter() + " filter quality levels: " + qualityRange.min + " " + qualityRange.max);
#endif
            if (qualityRange != null && qualityRange != QualityRange.All && t.def.FollowQualityThingFilter())
            {
                QualityCategory p;
                if (!t.TryGetQuality(out p))
                {
                    p = QualityCategory.Normal;
                }

                if (!qualityRange.Includes(p))
                {
#if DEBUG || DEBUG_DO_UNTIL_X
                    Log.Warning("StoredApparel.Allows End Quality [False - Quality]");
#endif
                    return false;
                }
            }

            if (filter != null && !filter.Allows(t.Stuff))
            {
#if DEBUG || DEBUG_DO_UNTIL_X
                Log.Warning("StoredApparel.Allows End Quality [False - filters.Allows]");
#endif
                return false;
            }
#if DEBUG || DEBUG_DO_UNTIL_X
            Log.Warning("    StoredApparel.Allows End [True]");
#endif
            return true;
        }

        [Obsolete("No StoredApparel Allowed.", true)]
        public bool Contains(Apparel apparel)
        {
            LinkedList<Apparel> l;
            if (this.StoredApparelLookup.TryGetValue(apparel.def, out l))
            {
                return l.Contains(apparel);
            }

            return false;
        }

        [Obsolete("No StoredApparel Allowed.", true)]
        public void Clear()
        {
            foreach (LinkedList<Apparel> l in this.StoredApparelLookup.Values)
            {
                l.Clear();
            }

            this.StoredApparelLookup.Clear();
        }

        [Obsolete("No StoredApparel Allowed.", true)]
        public bool TryRemoveApparel(ThingDef def, out Apparel apparel)
        {
            LinkedList<Apparel> l;
            if (this.StoredApparelLookup.TryGetValue(def, out l))
            {
                if (l.Count > 0)
                {
                    apparel = l.First.Value;
                    l.RemoveFirst();
                    return true;
                }
            }

            apparel = null;
            return false;
        }

        [Obsolete("No StoredApparel Allowed.", true)]
        public bool TryRemoveBestApparel(ThingDef def, out Apparel apparel)
        {
            LinkedList<Apparel> l;
            if (this.StoredApparelLookup.TryGetValue(def, out l))
            {
                if (l.Count > 0)
                {
                    apparel = l.First.Value;
                    l.RemoveFirst();
                    return true;
                }
            }

            apparel = null;
            return false;
        }

        [Obsolete("No StoredApparel Allowed.", true)]
        public bool RemoveApparel(Apparel apparel)
        {
            LinkedList<Apparel> l;
            if (this.StoredApparelLookup.TryGetValue(apparel.def, out l))
            {
                return l.Remove(apparel);
            }

            return false;
        }

        [Obsolete("No StoredApparel Allowed.", true)]
        internal bool TryRemoveBestApparel(ThingDef def, Pawn pawn, out Apparel apparel)
        {
            ThingFilter filter = pawn.outfits.CurrentApparelPolicy.filter;
#if DEBUG
            Log.Message(Environment.NewLine + "Start StoredApparel.TryRemoveBestApperal Def: " + def.label);
#endif
            LinkedList<Apparel> l;
            if (this.StoredApparelLookup.TryGetValue(def, out l))
            {
#if DEBUG
                Log.Warning("Apparel List found Count: " + l.Count);
#endif
                LinkedListNode<Apparel> n = l.First;
                while (n != null)
                {
#if DEBUG
                    Log.Warning("Apparel " + n.Value.Label);
#endif
                    try
                    {
                        var next = n.Next;
                        if (filter.Allows(n.Value) && n.Value.PawnCanWear(pawn))
                        {
                            l.Remove(n);
                            apparel = n.Value;
#if DEBUG
                            Log.Warning("Start StoredApparel.TryRemoveBestApperal Return: True Apparel:" + apparel.LabelShort + Environment.NewLine);
#endif
                            return true;
                        }
#if DEBUG
                        else
                            Log.Warning("Filter rejected");
#endif
                        n = next;
                    }
                    catch
                    {
                        Log.Error("catch");
                    }
                }
            }

            apparel = null;
#if DEBUG
            Log.Message("End StoredApparel.TryRemoveBestApperal Return: False" + Environment.NewLine);
#endif
            return false;
        }

        [Obsolete("No StoredApparel Allowed.", true)]
        public List<Apparel> RemoveFilteredApparel(StorageSettings settings)
        {
            List<Apparel> removed = new List<Apparel>(0);
            foreach (LinkedList<Apparel> ll in this.StoredApparelLookup.Values)
            {
                LinkedListNode<Apparel> n = ll.First;
                while (n != null)
                {
                    var next = n.Next;
                    if (!settings.AllowedToAccept(n.Value))
                    {
                        ll.Remove(n);
                        removed.Add(n.Value);
                    }

                    n = n.Next;
                }
            }

            return removed;
        }

    }
}