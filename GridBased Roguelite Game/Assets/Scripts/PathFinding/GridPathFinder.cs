// GridPathFinder.cs - Add null checks and safety
using System.Collections.Generic;
using UnityEngine;

public static class GridPathFinder
{
    /// <summary>
    /// Finds a path from start to target using BFS.
    /// Enemies are considered walkable for pathfinding (can path through them),
    /// but the final target must be walkable and not occupied.
    /// </summary>
    public static List<Vector2Int> FindPath(GridManager grid, Vector2Int start, Vector2Int target,
        int maxIterations = 500)
    {
        if (!grid) return null;
        if (!grid.Data.IsInBounds(start.x, start.y) || !grid.Data.IsInBounds(target.x, target.y)) return null;
        
        // If start and target are the same, return empty path
        if (start == target) return new List<Vector2Int>();
        
        // BFS setup
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(start);
        visited.Add(start);
        cameFrom[start] = start;

        Vector2Int[] directions =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
        };

        int iterations = 0;
        while (queue.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            Vector2Int current = queue.Dequeue();
            
            // If we reached a tile adjacent to target, reconstruct path
            if (Vector2Int.Distance(current, target) <= 1.1f)
            {
                return ReconstructPath(cameFrom, start, current);
            }

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = current + dir;
                
                // Check bounds
                if (!grid.Data.IsInBounds(neighbor.x, neighbor.y))
                    continue;
                
                if (visited.Contains(neighbor))
                    continue;
                
                // Check if the tile is walkable for pathfinding
                if (!IsWalkableForPathfinding(grid, neighbor.x, neighbor.y))
                    continue;

                visited.Add(neighbor);
                cameFrom[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a tile is walkable for pathfinding purposes.
    /// Enemies are considered walkable (can path through them),
    /// but walls, out of bounds, etc are not.
    /// </summary>
    private static bool IsWalkableForPathfinding(GridManager grid, int x, int y)
    {
        Tile tile = grid.GetTile(x, y);
        if (tile == null) return false;
        
        // Walls are never walkable
        if (tile.Type == TileType.Wall) return false;
        
        // Effect tiles and floors are walkable
        if (tile.Type == TileType.Floor || tile.Type == TileType.Effect || tile.Type == TileType.Stairs)
        {
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Checks if a tile is a valid final destination for movement.
    /// Cannot be occupied by any unit (player or enemy).
    /// </summary>
    public static bool IsValidDestination(GridManager grid, int x, int y, GameObject movingUnit = null)
    {
        Tile tile = grid.GetTile(x, y);
        if (tile == null) return false;
        
        // Must be a walkable tile type
        if (tile.Type != TileType.Floor && tile.Type != TileType.Effect && tile.Type != TileType.Stairs)
            return false;
        
        // Cannot be occupied by any unit (unless it's the moving unit itself)
        if (tile.Occupant != null && tile.Occupant != movingUnit)
            return false;
        
        return true;
    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start,
        Vector2Int current)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        int maxSteps = 100; // Prevent infinite loops
        int steps = 0;
        
        while (current != start && steps < maxSteps)
        {
            path.Add(current);
            current = cameFrom[current];
            steps++;
        }
        
        if (steps >= maxSteps)
        {
            Debug.LogWarning("Path reconstruction exceeded max steps");
            return null;
        }
        
        path.Reverse();
        return path;
    }
}