using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Misc.RGB
{
    public class RGBSyncManager : MonoBehaviour
    {
        [TitleGroup("Singleton Instance")]
        [ShowInInspector] // Show static property in Inspector
        [PropertyTooltip("The singleton instance of RGBSyncManager.")]
        [ReadOnly] // Prevent direct modification in Inspector
        public static RGBSyncManager Instance { get; private set; }

        [TitleGroup("Current RGB Settings")]
        [PropertyOrder(5)] // Place this group after the singleton instance
        [InfoBox("These are the currently active RGB synchronization settings. Changes made here will be applied directly.", InfoMessageType.Info)]
        [InlineProperty] // Display the RGBEffectSettings struct fields inline
        [HideLabel] // Hide the default 'Current Settings' label for the inline property
        public RGBEffectSettings CurrentSettings { get; private set; }

        [TitleGroup("Runtime Information")]
        [ShowInInspector] // Show in inspector
        [PropertyTooltip("The current hue value, cycling from 0 to 1.")]
        // FIX APPLIED: Changed ColorMember to ColorGetter
        [ProgressBar(0, 1, ColorGetter = "HueProgressBarColor", DrawValueLabel = true)] // Visual progress bar for hue
        [ReadOnly]
        private float _hue;

        [ShowInInspector] // Show in inspector
        [PropertyTooltip("Random hue offsets applied to different groups.")]
        [ReadOnly]
        private Dictionary<string, float> _groupHueOffsets = new();

        // Private getter for the progress bar color (Odin specific)
        private Color HueProgressBarColor => Color.HSVToRGB(_hue, 1f, 1f);


        /// <summary>
        /// Applies new RGB settings and updates the manager's state.
        /// </summary>
        /// <param name="newSettings">The new RGBEffectSettings to apply.</param>
        [Button(ButtonSizes.Medium)] // Create a button in the Inspector
        [GUIColor(0.4f, 0.8f, 0.4f)] // Greenish color for Apply
        [PropertyTooltip("Applies the provided RGB settings to the manager.")]
        public void ApplySettings(RGBEffectSettings newSettings)
        {
            CurrentSettings = newSettings;
            // No need to directly set cycleSpeed here if we use CurrentSettings.CycleSpeed in Update
        }

        public Color GetGroupColor(string groupId, float localOffset = 0f)
        {
            if (!CurrentSettings.Enabled)
            {
                return Color.white;
            }

            float baseHue = _hue;
            if (!string.IsNullOrEmpty(groupId))
            {
                if (!_groupHueOffsets.ContainsKey(groupId))
                    _groupHueOffsets[groupId] = Random.Range(0f, 1f);

                baseHue += _groupHueOffsets[groupId];
            }

            float finalHue = (baseHue + localOffset) % 1f;
            return Color.HSVToRGB(finalHue, CurrentSettings.Saturation, CurrentSettings.Brightness);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                LoadSettingsFromPlayerPrefs();
            }

            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (CurrentSettings.Enabled)
            {
                _hue += Time.deltaTime * CurrentSettings.CycleSpeed;
                if (_hue > 1f) _hue -= 1f;
            }
        }

        /// <summary>
        /// Loads RGB settings from PlayerPrefs into CurrentSettings.
        /// Should be called during initialization.
        /// </summary>
        [Button(ButtonSizes.Medium)] // Button to load from PlayerPrefs
        [GUIColor(0.7f, 0.7f, 1f)] // Light blue color
        [PropertyTooltip("Loads RGB settings from PlayerPrefs.")]
        private void LoadSettingsFromPlayerPrefs()
        {
            RGBEffectSettings loadedSettings = CurrentSettings;

            loadedSettings.Enabled = PlayerPrefs.GetInt("rgbEnabled", 1) == 1;
            loadedSettings.CycleSpeed = PlayerPrefs.GetFloat("rgbCycleSpeed", 1.0f);
            loadedSettings.Saturation = PlayerPrefs.GetFloat("rgbSaturation", 1.0f);
            loadedSettings.Brightness = PlayerPrefs.GetFloat("rgbBrightness", 1.0f);

            CurrentSettings = loadedSettings;

            Debug.Log("RGB Settings Loaded from PlayerPrefs.");
        }

        /// <summary>
        /// Saves the current RGB settings to PlayerPrefs.
        /// </summary>
        [Button(ButtonSizes.Medium)] // Button to save to PlayerPrefs
        [GUIColor(1f, 0.7f, 0.7f)] // Light red color
        [PropertyTooltip("Saves current RGB settings to PlayerPrefs.")]
        public void SaveSettingsToPlayerPrefs()
        {
            PlayerPrefs.SetInt("rgbEnabled", CurrentSettings.Enabled ? 1 : 0);
            PlayerPrefs.SetFloat("rgbCycleSpeed", CurrentSettings.CycleSpeed);
            PlayerPrefs.SetFloat("rgbSaturation", CurrentSettings.Saturation);
            PlayerPrefs.SetFloat("rgbBrightness", CurrentSettings.Brightness);
            PlayerPrefs.Save();
            Debug.Log("RGB Settings Saved to PlayerPrefs.");
        }
    }
}