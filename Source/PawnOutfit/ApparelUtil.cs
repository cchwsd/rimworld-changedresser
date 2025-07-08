using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;
using System;
using RimWorld.Planet;

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
        
        // rewrite JobGiver_OptimizeApparel.ApparelScoreGain(pawn, tmpApparel, wornApparelScores)
        public static float ApparelScoreGainAvoidingAutomaticallyDrop(Pawn pawn, Apparel ap, List<float> wornScoresCache)
        {
            if (ap.def == ThingDefOf.Apparel_ShieldBelt && pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsWeaponUsingProjectiles || ap.def.apparel.ignoredByNonViolent && pawn.WorkTagIsDisabled(WorkTags.Violent))
                return -1000f;
            float num = JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, ap);
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int index = 0; index < wornApparel.Count; ++index)
            {
                if (!ApparelUtility.CanWearTogether(wornApparel[index].def, ap.def, pawn.RaceProps.body))
                {
                    return -1000f;
                }
            }
            return num;
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
            
            var ocuppiedApparels = new HashSet<Apparel>();
            foreach (var tracker in WorldComp.PawnOutfits.Values)
            {
                foreach (var outfit in tracker.CustomOutfits)
                {
                    foreach (var used in outfit.Apparel)
                    {
                        ocuppiedApparels.Add(used);
                    }
                }
            }

            if (tmpApparelList.Count == 0)
            {
                return;
            }

            // NeededWarmth neededWarmth =
            //     PawnApparelGenerator.CalculateNeededWarmth(pawn, pawn.Map.TileInfo.tile,
            //         GenLocalDate.Twelfth((Thing)pawn));
            
            bool found;
            var trails = 0;
            do
            {
                Apparel topApparel = (Apparel)null;
                float topScoreGain = 0.0f;
                
                found = false;
                for (int index = 0; index < tmpApparelList.Count; ++index)
                {
                    Apparel tmpApparel = (Apparel)tmpApparelList[index];
                    if (currentApparelPolicy.filter.Allows((Thing)tmpApparel) && tmpApparel.IsInAnyStorage() &&
                        !tmpApparel.IsForbidden(pawn) && !tmpApparel.IsBurning() && !ocuppiedApparels.Contains(tmpApparel) &&
                        (tmpApparel.def.apparel.gender == Gender.None || tmpApparel.def.apparel.gender == pawn.gender))
                    {
                        float scoreGain = ApparelScoreGainAvoidingAutomaticallyDrop(pawn, tmpApparel, wornApparelScores);
                        if (((double)scoreGain >= topScoreGain &&
                             (!CompBiocodable.IsBiocoded((Thing)tmpApparel) ||
                              CompBiocodable.IsBiocodedFor((Thing)tmpApparel, pawn)) &&
                             ApparelUtility.HasPartsToWear(pawn, tmpApparel.def)) &&
                            tmpApparel.def.apparel.developmentalStageFilter.Has(pawn.DevelopmentalStage))
                        {
                            topApparel = tmpApparel;
                            topScoreGain = scoreGain;
                        }
                        if (topApparel != null)
                        {
                        }
                    }
                }

                if (topApparel != null)
                {
                    WorldComp.ApparelMapTracker.AddApparel(topApparel);
                    pawn.apparel.Wear(topApparel, dropReplacedApparel: true);
                    wornApparelScores.Add(JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, topApparel));
                    tmpApparelList.Remove(topApparel);
                    found = true;
                }
                
                trails++;
                if (trails >= 30)
                {
                    break;
                }

            } while (found);
            
            tmpApparelList.Clear();
            wornApparelScores.Clear();
        }
        
    }
}