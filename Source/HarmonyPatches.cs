using ChangeDresser.UI;
using ChangeDresser.UI.Util;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

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
    
    [HarmonyPatch(typeof(Thing), "get_Label")]
    internal static class Apparel_Label_Patch
    {
        static void Postfix(Thing __instance, ref string __result)
        {
            if (WorldComp.CachedCustomApparel.Contains(__instance))
            {
                foreach (var pawnOutfitTracker in WorldComp.PawnOutfits.Values)
                {
                    foreach (var customOutfit in pawnOutfitTracker.CustomOutfits)
                    {
                        if (customOutfit.Apparel.Contains(__instance))
                        {
                            __result = $"[{pawnOutfitTracker.Pawn.NameShortColored}] " + __result;
                        }
                    }
                }
            }
                
        }
    }


    [HarmonyPatch(typeof(Thing), "DrawGUIOverlay")]
    internal static class Thing_DrawGUIOverlay_Patch
    {
        private static bool Prefix(Thing __instance)
        {
            if (__instance.StoringThing() is Building_Dresser dresser && __instance != dresser)
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch]
    [HarmonyPriority(600)]
    public static class HideStoredThingsFromSectionLayerAndOverlayDrawer
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(SectionLayer_ThingsGeneral), "TakePrintFrom");
            yield return AccessTools.Method(typeof(OverlayDrawer), "RenderForbiddenOverlay");
        }

        [HarmonyPrefix]
        public static bool Prefix(Thing t)
        {
            if (t.StoringThing() is Building_Dresser dresser && t != dresser)
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ThingSelectionUtility), "MultiSelectableThingsInScreenRectDistinct")]
    public static class PreventSelectionInRect
    {
        [HarmonyPostfix]
        public static void Postfix(ref IEnumerable<Thing> __result)
        {
            Event current = Event.current;
            if (current.rawType != EventType.MouseUp || current.button != 0)
                return;

            __result = __result.Where(HideStoredThingsFromSectionLayerAndOverlayDrawer.Prefix);
        }
    }

    [HarmonyPatch(typeof(Selector), "SelectableObjectsUnderMouse")]
    public static class PreventSelectionUnderMouse
    {
        [HarmonyPostfix]
        public static IEnumerable<object> Postfix(IEnumerable<object> __result)
        {
            foreach (object obj in __result)
            {
                if (!(obj is Thing t) || HideStoredThingsFromSectionLayerAndOverlayDrawer.Prefix(t))
                    yield return obj;
            }
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


    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), "ApparelScoreGain")]
    static class Patch_JobGiver_OptimizeApparel_ApparelScoreGain
    {
        static bool Prefix(Pawn pawn, Apparel ap, List<float> wornScoresCache, ref float __result)
        {
            if (WorldComp.CachedCustomApparel.Contains(ap))
            {
                __result = -1000;
                return false;
            }

            return true;
        }
    }

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