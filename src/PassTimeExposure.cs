using Il2Cpp;
using HarmonyLib;
using System.Reflection;

namespace SleepWithoutABed
{
    public static class PassTimeExposureEffect
    {
        public static void ApplyPassTimeExposureEffect()
        {
            if (GameManager.m_IsPaused ||
                CinematicManager.s_IsCutsceneActive ||
                GameManager.GetPlayerManagerComponent().m_SuspendConditionUpdate ||
                GameManager.GetPlayerManagerComponent().GetControlMode() == PlayerControlMode.Dead)
            {
                return;
            }

            // Retrieve critical components
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

            float passTimeModifier = Settings.settings.passTimeEffectModifier;
            float passTimePenalty = penaltyMultiplier * 0.5f;

            var bed = restComponent.m_Bed;

            if (bed == null && penaltyMultiplier > 0)
            {
                // Apply freezing penalty while passing time 
                freezingComponent.AddFreezing(passTimePenalty * Settings.settings.freezingRate * passTimeModifier);

                // Additional freezing health loss while passing time
                if (freezingComponent.IsFreezing())
                {
                    conditionComponent.AddHealth(-passTimePenalty * Settings.settings.freezingHealthLoss * passTimeModifier, DamageSource.Freezing);

                    // Additional hypothermia health loss while passing time
                    if (hypothermiaComponent?.HasHypothermia() == true)
                    {
                        conditionComponent.AddHealth(-passTimePenalty * Settings.settings.hypothermicHealthLoss * passTimeModifier, DamageSource.Hypothermia);
                    }
                }
            }
        }
    }



    // Patch the Begin method of the PassTime class to apply exposure effects when the player passes time without a bed.
    [HarmonyPatch]
    public class PassTimeExposure
    {
        static MethodBase TargetMethod()
        {
            var method = typeof(PassTime).GetMethod("Begin", BindingFlags.Instance | BindingFlags.Public, null,
                new Type[] { typeof(float), typeof(Bed) }, null);

            if (method == null)
            {
                throw new NullReferenceException();
            }
            return method;
        }

        static void Prefix(float hours, Bed bed)
        {
            var getPassTime = GameManager.GetPassTime();

            if (GameManager.m_IsPaused || bed != null || getPassTime ==  null)
            {
                return;
            }

            PassTimeExposureEffect.ApplyPassTimeExposureEffect();
        }
    }
}