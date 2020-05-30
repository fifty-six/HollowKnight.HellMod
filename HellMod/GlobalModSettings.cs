using System;
using Modding;

namespace HellMod
{
    [Serializable]
    public class GlobalModSettings : ModSettings
    {
        public bool LimitSoulCapacity = true;
        public bool LimitNail = true;
        public bool LimitSoulGain = true;
        public bool LimitFocus = true;
        public bool DoubleDeamage = true;
        public bool LimitSpells = true;
    }
}