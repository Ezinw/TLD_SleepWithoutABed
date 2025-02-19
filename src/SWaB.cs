using Il2Cpp;
using HarmonyLib;
using UnityEngine;

namespace SleepWithoutABed
{
    [HarmonyPatch(typeof(Panel_Rest), nameof(Panel_Rest.Enable), new Type[] { typeof(bool), typeof(bool) })]
    public class SWaB
    {
        // Stored reference to cloned bedroll
        public static GearItem? _clonedBedroll = null; 

        static void Prefix(Panel_Rest __instance, ref bool enable, ref bool passTimeOnly)
        {
            passTimeOnly = false; // Enable sleep option

            // Rest panel opened
            if (enable)
            {
                if (__instance.m_Bed != null && __instance.m_Bed != _clonedBedroll?.GetComponent<Bed>())
                {
                    return; // If a real bed exists, use it
                }

                // If no bed exists, create one
                if (__instance.m_Bed == null && _clonedBedroll == null)
                {
                    GearItem bedrollPrefab = GearItem.LoadGearItemPrefab("GEAR_BedRoll");

                    if (bedrollPrefab != null)
                    {
                        GameObject bedrollObject = GearItem.InstantiateDepletedGearPrefab(bedrollPrefab.gameObject);
                        GearItem clonedBedroll = bedrollObject.GetComponent<GearItem>();

                        if (clonedBedroll != null)
                        {
                            clonedBedroll.m_CurrentHP = Mathf.Max(2f, clonedBedroll.m_CurrentHP); // Assign 2% condition to ensure there's no warmth bonus from cloned bedroll
                            clonedBedroll.m_WornOut = false; // Instantiated bedroll is not worn out
                            clonedBedroll.m_InPlayerInventory = false; // Do not place cloned bedroll in inventory
                            clonedBedroll.gameObject.transform.position = GameManager.GetPlayerTransform().position;
                            clonedBedroll.gameObject.SetActive(true);

                            Bed bedComponent = clonedBedroll.GetComponent<Bed>();
                            if (bedComponent != null)
                            {
                                __instance.m_Bed = bedComponent;
                                _clonedBedroll = clonedBedroll;

                                bedComponent.m_OpenAudio = null; // Disable bedroll opening audio
                                bedComponent.m_CloseAudio = null; // Disable bedroll closing audio
                                bedComponent.SetState(BedRollState.Placed); // Set bedroll as placed
                            }
                        }
                    }
                }
            }

            // Rest panel closed
            else if (!enable)
            {
                if (__instance.m_Bed != null && __instance.m_Bed != _clonedBedroll?.GetComponent<Bed>()) // If a real bed exists
                {
                    if (!GameManager.GetRestComponent().IsSleeping()) // If player is not sleeping
                    {
                        __instance.m_Bed = null; // Reset bed reference when rest panel is closed
                    }
                }

                if (__instance.m_Bed != null && __instance.m_Bed == _clonedBedroll?.GetComponent<Bed>()) // If the bed is cloned
                {
                    if (!GameManager.GetRestComponent().IsSleeping()) // If player is not sleeping
                    {
                        if (_clonedBedroll != null)
                        {
                            GearManager.DestroyGearObject(_clonedBedroll.gameObject); // Destroy cloned bedroll when rest panel is closed
                            _clonedBedroll = null;

                            __instance.m_Bed = null; // Reset bed reference
                        }
                    }
                }
            }
        }
    }
    
    
    // Disable pickup button and center sleep button when cloned bedroll is active
    [HarmonyPatch(typeof(Panel_Rest), nameof(Panel_Rest.UpdateButtonLegend))]
    public class SWaB_UpdateButtonLegend
    {
        static void Postfix(Panel_Rest __instance)
        {
            if (__instance.m_Bed != null && __instance.m_Bed.gameObject == SWaB._clonedBedroll?.gameObject)
            {
                if (__instance.m_PickUpButton.activeSelf) 
                {
                    Utils.SetActive(__instance.m_PickUpButton, false); // Disable pickup button if bedroll is cloned

                    __instance.m_SleepButton.transform.localPosition = __instance.m_SleepButtonCenteredPos; // Center the sleep button
                }
            }
        }
    }


    // Apply cold exposure penalties when sleeping
    [HarmonyPatch(typeof(Panel_Rest), nameof(Panel_Rest.OnRest))]
    public class SWaB_SleepExposure
    {
        static void Postfix(Panel_Rest __instance)
        {
            if (GameManager.m_IsPaused || GameManager.s_IsGameplaySuspended)
            {
                return;
            }

            var restComponent = GameManager.GetRestComponent();
            var freezingComponent = GameManager.GetFreezingComponent();
            var conditionComponent = GameManager.GetConditionComponent();
            var hypothermiaComponent = GameManager.GetHypothermiaComponent();

            if (__instance == null || restComponent  == null || freezingComponent == null || conditionComponent == null || hypothermiaComponent == null)
            {
                return;
            }

            if (__instance.m_Bed != null && __instance.m_Bed == SWaB._clonedBedroll?.GetComponent<Bed>() || __instance.m_Bed == null)
            {
                __instance.m_Bed = SWaB._clonedBedroll?.GetComponent<Bed>();
            }

            // Use these values for a real bed
            restComponent.m_ReduceFatiguePerHourRest = 8.33f;
            freezingComponent.m_FreezingIncreasePerHourPerDegreeCelsius = 6f;
            conditionComponent.m_HPDecreasePerDayFromFreezing = 450f;
            hypothermiaComponent.m_HPDrainPerHour = 40f;

            // If bed is a clone
            if (SWaB._clonedBedroll != null && __instance.m_Bed == SWaB._clonedBedroll?.GetComponent<Bed>())
            {
                // Apply cold exposure effects
                freezingComponent.m_FreezingIncreasePerHourPerDegreeCelsius = 6f * Settings.settings.freezingScale;
                conditionComponent.m_HPDecreasePerDayFromFreezing = 450f * Settings.settings.freezingHealthLoss + ExposurePenalty.ApplyExposurePenalty();
                hypothermiaComponent.m_HPDrainPerHour = 40f * Settings.settings.hypothermicHealthLoss + ExposurePenalty.ApplyExposurePenalty();
                restComponent.m_ReduceFatiguePerHourRest = 8.33f * Settings.settings.fatigueRecoveryPenalty;

                return;
            }
        }
    }


