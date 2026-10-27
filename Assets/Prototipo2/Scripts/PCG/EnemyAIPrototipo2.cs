using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Prototipo2
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyAIPrototipo2 : EnemyAI
    {
        [Header("Ajustes de Ciudad (Prototipo2)")]
        [Tooltip("Altura mínima del suelo de la ciudad.")]
        public float minCitySurfaceY = 0f;

        [Tooltip("Distancia de chequeo frontal para evitar atravesar colliders de edificios.")]
        public float buildingCheckDistance = 1.6f;

        private NavMeshAgent protoAgent;
        private Rigidbody protoRb;
        private float obstacleRedirectCooldown = 0f;
        private Vector3 lastValidCityPosition;

        private void Awake()
        {
            // Desactivar scripts de demostración / input del paquete Animals_FREE
            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            for (int i = 0; i < scripts.Length; i++)
            {
                if (scripts[i] == null || scripts[i] == this) continue;
                string typeName = scripts[i].GetType().Name;
                if (typeName == "AnimationShowcase" || typeName == "MovePlayerInput" || typeName == "CreatureMover")
                {
                    scripts[i].enabled = false;
                }
            }

            if (!gameObject.CompareTag("Enemy"))
            {
                gameObject.tag = "Enemy";
            }

            // Asegurar que no use waypoints de patrulla y tenga un radio amplio de deambulación
            SetPatrolWaypoints(null);
            if (wanderRadius < 30f)
            {
                wanderRadius = 35f;
            }
        }

        private void OnEnable()
        {
            protoAgent = GetComponent<NavMeshAgent>();
            protoRb = GetComponent<Rigidbody>();

            if (CityGeneratorPrototipo2.Instance != null)
            {
                minCitySurfaceY = CityGeneratorPrototipo2.Instance.roadHeightY;
            }

            ConfigureAgentForCity();
            SnapToCitySurfaceImmediate();
            SetPatrolWaypoints(null);
        }

        private void ConfigureAgentForCity()
        {
            if (protoAgent == null) protoAgent = GetComponent<NavMeshAgent>();
            if (protoAgent != null)
            {
                protoAgent.baseOffset = 0f;
                protoAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                protoAgent.autoRepath = true;
            }

            if (protoRb == null) protoRb = GetComponent<Rigidbody>();
            if (protoRb != null)
            {
                protoRb.isKinematic = true;
                protoRb.useGravity = false;
            }
        }

        private void LateUpdate()
        {
            if (CityGeneratorPrototipo2.Instance != null)
            {
                minCitySurfaceY = CityGeneratorPrototipo2.Instance.roadHeightY;
            }

            EnforceCitySurfaceHeight();
            PreventBuildingPenetration();
        }

        /// <summary>
        /// Garantiza que el enemigo se mantenga sobre la superficie de la ciudad horneada (Y >= minCitySurfaceY).
        /// </summary>
        public void SnapToCitySurfaceImmediate()
        {
            Vector3 pos = transform.position;
            float targetY = FindCitySurfaceHeight(pos);
            pos.y = targetY;
            transform.position = pos;
            lastValidCityPosition = pos;

            if (protoAgent != null && protoAgent.enabled)
            {
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                {
                    protoAgent.Warp(hit.position);
                    lastValidCityPosition = hit.position;
                }
            }
        }

        private void EnforceCitySurfaceHeight()
        {
            Vector3 pos = transform.position;
            if (pos.y < minCitySurfaceY - 0.15f)
            {
                float surfaceY = FindCitySurfaceHeight(pos);
                pos.y = surfaceY;

                if (protoAgent != null && protoAgent.enabled && protoAgent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
                    {
                        protoAgent.Warp(hit.position);
                        lastValidCityPosition = hit.position;
                    }
                    else
                    {
                        transform.position = pos;
                    }
                }
                else
                {
                    transform.position = pos;
                }
            }
            else
            {
                lastValidCityPosition = pos;
            }
        }

        private float FindCitySurfaceHeight(Vector3 worldPos)
        {
            if (NavMesh.SamplePosition(worldPos, out NavMeshHit navHit, 4.0f, NavMesh.AllAreas))
            {
                return navHit.position.y;
            }

            Vector3 rayOrigin = new Vector3(worldPos.x, Mathf.Max(worldPos.y, minCitySurfaceY) + 6f, worldPos.z);
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);

            float bestY = minCitySurfaceY;
            float closestDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null) continue;
                if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
                if (col.GetComponentInParent<EnemyAI>() != null) continue;
                if (col.GetComponentInParent<HamsterController>() != null) continue;

                // Ignorar techos de edificios altos; queremos el suelo/acera de la ciudad cerca de minCitySurfaceY
                if (hits[i].point.y >= minCitySurfaceY - 0.3f && hits[i].point.y <= minCitySurfaceY + 0.6f)
                {
                    float d = Mathf.Abs(hits[i].point.y - minCitySurfaceY);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        bestY = hits[i].point.y;
                    }
                }
            }

            return bestY;
        }

        /// <summary>
        /// Verifica si el enemigo está chocando o a punto de atravesar el BoxCollider de un edificio
        /// y corrige su posición/ruta inmediatamente.
        /// </summary>
        private void PreventBuildingPenetration()
        {
            if (protoAgent == null || !protoAgent.enabled || !protoAgent.isOnNavMesh) return;

            if (obstacleRedirectCooldown > 0f)
            {
                obstacleRedirectCooldown -= Time.deltaTime;
            }

            Vector3 checkCenter = transform.position + Vector3.up * 0.6f;
            float checkRadius = Mathf.Max(0.45f, protoAgent.radius);

            // 1. Verificar solapamiento directo con edificios
            Collider[] overlaps = Physics.OverlapSphere(checkCenter, checkRadius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider col = overlaps[i];
                if (col == null || col.transform == transform || col.transform.IsChildOf(transform)) continue;

                if (IsBuildingCollider(col))
                {
                    // Empujar fuera del collider del edificio
                    if (Physics.ComputePenetration(
                        GetComponent<Collider>(), transform.position, transform.rotation,
                        col, col.transform.position, col.transform.rotation,
                        out Vector3 pushDir, out float pushDist))
                    {
                        pushDir.y = 0f;
                        if (pushDir.sqrMagnitude > 0.001f)
                        {
                            pushDir.Normalize();
                            Vector3 safePos = transform.position + pushDir * (pushDist + 0.25f);
                            safePos.y = Mathf.Max(safePos.y, minCitySurfaceY);
                            if (NavMesh.SamplePosition(safePos, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas) && navHit.position.y >= minCitySurfaceY - 0.2f)
                            {
                                protoAgent.Warp(navHit.position);
                            }
                            else
                            {
                                transform.position = safePos;
                            }
                        }
                    }

                    RedirectAwayFromBuilding(col.bounds.center);
                    return;
                }
            }

            // 2. Raycast frontal en la dirección de avance para desviar antes de entrar al edificio
            Vector3 velocity = protoAgent.velocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 0.1f && obstacleRedirectCooldown <= 0f)
            {
                Vector3 moveDir = velocity.normalized;
                if (Physics.SphereCast(checkCenter, checkRadius * 0.7f, moveDir, out RaycastHit hit, buildingCheckDistance, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (IsBuildingCollider(hit.collider))
                    {
                        RedirectAwayFromBuilding(hit.point);
                    }
                }
            }
        }

        private bool IsBuildingCollider(Collider col)
        {
            if (col == null) return false;
            if (col.GetComponentInParent<BuildingData>() != null || col.GetComponentInParent<CitySocket>() != null)
            {
                // El objeto hijo "Pavement" de los edificios es acera caminable, no muro del edificio
                if (col.gameObject.name.Contains("Pavement")) return false;
                return true;
            }
            return false;
        }

        private void RedirectAwayFromBuilding(Vector3 obstaclePoint)
        {
            if (obstacleRedirectCooldown > 0f || protoAgent == null || !protoAgent.isOnNavMesh) return;
            obstacleRedirectCooldown = 0.6f;

            if (CurrentState == EnemyState.Wander)
            {
                Vector3 awayDir = transform.position - obstaclePoint;
                awayDir.y = 0f;
                if (awayDir.sqrMagnitude < 0.01f) awayDir = -transform.forward;
                awayDir.Normalize();

                for (int attempt = 0; attempt < 12; attempt++)
                {
                    Vector3 randomOffset = Quaternion.Euler(0f, UnityEngine.Random.Range(-110f, 110f), 0f) * awayDir;
                    float dist = UnityEngine.Random.Range(8f, wanderRadius);
                    Vector3 candidate = transform.position + randomOffset * dist;
                    candidate.y = minCitySurfaceY;

                    if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 6.0f, NavMesh.AllAreas) && hit.position.y >= minCitySurfaceY - 0.2f)
                    {
                        protoAgent.SetDestination(hit.position);
                        break;
                    }
                }
            }
        }
    }
}
