using ChangeDresser;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace MendingChangeDresserPatch
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            if (ModsConfig.ActiveModsInLoadOrder.Any(m => "MendAndRecycle".Equals(m.Name)))
            {
                try
                {
                    var harmony = new Harmony("com.mendingchangedresserpatch.rimworld.mod");

                    harmony.PatchAll(Assembly.GetExecutingAssembly());

                    Log.Message(
                        "MendingChangeDresserPatch Harmony Patches:" + Environment.NewLine +
                        "  Postfix:" + Environment.NewLine +
                        "    WorkGiver_DoBill.TryFindBestBillIngredients - Priority Last");
                }
                catch (Exception e)
                {
                    Log.Error("Failed to patch Mending & Recycling." + Environment.NewLine + e.Message);
                }
            }
            else
            {
                Log.Message("MendingChangeDresserPatch did not find MendAndRecycle. Will not load patch.");
            }
        }
    }

}