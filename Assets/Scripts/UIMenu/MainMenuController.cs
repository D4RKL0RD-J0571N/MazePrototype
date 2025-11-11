using Input;
using Level;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Misc.RGB;
using Sirenix.OdinInspector;

namespace UIMenu
{
    [DefaultExecutionOrder(-90)] // Ensures this script's OnEnable runs early
    public class MainMenuController : MonoBehaviour
    {
        [TitleGroup("Dependencies")]
        [SerializeField]
        [PropertyTooltip("The UIDocument component that holds the UI structure.")]
        public UIDocument uiDocument;
        [SerializeField]
        [PropertyTooltip("Reference to the MazeGenerator to start new mazes.")]
        private MazeGenerator mazeGenerator;
        [PropertyTooltip("Reference to the PlayerController to disable/enable input when pausing/unpausing.")]
        [SerializeField] // Keep this serialized if PlayerController is not a singleton
        private Player.PlayerController playerController;
        
        // InputHandler is a singleton, so we assign it in OnEnable.
        [PropertyTooltip("Reference to the InputHandler singleton. Assigned programmatically.")]
        private InputHandler _inputHandler; 

        // UI Element References - Marked ReadOnly as they are assigned in OnEnable
        [TitleGroup("UI Element References")]
        [BoxGroup("UI Element References/Root Elements")] // Nested group
        [ReadOnly]
        private VisualElement _rootElement;
        [BoxGroup("UI Element References/Root Elements")]
        [ReadOnly]
        private VisualElement _mainMenuPanel;
        [BoxGroup("UI Element References/Root Elements")]
        [ReadOnly]
        private VisualElement _optionsPanel;
        [BoxGroup("UI Element References/Root Elements")]
        [ReadOnly]
        private VisualElement _exitMenuPanel; // NEW: Exit Menu Panel
        [BoxGroup("UI Element References/Root Elements")]
        [ReadOnly]
        private Label _mainTitleLabel;

        [BoxGroup("UI Element References/Main Menu Buttons")]
        [ReadOnly]
        private Button _startButton;
        [BoxGroup("UI Element References/Main Menu Buttons")]
        [ReadOnly]
        private Button _optionsButton;
        [BoxGroup("UI Element References/Main Menu Buttons")]
        [ReadOnly]
        private Button _exitButton;

        // NEW: Exit Menu Buttons and Toggle
        [BoxGroup("UI Element References/Exit Menu Buttons")]
        [ReadOnly]
        private Button _exitNewMazeButton;
        [BoxGroup("UI Element References/Exit Menu Buttons")]
        [ReadOnly]
        private Button _exitReturnToMainMenuButton;
        [BoxGroup("UI Element References/Exit Menu Buttons")]
        [ReadOnly]
        private Toggle _exitAutoGenerateToggle;


        [BoxGroup("UI Element References/Options Panel Fields")]
        [ReadOnly]
        private IntegerField _widthField;
        [BoxGroup("UI Element References/Options Panel Fields")]
        [ReadOnly]
        private IntegerField _heightField;
        [BoxGroup("UI Element References/Options Panel Fields")]
        [ReadOnly]
        private IntegerField _seedField;
        [BoxGroup("UI Element References/Options Panel Fields")]
        [ReadOnly]
        private Toggle _autoPlaceToggle;
        [BoxGroup("UI Element References/Options Panel Fields")]
        [ReadOnly]
        private Toggle _rgbToggle;
        [BoxGroup("UI Element References/Options Panel Fields")]
        [ReadOnly]
        private Slider _cycleSpeedSlider, _saturationSlider, _brightnessSlider;
        [BoxGroup("UI Element References/Options Panel Fields")]
        [ReadOnly]
        private DropdownField _difficultyDropdown;
        [BoxGroup("UI Element References/Options Panel Fields")]
        [ReadOnly]
        private DropdownField _controlModeDropdown;
        [BoxGroup("UI Element References/Options Panel Fields")]
        [ReadOnly]
        private Button _applyButton;
        [BoxGroup("UI Element References/Options Panel Fields")]
        [ReadOnly]
        private Button _backButton;

