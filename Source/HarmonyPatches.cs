using ChangeDresser.UI;
using ChangeDresser.UI.Util;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ChangeDresser
{
    [StaticConstructorOnStartup]
    partial class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.changedresser.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static Texture2D GetIcon(ThingDef td)
        {
            Texture2D tex = null;
            if (td.uiIcon != null)
            {
                tex = td.uiIcon;
            }
            else if (td != null && td.graphicData != null && td.graphicData.texPath != null)
            {
                tex = ContentFinder<Texture2D>.Get(td.graphicData.texPath, true);
            }
            else
            {
                tex = null;
            }

            if (tex == null)
            {
                tex = WidgetUtil.noneTexture;
            }

            return tex;
        }
    }
    
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    static class Patch_Pawn_GetGizmos
    {
#if DEBUG
        private static int i = 0;
        private static readonly int WAIT = 1000;
#endif
        static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            List<Gizmo> l = new List<Gizmo>();
            if ((__instance.IsPrisoner || (Settings.ShowDresserButtonForPawns &&
                                           __instance.Faction == Faction.OfPlayer && __instance.def.race.Humanlike)) &&
                WorldComp.HasDressers())
            {
                l.Add(new Command_Action
                {
                    icon = WidgetUtil.yesDressFromTexture,
                    defaultLabel = "ChangeDresser.UseDresser".Translate(),
                    activateSound = SoundDef.Named("Click"),
                    action = delegate
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>(6)
                        {
                            new FloatMenuOption("ChangeDresser.Wearing".Translate(),
                                delegate() { Find.WindowStack.Add(new StorageUI(__instance)); }),
                        };
                        
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                });
                l.AddRange(__result);
            }

            if (!__instance.Drafted && __instance.Faction == Faction.OfPlayer && WorldComp.HasDressers())
            {
#if DEBUG
                ++i;
                if (i == WAIT)
                    Log.Warning("DraftController.Postfix: Pawn is Drafted");
#endif
                if (WorldComp.PawnOutfits.TryGetValue(__instance, out PawnOutfitTracker outfits))
                {
                    if (l.Count == 0)
                        l.AddRange(__result);
#if DEBUG
                    if (i == WAIT)
                        Log.Warning("DraftController.Postfix: Sets found! Pre Gizmo Count: " + l.Count);
#endif
                    foreach (IDresserOutfit o in outfits.CivilianOutfits)
                    {
                        if (o == null || !o.IsValid())
                            continue;
#if DEBUG && DRESSER_OUTFIT
                        string msg = "Patch_Pawn_GetGizmos Outfit: " + o.Label;
                        Log.ErrorOnce(msg, msg.GetHashCode());
#endif
                        Command_Action a = new Command_Action();
                        ThingDef icon = o.Icon;
                        if (icon != null)
                        {
                            a.icon = HarmonyPatches.GetIcon(icon);
                        }
                        else
                        {
                            a.icon = WidgetUtil.noneTexture;
                        }

                        StringBuilder sb = new StringBuilder();
                        if (!o.IsBeingWorn)
                        {
                            sb.Append("ChangeDresser.ChangeTo".Translate());
                            a.defaultDesc = "ChangeDresser.ChangeToDesc".Translate();
                        }
                        else
                        {
                            sb.Append("ChangeDresser.Wearing".Translate());
                            a.defaultDesc = "ChangeDresser.WearingDesc".Translate();
                        }

                        sb.Append(" ");
                        sb.Append(o.Label);
                        a.defaultLabel = sb.ToString();
                        a.activateSound = SoundDef.Named("Click");
                        a.action = delegate
                        {
#if DRESSER_OUTFIT
                            Log.Warning("Patch_Pawn_GetGizmos click for " + o.Label);
#endif
                            outfits.ChangeTo(o);
                            //HarmonyPatches.SwapApparel(pawn, o);
                            //outfits.ColorApparel(__instance);
                        };
                        l.Add(a);
                    }
#if DEBUG
                    if (i == WAIT)
                        Log.Warning("Post Gizmo Count: " + l.Count);
#endif
                }
            }
#if DEBUG
            else
            {
                if (i == WAIT)
                    Log.Warning("Pawn is not Drafted, could gizmo");
            }
#endif
#if DEBUG
            if (i == WAIT)
                i = 0;
#endif
            if (l.Count > 0)
                __result = l;
        }
    }

    [HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos")]
    static class Patch_Pawn_DraftController_GetGizmos
    {
#if DEBUG
        private static int i = 0;
        private static readonly int WAIT = 1000;
#endif
        static void Postfix(Pawn_DraftController __instance, ref IEnumerable<Gizmo> __result)
        {
            Pawn pawn = __instance.pawn;
            if (pawn.Drafted && WorldComp.HasDressers())
            {
#if DEBUG
                ++i;
                if (i == WAIT)
                    Log.Warning("DraftController.Postfix: Pawn is Drafted");
#endif
                if (WorldComp.PawnOutfits.TryGetValue(pawn, out PawnOutfitTracker outfits))
                {
                    List<Gizmo> l = new List<Gizmo>(__result);
#if DEBUG
                    if (i == WAIT)
                        Log.Warning("DraftController.Postfix: Sets found! Pre Gizmo Count: " + l.Count);
#endif
                    foreach (IDresserOutfit o in outfits.BattleOutfits)
                    {
                        if (o == null || !o.IsValid())
                            continue;
#if DEBUG && DRESSER_OUTFIT
                        string msg = "Patch_Pawn_DraftController_GetGizmos Outfit: " + o.Label;
                        Log.ErrorOnce(msg, msg.GetHashCode());
#endif
#if DEBUG
                        if (i == WAIT)
                            Log.Warning("DraftController.Postfix: Set: " + o.Label + ", Current Oufit: " + pawn.outfits.CurrentApparelPolicy.label);
#endif
                        Command_Action a = new Command_Action();
                        ThingDef icon = o.Icon;
                        if (icon != null)
                        {
                            a.icon = HarmonyPatches.GetIcon(icon);
                        }
                        else
                        {
                            a.icon = WidgetUtil.noneTexture;
                        }

                        StringBuilder sb = new StringBuilder();
                        if (!pawn.outfits.CurrentApparelPolicy.Equals(o))
                        {
                            sb.Append("ChangeDresser.ChangeTo".Translate());
                            a.defaultDesc = "ChangeDresser.ChangeToDesc".Translate();
                        }
                        else
                        {
                            sb.Append("ChangeDresser.Wearing".Translate());
                            a.defaultDesc = "ChangeDresser.WearingDesc".Translate();
                        }

                        sb.Append(" ");
                        sb.Append(o.Label);
                        a.defaultLabel = sb.ToString();
                        a.activateSound = SoundDef.Named("Click");
                        a.action = delegate
                        {
#if DRESSER_OUTFIT
                                Log.Warning("Patch_Pawn_DraftController_GetGizmos click for " + o.Label);
#endif
                            outfits.ChangeTo(o);
                            //HarmonyPatches.SwapApparel(pawn, o);
                            //outfits.ColorApparel(pawn);
                        };
                        l.Add(a);
                    }
#if DEBUG
                    if (i == WAIT)
                        Log.Warning("Post Gizmo Count: " + l.Count);
#endif
                    __result = l;
                }
            }
#if DEBUG
            else
            {
                if (i == WAIT)
                    Log.Warning("Pawn is not Drafted, could gizmo");
            }
#endif
#if DEBUG
            if (i == WAIT)
                i = 0;
#endif
        }
    }

    [HarmonyPatch(typeof(Pawn_DraftController), "set_Drafted")]
    static class Patch_Pawn_DraftController
    {
        static void Postfix(Pawn_DraftController __instance)
        {
            if (WorldComp.HasDressers())
            {
                Pawn pawn = __instance.pawn;
                if (pawn.Downed)
                {
                    return;
                }
                if (WorldComp.PawnOutfits.TryGetValue(pawn, out PawnOutfitTracker outfits))
                {
                    if (pawn.Drafted)
                    {
                        outfits.ChangeToBattleOutfit();
                    }
                    else
                    {
                        outfits.ChangeToCivilianOutfit();
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(ReservationManager), "CanReserve")]
    static class Patch_ReservationManager_CanReserve
    {
        private static FieldInfo mapFI = null;

        static void Postfix(ref bool __result, ReservationManager __instance, Pawn claimant, LocalTargetInfo target,
            int maxPawns, int stackCount, ReservationLayerDef layer, bool ignoreOtherReservations)
        {
            if (mapFI == null)
            {
                mapFI = typeof(ReservationManager).GetField("map", BindingFlags.NonPublic | BindingFlags.Instance);
            }

#if DEBUG
            Log.Warning("\nCanReserve original result: " + __result);
#endif
            if (!__result && mapFI != null &&
                (target.Thing == null || target.Thing.def.defName.Equals("ChangeDresser")))
            {
                Map m = (Map)mapFI.GetValue(__instance);
                if (m != null)
                {
                    IEnumerable<Thing> things = m.thingGrid.ThingsAt(target.Cell);
                    if (things != null)
                    {
#if DEBUG
                    Log.Warning("CanReserve - Found things");
#endif
                        foreach (Thing t in things)
                        {
#if DEBUG
                        Log.Warning("CanReserve - def " + t.def.defName);
#endif
                            if (t.def.defName.Equals("ChangeDresser"))
                            {
#if DEBUG
                            Log.Warning("CanReserve is now true\n");
#endif
                                __result = true;
                            }
                        }
                    }
                }
            }
        }
    }

    #region Caravan Forming

    [HarmonyPatch(typeof(Dialog_FormCaravan), "PostOpen")]
    static class Patch_Dialog_FormCaravan_PostOpen
    {
        static void Prefix(Window __instance)
        {
            Type type = __instance.GetType();
            if (type == typeof(Dialog_FormCaravan))
            {
                Map map = __instance.GetType().GetField("map", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(__instance) as Map;

                foreach (Building_Dresser d in WorldComp.GetDressers(map))
                {
                    d.Empty<Thing>();
                }
            }
        }
    }

    [HarmonyPatch(typeof(CaravanFormingUtility), "StopFormingCaravan")]
    static class Patch_CaravanFormingUtility_StopFormingCaravan
    {
        [HarmonyPriority(Priority.First)]
        static void Postfix(Lord lord)
        {
            foreach (Building_Dresser d in WorldComp.DressersToUse)
            {
                d.ReclaimApparel();
            }
        }
    }

    [HarmonyPatch(
        typeof(CaravanExitMapUtility), "ExitMapAndCreateCaravan",
        new Type[] { typeof(IEnumerable<Pawn>), typeof(Faction), typeof(PlanetTile), typeof(PlanetTile), typeof(PlanetTile), typeof(bool) })]
    static class Patch_CaravanExitMapUtility_ExitMapAndCreateCaravan
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(IEnumerable<Pawn> pawns, Faction faction, PlanetTile exitFromTile, PlanetTile directionTile,
            PlanetTile destinationTile, bool sendMessage)
        {
            if (faction == Faction.OfPlayer)
            {
                List<Pawn> p = new List<Pawn>(pawns);
                if (p.Count > 0)
                {
                    foreach (Building_Dresser d in WorldComp.DressersToUse)
                    {
                        d.ReclaimApparel();
                    }
                }
            }
        }
    }

    /*[HarmonyPatch(
        typeof(CaravanExitMapUtility), "ExitMapAndCreateCaravan",
        new Type[] { typeof(IEnumerable<Pawn>), typeof(Faction), typeof(int) })]
    static class Patch_CaravanExitMapUtility_ExitMapAndCreateCaravan_2
    {
        static void Prefix(IEnumerable<Pawn> pawns, Faction faction, int startingTile)
        {
            if (faction == Faction.OfPlayer)
            {
                List<Pawn> p = new List<Pawn>(pawns);
                if (p.Count > 0)
                {
                    foreach (Building_Dresser d in WorldComp.DressersToUse)
                    {
                        d.ReclaimApparel();
                    }
                }
            }
        }
    }*/

    #endregion

    #region Handle "Do until X" for stored weapons

    [HarmonyPatch(typeof(RecipeWorkerCounter), "CountProducts")]
    static class Patch_RecipeWorkerCounter_CountProducts
    {
        static void Postfix(ref int __result, RecipeWorkerCounter __instance, Bill_Production bill)
        {
            List<ThingDefCountClass> products = __instance.recipe.products;
            if (WorldComp.DressersToUse.Count > 0 && products != null)
            {
                foreach (ThingDefCountClass product in products)
                {
                    ThingDef def = product.thingDef;
                    foreach (Building_Dresser d in WorldComp.DressersToUse)
                    {
                        if (bill.Map == d.Map)
                        {
                            __result += d.GetApparelCount(def, bill.qualityRange, bill.hpRange,
                                (bill.limitToAllowedStuff) ? bill.ingredientFilter : null);
                        }
                    }
                }
            }
        }
    }

    #endregion

    // #region Pawn Death
    //
    // [HarmonyPatch(typeof(Pawn), "Kill")]
    // static class Patch_Pawn_Kill
    // {
    //     private static Map map;
    //
    //     [HarmonyPriority(Priority.First)]
    //     static void Prefix(Pawn __instance)
    //     {
    //         map = __instance.Map;
    //     }
    //
    //     [HarmonyPriority(Priority.First)]
    //     static void Postfix(Pawn __instance)
    //     {
    //         // if (__instance.Dead && __instance.apparel?.LockedApparel?.Count == 0)
    //         // {
    //         //     if (WorldComp.PawnOutfits.TryGetValue(__instance, out PawnOutfitTracker po))
    //         //     {
    //         //         WorldComp.PawnOutfits.Remove(__instance);
    //         //
    //         //         foreach (Apparel a in po.CustomApparel)
    //         //         {
    //         //             if (!WorldComp.AddApparel(a))
    //         //             {
    //         //                 BuildingUtil.DropThing(a, __instance.Position, map, true);
    //         //             }
    //         //         }
    //         //     }
    //         // }
    //     }
    // }
    //
    // #endregion

    #region Pawn Destroy

    [HarmonyPatch(typeof(Pawn), "Destroy")]
    static class Patch_Pawn_Destroy
    {
        private static Map map;

        [HarmonyPriority(Priority.First)]
        static void Prefix(Pawn __instance)
        {
            map = __instance.Map;
        }

        [HarmonyPriority(Priority.First)]
        static void Postfix(Pawn __instance, DestroyMode mode)
        {
            if (mode == DestroyMode.Vanish && WorldComp.PawnOutfits.TryGetValue(__instance, out PawnOutfitTracker po))
            {
                WorldComp.PawnOutfits.Remove(__instance);

                foreach (Apparel a in po.CustomApparel)
                {
                    if (!WorldComp.StoreApparel(a))
                    {
                        BuildingUtil.DropThing(a, __instance.Position, map, true);
                    }
                }
            }
        }
    }

    #endregion
    
    // #region Corpse Destroy
    //
    // [HarmonyPatch(typeof(Corpse), "Destroy")]
    // static class Patch_Corpse_Destroy
    // {
    //     private static Map map;
    //
    //     [HarmonyPriority(Priority.First)]
    //     static void Prefix(Corpse __instance)
    //     {
    //         map = __instance.Map;
    //     }
    //
    //     [HarmonyPriority(Priority.First)]
    //     static void Postfix(Corpse __instance, DestroyMode mode)
    //     {
    //         if (mode == DestroyMode.Vanish && WorldComp.PawnOutfits.TryGetValue(__instance.InnerPawn, out PawnOutfitTracker po))
    //         {
    //             WorldComp.PawnOutfits.Remove(__instance.InnerPawn);
    //
    //             foreach (Apparel a in po.CustomApparel)
    //             {
    //                 if (!WorldComp.AddApparel(a))
    //                 {
    //                     BuildingUtil.DropThing(a, __instance.Position, map, true);
    //                 }
    //             }
    //         }
    //     }
    // }
    // #endregion

    
    // #region Corpse Kill
    //
    // [HarmonyPatch(typeof(Corpse), "Kill")]
    // static class Patch_Corpse_Kill
    // {
    //     private static Map map;
    //
    //     [HarmonyPriority(Priority.First)]
    //     static void Prefix(Corpse __instance)
    //     {
    //         map = __instance.Map;
    //     }
    //
    //     [HarmonyPriority(Priority.First)]
    //     static void Postfix(Corpse __instance)
    //     {
    //         if (WorldComp.PawnOutfits.TryGetValue(__instance.InnerPawn, out PawnOutfitTracker po))
    //         {
    //             WorldComp.PawnOutfits.Remove(__instance.InnerPawn);
    //
    //             foreach (Apparel a in po.CustomApparel)
    //             {
    //                 if (!WorldComp.AddApparel(a))
    //                 {
    //                     BuildingUtil.DropThing(a, __instance.Position, map, true);
    //                 }
    //             }
    //         }
    //     }
    // }
    //
    // #endregion

    [HarmonyPatch(typeof(OutfitDatabase), "TryDelete")]
    static class Patch_OutfitDatabase_TryDelete
    {
        static void Postfix(AcceptanceReport __result, ApparelPolicy apparelPolicy)
        {
            if (__result.Accepted)
            {
                WorldComp.OutfitsForBattle.Remove(apparelPolicy);
                foreach (PawnOutfitTracker po in WorldComp.PawnOutfits.Values)
                {
                    po.Remove(apparelPolicy);
                }
            }
        }
    }

    [HarmonyPatch(typeof(ScribeSaver), "InitSaving")]
    static class Patch_ScribeSaver_InitSaving
    {
        static void Prefix()
        {
            try
            {
                foreach (Building_Dresser d in WorldComp.GetDressers(null))
                {
                    try
                    {
                        d.ReclaimApparel(true);
                    }
                    catch (Exception e)
                    {
                        Log.Warning("Error while reclaiming apparel for change dresser\n" + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("Error while reclaiming apparel\n" + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(SettlementAbandonUtility), "Abandon")]
    static class Patch_SettlementAbandonUtility_Abandon
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(MapParent settlement)
        {
            WorldComp.RemoveDressers(settlement.Map);
        }
    }

    [HarmonyPatch(typeof(Caravan), "AddPawn")]
    static class Patch_Caravan_AddPawn
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(Pawn p, bool addCarriedPawnToWorldPawnsIfAny)
        {
            try
            {
                if (p != null && p.Drafted)
                    p.drafter.Drafted = false;
            }
            catch (Exception e)
            {
                Log.Error("Exception thrown from ChangeDresser Patch_Caravan_AddPawn - " + e.GetType().Name + " " +
                          e.Message);
            }
        }
    }
    
    
    
    // [HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.CanWearTogether))]
    // static class Patch_ApparelUtility_CanWearTogether
    // {
    //     [HarmonyPriority(Priority.First)]
    //     static void Prefix(ref ThingDef A, ref ThingDef B, ref BodyDef body)
    //     {
    //         if (A == null) {
    //             Log.Warning("Argument 'A' is null.");
    //         }
    //
    //
    //         if (B == null) {
    //             Log.Error("Argument 'B' is null.");
    //         }
    //
    //         if (body == null) {
    //             Log.Error("Argument 'body' is null.");
    //         }
    //
    //     }
    // }
    /*
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), "ApparelScoreRaw")]
    static class Patch_JobGiver_OptimizeApparel_ApparelScoreRaw
    {
        static SimpleCurve HitPointsPercentScoreFactorCurve = null;
        static SimpleCurve InsulationColdScoreFactorCurve_NeedWarm = null;
        static FieldInfo NeedWarmthFI = null;

        [HarmonyPriority(Priority.First)]
        static bool Prefix(ref float __result, JobGiver_OptimizeApparel __instance, Pawn pawn, Apparel ap)
        {
            Log.Message("1 pawn is " + ((pawn == null) ? "null" : "not null"));

            if (pawn != null)
                return true;
            Log.Message("2");

            if (HitPointsPercentScoreFactorCurve == null)
            {
                HitPointsPercentScoreFactorCurve = typeof(JobGiver_OptimizeApparel).GetField("HitPointsPercentScoreFactorCurve", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as SimpleCurve;
                InsulationColdScoreFactorCurve_NeedWarm = typeof(JobGiver_OptimizeApparel).GetField("InsulationColdScoreFactorCurve_NeedWarm", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as SimpleCurve;
                NeedWarmthFI = typeof(JobGiver_OptimizeApparel).GetField("neededWarmth", BindingFlags.Static | BindingFlags.NonPublic);
            }
            Log.Message("HitPointsPercentScoreFactorCurve is " + ((HitPointsPercentScoreFactorCurve == null) ? "null" : "not null"));
            Log.Message("InsulationColdScoreFactorCurve_NeedWarm is " + ((InsulationColdScoreFactorCurve_NeedWarm == null) ? "null" : "not null"));
            Log.Message("NeedWarmthFI is " + ((NeedWarmthFI == null) ? "null" : "not null"));
            Log.Message("NeedWarmth is " + NeedWarmthFI.GetValue(null));

            float result = 0.1f + ap.GetStatValue(StatDefOf.ArmorRating_Sharp) + ap.GetStatValue(StatDefOf.ArmorRating_Blunt);
            if (ap.def.useHitPoints)
            {
                float x = (float)ap.HitPoints / (float)ap.MaxHitPoints;
                result *= HitPointsPercentScoreFactorCurve.Evaluate(x);
            }
            result += ap.GetSpecialApparelScoreOffset();
            float num3 = 1f;
            if ((NeededWarmth)NeedWarmthFI.GetValue(null) == NeededWarmth.Warm)
            {
                float statValue = ap.GetStatValue(StatDefOf.Insulation_Cold);
                num3 *= InsulationColdScoreFactorCurve_NeedWarm.Evaluate(statValue);
            }
            result *= num3;
            if (ap.WornByCorpse)
            {
                result -= 0.5f;
                if (result > 0f)
                {
                    result *= 0.1f;
                }
            }
            if (ap.Stuff == ThingDefOf.Human.race.leatherDef)
            {
                result -= 0.5f;
                if (result > 0f)
                {
                    result *= 0.1f;
                }
            }
            __result = result;
            return false;
        }
    }*/
}