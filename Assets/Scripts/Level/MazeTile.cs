using System.Collections;
using Mechanics;
using Misc.RGB;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Level
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class MazeTile : MonoBehaviour, IEchoRevealable
    {
        [FoldoutGroup("Reveal Settings")] // Grouping for reveal-related fields
        [SerializeField]
        [PropertyTooltip("The color of the tile when it is fully revealed.")]
        private Color revealedColor = Color.white;
        
        [FoldoutGroup("Reveal Settings")]
        [SerializeField]
        [PropertyTooltip("The color of the tile when it is hidden.")]
        private Color hiddenColor = Color.black;
        
        [FoldoutGroup("Reveal Settings")]
        [SerializeField, Min(0f)]
        [PropertyTooltip("The duration over which the tile fades between hidden and revealed colors.")]
        private float fadeDuration = 0.25f;

        [FoldoutGroup("RGB Sync Settings")] // Grouping for RGB sync-related fields
        [SerializeField]
        [ToggleLeft] // Places the toggle on the left
        [PropertyTooltip("If true, this specific tile will attempt to synchronize its color with the global RGB effect managed by RGBSyncManager. If false, it will use standard reveal/hide colors.")]
        [InfoBox("Note: RGB Sync for this tile will only be active if 'Enable RGB Cycle' is also turned on in the Main Menu's Options.", InfoMessageType.Info, VisibleIf = "enableRGBSync")]
        private bool enableRGBSync; // Controls if *this specific tile* uses RGB
        
        [FoldoutGroup("RGB Sync Settings")]
        [SerializeField]
        [PropertyTooltip("Optional: Assign a group ID to synchronize this tile's color with other tiles in the same group. Leave empty for global synchronization.")]
        private string groupId = ""; // Leave empty for global sync
        
        [FoldoutGroup("RGB Sync Settings")]
        [SerializeField, Range(0f, 1f)]
        [PropertyTooltip("A hue offset for this tile within its group (or globally). Used to create variations in the RGB cycle.")]
        private float colorOffset;

        // Runtime References - Marked ReadOnly
        [FoldoutGroup("Runtime Information")]
        [ReadOnly]
        [PropertyTooltip("Reference to the SpriteRenderer component.")]
        private SpriteRenderer _renderer;
        
        [FoldoutGroup("Runtime Information")]
        [ReadOnly]
        [PropertyTooltip("Coroutine managing the fading animation.")]
        private Coroutine _fadeCoroutine;
        
        [FoldoutGroup("Runtime Information")]
        [ReadOnly]
        [PropertyTooltip("Coroutine managing the tile's temporary visibility duration.")]
        private Coroutine _visibilityTimer;
        
        // Runtime State - Shown in Inspector but ReadOnly
        [FoldoutGroup("Runtime Information")]
        [ShowInInspector] // Shows this property in the Inspector
        [ReadOnly]
        [PropertyTooltip("Indicates if the tile is currently performing a fade or visibility timer.")]
        public bool IsBusy => _fadeCoroutine != null || _visibilityTimer != null;

        [FoldoutGroup("Runtime Information")]
        [ShowInInspector] // Shows this property in the Inspector
        [ReadOnly]
        [PropertyTooltip("Indicates if the tile is currently in a revealed state (visible).")]
        private bool _isRevealed;
        public bool IsRevealed => _isRevealed; // Public getter, but not directly shown/editable due to _isRevealed being shown

        // Simplified logic for when this tile should use RGB sync
        private bool ShouldUseRGBSync
        {
            get
            {
                // Only sync if RGBSyncManager exists, this tile is revealed,
                // this tile has RGB sync enabled, AND the global RGB effect is enabled
                return _isRevealed &&
                       enableRGBSync &&
                       RGBSyncManager.Instance != null &&
                       RGBSyncManager.Instance.CurrentSettings.Enabled; // Use the new centralized setting
            }
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.color = hiddenColor;
        }

        private void Update()
        {
            // Only update color if RGB sync is active for this tile
            if (ShouldUseRGBSync)
            {
                // The GetGroupColor method already applies saturation and brightness
                _renderer.color = RGBSyncManager.Instance.GetGroupColor(groupId, colorOffset);
            }
        }

        public void Reveal(float duration, bool permanent = false)
        {
            _isRevealed = true;
            StopCoroutineSafe(ref _visibilityTimer);
            StopCoroutineSafe(ref _fadeCoroutine);

            // If RGB sync is active, immediately apply RGB color.
            // Otherwise, fade to revealedColor.
            if (ShouldUseRGBSync)
            {
                _renderer.color = RGBSyncManager.Instance.GetGroupColor(groupId, colorOffset);
            }
            else
            {
                _fadeCoroutine = StartCoroutine(FadeToColor(revealedColor, fadeDuration));
            }

            // The visibility timer should only apply if the tile is NOT permanently revealed
            // and NOT using RGB sync (since RGB sync typically implies continuous coloring).
            // If permanent or using RGB sync, the tile stays revealed/colored until explicitly reset.
            if (!permanent && !ShouldUseRGBSync)
            {
                _visibilityTimer = StartCoroutine(VisibilityTimer(duration));
            }
        }

        private IEnumerator VisibilityTimer(float duration)
        {
            yield return new WaitForSeconds(duration);
            _isRevealed = false;

            StopCoroutineSafe(ref _fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeToColor(hiddenColor, fadeDuration));
            _visibilityTimer = null;
        }

        private IEnumerator FadeToColor(Color targetColor, float time)
        {
            Color startColor = _renderer.color;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / time;
                _renderer.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            _renderer.color = targetColor;
            _fadeCoroutine = null;
        }
        
        public IEnumerator WaitUntilIdle()
        {
            while (IsBusy)
                yield return null;
        }

        private void StopCoroutineSafe(ref Coroutine coroutine)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
            }
        }

        // This method can be called to dynamically enable/disable RGB sync on a tile
        public void SetRGBSyncEnabled(bool switchOn)
        {
            enableRGBSync = switchOn;

            // Immediately update the tile's color if its state changes and should use RGB
            if (_isRevealed) // Only update if tile is in a 'revealed' state
            {
                if (ShouldUseRGBSync)
                {
                    // Stop any ongoing fade as RGB will take over
                    StopCoroutineSafe(ref _fadeCoroutine);
                    _renderer.color = RGBSyncManager.Instance.GetGroupColor(groupId, colorOffset);
                }
                else
                {
                    // If RGB sync is now disabled but the tile is revealed,
                    // it should revert to its revealedColor (or fade to it)
                    StopCoroutineSafe(ref _fadeCoroutine);
                    _fadeCoroutine = StartCoroutine(FadeToColor(revealedColor, fadeDuration));
                }
            }
            // If the tile is not revealed and RGB sync is being disabled, ensure it goes back to hiddenColor
            else if (!enableRGBSync) // if it's not revealed AND RGB is being turned off
            {
                StopCoroutineSafe(ref _fadeCoroutine);
                _renderer.color = hiddenColor;
            }
        }
        
        // New public methods to set revealed and hidden colors dynamically
        public void SetRevealedColor(Color color)
        {
            revealedColor = color;
            // If the tile is currently revealed and not using RGB, update its color immediately
            if (_isRevealed && !ShouldUseRGBSync)
            {
                StopCoroutineSafe(ref _fadeCoroutine); // Stop any ongoing fade
                _renderer.color = revealedColor; // Apply the new color
            }
        }

        public void SetHiddenColor(Color color)
        {
            hiddenColor = color;
            // If the tile is currently hidden and not using RGB, update its color immediately
            if (!_isRevealed && !ShouldUseRGBSync)
            {
                StopCoroutineSafe(ref _fadeCoroutine); // Stop any ongoing fade
                _renderer.color = hiddenColor; // Apply the new color
            }
        }

        public void ResetTile()
        {
            _isRevealed = false;
            StopCoroutineSafe(ref _fadeCoroutine);
            StopCoroutineSafe(ref _visibilityTimer);
            // When resetting, ensure it goes back to the currently set hiddenColor,
            // unless RGB sync is enabled globally and for this tile, in which case Update() will handle it.
            // However, ResetTile usually means going back to hidden state, so hiddenColor is appropriate here.
            _renderer.color = hiddenColor; 
        }
    }
}