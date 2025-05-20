using Input;
using Level;
using Mechanics;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Player
{
    
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [TitleGroup("Movement Settings")] // Group movement-related fields
        [SerializeField, Min(0f)]
        [PropertyTooltip("The speed at which the player moves.")]
        private float moveSpeed = 5f;

        [TitleGroup("Movement Settings")]
        [SerializeField]
        [ToggleLeft] // Nicer toggle appearance
        [PropertyTooltip("If true, player input is currently active and can control movement.")]
        private bool allowInput = true;

        [TitleGroup("Dependencies")] // Group essential references
        [SerializeField]
        [PropertyTooltip("Reference to the EchoSystem component to trigger echo abilities.")]
        private EchoSystem echoSystem;

        // Internal References - Marked ReadOnly
        // [TitleGroup("Runtime References")]
        // [ReadOnly]
        // [PropertyTooltip("Automatically found InputHandler component on this GameObject.")]
        // private InputHandler _input; // This line is correctly commented out

        [TitleGroup("Runtime References")]
        [ReadOnly]
        [PropertyTooltip("Automatically found Rigidbody2D component on this GameObject.")]
        private Rigidbody2D _rigidbody;

        // Runtime State - Show in Inspector as ReadOnly
        [TitleGroup("Runtime State")]
        [ShowInInspector] // Show this private field in the Inspector
        [ReadOnly]
        [PropertyTooltip("The current raw input vector received from the InputHandler.")]
        private Vector2 _moveInput;

        [TitleGroup("Runtime State")]
        [ShowInInspector] // Show this property in the Inspector
        [ReadOnly]
        [PropertyTooltip("The calculated velocity applied to the Rigidbody2D based on input and move speed.")]
        public Vector2 Velocity => _moveInput * moveSpeed;

        // Odin-specific methods for controlling input state
        [TitleGroup("Input Control")]
        [Button(ButtonSizes.Medium)] // Create a button
        [GUIColor(0.4f, 0.8f, 0.4f)] // Greenish color
        [ShowIf("@!allowInput")] // Only show if input is currently disabled
        [PropertyTooltip("Enables player input, allowing movement.")]
        public void EnableInputInInspector()
        {
            SetInputEnabled(true);
            Debug.Log("Player input enabled via Inspector.");
        }

        [TitleGroup("Input Control")]
        [Button(ButtonSizes.Medium)] // Create a button
        [GUIColor(0.8f, 0.4f, 0.4f)] // Reddish color
        [ShowIf("@allowInput")] // Only show if input is currently enabled
        [PropertyTooltip("Disables player input, preventing movement.")]
        public void DisableInputInInspector()
        {
            SetInputEnabled(false);
            Debug.Log("Player input disabled via Inspector.");
        }

        // Public method to set input state (used by internal logic and Odin buttons)
        public void SetInputEnabled(bool enabled)
        {
            allowInput = enabled;
            // Optionally, you might want to stop movement immediately when input is disabled
            if (!allowInput)
            {
                _moveInput = Vector2.zero; // Clear stored input
                if (_rigidbody != null)
                {
                    _rigidbody.linearVelocity = Vector2.zero; // Stop current movement
                }
            }
        }

        private void Awake()
        {
            // _input = GetComponent<InputHandler>(); // This line is correctly commented out
            _rigidbody = GetComponent<Rigidbody2D>();

            // Ensure the rigidbody is found
            if (_rigidbody == null) Debug.LogError("Rigidbody2D not found on PlayerController!", this);
        }

        private void OnEnable()
        {
            // Subscribe to input events using the global instance
            if (InputHandler.Instance != null) // Access InputHandler.Instance directly
            {
                InputHandler.Instance.OnMoveInput += OnMove;
                InputHandler.Instance.OnAttackInput += OnEcho;
            }
            // Subscribe to game events
            GameEvents.OnPlayerReachedExit += DisableInput;
        }

        private void OnDisable()
        {
            // Unsubscribe from input events using the global instance
            if (InputHandler.Instance != null) // Access InputHandler.Instance directly
            {
                InputHandler.Instance.OnMoveInput -= OnMove;
                InputHandler.Instance.OnAttackInput -= OnEcho;
            }
            // Unsubscribe from game events
            GameEvents.OnPlayerReachedExit -= DisableInput;
        }

        private void FixedUpdate()
        {
            if (!allowInput)
            {
                // Ensure velocity is zero when input is not allowed
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            _rigidbody.linearVelocity = _moveInput * moveSpeed;
        }

        private void OnMove(Vector2 direction)
        {
            // Only update _moveInput if input is allowed
            if (allowInput)
            {
                _moveInput = direction;
            }
            else
            {
                _moveInput = Vector2.zero; // Clear input if not allowed
            }
        }

        private void OnEcho()
        {
            // Only allow echo if input is allowed
            if (allowInput && echoSystem != null)
                echoSystem.TryEmitEcho(transform.position);
        }

        // This method is called by GameEvents.OnPlayerReachedExit
        private void DisableInput()
        {
            SetInputEnabled(false);
            Debug.Log("Player input disabled due to reaching exit.");
        }
    }
}