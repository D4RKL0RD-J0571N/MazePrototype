using System;
using System.Collections;
using System.Collections.Generic;
using Mechanics;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;
using Misc.RGB;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor; // Added for GUIHelper.RequestRepaint()

namespace Level
{
    public class MazeGenerator : MonoBehaviour
    {
        [TitleGroup("Prefab References")] // Group all prefabs
        [SerializeField]
        public GameObject tilePrefab;
        [SerializeField]
        public GameObject wallPrefab;
        [SerializeField]
        public GameObject playerPrefab;
        [SerializeField]
        public GameObject endMarker; // This is a prefab

        [TitleGroup("Dependencies")] // Group other essential components
        [SerializeField]
        public CameraFollow cameraFollow;

        [TitleGroup("Maze Generation Settings")] // Settings for how the maze is built
        [Range(3, 101)] // Restrict width to odd numbers
        [OnValueChanged("EnsureOddWidth")] // Odin will call this method if width changes
        [InfoBox("Width and Height should be odd numbers for the Prim's algorithm to work correctly.", InfoMessageType.Warning, VisibleIf = "IsMazeDimensionInvalidForPrim")] // Updated VisibleIf
        public int Width;

        [Range(3, 101)] // Restrict height to odd numbers
        [OnValueChanged("EnsureOddHeight")] // Odin will call this method if height changes
        public int Height;

        [Tooltip("Use -1 for a random seed each time, or a specific number for repeatable mazes.")]
        public int Seed = -1;

        [ToggleLeft] // Show toggle on the left of the label
        [Tooltip("If true, start and end points will be automatically placed. Otherwise, CustomStart/End can be used.")]
        public bool AutoPlaceStartEnd = true;

        [ShowIf("!AutoPlaceStartEnd")] // Only show if AutoPlaceStartEnd is false
        [Tooltip("Custom start position for the player. Will be ignored if AutoPlaceStartEnd is true.")]
        public Vector2Int? CustomStart { get; set; } = null; // Initialize to null

        [ShowIf("!AutoPlaceStartEnd")] // Only show if AutoPlaceStartEnd is false
        [Tooltip("Custom end position for the maze. Will be ignored if AutoPlaceStartEnd is true.")]
        public Vector2Int? CustomEnd { get; set; } = null; // Initialize to null

        // Runtime/Internal State - Marked ReadOnly and grouped
        [TitleGroup("Runtime Information")]
        [BoxGroup("Runtime Information/Maze Structure")]
        [ReadOnly]
        private Vector2Int[] directions; // Fixed during Awake
        [BoxGroup("Runtime Information/Maze Structure")]
        [ReadOnly]
        [PropertyTooltip("The internal grid representation of the maze.")]
        private MazeCell[,] _grid;

        [BoxGroup("Runtime Information/Object Pools")]
        [ReadOnly]
        private ObjectPool<GameObject> _tilePool;
        [BoxGroup("Runtime Information/Object Pools")]
        [ReadOnly]
        private ObjectPool<GameObject> _wallPool;
        [BoxGroup("Runtime Information/Object Pools")]
        [ReadOnly]
        [PropertyTooltip("List of currently active maze tiles and walls in the scene.")]
        private List<GameObject> activeTiles = new List<GameObject>();

        [BoxGroup("Runtime Information/Spawned Objects")]
        [ReadOnly]
        [PropertyTooltip("The player GameObject spawned by the generator.")]
        private GameObject _spawnedPlayer;
        [BoxGroup("Runtime Information/Spawned Objects")]
        [ReadOnly]
        [PropertyTooltip("Container GameObject for all maze tiles and walls.")]
        private GameObject _tileContainer;
        [BoxGroup("Runtime Information/Spawned Objects")]
        [ReadOnly]
        [PropertyTooltip("The spawned end marker GameObject.")]
        private GameObject _spawnedEndMarker; // Keep a reference to the spawned end marker


        [BoxGroup("Runtime Information/Generation Status")]
        [ReadOnly]
        [PropertyTooltip("Coroutine reference for the maze generation process.")]
        private Coroutine _generateCoroutine;

        // Display current maze dimensions and state (useful for debugging)
        [TitleGroup("Current Maze State")]
        [ShowInInspector] // Forces a property to show in inspector
        [PropertyOrder(999)] // Pushes this to the bottom of the Inspector
        [ReadOnly]
        public bool IsWallAt(int x, int y) => _grid?[x, y].IsWall ?? true; // This method is shown here but won't be editable

