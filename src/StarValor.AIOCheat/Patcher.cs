using System;
using HarmonyLib;
using BepInEx.Logging;

namespace ElementalCyclone.UnityMod.StarValor.AIOCheat
{
    /// <summary>
    /// Contains required method(s) patcher for AIOCheat Functionality
    /// </summary>
    public class Patcher
    {
        /// <summary>
        /// Backing value for <see cref="XPSlider_Multiplier"/>
        /// </summary>
        protected static float backVal_XPSlidMult = 1f;

        /// <summary>
        /// Reference to BepInEx's <see cref="BaseUnityPlugin.Logger"/> instance, supposed to be initialized by <see cref="Plugin"/>.
        /// </summary>
        protected internal static ManualLogSource loggerInstance = null;

        /// <summary>
        /// Get or set multiplier value for "Set : XP Multiplier" cheat 
        /// </summary>
        /// <remarks>
        /// Cannot be set with negative number, will absolutes the received value.
        /// </remarks>
        public static float XPSlider_Multiplier
        {
            get => backVal_XPSlidMult;
            set
            {
                backVal_XPSlidMult = Math.Abs(value);
            }
        }

        [HarmonyPatch(typeof(PChar), nameof(PChar.EarnXP))]
        [HarmonyPrefix]
        public static void Pre_PCharEarnXP(ref float amount)
        {
            amount *= XPSlider_Multiplier;
        }
    }
}