        // NEW: Tile Color Customization Fields
        [BoxGroup("UI Element References/Color Settings")]
        [ReadOnly]
        private VisualElement _floorColorDisplay;
        [ReadOnly] private Slider _floorRSlider, _floorGSlider, _floorBSlider;
        [BoxGroup("UI Element References/Color Settings")]
        [ReadOnly]
        private VisualElement _wallColorDisplay;
        [ReadOnly] private Slider _wallRSlider, _wallGSlider, _wallBSlider;


        // Info box to explain ReadOnly fields
        [InfoBox("UI Element references below are assigned programmatically in OnEnable and should not be modified directly in the Inspector.", InfoMessageType.None, VisibleIf = "@_rootElement == null")]
        [InfoBox("These fields display the dynamically assigned UI Elements. Do not modify them directly.", InfoMessageType.Info, VisibleIf = "@_rootElement != null")]

        // Runtime State
        [TitleGroup("Runtime State")]
        [ShowInInspector]
        [ReadOnly]
        [PropertyTooltip("True if any UI panel (Main Menu, Options, etc.) is currently visible.")]
        private bool _isMenuShown;


        void OnEnable()
        {
            _inputHandler = InputHandler.Instance;
            if (_inputHandler == null)
            {
                Debug.LogError("MainMenuController: InputHandler.Instance is null. Ensure InputHandler is initialized before MainMenuController.", this);
                enabled = false; // Disable this script if essential dependency is missing
                return;
            }

            if (uiDocument == null)
            {
                Debug.LogError("MainMenuController: UIDocument is not assigned. Cannot initialize UI.", this);
                enabled = false;
                return;
            }

            // IMPORTANT: Add checks for MazeGenerator and PlayerController here
            if (mazeGenerator == null)
            {
                Debug.LogError("MainMenuController: MazeGenerator is not assigned. Please assign it in the Inspector.", this);
                enabled = false;
                return;
            }
            if (playerController == null)
            {
                Debug.LogError("MainMenuController: PlayerController is not assigned. Please assign it in the Inspector.", this);
                enabled = false;
                return;
            }
            // -------------------------------------------------------------------------

            _rootElement = uiDocument.rootVisualElement;
            if (_rootElement == null)
            {
                Debug.LogError("MainMenuController: Root VisualElement not found. Check UIDocument setup.", this);
                enabled = false;
                return;
            }

            // Assign UI elements (rest of your UI element assignments are fine)
            _mainTitleLabel = _rootElement.Q<Label>("main-title");
            _mainMenuPanel = _rootElement.Q<VisualElement>("main-menu");
            _optionsPanel = _rootElement.Q<VisualElement>("options-panel");
            _exitMenuPanel = _rootElement.Q<VisualElement>("exit-menu-panel"); 

            // Main Menu Elements
            _startButton = _rootElement.Q<Button>("start-button");
            _optionsButton = _rootElement.Q<Button>("options-button");
            _exitButton = _rootElement.Q<Button>("exit-button");

            // Exit Menu Elements
            _exitNewMazeButton = _rootElement.Q<Button>("exit-new-maze-button");
            _exitReturnToMainMenuButton = _rootElement.Q<Button>("exit-main-menu-button");
            _exitAutoGenerateToggle = _rootElement.Q<Toggle>("auto-generate-toggle");

            // Options Panel Elements
            _widthField = _rootElement.Q<IntegerField>("width-field");
            _heightField = _rootElement.Q<IntegerField>("height-field");
            _seedField = _rootElement.Q<IntegerField>("seed-field");
            _autoPlaceToggle = _rootElement.Q<Toggle>("auto-place-toggle");
            _rgbToggle = _rootElement.Q<Toggle>("rgb-toggle");
            _cycleSpeedSlider = _rootElement.Q<Slider>("cycle-speed-slider");
            _saturationSlider = _rootElement.Q<Slider>("saturation-slider");
            _brightnessSlider = _rootElement.Q<Slider>("brightness-slider");
            _difficultyDropdown = _rootElement.Q<DropdownField>("difficulty-dropdown");
            _controlModeDropdown = _rootElement.Q<DropdownField>("control-mode-dropdown");
            _applyButton = _rootElement.Q<Button>("apply-button");
            _backButton = _rootElement.Q<Button>("back-button");

            // Tile Color Customization Elements
            _floorColorDisplay = _rootElement.Q<VisualElement>("floor-color-display");
            _floorRSlider = _rootElement.Q<Slider>("floor-r-slider");
            _floorGSlider = _rootElement.Q<Slider>("floor-g-slider");
            _floorBSlider = _rootElement.Q<Slider>("floor-b-slider");

            _wallColorDisplay = _rootElement.Q<VisualElement>("wall-color-display");
            _wallRSlider = _rootElement.Q<Slider>("wall-r-slider");
            _wallGSlider = _rootElement.Q<Slider>("wall-g-slider");
            _wallBSlider = _rootElement.Q<Slider>("wall-b-slider");


            // --- Populate UI Element Text and Properties ---
            _mainTitleLabel.text = "ECHO MAZE";
            _startButton.text = "Start Game";
            _optionsButton.text = "Options";
            _exitButton.text = "Exit";

            _exitNewMazeButton.text = "New Maze";
            _exitReturnToMainMenuButton.text = "Main Menu";
            _exitAutoGenerateToggle.label = "Auto-Generate on Exit";

            _widthField.label = "MAZE WIDTH";
            _heightField.label = "MAZE HEIGHT";
            _seedField.label = "SEED";
            _autoPlaceToggle.label = "AUTO PLACE START/END";
            _rgbToggle.label = "ENABLE RGB CYCLE";
            _cycleSpeedSlider.label = "CYCLE SPEED";
            _saturationSlider.label = "SATURATION";
            _brightnessSlider.label = "BRIGHTNESS";
            _difficultyDropdown.label = "DIFFICULTY";
            _controlModeDropdown.label = "CONTROL MODE";
            _applyButton.text = "Apply Settings";
            _backButton.text = "Back to Main Menu";

            _cycleSpeedSlider.lowValue = 0.1f;
            _cycleSpeedSlider.highValue = 10f;
            _saturationSlider.lowValue = 0f;
            _saturationSlider.highValue = 1f;
            _brightnessSlider.lowValue = 0f;
            _brightnessSlider.highValue = 1f;

            _floorRSlider.lowValue = 0f; _floorRSlider.highValue = 1f; _floorRSlider.label = "R";
            _floorGSlider.lowValue = 0f; _floorGSlider.highValue = 1f; _floorGSlider.label = "G";
            _floorBSlider.lowValue = 0f; _floorBSlider.highValue = 1f; _floorBSlider.label = "B";

            _wallRSlider.lowValue = 0f; _wallRSlider.highValue = 1f; _wallRSlider.label = "R";
            _wallGSlider.lowValue = 0f; _wallGSlider.highValue = 1f; _wallGSlider.label = "G";
            _wallBSlider.lowValue = 0f; _wallBSlider.highValue = 1f; _wallBSlider.label = "B";
            // --- End Populate UI Element Text and Properties ---

            // Register event callbacks
            _inputHandler.OnCancelInput += HandleCancel;
            GameEvents.OnPlayerReachedExit += HandlePlayerReachedExit;

            // Load saved settings (includes RGB from RGBSyncManager)
            LoadSettings();

            // Set up dropdown options (can be done once as they are static)
            _difficultyDropdown.choices = new List<string> { "Easy", "Normal", "Hard" };
            _controlModeDropdown.choices = new List<string> { "Keyboard", "Gamepad" };

            // Initialize UI state: Show main menu and set input maps accordingly
            ShowPanel(_mainMenuPanel);
            SetGamePaused(true); 

            // Register button callbacks
            _startButton.clicked += OnStartButtonClicked;
            _optionsButton.clicked += OnOptionsButtonClicked;
            _exitButton.clicked += OnExitButtonClicked;
            _applyButton.clicked += OnApplyButtonClicked;
            _backButton.clicked += OnBackButtonClicked;

            // Register Exit Menu button callbacks
            _exitNewMazeButton.clicked += OnExitNewMazeButtonClicked;
            _exitReturnToMainMenuButton.clicked += OnExitReturnToMainMenuButtonClicked;
            _exitAutoGenerateToggle.RegisterValueChangedCallback(evt => PlayerPrefs.SetInt("autoGenerateOnExit", evt.newValue ? 1 : 0));

            // Register color slider callbacks to update display immediately
            _floorRSlider.RegisterValueChangedCallback(evt => UpdateColorDisplay(_floorRSlider, _floorGSlider, _floorBSlider, _floorColorDisplay));
            _floorGSlider.RegisterValueChangedCallback(evt => UpdateColorDisplay(_floorRSlider, _floorGSlider, _floorBSlider, _floorColorDisplay));
            _floorBSlider.RegisterValueChangedCallback(evt => UpdateColorDisplay(_floorRSlider, _floorGSlider, _floorBSlider, _floorColorDisplay));
            _wallRSlider.RegisterValueChangedCallback(evt => UpdateColorDisplay(_wallRSlider, _wallGSlider, _wallBSlider, _wallColorDisplay));
            _wallGSlider.RegisterValueChangedCallback(evt => UpdateColorDisplay(_wallRSlider, _wallGSlider, _wallBSlider, _wallColorDisplay));
            _wallBSlider.RegisterValueChangedCallback(evt => UpdateColorDisplay(_wallRSlider, _wallGSlider, _wallBSlider, _wallColorDisplay));
        }

