using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Object = UnityEngine.Object;

namespace ChangeDresser.UI
{
    [StaticConstructorOnStartup]
    public class PawnColumnWorker_AssignOutfit : PawnColumnWorker_Checkbox
    {
        private Vector2 cachedOutfitLabelSize;
        private static readonly Texture2D SortingIcon = ContentFinder<Texture2D>.Get("UI/Icons/Sorting");

        private static readonly Texture2D SortingDescendingIcon =
            ContentFinder<Texture2D>.Get("UI/Icons/SortingDescending");

        public ApparelPolicy ApparelPolicy => ((PawnColumnDef_AssignOutfit)def).apparelPolicy;

        public void DoHeaderCheckon(Rect rect, PawnTable table)
        {
            bool checkOn = this.GetHeadValue();
            bool flag = checkOn;

            // Calculate the center position for the checkbox
            float checkboxSize = 24f; // Size of the checkbox
            float centerX = rect.x + (rect.width / 2) - (checkboxSize / 2);

            Rect rect1 = new Rect(centerX, rect.y, checkboxSize, checkboxSize);
            Widgets.Checkbox(centerX, rect.y, ref checkOn);

            if (Mouse.IsOver(rect1))
            {
                string tip = "ChangeDresser.UseForBattle".Translate();
                if (!tip.NullOrEmpty())
                    TooltipHandler.TipRegion(rect1, (TipSignal)tip);
            }

            if (checkOn == flag)
                return;
            this.SetHeadValue(checkOn, table);
        }

        public override void DoHeader(Rect rect, PawnTable table)
        {
            // base function
            base.DoHeader(rect, table);

            DoHeaderCheckon(rect, table);

            Verse.Text.Font = GameFont.Small;
            if (this.cachedOutfitLabelSize == new Vector2())
                this.cachedOutfitLabelSize =
                    Verse.Text.CalcSize(((PawnColumnDef_AssignOutfit)def).apparelPolicy.label.CapitalizeFirst());
            Rect labelRect = this.GetLabelRect(rect);
            MouseoverSounds.DoRegion(labelRect);
            Verse.Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(labelRect, ((PawnColumnDef_AssignOutfit)def).apparelPolicy.label.CapitalizeFirst());
            GUI.color = new Color(1f, 1f, 1f, 0.3f);
            Widgets.DrawLineVertical(labelRect.center.x, labelRect.yMax - 3f,
                (float)((double)rect.y + 50.0 - (double)labelRect.yMax + 3.0));
            Widgets.DrawLineVertical(labelRect.center.x + 1f, labelRect.yMax - 3f,
                (float)((double)rect.y + 50.0 - (double)labelRect.yMax + 3.0));
            GUI.color = Color.white;
            Verse.Text.Anchor = TextAnchor.UpperLeft;
        }

        private Rect GetLabelRect(Rect headerRect)
        {
            Rect labelRect = new Rect(headerRect.center.x - this.cachedOutfitLabelSize.x / 2f, headerRect.y,
                this.cachedOutfitLabelSize.x, this.cachedOutfitLabelSize.y);
            // For checkbox
            labelRect.y += 28f;
            if (this.def.moveWorkTypeLabelDown)
                labelRect.y += 20f;
            return labelRect;
        }

        protected override Rect GetInteractableHeaderRect(Rect headerRect, PawnTable table)
        {
            return this.GetLabelRect(headerRect);
        }

        public override int GetMinHeaderHeight(PawnTable table) => 50 + 28;

        public override int GetMinWidth(PawnTable table)
        {
            Verse.Text.Font = GameFont.Small;
            int minWidth =
                Mathf.CeilToInt(Verse.Text
                    .CalcSize(((PawnColumnDef_AssignOutfit)def).apparelPolicy.label.CapitalizeFirst()).x) / 2 + 10;
            return Mathf.Max(minWidth, base.GetMinWidth(table));
        }


        public override int GetOptimalWidth(PawnTable table)
        {
            return Mathf.Clamp(39, this.GetMinWidth(table), this.GetMaxWidth(table));
        }

        public override int GetMaxWidth(PawnTable table) => Mathf.Min(base.GetMaxWidth(table), 80);


        protected override bool HasCheckbox(Pawn pawn)
        {
            return true;
        }

        protected override bool GetValue(Pawn pawn)
        {
            if (WorldComp.PlayFunctionPawnOutfits.TryGetValue(pawn, out PawnOutfitTracker po))
            {
                bool assign = po.Contains(this.ApparelPolicy);
                return assign;
            }
            else
            {
                return false;
            }
        }

        protected override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            if (WorldComp.PlayFunctionPawnOutfits.TryGetValue(pawn, out PawnOutfitTracker po))
            {
                HandleOutfitAssign(value, this.ApparelPolicy, po);
            }
        }

        protected bool GetHeadValue()
        {
            ApparelPolicy o = ((PawnColumnDef_AssignOutfit)def).apparelPolicy;
            return WorldComp.OutfitsForBattle.Contains(o);
        }

        protected void SetHeadValue(bool value, PawnTable table)
        {
            ApparelPolicy o = ((PawnColumnDef_AssignOutfit)def).apparelPolicy;
            if (value)
            {
                WorldComp.OutfitsForBattle.Add(o);
            }
            else
            {
                bool removed = WorldComp.OutfitsForBattle.Remove(o);
            }

            foreach (PawnOutfitTracker po in WorldComp.PawnOutfits.Values)
            {
                po.UpdateOutfitType(o, (value) ? OutfitType.Battle : OutfitType.Civilian);
            }
        }

        private void HandleOutfitAssign(bool assign, ApparelPolicy outfit, PawnOutfitTracker po)
        {
            Pawn pawn = po.Pawn;
            if (assign)
            {
                po.DefinedOutfits.Add(new DefinedOutfit(outfit, WorldComp.GetOutfitType(outfit)));
            }
            else
            {
                po.Remove(outfit);
                if (pawn.outfits.CurrentApparelPolicy.Equals(outfit))
                {
                    bool newOutfitFound;
                    if (pawn.Drafted)
                    {
                        newOutfitFound = !po.ChangeToBattleOutfit();
                    }
                    else
                    {
                        newOutfitFound = !po.ChangeToCivilianOutfit();
                    }

                    if (!newOutfitFound)
                    {
                        Messages.Message(
                            pawn.Name.ToStringShort + " will no longer wear " + outfit.label +
                            ". Could not find another Outfit for them to wear. Please fix this manually.",
                            MessageTypeDefOf.CautionInput);
                    }
                    else
                    {
                        IDresserOutfit o = po.CurrentOutfit;
                        if (o != null)
                        {
                            Messages.Message(
                                pawn.Name.ToStringShort + " will no longer wear " + outfit.label +
                                " and will instead be assigned to wear " + o.Label, MessageTypeDefOf.CautionInput);
                        }
                        else
                        {
                            Messages.Message(
                                pawn.Name.ToStringShort + " will no longer wear " + outfit.label +
                                " but could not be assigned anything else to wear.", MessageTypeDefOf.CautionInput);
                        }
                    }
                }
            }
        }
    }
}