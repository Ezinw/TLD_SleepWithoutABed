using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace SleepWithoutABed
{
    //Health gain while sleeping with no bed
    [HarmonyPatch(typeof(Condition), nameof(Condition.Update))]
    public static class NullBedConditionRecovery
    {
        static void Postfix(Condition __instance)
        {
            if (GameManager.m_IsPaused ||
                GameManager.s_IsGameplaySuspended ||
                GameManager.GetPlayerManagerComponent().m_SuspendConditionUpdate ||
                Utils.IsZero(GameManager.GetTimeOfDayComponent().GetDayLengthSeconds(), 0.0001f))
            {
                return;
            }

            var restComponent = GameManager.GetRestComponent();
            var conditionComponent = GameManager.GetConditionComponent();
            var bed = restComponent?.m_Bed;

            if (restComponent == null ||
                conditionComponent == null ||
                restComponent.IsForcedSleep() ||
                CinematicManager.s_IsCutsceneActive ||
                GameManager.GetPlayerManagerComponent().GetControlMode() == PlayerControlMode.Dead)
            {
                return;
            }

            if (bed == null && restComponent.IsSleeping())
            {
                //Set the recovery scale
                float recoveryScale = bed == null ? Settings.settings.nullBedConditionGainPerHour : 1.0f;

                // Calculate the base health recovery
                float baseHealthRecovery = conditionComponent.m_HPIncreasePerDayWhileHealthy * GameManager.GetExperienceModeManagerComponent().GetConditonRecoveryFromRestScale();

                // Calculate the delta time as a fraction of the in-game day length
                // This ensures health recovery is scaled properly based on the passage of in-game time
                float deltaTime = Time.deltaTime / GameManager.GetTimeOfDayComponent().GetDayLengthSeconds();

                // Adjust the base health recovery using the recovery scale and delta time
                float adjustedHealthRecovery = baseHealthRecovery * recoveryScale * deltaTime;

                // Apply health recovery
                conditionComponent.AddHealth(adjustedHealthRecovery, DamageSource.Sleeping);
            }
        }
    }
}