        void OnDisable()
        {
            // Unsubscribe from events to prevent memory leaks
            if (_inputHandler != null)
            {
                _inputHandler.OnCancelInput -= HandleCancel;
            }
            GameEvents.OnPlayerReachedExit -= HandlePlayerReachedExit;

            // Unregister button callbacks
            _startButton.clicked -= OnStartButtonClicked;
            _optionsButton.clicked -= OnOptionsButtonClicked;
            _exitButton.clicked -= OnExitButtonClicked;
            _applyButton.clicked -= OnApplyButtonClicked;
            _backButton.clicked -= OnBackButtonClicked;

            // Unregister Exit Menu button callbacks
            _exitNewMazeButton.clicked -= OnExitNewMazeButtonClicked;
            _exitReturnToMainMenuButton.clicked -= OnExitReturnToMainMenuButtonClicked;
            _exitAutoGenerateToggle.UnregisterValueChangedCallback(evt => PlayerPrefs.SetInt("autoGenerateOnExit", evt.newValue ? 1 : 0)); // Unregister

            // Unregister color slider callbacks
            _floorRSlider.UnregisterValueChangedCallback(evt => UpdateColorDisplay(_floorRSlider, _floorGSlider, _floorBSlider, _floorColorDisplay));
            _floorGSlider.UnregisterValueChangedCallback(evt => UpdateColorDisplay(_floorRSlider, _floorGSlider, _floorBSlider, _floorColorDisplay));
            _floorBSlider.UnregisterValueChangedCallback(evt => UpdateColorDisplay(_floorRSlider, _floorGSlider, _floorBSlider, _floorColorDisplay));
            _wallRSlider.UnregisterValueChangedCallback(evt => UpdateColorDisplay(_wallRSlider, _wallGSlider, _wallBSlider, _wallColorDisplay));
            _wallGSlider.UnregisterValueChangedCallback(evt => UpdateColorDisplay(_wallRSlider, _wallGSlider, _wallBSlider, _wallColorDisplay));
            _wallBSlider.UnregisterValueChangedCallback(evt => UpdateColorDisplay(_wallRSlider, _wallGSlider, _wallBSlider, _wallColorDisplay));
        }

