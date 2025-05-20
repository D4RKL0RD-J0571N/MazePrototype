using UnityEngine;
using Sirenix.OdinInspector; // Add this namespace

namespace Level
{
    // Important: Mark the class as Serializable for Unity and Odin to display it properly
    [System.Serializable]
    public class MazeCell
    {
        [TitleGroup("Cell Properties")] // Group basic properties
        [PropertyTooltip("The grid coordinates of this maze cell.")]
        [ReadOnly] // Position is set once in the constructor
        public Vector2Int Position { get; private set; }

        [TitleGroup("Cell Properties")]
        [PropertyTooltip("True if this cell represents a wall, false if it's a walkable path.")]
        public bool IsWall { get; set; }

        // Pathfinding metadata
        [TitleGroup("Pathfinding Data")] // Group pathfinding properties
        [PropertyTooltip("Used in pathfinding algorithms (e.g., BFS/DFS) to mark if the cell has been processed.")]
        public bool Visited { get; set; }

        [TitleGroup("Pathfinding Data")]
        [PropertyTooltip("The cumulative cost from the start node (g-score in A*).")]
        public float DistanceFromStart { get; set; } = Mathf.Infinity; // Dijkstra / A*

        [TitleGroup("Pathfinding Data")]
        [PropertyTooltip("The estimated total cost (f-score = g + h) for A* pathfinding.")]
        public float EstimatedTotalCost { get; set; } = Mathf.Infinity; // f = g + h for A*

        [TitleGroup("Pathfinding Data")]
        [PropertyTooltip("The preceding cell in the shortest path found so far.")]
        [ReadOnly] // Parent is set by pathfinding algorithm
        public MazeCell Parent { get; set; } // For backtracking path

        [TitleGroup("Game State & Type")] // Group game-specific properties
        [PropertyTooltip("The type of this maze cell, influencing its behavior (e.g., Normal, Trap, Goal).")]
        public CellType Type { get; set; } = CellType.Normal;

        [TitleGroup("Game State & Type")]
        [ToggleLeft] // Nicer toggle appearance
        [PropertyTooltip("True if the cell's contents have been revealed to the player.")]
        public bool IsRevealed { get; set; } = false;

        [TitleGroup("Game State & Type")]
        [ShowInInspector] // Show calculated property
        [ReadOnly] // Derived value
        [PropertyTooltip("True if the cell is currently obscured by fog (i.e., not revealed).")]
        public bool HasFog => !IsRevealed;

        // Enum definition (no Odin attributes needed here)
        public enum CellType { Normal, Trap, Goal, Spawn, Special }


        public MazeCell(int x, int y, bool isWall)
        {
            Position = new Vector2Int(x, y);
            IsWall = isWall;
        }

        [Button(ButtonSizes.Medium)] // Add a button to reset pathfinding data
        [GUIColor(0.9f, 0.9f, 0.4f)] // Yellowish color
        [PropertyTooltip("Resets all pathfinding-related data for this cell.")]
        public void ResetPathfindingData()
        {
            Visited = false;
            DistanceFromStart = Mathf.Infinity;
            EstimatedTotalCost = Mathf.Infinity;
            Parent = null;
        }
    }
}