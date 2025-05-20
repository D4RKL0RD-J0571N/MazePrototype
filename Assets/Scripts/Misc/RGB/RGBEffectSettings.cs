using Sirenix.OdinInspector;
using UnityEngine;

namespace Misc.RGB
{
    // Mark RGBEffectSettings as Serializable and potentially add Odin attributes here too
    // This struct should be in Misc.RGB namespace as per your setup.
    [System.Serializable]
    public struct RGBEffectSettings
    {
        [ToggleLeft]
        [LabelText("Enable RGB")] // Custom label for toggle
        public bool Enabled; // Default: false (automatically by C# for bool)

        [Range(0.1f, 10f)] // Define slider range
        [PropertyTooltip("How fast the RGB colors cycle.")]
        public float CycleSpeed; // Default: 0f (automatically by C# for float)

        [Range(0f, 1f)] // Define slider range
        [PropertyTooltip("The saturation of the RGB colors (0 = grayscale, 1 = full color).")]
        public float Saturation; // Default: 0f (automatically by C# for float)

        [Range(0f, 1f)] // Define slider range
        [PropertyTooltip("The brightness of the RGB colors (0 = black, 1 = full brightness).")]
        public float Brightness; // Default: 0f (automatically by C# for float)
        
        // To provide default values in a struct, you need to use a parameterized constructor.
        // This is the common pattern for structs in older C# versions.
        public RGBEffectSettings(bool enabled, float cycleSpeed, float saturation, float brightness)
        {
            Enabled = enabled;
            CycleSpeed = cycleSpeed;
            Saturation = saturation;
            Brightness = brightness;
        }
    }
}