        // Odin Debugging Buttons (remain unchanged)
        [TitleGroup("Debug UI Control")]
        [Button(ButtonSizes.Medium)]
        [GUIColor(0.7f, 1f, 0.7f)]
        [ShowIf("@!_isMenuShown")]
        [PropertyTooltip("Forcefully displays the main menu and pauses the game.")]
        private void ShowMainMenuInInspector()
        {
            ShowPanel(_mainMenuPanel);
            SetGamePaused(true);
            Debug.Log("Main Menu forced shown via Inspector.");
        }

        [TitleGroup("Debug UI Control")]
        [Button(ButtonSizes.Medium)]
        [GUIColor(1f, 0.7f, 0.7f)]
        [ShowIf("@_isMenuShown")]
        [PropertyTooltip("Forcefully hides all UI panels and resumes the game.")]
        private void HideMenuInInspector()
        {
            HideAllPanels();
            SetGamePaused(false);
            Debug.Log("Menu forced hidden via Inspector.");
        }

        private void LoadSettings()
        {
            // Load maze settings into UI fields AND MazeGenerator's properties
            _widthField.value = PlayerPrefs.GetInt("mazeWidth", mazeGenerator.Width);
            _heightField.value = PlayerPrefs.GetInt("mazeHeight", mazeGenerator.Height);
            _seedField.value = PlayerPrefs.GetInt("mazeSeed", mazeGenerator.Seed);
            _autoPlaceToggle.value = PlayerPrefs.GetInt("autoPlace", mazeGenerator.AutoPlaceStartEnd ? 1 : 0) == 1;

            // For MazeGenerator's public fields, directly assign them here on load
            // This ensures the Inspector values of MazeGenerator reflect loaded settings
            mazeGenerator.Width = _widthField.value;
            mazeGenerator.Height = _heightField.value;
            mazeGenerator.Seed = _seedField.value;
            mazeGenerator.AutoPlaceStartEnd = _autoPlaceToggle.value;


            // Load RGB settings from RGBSyncManager's current state
            if (RGBSyncManager.Instance != null)
            {
                _rgbToggle.value = RGBSyncManager.Instance.CurrentSettings.Enabled;
                _cycleSpeedSlider.value = RGBSyncManager.Instance.CurrentSettings.CycleSpeed;
                _saturationSlider.value = RGBSyncManager.Instance.CurrentSettings.Saturation;
                _brightnessSlider.value = RGBSyncManager.Instance.CurrentSettings.Brightness;
            }

            // Load dropdown values, ensuring they exist in choices
            string savedDifficulty = PlayerPrefs.GetString("difficulty", "Normal");
            _difficultyDropdown.index = _difficultyDropdown.choices.IndexOf(savedDifficulty);
            if (_difficultyDropdown.index == -1) _difficultyDropdown.index = 1; // Default to Normal if not found

            string savedControlMode = PlayerPrefs.GetString("controlMode", "Keyboard");
            _controlModeDropdown.index = _controlModeDropdown.choices.IndexOf(savedControlMode);
            if (_controlModeDropdown.index == -1) _controlModeDropdown.index = 0; // Default to Keyboard if not found

            // Load auto-generate on exit toggle
            _exitAutoGenerateToggle.value = PlayerPrefs.GetInt("autoGenerateOnExit", 0) == 1; // Default to false

            // Load tile colors
            Color loadedFloorColor = GetColorFromPlayerPrefs("floorColor", Color.white);
            SetSlidersFromColor(loadedFloorColor, _floorRSlider, _floorGSlider, _floorBSlider, _floorColorDisplay);

            Color loadedWallColor = GetColorFromPlayerPrefs("wallColor", Color.black);
            SetSlidersFromColor(loadedWallColor, _wallRSlider, _wallGSlider, _wallBSlider, _wallColorDisplay);

            // Apply loaded colors to maze generator immediately
            if (mazeGenerator != null)
            {
                mazeGenerator.SetTileColors(loadedFloorColor, loadedWallColor);
            }
        }