        [ShowInInspector]
        [ReadOnly]
        public MazeCell[,] Grid => _grid; // Read-only access to the grid

        [ShowInInspector]
        [ReadOnly]
        public bool IsGenerating => _generateCoroutine != null; // Status indicator


        // Internal properties for external access (less relevant for Odin's Inspector)
        public event Action<float> OnBuildProgress;

        // NEW: Store current custom colors (initialized to defaults)
        private Color _currentFloorColor = Color.white;
        private Color _currentWallColor = Color.black;


        private void Awake()
        {
            directions = new[] {
                Vector2Int.up * 2,
                Vector2Int.down * 2,
                Vector2Int.left * 2,
                Vector2Int.right * 2
            };

            _tilePool = new ObjectPool<GameObject>(
                () => Instantiate(tilePrefab),
                obj => obj.SetActive(true),
                obj => obj.SetActive(false),
                Destroy);

            _wallPool = new ObjectPool<GameObject>(
                () => Instantiate(wallPrefab),
                obj => obj.SetActive(true),
                obj => obj.SetActive(false),
                Destroy);

            // Prewarm pools
            for (int i = 0; i < 50; i++) _tilePool.Release(_tilePool.Get());
            for (int i = 0; i < 50; i++) _wallPool.Release(_wallPool.Get());
        }

        // Odin Callbacks to ensure odd dimensions
        private void EnsureOddWidth()
        {
            if (Width % 2 == 0)
            {
                Width++;
            }
            GUIHelper.RequestRepaint(); // Force Inspector to repaint
        }

        private void EnsureOddHeight()
        {
            if (Height % 2 == 0)
            {
                Height++;
            }
            GUIHelper.RequestRepaint(); // Force Inspector to repaint
        }

        // New method for Odin's VisibleIf condition
        private bool IsMazeDimensionInvalidForPrim()
        {
            return Width % 2 == 0 || Height % 2 == 0;
        }


        // Exposed button to trigger maze generation from Inspector
        [Button(ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 0.4f)] // Greenish color
        [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)] // Add some space around the button
        /// <summary>
        /// Starts the maze generation process using specified parameters.
        /// If no parameters are provided, it falls back to the Inspector/default values.
        /// </summary>
        public void GenerateMazeAsync(int? width = null, int? height = null, int? seed = null, bool? autoPlaceStartEnd = null, Vector2Int? customStart = null, Vector2Int? customEnd = null)
        {
            if (_generateCoroutine != null)
            {
                Debug.LogWarning("Maze generation already in progress. Stopping previous generation.");
                StopCoroutine(_generateCoroutine);
            }

            // Use provided parameters, otherwise fall back to current Inspector values
            int effectiveWidth = width ?? Width;
            int effectiveHeight = height ?? Height;
            int effectiveSeed = seed ?? Seed;
            bool effectiveAutoPlaceStartEnd = autoPlaceStartEnd ?? AutoPlaceStartEnd;
            Vector2Int? effectiveCustomStart = customStart ?? CustomStart;
            Vector2Int? effectiveCustomEnd = customEnd ?? CustomEnd;


            _generateCoroutine = StartCoroutine(GenerateMazeCoroutineInternal(
                effectiveWidth, effectiveHeight, effectiveSeed, effectiveAutoPlaceStartEnd, effectiveCustomStart, effectiveCustomEnd
            ));
        }

        // Exposed button to clear maze from Inspector
        [Button(ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.4f, 0.4f)] // Reddish color
        public void ClearCurrentMaze() // Renamed to avoid confusion with internal ClearMaze()
        {
            ClearMaze();
        }


