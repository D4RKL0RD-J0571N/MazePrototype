using Level;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Mechanics
{
    public class EchoSystem : MonoBehaviour
    {
        [TitleGroup("Core Echo Settings")]
        [SerializeField, Min(0.1f)]
        [PropertyTooltip("The radius within which maze tiles will be revealed when an echo is emitted.")]
        private float echoRadius = 5f;

        [TitleGroup("Core Echo Settings")]
        [SerializeField]
        [PropertyTooltip("The duration for which temporarily revealed tiles remain visible.")]
        private float revealDuration = 2f;

        [TitleGroup("Core Echo Settings")]
        [SerializeField]
        [PropertyTooltip("The LayerMask used to identify maze tiles when casting the echo sphere.")]
        private LayerMask mazeLayer;

        [TitleGroup("Difficulty Scaling")]
        [SerializeField]
        [PropertyTooltip("A curve to adjust the echo radius based on the selected difficulty (0 = Easy, 0.5 = Normal, 1 = Hard).")]
        private AnimationCurve difficultyEchoRadiusCurve;

        [TitleGroup("Cooldown Settings")]
        [SerializeField, Min(0f)]
        [PropertyTooltip("The minimum time (in seconds) between consecutive echo emissions.")]
        private float echoCooldown = 1f;

        [TitleGroup("Runtime Information")]
        [ShowInInspector]
        [ReadOnly]
        [PropertyTooltip("The last time an echo was successfully emitted.")]
        private float lastEchoTime = -999f;

        [TitleGroup("Runtime Information")]
        [ShowInInspector]
        // FIX APPLIED: Removed LabelFormat
        [ProgressBar(0, 1, ColorGetter = "CooldownProgressBarColor", DrawValueLabel = true)]
        [PropertyTooltip("Current cooldown progress. Displays time remaining until the next echo can be emitted.")]
        private float CurrentEchoCooldownProgress
        {
            get
            {
                if (echoCooldown <= 0) return 1f;
                float timeSinceLastEcho = Time.time - lastEchoTime;
                return Mathf.Clamp01(timeSinceLastEcho / echoCooldown);
            }
        }

        private Color CooldownProgressBarColor => CurrentEchoCooldownProgress >= 1f ? Color.green : Color.yellow;


        private void Start()
        {
            string difficulty = PlayerPrefs.GetString("difficulty", "Normal");

            float difficultyValue;
            switch (difficulty)
            {
                case "Easy":
                    difficultyValue = 0f;
                    break;
                case "Normal":
                    difficultyValue = 0.5f;
                    break;
                case "Hard":
                    difficultyValue = 1f;
                    break;
                default:
                    difficultyValue = 0.5f;
                    break;
            }

            echoRadius = difficultyEchoRadiusCurve.Evaluate(difficultyValue);
            Debug.Log($"EchoSystem initialized. Difficulty: {difficulty}, Final Echo Radius: {echoRadius:F2}");
        }

        [TitleGroup("Debugging Tools")]
        [Button(ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.4f, 0.8f)]
        [PropertyTooltip("Manually emits an echo from the current GameObject's position, ignoring cooldown.")]
        public void ForceEmitEchoFromSelf()
        {
            EmitEcho(transform.position);
            Debug.Log("Forced echo emitted from " + transform.position);
            lastEchoTime = Time.time;
        }

        [TitleGroup("Debugging Tools")]
        [Button(ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 0.4f)]
        [PropertyTooltip("Resets the echo cooldown, allowing an immediate echo.")]
        public void ResetEchoCooldown()
        {
            lastEchoTime = -999f;
            Debug.Log("Echo cooldown reset.");
        }


        public void TryEmitEcho(Vector2 origin)
        {
            if (Time.time - lastEchoTime < echoCooldown)
            {
                return;
            }

            EmitEcho(origin);
            lastEchoTime = Time.time;
        }

        public void EmitEcho(Vector2 origin)
        {
            var hits = Physics2D.OverlapCircleAll(origin, echoRadius, mazeLayer);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out IEchoRevealable revealable))
                {
                    revealable.Reveal(revealDuration);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, echoRadius);
            }
            else
            {
                Gizmos.color = Color.cyan * 0.7f;
                 Gizmos.DrawWireSphere(transform.position, echoRadius);
            }
        }
#endif
    }
}