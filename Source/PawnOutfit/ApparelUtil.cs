using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;
using System;

namespace ChangeDresser
{
    public static class ApparelUtil
    {
        public static List<Apparel> RemoveApparel(Pawn pawn)
        {
#if DRESSER_OUTFIT
            Log.Warning("Begin ApparelUtil.RemoveApparel(Pawn: " + pawn.Name.ToStringShort + ")");
            Log.Message("    Remove Apparel:");
#endif
            List<Apparel> wornApparel = new List<Apparel>(pawn.apparel.WornApparel);
            foreach (Apparel a in wornApparel)
            {
#if DRESSER_OUTFIT
                Log.Message("        " + a.Label);
#endif
                pawn.apparel.Remove(a);
            }

            pawn.outfits.forcedHandler.ForcedApparel.Clear();
#if DRESSER_OUTFIT
            Log.Warning("End ApparelUtil.RemoveApparel Removed Count: " + wornApparel.Count);
#endif
            return wornApparel;
        }

        [Obsolete("No Dresser", true)]
        public static void StoreApparelInWorldDresser(List<Apparel> apparel, Pawn pawn)
        {
#if DRESSER_OUTFIT
            Log.Warning("Begin ApparelUtil.StoreApparelInWorldDresser(Pawn: " + pawn.Name.ToStringShort + ")");
            Log.Message("    Store Apparel in World Dressers:");
#endif
            foreach (Apparel a in apparel)
            {
#if DRESSER_OUTFIT
                Log.Message("        " + a.Label);
#endif
                if (!WorldComp.AddApparel(a))
                {
#if DRESSER_OUTFIT
                    Log.Warning("            Unable to place apparel in dresser, dropping to floor");
#endif
                    BuildingUtil.DropThing(a, pawn.Position, pawn.Map, false);
                }
            }
#if DRESSER_OUTFIT
            Log.Warning("End ApparelUtil.StoreApparelInWorldDresser");
#endif
        }

        public static void StoreApparelInWorld(List<Apparel> apparel, Pawn pawn)
        {
            foreach (Apparel a in apparel)
            {
                if (!WorldComp.StoreApparel(a))
                {
                    BuildingUtil.DropThing(a, pawn.Position, pawn.Map, false);
                }
            }
        }

        public static void GetApparelsAfterNude(Pawn pawn)
        {
            if (pawn.outfits == null)
            {
                return;
            }

            if (pawn.Faction != Faction.OfPlayer)
            {
                return;
            }

            if (pawn.IsMutant && pawn.mutant.Def.disableApparel)
                return;
            if (pawn.IsQuestLodger())
                return;


            List<Thing> tmpApparelList = new List<Thing>();
            List<float> wornApparelScores = new List<float>();
            ApparelPolicy currentApparelPolicy = pawn.outfits.CurrentApparelPolicy;

            foreach (var map in Find.Maps)
            {
                map.listerThings.GetAllThings(in tmpApparelList, ThingRequestGroup.Apparel,
                    lookInHaulSources: true);
            }

            foreach (IThingHolder thingHolder in pawn.Map.haulDestinationManager.AllHaulSourcesListForReading)
            {
                foreach (Thing directlyHeldThing in (IEnumerable<Thing>)thingHolder.GetDirectlyHeldThings())
                {
                    if (directlyHeldThing is Apparel apparel)
                        tmpApparelList.Add((Thing)apparel);
                }
            }

            if (tmpApparelList.Count == 0)
            {
                return;
            }

            NeededWarmth neededWarmth =
                PawnApparelGenerator.CalculateNeededWarmth(pawn, pawn.Map.TileInfo.tile,
                    GenLocalDate.Twelfth((Thing)pawn));


            bool found;
            do
            {
                found = false;
                for (int index = 0; index < tmpApparelList.Count; ++index)
                {
                    Apparel tmpApparel = (Apparel)tmpApparelList[index];
                    if (currentApparelPolicy.filter.Allows((Thing)tmpApparel) && tmpApparel.IsInAnyStorage() &&
                        !tmpApparel.IsForbidden(pawn) && !tmpApparel.IsBurning() &&
                        (tmpApparel.def.apparel.gender == Gender.None || tmpApparel.def.apparel.gender == pawn.gender))
                    {
                        float num2 = JobGiver_OptimizeApparel.ApparelScoreGain(pawn, tmpApparel, wornApparelScores);

                        if ((double)num2 >= 0.05000000074505806 &&
                            (!CompBiocodable.IsBiocoded((Thing)tmpApparel) ||
                             CompBiocodable.IsBiocodedFor((Thing)tmpApparel, pawn)) &&
                            ApparelUtility.HasPartsToWear(pawn, tmpApparel.def))
                        {
                            LocalTargetInfo target = (LocalTargetInfo)(Thing)tmpApparel;
                            if (tmpApparel.ParentHolder is IApparelSource parentHolder && parentHolder is Thing thing)
                            {
                                if (!parentHolder.ApparelSourceEnabled)
                                    continue;
                                target = (LocalTargetInfo)thing;
                            }

                            if (tmpApparel.def.apparel.developmentalStageFilter.Has(pawn.DevelopmentalStage))
                            {
                                pawn.apparel.Wear(tmpApparel, dropReplacedApparel: true);
                                wornApparelScores.Add(JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, tmpApparel));
                                found = true;
                                break; // restart loop from beginning
                            }
                        }
                    }
                }
            } while (found);