        private IEnumerator GenerateMazeCoroutineInternal(int width, int height, int seed, bool autoPlaceStartEnd, Vector2Int? customStart, Vector2Int? customEnd)
        {
            ClearMaze();

            if (tilePrefab == null || wallPrefab == null)
            {
                Debug.LogError("Tile or Wall prefab is missing! Cannot generate maze.");
                yield break;
            }
            
            // Set the effective dimensions and seed for THIS generation
            // This updates the *internal* state for the current generation, but not the serialized fields.
            int currentWidth = width % 2 == 0 ? width + 1 : width;
            int currentHeight = height % 2 == 0 ? height + 1 : height;
            int currentSeed = seed;
            bool currentAutoPlaceStartEnd = autoPlaceStartEnd;
            Vector2Int? currentCustomStart = customStart;
            Vector2Int? currentCustomEnd = customEnd;

            if (currentSeed >= 0) Random.InitState(currentSeed);
            else Random.InitState(System.Environment.TickCount); // Use a new random seed if not specified

            // Update the MazeGenerator's *display* properties in the Inspector
            // This is optional, but makes debugging easier to see what was actually used.
            Width = currentWidth;
            Height = currentHeight;
            Seed = currentSeed;
            AutoPlaceStartEnd = currentAutoPlaceStartEnd;
            CustomStart = currentCustomStart;
            CustomEnd = currentCustomEnd;


            _grid = new MazeCell[currentWidth, currentHeight];
            InitializeGrid(currentWidth, currentHeight); // Pass dimensions to InitializeGrid

            yield return RunPrimAlgorithmAsync(currentWidth, currentHeight); // Pass dimensions
            yield return BuildVisualTilesAsync(currentWidth, currentHeight, currentAutoPlaceStartEnd, currentCustomStart, currentCustomEnd); // Pass relevant generation parameters

            _generateCoroutine = null;
        }

        private void InitializeGrid(int width, int height) // Accept dimensions
        {
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                _grid[x, y] = new MazeCell(x, y, true);
        }

        private IEnumerator RunPrimAlgorithmAsync(int width, int height) // Accept dimensions
        {
            var frontier = new List<Vector2Int>();
            Vector2Int start = new Vector2Int(1, 1);
            _grid[start.x, start.y].IsWall = false;
            AddFrontier(start, frontier, width, height); // Pass dimensions

            while (frontier.Count > 0)
            {
                int index = Random.Range(0, frontier.Count);
                Vector2Int cellPos = frontier[index];
                frontier.RemoveAt(index);

                Vector2Int? neighbor = GetVisitedNeighbor(cellPos, width, height); // Pass dimensions
                if (neighbor.HasValue)
                {
                    Vector2Int between = (cellPos + neighbor.Value) / 2;
                    _grid[cellPos.x, cellPos.y].IsWall = false;
                    _grid[between.x, between.y].IsWall = false;
                    AddFrontier(cellPos, frontier, width, height); // Pass dimensions
                }

                if (frontier.Count % 10 == 0) yield return null;
            }
        }

        private void AddFrontier(Vector2Int cell, List<Vector2Int> frontier, int width, int height) // Accept dimensions
        {
            foreach (var dir in directions)
            {
                Vector2Int next = cell + dir;
                if (IsInside(next, width, height) && _grid[next.x, next.y].IsWall && !frontier.Contains(next)) // Use passed dimensions
                    frontier.Add(next);
            }
        }

        private Vector2Int? GetVisitedNeighbor(Vector2Int cell, int width, int height) // Accept dimensions
        {
            var visited = new List<Vector2Int>();
            foreach (var dir in directions)
            {
                Vector2Int next = cell + dir;
                if (IsInside(next, width, height) && !_grid[next.x, next.y].IsWall) // Use passed dimensions
                    visited.Add(next);
            }

            return visited.Count > 0 ? visited[Random.Range(0, visited.Count)] : null;
        }

