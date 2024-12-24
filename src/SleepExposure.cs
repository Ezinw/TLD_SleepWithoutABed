using Il2Cpp;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SleepWithoutABed
{
    public static class SleepExposureEffect
    {
        public static void ApplySleepExposureEffect()
        {
            if (GameManager.m_IsPaused ||
                CinematicManager.s_IsCutsceneActive ||
                GameManager.GetPlayerManagerComponent().m_SuspendConditionUpdate ||
                GameManager.GetPlayerManagerComponent().GetControlMode() == PlayerControlMode.Dead)
            {
                return;
            }

            var freezingComponent = GameManager.GetFreezingComponent();
            var conditionComponent = GameManager.GetConditionComponent();
            var hypothermiaComponent = GameManager.GetHypothermiaComponent();
            var restComponent = GameManager.GetRestComponent();

            if (freezingComponent == null || 
                conditionComponent == null || 
                restComponent == null ||
                restComponent.IsForcedSleep())
            {
                return;
            }

            // Calculate penalty multiplier 
            float penaltyMultiplier = PenaltyMultiplier.CalculatePenaltyMultiplier();

            var bed = restComponent.m_Bed;

            if (bed == null && penaltyMultiplier > 0)
            {
                // Apply freezing penalty while sleeping
                freezingComponent.AddFreezing(penaltyMultiplier * Settings.settings.freezingRate);

                // Additional freezing health loss while sleeping
                if (freezingComponent.IsFreezing())
                {
                    conditionComponent.AddHealth(-penaltyMultiplier * Settings.settings.freezingHealthLoss, DamageSource.Freezing);

                    // Additional hypothermia health loss while sleeping
                    if (hypothermiaComponent?.HasHypothermia() == true)
                    {
                        conditionComponent.AddHealth(-penaltyMultiplier * Settings.settings.hypothermicHealthLoss, DamageSource.Hypothermia);
                    }
                }
            }
        }
    }

    
    [HarmonyPatch(typeof(Rest), nameof(Rest.ShouldInterruptWithPredator))]
    public static class PredatorInterruptPatch
    {
        static bool Prefix(Rest __instance, ref bool __result)
        {
            if (GameManager.m_IsPaused)
            {
                return true;
            }

            var restComponent = GameManager.GetRestComponent();

            if (__instance.m_Bed == null && restComponent != null)
            {
                // Force interruption calculation when no bed is present
                __result = true; // Let the game calculate further interruption logic
                return false;    // Skip the original method
            }

            return true; // Allow the original method to run if a bed is present
        }
    }


    [HarmonyPatch(typeof(Rest), nameof(Rest.RollForRestInterruption))]
    public static class RollForRestInterruptionPatch
    {
        static bool Prefix(Rest __instance)
        {
            if (GameManager.m_IsPaused)
            {
                return true; // Allow original code to execute
            }

            var restComponent = GameManager.GetRestComponent();

            if (__instance.m_Bed == null && restComponent != null)
            {
                // Keep the original behavior but ensure predator interruptions are considered
                __instance.m_PredatorInterruption = __instance.ShouldInterruptWithPredator();
                if (__instance.m_PredatorInterruption)
                {
                    // Randomize interruption time as in the original logic
                    __instance.m_InterruptionAfterSecondsSleeping = Mathf.RoundToInt(Random.Range(0.3f, 0.9f) * __instance.m_SleepDurationSeconds);
                }
            }

            return true;
        }

    }




    // Patch the BeginSleeping method of the Rest class to apply exposure effects when the player sleeps without a bed.
    [HarmonyPatch]
    public class SleepExposure
    {
        static MethodBase TargetMethod()
        {
            var method = typeof(Rest).GetMethod("BeginSleeping", BindingFlags.Instance | BindingFlags.Public, null,
                new Type[] { typeof(Bed), typeof(int), typeof(int), typeof(float), typeof(Rest.PassTimeOptions), typeof(Il2CppSystem.Action) },
                null);

            if (method == null)
            {
                throw new NullReferenceException();
            }

            return method;
        }

        static void Prefix(Rest __instance, Bed? b, int durationHours, int maxHours, float fadeOutDuration, Rest.PassTimeOptions options, ref Il2CppSystem.Action? onSleepEnd)
        {
            var restComponent = GameManager.GetRestComponent();

            if (__instance == null || GameManager.m_IsPaused || b != null|| restComponent == null)
            {
                return;
            }

            SleepExposureEffect.ApplySleepExposureEffect();
        }
    }
}