            // original code
            // for (int index = 0; index < tmpApparelList.Count; ++index)
            // {
            //     Apparel tmpApparel = (Apparel)tmpApparelList[index];
            //     if (currentApparelPolicy.filter.Allows((Thing)tmpApparel) && tmpApparel.IsInAnyStorage() &&
            //         !tmpApparel.IsForbidden(pawn) && !tmpApparel.IsBurning() &&
            //         (tmpApparel.def.apparel.gender == Gender.None || tmpApparel.def.apparel.gender == pawn.gender))
            //     {
            //         float num2 = JobGiver_OptimizeApparel.ApparelScoreGain(pawn, tmpApparel,
            //             wornApparelScores);
            //
            //         if ((double)num2 >= 0.05000000074505806 && (double)num2 >= (double)num1 &&
            //             (!CompBiocodable.IsBiocoded((Thing)tmpApparel) ||
            //              CompBiocodable.IsBiocodedFor((Thing)tmpApparel, pawn)) &&
            //             ApparelUtility.HasPartsToWear(pawn, tmpApparel.def))
            //         {
            //             LocalTargetInfo target = (LocalTargetInfo)(Thing)tmpApparel;
            //             if (tmpApparel.ParentHolder is IApparelSource parentHolder && parentHolder is Thing thing)
            //             {
            //                 if (parentHolder.ApparelSourceEnabled)
            //                     target = (LocalTargetInfo)thing;
            //                 else
            //                     continue;
            //             }
            //
            //             if (pawn.CanReserveAndReach(target, PathEndMode.OnCell, pawn.NormalMaxDanger()) &&
            //                 tmpApparel.def.apparel.developmentalStageFilter.Has(pawn.DevelopmentalStage))
            //             {
            //                 targetA = (Thing)tmpApparel;
            //                 num1 = num2;
            //             }
            //         }
            //     }
            // }

            tmpApparelList.Clear();
            wornApparelScores.Clear();
        }


