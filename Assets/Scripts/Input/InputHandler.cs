using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

namespace Input
{
    [DefaultExecutionOrder(-100)] // Ensures this runs very early
    public class InputHandler : MonoBehaviour, PlayerInputActions.IPlayerActions, PlayerInputActions.IUIActions
    {
        // Singleton Instance
        public static InputHandler Instance { get; set; }

        [TitleGroup("Input Actions Setup")]
        [SerializeField]
        [PropertyTooltip("The auto-generated PlayerInputActions asset that defines all input mappings.")]
        [ReadOnly]
        private PlayerInputActions _inputActions;

        // === Input Events ===
        public event Action<Vector2> OnMoveInput;
        public event Action<Vector2> OnLookInput;
        public event Action OnJumpInput;
        public event Action OnSprintStarted;
        public event Action OnSprintCanceled;
        public event Action OnAttackInput;
        public event Action OnInteractInput;
        public event Action OnCrouchInput;
        public event Action OnNextInput;
        public event Action OnPreviousInput;
        public event Action OnCancelInput; // This event will be triggered by Cancel from the UI map

        [TitleGroup("Runtime Status")]
        [ShowInInspector]
        [ReadOnly]
        [PropertyTooltip("Indicates if the Player input action map is currently enabled and receiving input.")]
        public bool IsPlayerInputEnabled => _inputActions is { Player: { enabled: true } };

        [TitleGroup("Runtime Status")]
        [ShowInInspector]
        [ReadOnly]
        [PropertyTooltip("Indicates if the UI input action map is currently enabled and receiving input.")]
        public bool IsUIInputEnabled => _inputActions is { UI: { enabled: true } };


        private void Awake()
        {
            // Debug log to confirm Awake execution
            Debug.Log("InputHandler: Awake called. Attempting to set Instance.", this);

            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // Destroy duplicate
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this instance alive across scenes if needed

            // Initialize input actions
            if (_inputActions == null)
            {
                _inputActions = new PlayerInputActions();
                _inputActions.Player.SetCallbacks(this);
                _inputActions.UI.SetCallbacks(this);
            }
        }

        private void OnEnable()
        {
            if (_inputActions != null)
            {
                _inputActions.Player.Enable();
            }
        }

        private void OnDisable()
        {
            if (_inputActions != null)
            {
                _inputActions.Player.Disable();
                _inputActions.UI.Disable();
            }
        }

        private void OnDestroy()
        {
            // Clean up when destroyed (especially important if DontDestroyOnLoad is used)
            if (_inputActions != null)
            {
                _inputActions.Dispose();
                _inputActions = null;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // === Public methods to control action maps ===
        // Null checks here are still useful as defensive programming, though with singleton they are less likely to trigger.
        public void EnablePlayerActions()
        {
            if (_inputActions != null) _inputActions.Player.Enable();
            else Debug.LogWarning("InputActions not initialized when trying to enable Player actions.");
        }
        public void DisablePlayerActions()
        {
            if (_inputActions != null) _inputActions.Player.Disable();
            else Debug.LogWarning("InputActions not initialized when trying to disable Player actions.");
        }
        public void EnableUIActions()
        {
            if (_inputActions != null) _inputActions.UI.Enable();
            else Debug.LogWarning("InputActions not initialized when trying to enable UI actions.");
        }
        public void DisableUIActions()
        {
            if (_inputActions != null) _inputActions.UI.Disable();
            else Debug.LogWarning("InputActions not initialized when trying to disable UI actions.");
        }


        // === Input Callback Implementations (for IPlayerActions) ===
        public void OnMove(InputAction.CallbackContext context)
        {
            if (context.performed || context.canceled)
                OnMoveInput?.Invoke(context.ReadValue<Vector2>());
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if (context.performed || context.canceled)
                OnLookInput?.Invoke(context.ReadValue<Vector2>());
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnJumpInput?.Invoke();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (context.started) OnSprintStarted?.Invoke();
            else if (context.canceled) OnSprintCanceled?.Invoke();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnAttackInput?.Invoke();
        }

        public void OnInteract(InputAction.CallbackContext context)
            {
                if (context.performed)
                    OnInteractInput?.Invoke();
            }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnCrouchInput?.Invoke();
        }

        public void OnNext(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnNextInput?.Invoke();
        }

        public void OnPrevious(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnPreviousInput?.Invoke();
        }

        // === Input Callback Implementations (for IUIActions) ===
        public void OnNavigate(InputAction.CallbackContext context) { }
        public void OnSubmit(InputAction.CallbackContext context) { }
        public void OnPoint(InputAction.CallbackContext context) { }
        public void OnClick(InputAction.CallbackContext context) { }
        public void OnRightClick(InputAction.CallbackContext context) { }
        public void OnMiddleClick(InputAction.CallbackContext context) { }
        public void OnScrollWheel(InputAction.CallbackContext context) { }
        public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }
        public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }

        // Explicit interface implementation for IUIActions.OnCancel
        void PlayerInputActions.IUIActions.OnCancel(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnCancelInput?.Invoke();
        }

        [TitleGroup("Debugging Tools")]
        [Button(ButtonSizes.Medium)]
        [PropertyTooltip("Prints a list of all actions and their bound controls in the 'Player' action map to the console.")]
        [GUIColor(0.8f, 0.9f, 1f)]
        private void PrintBindings()
        {
            if (_inputActions == null)
            {
                Debug.LogWarning("InputActions not initialized yet.");
                return;
            }

            Debug.Log($"--- Player Action Map Bindings ({_inputActions.Player}) ---");
            foreach (var action in _inputActions.Player.Get())
            {
                string bindingInfo = "";
                foreach (var binding in action.bindings)
                {
                    bindingInfo += $"  [{binding.groups}] {binding.path}";
                    if (binding.isComposite) bindingInfo += " (Composite)";
                    bindingInfo += "\n";
                }
                Debug.Log($"{action.name}:\n{bindingInfo.Trim()}");
            }
            Debug.Log("\n--- UI Action Map Bindings ---");
            foreach (var action in _inputActions.UI.Get())
            {
                string bindingInfo = "";
                foreach (var binding in action.bindings)
                {
                    bindingInfo += $"  [{binding.groups}] {binding.path}";
                    if (binding.isComposite) bindingInfo += " (Composite)";
                    bindingInfo += "\n";
                }
                Debug.Log($"{action.name}:\n{bindingInfo.Trim()}");
            }
            Debug.Log("-------------------------------------");
        }
    }
}