        private IEnumerator BuildVisualTilesAsync(int width, int height, bool autoPlaceStartEnd, Vector2Int? customStart, Vector2Int? customEnd) // Accept parameters
        {
            int total = width * height; // Use passed width/height

            if (_tileContainer == null)
            {
                _tileContainer = new GameObject("MazeTiles");
                _tileContainer.transform.SetParent(transform);
            }

            // Before populating, ensure _spawnedEndMarker is clear
            if (_spawnedEndMarker != null)
            {
                Destroy(_spawnedEndMarker);
                _spawnedEndMarker = null;
            }

            yield return WaitForTilesToBecomeIdle();

            // Iterate activeTiles by index to avoid modifying collection during iteration
            for(int i = activeTiles.Count - 1; i >= 0; i--)
            {
                GameObject obj = activeTiles[i];
                if (obj == null) continue; // Safety check
                
                // Determine if it's a wall or floor based on its prefab origin or internal state
                // Since we don't use tags, compare against the wallPrefab itself or check MazeTile component properties if applicable.
                // Assuming wallPrefab is the one that produces wall objects:
                if (obj.name.Contains(wallPrefab.name)) // Simple check, might not be robust if names are similar
                {
                    _wallPool.Release(obj);
                }
                else // Assume it's a floor tile if not a wall
                {
                    _tilePool.Release(obj);
                }
            }
            activeTiles.Clear();

            // Detach children before pooling to prevent errors if pool.Release tries to access transform hierarchy
            if (_tileContainer != null)
            {
                 // Create a temporary list to avoid modifying during iteration
                List<Transform> childrenToDetach = new List<Transform>();
                foreach (Transform child in _tileContainer.transform)
                {
                    childrenToDetach.Add(child);
                }
                foreach (Transform child in childrenToDetach)
                {
                    child.SetParent(null); // Detach from container before pooling
                }
            }


            bool globalRGBSyncEnabled = RGBSyncManager.Instance != null && RGBSyncManager.Instance.CurrentSettings.Enabled;

            for (int x = 0; x < width; x++) // Use passed width
            {
                for (int y = 0; y < height; y++) // Use passed height
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    GameObject obj = _grid[x, y].IsWall ? _wallPool.Get() : _tilePool.Get();

                    obj.transform.position = AlignToGrid(pos);
                    obj.transform.SetParent(_tileContainer.transform);
                    activeTiles.Add(obj);
                    if (!_grid[x, y].IsWall && obj.TryGetComponent(out MazeTile tile))
                    {
                        tile.ResetTile();
                        tile.SetRGBSyncEnabled(globalRGBSyncEnabled);
                        // Apply custom colors immediately after setting RGB sync
                        tile.SetRevealedColor(_currentFloorColor);
                        tile.SetHiddenColor(_currentWallColor); // Even though it's a floor tile, its hidden state uses this color
                    }
                    // For walls, just set their hidden color as they are always hidden
                    else if (_grid[x, y].IsWall && obj.TryGetComponent(out MazeTile wallTile))
                    {
                        wallTile.ResetTile(); // Ensure it's hidden state
                        wallTile.SetRGBSyncEnabled(false); // Walls should not participate in RGB sync
                        wallTile.SetHiddenColor(_currentWallColor);
                    }


                    int built = x * height + y; // Use passed height
                    if (built % 50 == 0)
                    {
                        float progress = built / (float)total;
                        OnBuildProgress?.Invoke(progress);
                        yield return null;
                    }
                }
            }

            OnBuildProgress?.Invoke(1f);

