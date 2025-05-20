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
        public GameObject endMarker;

        [TitleGroup("Dependencies")] // Group other essential components
        [SerializeField]
        public CameraFollow cameraFollow;

        [TitleGroup("Maze Generation Settings")] // Settings for how the maze is built
        [Range(3, 101)] // Restrict width to odd numbers
        [OnValueChanged("EnsureOddWidth")] // Odin will call this method if width changes
        [InfoBox("Width and Height should be odd numbers for the Prim's algorithm to work correctly.", InfoMessageType.Warning, VisibleIf = "IsMazeDimensionInvalidForPrim")] // Updated VisibleIf
        public int Width = 21;

        [Range(3, 101)] // Restrict height to odd numbers
        [OnValueChanged("EnsureOddHeight")] // Odin will call this method if height changes
        public int Height = 21;

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
        public void GenerateMazeAsync()
        {
            if (_generateCoroutine != null)
            {
                Debug.LogWarning("Maze generation already in progress. Stopping previous generation.");
                StopCoroutine(_generateCoroutine);
            }
            _generateCoroutine = StartCoroutine(GenerateMazeCoroutine());
        }

        // Exposed button to clear maze from Inspector
        [Button(ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.4f, 0.4f)] // Reddish color
        public void ClearCurrentMaze() // Renamed to avoid confusion with internal ClearMaze()
        {
            ClearMaze();
        }


        private IEnumerator GenerateMazeCoroutine()
        {
            ClearMaze();

            if (tilePrefab == null || wallPrefab == null)
            {
                Debug.LogError("Tile or Wall prefab is missing! Cannot generate maze.");
                yield break;
            }
            if (Seed >= 0) Random.InitState(Seed);

            // These ensure statements are technically redundant now due to OnValueChanged,
            // but keeping them as a safeguard for runtime calls or if OnValueChanged is somehow skipped.
            Width = Width % 2 == 0 ? Width + 1 : Width;
            Height = Height % 2 == 0 ? Height + 1 : Height;

            _grid = new MazeCell[Width, Height];
            InitializeGrid();

            yield return RunPrimAlgorithmAsync();
            yield return BuildVisualTilesAsync();

            _generateCoroutine = null;
        }

        private void InitializeGrid()
        {
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                _grid[x, y] = new MazeCell(x, y, true);
        }

        private IEnumerator RunPrimAlgorithmAsync()
        {
            var frontier = new List<Vector2Int>();
            Vector2Int start = new Vector2Int(1, 1);
            _grid[start.x, start.y].IsWall = false;
            AddFrontier(start, frontier);

            while (frontier.Count > 0)
            {
                int index = Random.Range(0, frontier.Count);
                Vector2Int cellPos = frontier[index];
                frontier.RemoveAt(index);

                Vector2Int? neighbor = GetVisitedNeighbor(cellPos);
                if (neighbor.HasValue)
                {
                    Vector2Int between = (cellPos + neighbor.Value) / 2;
                    _grid[cellPos.x, cellPos.y].IsWall = false;
                    _grid[between.x, between.y].IsWall = false;
                    AddFrontier(cellPos, frontier);
                }

                if (frontier.Count % 10 == 0) yield return null;
            }
        }

        private void AddFrontier(Vector2Int cell, List<Vector2Int> frontier)
        {
            foreach (var dir in directions)
            {
                Vector2Int next = cell + dir;
                if (IsInside(next) && _grid[next.x, next.y].IsWall && !frontier.Contains(next))
                    frontier.Add(next);
            }
        }

        private Vector2Int? GetVisitedNeighbor(Vector2Int cell)
        {
            var visited = new List<Vector2Int>();
            foreach (var dir in directions)
            {
                Vector2Int next = cell + dir;
                if (IsInside(next) && !_grid[next.x, next.y].IsWall)
                    visited.Add(next);
            }

            return visited.Count > 0 ? visited[Random.Range(0, visited.Count)] : null;
        }

        private IEnumerator BuildVisualTilesAsync()
        {
            int total = Width * Height;

            if (_tileContainer == null)
            {
                _tileContainer = new GameObject("MazeTiles");
                _tileContainer.transform.SetParent(transform);
            }

            yield return WaitForTilesToBecomeIdle();

            foreach (var obj in activeTiles)
            {
                if (obj.CompareTag("Wall"))
                    _wallPool.Release(obj);
                else
                    _tilePool.Release(obj);
            }
            activeTiles.Clear();

            _tileContainer.transform.DetachChildren();

            bool globalRGBSyncEnabled = RGBSyncManager.Instance != null && RGBSyncManager.Instance.CurrentSettings.Enabled;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
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


                    int built = x * Height + y;
                    if (built % 50 == 0)
                    {
                        float progress = built / (float)total;
                        OnBuildProgress?.Invoke(progress);
                        yield return null;
                    }
                }
            }

            OnBuildProgress?.Invoke(1f);

            if (AutoPlaceStartEnd)
                PlaceStartAndEnd();
        }

        private IEnumerator WaitForTilesToBecomeIdle()
        {
            int count = 0;
            foreach (var obj in activeTiles)
            {
                if (!obj.CompareTag("Wall") && obj.TryGetComponent(out MazeTile tile))
                {
                    yield return tile.WaitUntilIdle();
                    count++;
                    if (count % 10 == 0)
                        yield return null;
                }
            }
        }


        private void PlaceStartAndEnd()
        {
            Vector2Int startPos = CustomStart ?? new Vector2Int(1, 1);
            Vector2Int endPos = CustomEnd ?? new Vector2Int(Width - 2, Height - 2);

            if (_spawnedPlayer != null)
                Destroy(_spawnedPlayer);

            _spawnedPlayer = Instantiate(playerPrefab, AlignToGrid(startPos), Quaternion.identity);
            Instantiate(endMarker, AlignToGrid(endPos), Quaternion.identity, transform);
            cameraFollow.Target = _spawnedPlayer.transform;
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

            foreach (var obj in activeTiles)
            {
                if (obj.CompareTag("Wall"))
                {
                    _wallPool.Release(obj);
                }
                else
                {
                    if (obj.TryGetComponent(out MazeTile tile))
                        tile.ResetTile();

                    _tilePool.Release(obj);
                }
            }
            activeTiles.Clear();

            if (_spawnedPlayer != null)
            {
                Destroy(_spawnedPlayer);
                _spawnedPlayer = null;
            }

            var existingEndMarker = transform.Find(endMarker.name.Replace("(Clone)", ""));
            if (existingEndMarker != null)
            {
                Destroy(existingEndMarker.gameObject);
            }

            _grid = null;
        }


        private bool IsInside(Vector2Int pos) =>
            pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;

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