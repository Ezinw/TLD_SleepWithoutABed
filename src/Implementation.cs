using MelonLoader;

namespace SleepWithoutABed
{
    internal sealed class Implementation : MelonMod
    {
        public override void OnInitializeMelon()
        { 
            Settings.OnLoad();
        }
    }
}