        private void SaveSettings()
        {
            // Save maze settings from UI fields to PlayerPrefs
            PlayerPrefs.SetInt("mazeWidth", _widthField.value);
            PlayerPrefs.SetInt("mazeHeight", _heightField.value);
            PlayerPrefs.SetInt("mazeSeed", _seedField.value);
            PlayerPrefs.SetInt("autoPlace", _autoPlaceToggle.value ? 1 : 0);

            // OPTIONAL: Also update MazeGenerator's public fields directly here,
            // so if you check its Inspector, they reflect the saved values.
            // This is primarily for visual debugging/inspection in the editor.
            if (mazeGenerator != null)
            {
                mazeGenerator.Width = _widthField.value;
                mazeGenerator.Height = _heightField.value;
                mazeGenerator.Seed = _seedField.value;
                mazeGenerator.AutoPlaceStartEnd = _autoPlaceToggle.value;
            }


            // Create a temporary RGBEffectSettings object from UI values
            RGBEffectSettings currentRgbUISettings = new RGBEffectSettings
            {
                Enabled = _rgbToggle.value,
                CycleSpeed = _cycleSpeedSlider.value,
                Saturation = _saturationSlider.value,
                Brightness = _brightnessSlider.value
            };

            // Apply RGB settings via RGBSyncManager and save them
            if (RGBSyncManager.Instance != null)
            {
                RGBSyncManager.Instance.ApplySettings(currentRgbUISettings);
                RGBSyncManager.Instance.SaveSettingsToPlayerPrefs();
            }

            // Save dropdown values
            PlayerPrefs.SetString("difficulty", _difficultyDropdown.value);
            PlayerPrefs.SetString("controlMode", _controlModeDropdown.value);

            // Save tile colors
            Color currentFloorColor = GetColorFromSliders(_floorRSlider, _floorGSlider, _floorBSlider);
            SaveColorToPlayerPrefs("floorColor", currentFloorColor);

            Color currentWallColor = GetColorFromSliders(_wallRSlider, _wallGSlider, _wallBSlider);
            SaveColorToPlayerPrefs("wallColor", currentWallColor);
            
            // Call MazeGenerator to apply new colors (MazeGenerator will then update MazeTiles)
            if (mazeGenerator != null)
            {
                mazeGenerator.SetTileColors(currentFloorColor, currentWallColor);
            }

            PlayerPrefs.Save(); // Persist all PlayerPrefs changes
            Debug.Log("Settings Applied!");
        }

