using System.Collections.Generic;
using UnityEngine;

namespace KnightSurvivors.Enemies
{
    /// <summary>
    /// A grid of buckets on the ground plane. Each tick we drop every zombie into the bucket
    /// ("cell") under it. To find zombies near a point we only look in the few cells around it
    /// instead of checking all 350 zombies. That turns 350 x 350 = 122,500 checks into a few thousand.
    ///
    /// Cell size ~2 m: a bit bigger than the largest common zombie (Bloater, 0.7 m radius).
    /// </summary>
    public class SpatialHash
    {
        private readonly float cellSize;
        private readonly float inverseCellSize;
        private readonly Dictionary<long, List<Enemy>> cells = new Dictionary<long, List<Enemy>>(512);
        private readonly Stack<List<Enemy>> spareLists = new Stack<List<Enemy>>();
        private readonly List<long> usedKeys = new List<long>(512);

        public float CellSize => cellSize;

        public SpatialHash(float cellSize)
        {
            this.cellSize = Mathf.Max(0.5f, cellSize);
            inverseCellSize = 1f / this.cellSize;
        }

        private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        public int CellCoord(float v) => Mathf.FloorToInt(v * inverseCellSize);

        /// <summary>Empty all buckets (the lists are kept and reused to avoid garbage).</summary>
        public void Clear()
        {
            for (int i = 0; i < usedKeys.Count; i++)
            {
                var list = cells[usedKeys[i]];
                list.Clear();
                spareLists.Push(list);
            }

            cells.Clear();
            usedKeys.Clear();
        }

        public void Insert(Enemy enemy, Vector3 position)
        {
            long key = Key(CellCoord(position.x), CellCoord(position.z));
            if (!cells.TryGetValue(key, out var list))
            {
                list = spareLists.Count > 0 ? spareLists.Pop() : new List<Enemy>(8);
                cells[key] = list;
                usedKeys.Add(key);
            }

            list.Add(enemy);
        }

        /// <summary>Adds every zombie in the cells overlapping a circle to <paramref name="results"/> (rough; caller checks distance).</summary>
        public void Query(Vector3 center, float radius, List<Enemy> results)
        {
            int minX = CellCoord(center.x - radius);
            int maxX = CellCoord(center.x + radius);
            int minZ = CellCoord(center.z - radius);
            int maxZ = CellCoord(center.z + radius);

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (cells.TryGetValue(Key(x, z), out var list)) results.AddRange(list);
                }
            }
        }
    }
}
