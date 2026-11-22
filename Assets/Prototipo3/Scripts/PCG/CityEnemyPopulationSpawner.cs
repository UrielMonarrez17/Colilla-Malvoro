using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3
{
    /// <summary>
    /// Componente único (SRP) para la distribución y generación de enemigos ferales en la ciudad procedural.
    /// </summary>
    public class CityEnemyPopulationSpawner : MonoBehaviour
    {
        [Header("Configuración de Enemigos Ferales")]
        public List<GameObject> enemyPrefabs = new List<GameObject>();
        public int maxEnemiesToSpawn = 35;
        public float minEnemyDistanceFromSpawn = 26f;
        public float minSpacingBetweenEnemies = 12f;

        public void ClearEnemies()
        {
            Transform enemyRoot = transform.Find("SPAWNERS_ENEMIGOS");
            if (enemyRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(enemyRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(enemyRoot.gameObject);
                }
            }
        }

        public void SpawnEnemies(CityGeneratorPrototipo3 cityGen, CityRandom rng)
        {
            if (cityGen == null || rng == null) return;

            ClearEnemies();

            List<GameObject> validPrefabs = new List<GameObject>();
            if (enemyPrefabs != null)
            {
                for (int i = 0; i < enemyPrefabs.Count; i++)
                {
                    if (enemyPrefabs[i] != null) validPrefabs.Add(enemyPrefabs[i]);
                }
            }
            if (validPrefabs.Count == 0 || maxEnemiesToSpawn <= 0) return;

            GameObject enemyRoot = new GameObject("SPAWNERS_ENEMIGOS");
            enemyRoot.transform.SetParent(transform, false);

            List<Vector3> allCandidates = CollectRoadCandidates(cityGen);
            List<Vector3> filtered = FilterCandidates(allCandidates, cityGen);
            Shuffle(filtered, rng);

            List<Vector3> selected = SelectSpacedPoints(filtered, maxEnemiesToSpawn, minSpacingBetweenEnemies);
            int countToSpawn = Mathf.Min(maxEnemiesToSpawn, selected.Count);
            if (countToSpawn == 0) return;

            List<GameObject> balancedQueue = BuildBalancedSpeciesQueue(validPrefabs, countToSpawn, rng);

            for (int i = 0; i < countToSpawn; i++)
            {
                Vector3 spawnPos = selected[i];
                spawnPos.y = cityGen.roadHeightY;

                GameObject chosenPrefab = balancedQueue[i];
                GameObject spawnerObj = new GameObject($"EnemySpawner_{i + 1}_{chosenPrefab.name}");
                spawnerObj.transform.SetParent(enemyRoot.transform, false);
                spawnerObj.transform.position = spawnPos;
                spawnerObj.transform.rotation = Quaternion.Euler(0f, rng.Range(0, 360), 0f);

                EnemySpawner spawner = spawnerObj.AddComponent<EnemySpawner>();
                spawner.enemyPrefab = chosenPrefab;
                spawner.spawnOnAwake = false;
                spawner.spawnOnlyAtNight = false;
                spawner.SpawnEnemy();

                cityGen.EnemySpawnPoints.Add(spawnPos);
            }
        }

        private List<Vector3> CollectRoadCandidates(CityGeneratorPrototipo3 cityGen)
        {
            var result = new List<Vector3>();
            AddUniquePoints(result, cityGen.LoopCenterPoints, 3.0f);
            AddUniquePoints(result, cityGen.IntersectionPoints, 3.0f);
            AddUniquePoints(result, cityGen.DeadEndPoints, 3.0f);

            CityGrid grid = cityGen.Grid;
            if (grid != null)
            {
                var roadCells = new List<Vector3>();
                for (int x = 0; x < grid.Width; x++)
                {
                    for (int y = 0; y < grid.Length; y++)
                    {
                        Vector2Int cell = new Vector2Int(x, y);
                        if (grid.IsRoad(cell))
                        {
                            roadCells.Add(cityGen.CellToWorldCenter(cell));
                        }
                    }
                }
                AddUniquePoints(result, roadCells, 3.0f);
            }

            return result;
        }

        private static void AddUniquePoints(List<Vector3> target, List<Vector3> source, float minDedupDist)
        {
            if (source == null) return;
            float minSq = minDedupDist * minDedupDist;
            for (int i = 0; i < source.Count; i++)
            {
                Vector3 candidate = source[i];
                bool duplicate = false;
                for (int j = 0; j < target.Count; j++)
                {
                    if ((target[j] - candidate).sqrMagnitude < minSq)
                    {
                        duplicate = true;
                        break;
                    }
                }
                if (!duplicate) target.Add(candidate);
            }
        }

        private List<Vector3> FilterCandidates(List<Vector3> candidates, CityGeneratorPrototipo3 cityGen)
        {
            var safe = new List<Vector3>();
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3 pt = candidates[i];
                if (Vector3.Distance(pt, cityGen.PlayerSpawnPoint) < minEnemyDistanceFromSpawn) continue;
                if (cityGen.SafeRefugeCenter != Vector3.zero && Vector3.Distance(pt, cityGen.SafeRefugeCenter) < 12f) continue;
                if (SafeRefugeZone.ActiveZone != null && SafeRefugeZone.ActiveZone.ContainsPoint(pt, 2.5f)) continue;
                safe.Add(pt);
            }
            return safe;
        }

        private static List<Vector3> SelectSpacedPoints(List<Vector3> shuffled, int targetCount, float preferredSpacing)
        {
            var chosen = new List<Vector3>();
            var used = new bool[shuffled.Count];
            float[] thresholds = { preferredSpacing, Mathf.Max(6f, preferredSpacing * 0.5f), 0f };

            for (int t = 0; t < thresholds.Length && chosen.Count < targetCount; t++)
            {
                float minSq = thresholds[t] * thresholds[t];
                for (int i = 0; i < shuffled.Count && chosen.Count < targetCount; i++)
                {
                    if (used[i]) continue;
                    Vector3 pt = shuffled[i];
                    bool farEnough = true;
                    if (minSq > 0f)
                    {
                        for (int c = 0; c < chosen.Count; c++)
                        {
                            if ((chosen[c] - pt).sqrMagnitude < minSq)
                            {
                                farEnough = false;
                                break;
                            }
                        }
                    }
                    if (farEnough)
                    {
                        chosen.Add(pt);
                        used[i] = true;
                    }
                }
            }

            return chosen;
        }

        private static List<GameObject> BuildBalancedSpeciesQueue(List<GameObject> validPrefabs, int totalCount, CityRandom rng)
        {
            var speciesCopy = new List<GameObject>(validPrefabs);
            Shuffle(speciesCopy, rng);

            var queue = new List<GameObject>(totalCount);
            // Garantizar al menos 1 de cada especie disponible
            for (int i = 0; i < speciesCopy.Count && queue.Count < totalCount; i++)
            {
                queue.Add(speciesCopy[i]);
            }
            while (queue.Count < totalCount)
            {
                queue.Add(validPrefabs[rng.Range(0, validPrefabs.Count)]);
            }
            Shuffle(queue, rng);
            return queue;
        }

        private static void Shuffle<T>(List<T> list, CityRandom rng)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int r = rng.Range(i, list.Count);
                (list[i], list[r]) = (list[r], list[i]);
            }
        }
    }
}