            // Pass parameters to PlaceStartAndEnd
            if (autoPlaceStartEnd || (customStart.HasValue && customEnd.HasValue)) // Only place if custom values are provided AND auto-place is false
                PlaceStartAndEnd(width, height, customStart, customEnd);
            
        }

        private IEnumerator WaitForTilesToBecomeIdle()
        {
            // Iterate through a copy or use a while loop with activeTiles.Count to prevent issues
            // if tiles are removed from activeTiles during iteration (e.g. if an external system clears them).
            // For now, assuming activeTiles is only modified by this class, the current loop is fine.
            // Also, since we are not using tags, we need to check if the object is actually a floor tile
            // before waiting for it to become idle.
            for(int i = 0; i < activeTiles.Count; i++)
            {
                var obj = activeTiles[i];
                // Only wait for "tile" prefabs (floor tiles) to become idle. Walls don't have this behavior.
                if (obj != null && obj.name.Contains(tilePrefab.name) && obj.TryGetComponent(out MazeTile tile))
                {
                    yield return tile.WaitUntilIdle();
                    if (i % 10 == 0) // Yield periodically to avoid freezing
                        yield return null;
                }
            }
        }


        // Updated to accept generation parameters
        private void PlaceStartAndEnd(int width, int height, Vector2Int? customStart, Vector2Int? customEnd)
        {
            // Use provided custom positions, otherwise calculate default based on *current* width/height
            Vector2Int startPos = customStart ?? new Vector2Int(1, 1);
            Vector2Int endPos = customEnd ?? new Vector2Int(width - 2, height - 2);

            // Ensure startPos and endPos are within bounds and on a path tile
            // This is crucial if custom values are arbitrary or if the maze generation
            // doesn't guarantee specific start/end path tiles
            startPos = ClampToValidPosition(startPos, width, height);
            endPos = ClampToValidPosition(endPos, width, height);


            if (_spawnedPlayer != null)
                Destroy(_spawnedPlayer);

            _spawnedPlayer = Instantiate(playerPrefab, AlignToGrid(startPos), Quaternion.identity);
            
            // Destroy existing end markers before creating a new one
            if (_spawnedEndMarker != null)
            {
                Destroy(_spawnedEndMarker);
            }
            _spawnedEndMarker = Instantiate(endMarker, AlignToGrid(endPos), Quaternion.identity, transform); // Store reference
            
            if (cameraFollow != null)
            {
                cameraFollow.Target = _spawnedPlayer.transform;
            }
            else
            {
                Debug.LogWarning("MazeGenerator: CameraFollow not assigned, player camera will not track.");
            }

            Debug.Log($"Maze generated. Player at: {startPos}, End at: {endPos}.");
        }

        // Helper to clamp position to a valid, non-wall tile
        private Vector2Int ClampToValidPosition(Vector2Int pos, int width, int height)
        {
            // Basic clamping to ensure within grid boundaries
            pos.x = Mathf.Clamp(pos.x, 1, width - 2);
            pos.y = Mathf.Clamp(pos.y, 1, height - 2);

            // Find nearest non-wall cell if the clamped position is a wall
            if (_grid[pos.x, pos.y].IsWall)
            {
                for (int d = 0; d < Mathf.Max(width, height); d++) // Search radius
                {
                    for (int i = -d; i <= d; i++)
                    {
                        for (int j = -d; j <= d; j++)
                        {
                            Vector2Int searchPos = new Vector2Int(pos.x + i, pos.y + j);
                            if (IsInside(searchPos, width, height) && !_grid[searchPos.x, searchPos.y].IsWall)
                            {
                                return searchPos;
                            }
                        }
                    }
                }
                Debug.LogWarning($"Could not find a valid path tile near {pos}. Defaulting to (1,1).");
                return new Vector2Int(1,1); // Fallback if no path tile found (unlikely for well-formed mazes)
            }
            return pos;
        }

        private Vector3 AlignToGrid(Vector2Int pos) => new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0);

        public void ClearMaze()
        {
            // Stop any ongoing generation if ClearMaze is called mid-process
            if (_generateCoroutine != null)
            {
                StopCoroutine(_generateCoroutine);
                _generateCoroutine = null;
            }

            // Before releasing to pool, ensure all active tiles are detached from container
            if (_tileContainer != null)
            {
                List<Transform> childrenToDetach = new List<Transform>();
                foreach (Transform child in _tileContainer.transform)
                {
                    childrenToDetach.Add(child);
                }
                foreach (Transform child in childrenToDetach)
                {
                    child.SetParent(null); // Detach from container before pooling
                }
            }


            // Iterate activeTiles by index to avoid modifying collection during iteration
            for(int i = activeTiles.Count - 1; i >= 0; i--)
            {
                GameObject obj = activeTiles[i];
                if (obj == null) continue; // Safety check
                
                // Determine if it's a wall or floor based on its prefab origin or internal state
                if (obj.name.Contains(wallPrefab.name))
                {
                    _wallPool.Release(obj);
                }
                else
                {
                    if (obj.TryGetComponent(out MazeTile tile))
                        tile.ResetTile(); // Reset state before pooling

                    _tilePool.Release(obj);
                }
            }
            activeTiles.Clear();

            if (_spawnedPlayer != null)
            {
                Destroy(_spawnedPlayer);
                _spawnedPlayer = null;
            }

            // Destroy the specific spawned end marker reference
            if (_spawnedEndMarker != null)
            {
                Destroy(_spawnedEndMarker);
                _spawnedEndMarker = null;
            }

            _grid = null;
        }


        // Updated to accept dimensions for the current generation context
        private bool IsInside(Vector2Int pos, int width, int height) =>
            pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;

        /// <summary>
        /// Sets the custom colors for floor (revealed) and wall (hidden) tiles and applies them to existing tiles.
        /// </summary>
        /// <param name="floorColor">The color to be used for revealed floor tiles.</param>
        /// <param name="wallColor">The color to be used for wall tiles (and hidden floor tiles).</param>
        public void SetTileColors(Color floorColor, Color wallColor)
        {
            _currentFloorColor = floorColor;
            _currentWallColor = wallColor;

            // Apply these colors to all currently active MazeTile instances
            foreach (var obj in activeTiles)
            {
                if (obj == null) continue; // Safety check
                if (obj.TryGetComponent(out MazeTile tile))
                {
                    // If it's a floor tile (not a wall in the maze grid logic, but represented by MazeTile)
                    // The MazeTile's 'revealedColor' is effectively the floor color
                    // The MazeTile's 'hiddenColor' is effectively the wall/hidden state color
                    tile.SetRevealedColor(_currentFloorColor);
                    tile.SetHiddenColor(_currentWallColor);

                    // If RGB sync is not active for this specific tile, ensure its current color updates
                    // The MazeTile.SetRevealedColor and SetHiddenColor methods already handle this internally.
                }
            }
        }
        
        
    }
}