    // Condition recovery
    [HarmonyPatch(typeof(Rest), nameof(Rest.UpdateWhenSleeping))]
    public class SWaB_ConditionRecovery
    {
        static void Postfix(Rest __instance)
        {
            if (__instance == null || __instance.m_Bed == null || GameManager.m_IsPaused || GameManager.s_IsGameplaySuspended)
            {
                return;
            }

            // Use these values for a real bed
            __instance.m_Bed.m_ConditionPercentGainPerHour = 1f;
            __instance.m_Bed.m_UinterruptedRestPercentGainPerHour = 1f;

            // If bed is a clone
            if (__instance.m_Bed != null && __instance.m_Bed == SWaB._clonedBedroll?.GetComponent<Bed>())
            {
                //Apply condition recovery penalty
                __instance.m_Bed.m_ConditionPercentGainPerHour = 1f * Settings.settings.cloneBedConditionGainPerHour;
                __instance.m_Bed.m_UinterruptedRestPercentGainPerHour = 1f * Settings.settings.cloneBedConditionGainPerHour;

                return;
            }
        }
    }
    

    // When sleep ends, destroy cloned bedroll if it exists and reset bed reference
    [HarmonyPatch(typeof(Rest), nameof(Rest.EndSleeping), new Type[] { typeof(bool) })]
    public class SWaB_EndSleeping
    {
        static void Postfix(Rest __instance, ref bool interrupted)
        {
            if (__instance == null)
            {
                return;
            }

            // Check if the cloned bedroll exists
            if ((interrupted || !interrupted) && SWaB._clonedBedroll != null)
            {
                GearManager.DestroyGearObject(SWaB._clonedBedroll.gameObject); // Destroy cloned bedroll if it does exist
                SWaB._clonedBedroll = null;
            }

            __instance.m_Bed = null; // Reset bed reference
        }
    }


    // Apply cold exposure penalties when passing time
    [HarmonyPatch(typeof(Panel_Rest), nameof(Panel_Rest.OnPassTime))]
    public class SWaB_PassTimeExposure
    {
        static void Postfix(Panel_Rest __instance)
        {
            if (GameManager.m_IsPaused || GameManager.s_IsGameplaySuspended)
            {
                return;
            }

            var freezingComponent = GameManager.GetFreezingComponent();
            var conditionComponent = GameManager.GetConditionComponent();
            var hypothermiaComponent = GameManager.GetHypothermiaComponent();

            if (__instance == null || freezingComponent == null || conditionComponent == null || hypothermiaComponent == null)
            {
                return;
            }

            if (__instance.m_Bed != null && __instance.m_Bed == SWaB._clonedBedroll?.GetComponent<Bed>() || __instance.m_Bed == null)
            {
                __instance.m_Bed = SWaB._clonedBedroll?.GetComponent<Bed>();
            }

            // Use these values for a real bed
            freezingComponent.m_FreezingIncreasePerHourPerDegreeCelsius = 6f;
            conditionComponent.m_HPDecreasePerDayFromFreezing = 450f;
            hypothermiaComponent.m_HPDrainPerHour = 40f;

            // If bed is a clone
            if (SWaB._clonedBedroll != null && __instance.m_Bed == SWaB._clonedBedroll?.GetComponent<Bed>())
            {
                // Apply cold exposure effects
                freezingComponent.m_FreezingIncreasePerHourPerDegreeCelsius = (6f * Settings.settings.freezingScale) * Settings.settings.passTimeEffectModifier;
                conditionComponent.m_HPDecreasePerDayFromFreezing = (450f * Settings.settings.freezingHealthLoss + ExposurePenalty.ApplyExposurePenalty()) * Settings.settings.passTimeEffectModifier;
                hypothermiaComponent.m_HPDrainPerHour = (40f * Settings.settings.hypothermicHealthLoss + ExposurePenalty.ApplyExposurePenalty()) * Settings.settings.passTimeEffectModifier;

                return;
            }
        }
    }

    
    // Close rest panel when the player begins sleeping
    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.PlayerIsSleeping))]
    public class SWaB_RestPanelX
    {
        static void Postfix(PlayerManager __instance, ref bool __result)
        {
            if (__instance == null || GameManager.m_IsPaused || GameManager.s_IsGameplaySuspended)
            {
                return;
            }

            if (__result) // If player is sleeping
            {
                var restPanel = InterfaceManager.GetPanel<Panel_Rest>();
                if (restPanel != null && restPanel.IsEnabled()) // If rest panel is enabled
                {
                    restPanel.Enable(false); // Close the rest panel
                }
            }
        }
    }
}