        private void OnStartButtonClicked()
        {
            Debug.Log("OnStartButtonClicked: Start button clicked.");
            SaveSettings(); // Ensure all latest settings are saved and applied

            // Immediately remove the fade-out class to ensure UI doesn't block later actions
            _rootElement.RemoveFromClassList("fade-out"); 
            // Then, add the fade-out class if you still want a visual transition
            _rootElement.AddToClassList("fade-out"); // Assumes CSS handles the fade effect

            // Instead of Invoke, directly call a coroutine helper.
            // This bypasses issues with Time.timeScale affecting Invoke.
            StartCoroutine(StartGameRoutine()); 
        }

        private System.Collections.IEnumerator StartGameRoutine()
        {
            // Give a short delay to allow the fade-out CSS to start
            yield return new WaitForSecondsRealtime(0.5f); // Use Realtime to ignore Time.timeScale

            GenerateMazeAndResumeGame();
        }


        private void OnOptionsButtonClicked()
        {
            Debug.Log("OnOptionsButtonClicked: Options button clicked.");
            ShowPanel(_optionsPanel);
            // Ensure UI sliders reflect current active RGB settings when opening options
            if (RGBSyncManager.Instance != null)
            {
                _rgbToggle.value = RGBSyncManager.Instance.CurrentSettings.Enabled;
                _cycleSpeedSlider.value = RGBSyncManager.Instance.CurrentSettings.CycleSpeed;
                _saturationSlider.value = RGBSyncManager.Instance.CurrentSettings.Saturation;
                _brightnessSlider.value = RGBSyncManager.Instance.CurrentSettings.Brightness;
            }
        }

        private void OnExitButtonClicked()
        {
            Debug.Log("OnExitButtonClicked: Exit button clicked. Quitting application.");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // Stop play mode in editor
#endif
        }

        private void OnApplyButtonClicked()
        {
            Debug.Log("OnApplyButtonClicked: Apply Settings button clicked.");
            SaveSettings();
        }

        private void OnBackButtonClicked()
        {
            Debug.Log("OnBackButtonClicked: Back button clicked.");
            ShowPanel(_mainMenuPanel);
        }

        // Handlers for Exit Menu buttons
        private void OnExitNewMazeButtonClicked()
        {
            Debug.Log("OnExitNewMazeButtonClicked: New Maze button clicked (from Exit Menu).");
            SaveSettings(); // Ensure all latest settings are saved and applied
            _rootElement.AddToClassList("fade-out");
            StartCoroutine(StartGameRoutine()); // Use the same routine for consistency
        }

        private void OnExitReturnToMainMenuButtonClicked()
        {
            Debug.Log("OnExitReturnToMainMenuButtonClicked: Return to Main Menu button clicked.");
            ShowPanel(_mainMenuPanel);
            SetGamePaused(true); // Ensure UI input is active for main menu
        }

