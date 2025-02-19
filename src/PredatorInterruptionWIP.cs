using Il2Cpp;
using HarmonyLib;
using UnityEngine;

namespace SleepWithoutABed
{
    // It seems that predator attacks during rest are bugged. Sometimes the camera will act strangely or break keeping the screen black (faded out) throughout a struggle
    // This helps to make struggle camera issues less frequent but there will still be issues with the struggle camera animations
    // WIP:
    [HarmonyPatch(typeof(Rest), nameof(Rest.ShouldInterruptWithPredator))]
    public class SWaB_ShouldInterruptWithPredator
    {
        // Track the current predator engaged in an attack to prevent multiple predators from attacking simultaneously
        public static BaseAi? activePredator = null;
        static void Postfix(Rest __instance, ref bool __result)
        {
            if (__instance == null)
                return;

            if (!__result)
                return;

            // Prevent multiple predators attacking simultaneously
            if (activePredator != null && activePredator.GetAiMode() == AiMode.Attack)
            {
                __result = false;
                return;
            }

            SpawnRegionManager? spawnManager = GameManager.GetSpawnRegionManager();
            if (spawnManager == null)
            {
                __result = false; // No spawn manager, so no predators to check
                return;
            }

            // Get the closest wolf 
            GameObject? closestWolf = GameManager.GetSpawnRegionManager()?.GetClosestActiveSpawn(
                GameManager.GetPlayerTransform().position, __instance.m_WolfPrefab?.name ?? "");

            // Get the closest bear
            GameObject? closestBear = GameManager.GetSpawnRegionManager()?.GetClosestActiveSpawn(
                GameManager.GetPlayerTransform().position, __instance.m_BearPrefab?.name ?? "");

            // Determine which predator is closer
            GameObject? predator = null;
            float playerDistance = Mathf.Infinity;
            if (closestWolf != null)
            {
                float wolfDistance = Vector3.Distance(GameManager.GetPlayerTransform().position, closestWolf.transform.position);
                if (wolfDistance < playerDistance)
                {
                    predator = closestWolf;
                    playerDistance = wolfDistance;
                }
            }
            if (closestBear != null)
            {
                float bearDistance = Vector3.Distance(GameManager.GetPlayerTransform().position, closestBear.transform.position);
                if (bearDistance < playerDistance)
                {
                    predator = closestBear;
                }
            }
            if (predator == null)
            {
                __result = false;
                return;
            }

            BaseAi? ai = predator.GetComponent<BaseAi>();
            if (ai == null)
            {
                __result = false;
                return;
            }

            AiTarget? playerTarget = GameManager.GetPlayerObject()?.GetComponent<AiTarget>();
            if (playerTarget == null)
            {
                __result = false;
                return;
            }
            if (__result && ai.m_CurrentTarget != playerTarget)
            {
                ai.m_CurrentTarget = playerTarget;
            }

            // If AI cannot pathfind to the player, stop
            if (!ai.CanPathfindToPosition(GameManager.GetPlayerTransform().position))
            {
                __result = false;
                return;
            }

            if (ai.GetAiMode() != AiMode.Attack)
            {
                Vector3 attackPosition = GameManager.GetPlayerTransform().position;
                bool attackStarted = ai.EnterAttackModeIfPossible(attackPosition, true);
                if (!attackStarted)
                {
                    __result = false;
                    return;
                }

                // Set a timer to make sure AI is in attack mode before a struggle occurs
                float startTime = Time.realtimeSinceStartup;
                float timeout = 1.0f;
                while (Time.realtimeSinceStartup - startTime < timeout && ai.GetAiMode() != AiMode.Attack)
                {
                    Thread.Sleep(1);
                }
                if (ai.GetAiMode() != AiMode.Attack)
                {
                    __result = false;
                    return;
                }

                if (ai.Bear)
                {
                    ai.SuppressAttackStartAnimation();
                    ai.AnimSetTrigger(ai.m_AnimParameter_Roar_Trigger);
                }
                ai.SuppressAttackStartAnimation();
            }

            activePredator = ai;
            __result = true;
        }
    }


    // Reset activePredator when the struggle ends
    [HarmonyPatch(typeof(PlayerStruggle), nameof(PlayerStruggle.GetUpAnimationComplete))]
    public class SWaB_ResetActivePredator
    {
        static void Postfix(PlayerStruggle __instance)
        {
            SWaB_ShouldInterruptWithPredator.activePredator = null; // Reset active predator

            __instance.StopAllAudio(); // Stop all struggle related audio
        }
    }


    // Patch the camera interpolation method to validate transform data
    [HarmonyPatch(typeof(PlayerStruggle), nameof(PlayerStruggle.BashCameraInterpolateToPartner))]
    public class SWaB_BashCameraInterpolateToPartner
    {
        // Before the original method runs, ensure the partner camera bones are valid
        static bool Prefix(PlayerStruggle __instance)
        {
            if (__instance.m_PartnerCameraBone == null || __instance.m_PartnerEffectsBone == null)
            {
                __instance.BreakStruggle();
                return false;
            }
            return true;
        }

        // After camera interpolation, check that the main camera’s transform is valid.
        static void Postfix(PlayerStruggle __instance)
        {
            Camera mainCamera = GameManager.GetMainCamera();
            if (mainCamera == null)
                return;
            Vector3 pos = mainCamera.transform.position;
            Quaternion rot = mainCamera.transform.rotation;
            if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z))
            {
                // Reset to a safe default based on the player’s position and camera offset
                mainCamera.transform.position = GameManager.GetPlayerTransform().position + GameManager.GetVpFPSCamera().PositionOffset;
            }
            if (float.IsNaN(rot.x) || float.IsNaN(rot.y) || float.IsNaN(rot.z) || float.IsNaN(rot.w))
            {
                // Reset to an identity rotation
                mainCamera.transform.rotation = Quaternion.identity;
            }
        }
    }
}