        public static bool FindBetterApparel(
            ref float baseApparelScore, ref Apparel betterApparel, Pawn pawn, ApparelPolicy currentOutfit,
            IEnumerable<Apparel> apparelToCheck, Building dresser)
        {
            if (betterApparel == null)
                baseApparelScore = 0f;
#if BETTER_OUTFIT
            Log.Warning("Begin ApparelUtil.FindBetterApparel(Score: " + baseApparelScore + "    Apparel: " + ((betterApparel == null) ? "<null>" : betterApparel.Label));
#endif
            bool result = false;
#if TRACE && BETTER_OUTFIT
            Log.Message("    Apparel:");
#endif
            foreach (Apparel apparel in apparelToCheck)
            {
#if TRACE && BETTER_OUTFIT
                Log.Message("        " + ((apparel == null) ? "<null>" : apparel.Label));
#endif
                if (!currentOutfit.filter.Allows(apparel.def))
                {
#if TRACE && BETTER_OUTFIT
                    Log.Message("        Filters does not allow");
#endif
                    break;
                }

                if (!currentOutfit.filter.Allows(apparel) ||
                    apparel.IsForbidden(pawn))
                {
#if TRACE && BETTER_OUTFIT
                    Log.Message("        Current Outfit Does Not Allow: " + !currentOutfit.filter.Allows(apparel) + "    or     Is Forbidden: " + apparel.IsForbidden(pawn));
#endif
                    continue;
                }

#if TRACE && BETTER_OUTFIT
                Log.Message("        Keep Forced Apparel: " + Settings.KeepForcedApparel);
#endif
                List<Apparel> wornApparel = pawn.apparel.WornApparel;
                List<float> cachedValues = new List<float>(wornApparel.Count);
                if (Settings.KeepForcedApparel)
                {
                    bool skipApparelType = false;
                    foreach (Apparel a in wornApparel)
                    {
                        cachedValues.Add(JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, a));
                        try
                        {
                            if (!pawn.outfits.forcedHandler.IsForced(a) &&
                                !ApparelUtility.CanWearTogether(a.def, apparel.def, pawn.RaceProps.body))
                            {
#if TRACE && BETTER_OUTFIT
                            Log.Message("        Cannot wear together");
#endif
                                skipApparelType = true;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                Log.Warning("Problem when calling CanWearTogether (" + a?.Label + ", " +
                                            apparel?.Label + ", " + pawn?.RaceProps?.body?.label + ") - " +
                                            e.GetType().Name + " " + e.Message);
                            }
                            catch
                            {
                            }

                            skipApparelType = true;
                            break;
                        }
                    }

                    if (skipApparelType)
                    {
                        break;
                    }
                }

                /*else
                {
                    foreach (Apparel a in wornApparel)
                    {
                        cachedValues.Add(JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, a));
                    }
                }*/
                float gain = JobGiver_OptimizeApparel.ApparelScoreGain(pawn, apparel, cachedValues);
#if TRACE && BETTER_OUTFIT
                Log.Message("    Gain: " + gain + "     Base Score: " + baseApparelScore);
#endif
                if (gain >= 0.05f && gain > baseApparelScore)
                {
#if TRACE && BETTER_OUTFIT
                    Log.Message("    Gain is better");
#endif
                    if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
                    {
#if TRACE && BETTER_OUTFIT
                        Log.Message("    Has parts to wear");
#endif
                        if (dresser == null ||
                            ReservationUtility.CanReserveAndReach(pawn, dresser, PathEndMode.OnCell,
                                pawn.NormalMaxDanger(), 1))
                        {
#if TRACE && BETTER_OUTFIT
                            Log.Message("    Can reach dresser");
#endif
                            betterApparel = apparel;
                            baseApparelScore = gain;
                            result = true;
                        }
                    }
                }
            }
#if BETTER_OUTFIT
            Log.Warning("End ApparelUtil.FindBetterApparel    result = " + result);
#endif
            return result;
        }
        /*
        public static bool TryFindBestApparel(Pawn pawn, out Apparel a, out Building_Dresser dresser)
        {
#if BETTER_OUTFIT
            Log.Warning("Begin WorldComp.TryFindBestApparel(Pawn: " + pawn.Name.ToStringShort);
#endif
            a = null;
            dresser = null;
            float baseApparelScore = 0;

            PawnOutfitTracker po;
            if (PawnOutfits.TryGetValue(pawn, out po))
            {
                ApparelUtil.FindBetterApparel(ref baseApparelScore, ref a, pawn, pawn.outfits.CurrentApparelPolicy, po.CustomApparel, null);
#if BETTER_OUTFIT
                Log.Warning("    CustomApparel Result: " + ((a == null) ? "<null>" : a.Label) + "    Score: " + baseApparelScore);
#endif
            }

            foreach (Building_Dresser d in DressersToUse)
            {
                if (d.FindBetterApparel(ref baseApparelScore, ref a, pawn, pawn.outfits.CurrentApparelPolicy))
                {
                    dresser = d;
                }
            }
#if BETTER_OUTFIT
            Log.Warning("    Dresser Result: " + ((a == null) ? "<null>" : a.Label) + "    Score: " + baseApparelScore);
#endif

#if BETTER_OUTFIT
            Log.Warning("Begin WorldComp.TryFindBestApparel -- " + ((a == null) ? "<null>" : a.Label));
#endif
            return a != null;
        }*/
    }
}