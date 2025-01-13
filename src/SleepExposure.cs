using Il2Cpp;
using HarmonyLib;
using System.Reflection;

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
    public static class ShouldInterruptWithPredatorPatch
    {
        static bool Prefix(Rest __instance, ref bool __result)
        {
            if (__instance == null)
            {
                __result = false;
                return false;
            }

            if (__instance.m_Bed == null)
            {
                __result = false;
            }

            return true;
        }
    }



    [HarmonyPatch(typeof(Rest), nameof(Rest.RollForRestInterruption))]
    public static class RollForRestInterruptionPatch
    {
        static bool Prefix(Rest __instance)
        {
            if (__instance == null)
            {
                return false; // Skip the original method
            }

            if (__instance.m_Bed == null)
            {
                __instance.m_PredatorInterruption = false; // No predator interruptions without a bed
                __instance.m_InterruptionAfterSecondsSleeping = 0; // Reset interruption time
                return false; // Skip original method to avoid further execution
            }

            // Allow the original method to execute if conditions are met
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
                throw new NullReferenceException("NRE 'BeginSleeping' ");
            }

            return method;
        }

        static void Prefix(Rest __instance, Bed? b, int durationHours, int maxHours, float fadeOutDuration, Rest.PassTimeOptions options, ref Il2CppSystem.Action? onSleepEnd)
        {
            if (__instance == null)
            {
                return;
            }

            if (__instance.m_Bed == null)
            {
                SleepExposureEffect.ApplySleepExposureEffect();
            }
        }
    }


}

