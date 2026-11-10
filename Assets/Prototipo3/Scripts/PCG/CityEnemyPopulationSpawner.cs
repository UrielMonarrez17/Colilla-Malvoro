using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3
{
    /// <summary>
    /// Componente especializado SOLID (Principio de Responsabilidad Única) para la distribución
    /// y generación de enemigos ferales en la ciudad procedural.
    /// </summary>
    public class CityEnemyPopulationSpawner : MonoBehaviour
    {
        [Header("Configuración de Enemigos Ferales")]
        public List<GameObject> enemyPrefabs = new List<GameObject>();
        public int maxEnemiesToSpawn = 12;
        public float minEnemyDistanceFromSpawn = 30f;

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

            List<GameObject> effectivePrefabs = (enemyPrefabs != null && enemyPrefabs.Count > 0)
                ? enemyPrefabs
                : cityGen.enemyPrefabs;

            if (effectivePrefabs == null || effectivePrefabs.Count == 0) return;

            int effectiveMax = maxEnemiesToSpawn > 0 ? maxEnemiesToSpawn : cityGen.maxEnemiesToSpawn;
            float effectiveMinDist = minEnemyDistanceFromSpawn > 0 ? minEnemyDistanceFromSpawn : cityGen.minEnemyDistanceFromSpawn;

            GameObject enemyRoot = new GameObject("SPAWNERS_ENEMIGOS");
            enemyRoot.transform.SetParent(transform, false);

            List<Vector3> candidatePoints = new List<Vector3>();
            candidatePoints.AddRange(cityGen.LoopCenterPoints);
            candidatePoints.AddRange(cityGen.IntersectionPoints);
            candidatePoints.AddRange(cityGen.DeadEndPoints);

            List<Vector3> safeEnemySpawns = new List<Vector3>();
            foreach (Vector3 pt in candidatePoints)
            {
                if (Vector3.Distance(pt, cityGen.PlayerSpawnPoint) >= effectiveMinDist)
                {
                    safeEnemySpawns.Add(pt);
                }
            }

            for (int i = 0; i < safeEnemySpawns.Count; i++)
            {
                int r = rng.Range(i, safeEnemySpawns.Count);
                (safeEnemySpawns[i], safeEnemySpawns[r]) = (safeEnemySpawns[r], safeEnemySpawns[i]);
            }

            int countToSpawn = Mathf.Min(effectiveMax, safeEnemySpawns.Count);
            for (int i = 0; i < countToSpawn; i++)
            {
                Vector3 spawnPos = safeEnemySpawns[i];
                spawnPos.y = cityGen.roadHeightY;

                int prefabIdx = rng.Range(0, effectivePrefabs.Count);
                GameObject chosenPrefab = effectivePrefabs[prefabIdx];
                if (chosenPrefab != null)
                {
                    GameObject spawnerObj = new GameObject($"EnemySpawner_{i + 1}_{chosenPrefab.name}");
                    spawnerObj.transform.SetParent(enemyRoot.transform, false);
                    spawnerObj.transform.position = spawnPos;
                    spawnerObj.transform.rotation = Quaternion.Euler(0, rng.Range(0, 360), 0);

                    EnemySpawner spawner = spawnerObj.AddComponent<EnemySpawner>();
                    spawner.enemyPrefab = chosenPrefab;
                    spawner.spawnOnAwake = false;
                    spawner.spawnOnlyAtNight = false;
                    spawner.SpawnEnemy();

                    cityGen.EnemySpawnPoints.Add(spawnPos);
                }
            }
        }
    }
}
