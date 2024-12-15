using Il2Cpp;
using HarmonyLib;
using System.Runtime.InteropServices;

namespace SleepWithoutABed
{

    [HarmonyPatch(typeof(Panel_Rest), nameof(Panel_Rest.Enable), new Type[] { typeof(bool), typeof(bool) })]
    public class EnableSleep
    {
        static void Prefix(Panel_Rest __instance, ref bool enable, ref bool passTimeOnly)
        {
            if (GameManager.m_IsPaused ||
                GameManager.s_IsGameplaySuspended)
            {
                return;
            }

            bool inSnowShelter = GameManager.GetSnowShelterManager().PlayerInShelter();
            bool inVehicle = GameManager.GetPlayerInVehicle().IsInside();
            var restComponent = GameManager.GetRestComponent();
            var bed = __instance.m_Bed;

            if (restComponent == null)
            {
                return;
            }

            if (enable)
            {
                if (bed != null)
                {
                    // Default fatigue recovery while sleeping in a bed/bedroll
                    restComponent.m_ReduceFatiguePerHourRest = 8.333333333333333f;
                }
                else if (bed == null && !inVehicle && !inSnowShelter)
                {

                    passTimeOnly = false;

                    // Fatigue recovery penalties
                    restComponent.m_ReduceFatiguePerHourRest = Settings.settings.fatigueRecoveryPenalty switch
                    {
                        Settings.Choice.Default => 8.333333333333333f,  // Full recovery
                        Settings.Choice.ThreeQuarters => 6.25f,               // 3/4 recovery
                        Settings.Choice.Half => 4.166666666666667f,  // Half recovery
                        Settings.Choice.Quarter => 2.083333333333333f,  // 1/4 recovery
                        Settings.Choice.Eighth => 1.041666666666667f,  // 1/8 recovery
                        _ => 8.333333333333333f
                    };
                }
            }

            else if (!enable && bed != null)
            {
                // Reset bed reference when disabling rest
                __instance.m_Bed = null;
            }
        }
    }


    //Close the rest panel after clicking the sleep button if accessed through the pass time radial option.
    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.PlayerIsSleeping))]
    public class RestPanelX
    {
        static void Postfix(PlayerManager __instance, ref bool __result)
        {
            if (GameManager.m_IsPaused ||
                GameManager.s_IsGameplaySuspended)
            {
                return;
            }

            if (__result)
            {
                CloseRestPanel();
            }
        }

        //Simulate Escape Key Press.
        public static void CloseRestPanel()
        {
            const ushort VK_ESCAPE = 0x1B;
            INPUT[] inputs = new INPUT[2];

            // Press Escape key
            inputs[0] = new INPUT();
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = VK_ESCAPE;

            // Release Escape key
            inputs[1] = new INPUT();
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = VK_ESCAPE;
            inputs[1].u.ki.dwFlags = KEYEVENTF_KEYUP;

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public INPUTUNION u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }
    }
}