        /// <summary>
        /// Handles the Cancel input from the player (typically Escape key or B button on gamepad).
        /// Acts as a back button for options, or a pause/resume toggle for the game.
        /// </summary>
        private void HandleCancel()
        {
            Debug.Log("HandleCancel: Cancel input detected.");
            // If Options panel is visible, pressing Cancel goes back to Main Menu
            if (_optionsPanel.resolvedStyle.display == DisplayStyle.Flex)
            {
                ShowPanel(_mainMenuPanel);
            }
            // If Exit Menu panel is visible, pressing Cancel goes back to Main Menu
            else if (_exitMenuPanel.resolvedStyle.display == DisplayStyle.Flex)
            {
                ShowPanel(_mainMenuPanel);
            }
            // If Main Menu is visible (and not Options/Exit), pressing Cancel hides the menu (unpauses)
            else if (_mainMenuPanel.resolvedStyle.display == DisplayStyle.Flex)
            {
                // Only allow hiding the menu if the game is currently paused (meaning we're in-game and accessed menu)
                if (Time.timeScale == 0f) 
                {
                    HideAllPanels();
                    SetGamePaused(false); // Unpause the game
                }
            }
            // If no menu is visible (i.e., in game), pressing Cancel shows the Main Menu (pauses)
            else
            {
                ShowPanel(_mainMenuPanel);
                SetGamePaused(true); // Pause the game
            }
        }

        /// <summary>
        /// Manages which UI panel is visible and hides others. Also updates _isMenuShown state.
        /// Does NOT manage Time.timeScale or input maps directly; `SetGamePaused` handles that.
        /// </summary>
        /// <param name="panelToShow">The VisualElement panel to display. If null, all panels are hidden.</param>
        private void ShowPanel(VisualElement panelToShow)
        {
            Debug.Log($"ShowPanel: Attempting to show {panelToShow?.name ?? "null"} panel.");
            _mainMenuPanel.style.display = DisplayStyle.None;
            _optionsPanel.style.display = DisplayStyle.None;
            _exitMenuPanel.style.display = DisplayStyle.None; 
            _backButton.style.display = DisplayStyle.None; 

            if (panelToShow != null)
            {
                _rootElement.style.display = DisplayStyle.Flex; // Show the root UI if any panel is to be shown
                
                panelToShow.style.display = DisplayStyle.Flex; // Show the requested panel directly

                if (panelToShow == _optionsPanel)
                {
                    _backButton.style.display = DisplayStyle.Flex; // Show back button only for options
                }
            }
            else
            {
                HideAllPanels(); 
            }
             Debug.Log($"ShowPanel: {_mainMenuPanel.name} display: {_mainMenuPanel.style.display}, {_optionsPanel.name} display: {_optionsPanel.style.display}, {_exitMenuPanel.name} display: {_exitMenuPanel.style.display}");
        }

        /// <summary>
        /// Hides all UI panels and sets _isMenuShown to false.
        /// Does NOT manage Time.timeScale or input maps directly; `SetGamePaused` handles that.
        /// </summary>
        private void HideAllPanels()
        {
            Debug.Log("HideAllPanels: Hiding all UI panels.");
            _mainMenuPanel.style.display = DisplayStyle.None;
            _optionsPanel.style.display = DisplayStyle.None;
            _exitMenuPanel.style.display = DisplayStyle.None; 
            _backButton.style.display = DisplayStyle.None;
            _rootElement.style.display = DisplayStyle.None; // Hide the root UI element
        }

        /// <summary>
        /// Sets the game's paused state, manages Time.timeScale,
        /// and switches between Gameplay and UI input action maps.
        /// This method is now public for external calls if needed (e.g., by GameManager).
        /// </summary>
        /// <param name="isPaused">True to pause the game, false to unpause.</param>
        public void SetGamePaused(bool isPaused)
        {
            _isMenuShown = isPaused; // Update internal state based on pause status
            Debug.Log($"SetGamePaused: isPaused = {isPaused}");

            if (isPaused)
            {
                Time.timeScale = 0f; // Stop game time
                _inputHandler.EnableUIActions(); // Enable UI input actions
                _inputHandler.DisablePlayerActions(); // Disable gameplay input actions
                if (playerController != null) 
                {
                    playerController.SetInputEnabled(false); // Ensure player input is disabled
                }
                else
                {
                    Debug.LogWarning("SetGamePaused: PlayerController is null. Cannot disable player input.");
                }
                Debug.Log($"Game Paused. Time.timeScale: {Time.timeScale}. Input: UI Enabled, Player Disabled.");
            }
            else
            {
                Time.timeScale = 1f; // Resume game time
                _inputHandler.EnablePlayerActions(); // Enable gameplay input actions
                _inputHandler.DisableUIActions(); // Disable UI input actions
                if (playerController != null) 
                {
                    playerController.SetInputEnabled(true); // Ensure player input is enabled
                }
                else
                {
                    Debug.LogWarning("SetGamePaused: PlayerController is null. Cannot enable player input.");
                }
                Debug.Log($"Game Unpaused. Time.timeScale: {Time.timeScale}. Input: Player Enabled, UI Disabled.");
            }
        }

        /// <summary>
        /// Handles the sequence of generating a new maze and resuming the game after a UI fade-out.
        /// </summary>
        private void GenerateMazeAndResumeGame()
        {
            Debug.Log("GenerateMazeAndResumeGame: Initiating maze generation and game resume.");
            HideAllPanels(); // Ensure all panels are hidden
            _rootElement.RemoveFromClassList("fade-out"); // Remove fade out class (important to do before generation)
            
            if (mazeGenerator != null) 
            {
                // Pass the current UI values to the MazeGenerator's generation method
                mazeGenerator.GenerateMazeAsync(
                    _widthField.value,
                    _heightField.value,
                    _seedField.value,
                    _autoPlaceToggle.value,
                    null, // CustomStart/End can be null for auto-place, or you could add UI for them
                    null
                );
                Debug.Log($"GenerateMazeAndResumeGame: Called mazeGenerator.GenerateMazeAsync with W:{_widthField.value}, H:{_heightField.value}, Seed:{_seedField.value}, AutoPlace:{_autoPlaceToggle.value}");
            }
            else
            {
                Debug.LogError("GenerateMazeAndResumeGame: MazeGenerator is null. Cannot generate maze.");
            }
            SetGamePaused(false); // Unpause game and enable gameplay input
            Debug.Log("GenerateMazeAndResumeGame: Game should now be unpaused and maze generation started.");
        }

        /// <summary>
        /// This method is called when the player reaches the exit tile.
        /// It displays the exit menu and pauses the game.
        /// </summary>
        private void HandlePlayerReachedExit()
        {
            Debug.Log("HandlePlayerReachedExit: Player reached exit. Showing exit menu.");
            // First, ensure the root UI is visible to allow showing the exit menu
            _rootElement.style.display = DisplayStyle.Flex; 
            ShowPanel(_exitMenuPanel); // Show the exit menu
            SetGamePaused(true); // Pause the game and switch input maps

            Debug.Log("Player reached exit, showing exit menu.");

            // If the "Auto-Generate on Exit" toggle is true, simulate clicking the "New Maze" button
            // This will ensure the exit menu appears briefly, then automatically proceeds to generate a new maze.
            if (_exitAutoGenerateToggle.value)
            {
                Debug.Log("Auto-generating new maze via exit menu toggle.");
                // Use Invoke to allow the UI to render the exit menu briefly before transitioning
                // No, better to use StartCoroutine and WaitForSecondsRealtime for consistency
                StartCoroutine(StartGameRoutine()); 
            }
        }
        
        // Helper methods for Color saving/loading and UI updates
        private Color GetColorFromSliders(Slider r, Slider g, Slider b)
        {
            return new Color(r.value, g.value, b.value);
        }

        private void SetSlidersFromColor(Color color, Slider r, Slider g, Slider b, VisualElement display)
        {
            r.value = color.r;
            g.value = color.g;
            b.value = color.b;
            UpdateColorDisplay(r, g, b, display); // Call helper to update the display element
        }

        private void UpdateColorDisplay(Slider r, Slider g, Slider b, VisualElement display)
        {
            if (display != null)
            {
                display.style.backgroundColor = GetColorFromSliders(r, g, b);
            }
        }

        private void SaveColorToPlayerPrefs(string keyPrefix, Color color)
        {
            PlayerPrefs.SetFloat(keyPrefix + "R", color.r);
            PlayerPrefs.SetFloat(keyPrefix + "G", color.g);
            PlayerPrefs.SetFloat(keyPrefix + "B", color.b);
        }

        private Color GetColorFromPlayerPrefs(string keyPrefix, Color defaultValue)
        {
            float r = PlayerPrefs.GetFloat(keyPrefix + "R", defaultValue.r);
            float g = PlayerPrefs.GetFloat(keyPrefix + "G", defaultValue.g);
            float b = PlayerPrefs.GetFloat(keyPrefix + "B", defaultValue.b);
            return new Color(r, g, b);
        }
    }
}