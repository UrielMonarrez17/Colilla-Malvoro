using UnityEngine;
using UnityEngine.AI;

namespace Prototipo3
{
    /// <summary>
    /// Ajusta dinámicamente NavMeshAgent.baseOffset hacia la superficie física real (asfalto, acera o piso del refugio),
    /// compensando el desfase vertical de la malla NavMesh horneada y cualquier margen residual bajo las patas del modelo.
    /// </summary>
    public sealed class NavMeshAgentGroundAligner
    {
        private static readonly RaycastHit[] SharedHits = new RaycastHit[16];

        private float nextSampleTime;
        private Vector3 lastSampleXZ;
        private float targetBaseOffset;
        private float initialFootClearance;
        private bool initialized;

        public void Initialize(Transform root, NavMeshAgent agent)
        {
            if (root == null) return;

            float minRendererY = float.MaxValue;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled) continue;
                if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || r is SpriteRenderer) continue;
                if (r.gameObject.name.IndexOf("VisionCone", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;

                minRendererY = Mathf.Min(minRendererY, r.bounds.min.y);
            }

            if (minRendererY < float.MaxValue && minRendererY > root.position.y + 0.015f)
            {
                initialFootClearance = Mathf.Clamp(minRendererY - root.position.y, 0f, 0.22f);
            }
            else
            {
                initialFootClearance = 0f;
            }

            initialized = true;
            SnapImmediate(root, agent);
        }

        public void SnapImmediate(Transform root, NavMeshAgent agent)
        {
            if (root == null || agent == null || !agent.enabled) return;

            targetBaseOffset = ComputeTargetBaseOffset(root, agent);
            agent.baseOffset = targetBaseOffset;
            lastSampleXZ = new Vector3(root.position.x, 0f, root.position.z);
            nextSampleTime = Time.time + 0.08f;
        }

        public void Tick(Transform root, NavMeshAgent agent)
        {
            if (root == null || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

            if (!initialized)
            {
                Initialize(root, agent);
                return;
            }

            float dx = root.position.x - lastSampleXZ.x;
            float dz = root.position.z - lastSampleXZ.z;
            if ((dx * dx + dz * dz) > 0.0625f || Time.time >= nextSampleTime)
            {
                targetBaseOffset = ComputeTargetBaseOffset(root, agent);
                lastSampleXZ = new Vector3(root.position.x, 0f, root.position.z);
                nextSampleTime = Time.time + 0.08f;
            }

            agent.baseOffset = Mathf.MoveTowards(agent.baseOffset, targetBaseOffset, 4.5f * Time.deltaTime);
        }

        private float ComputeTargetBaseOffset(Transform root, NavMeshAgent agent)
        {
            float navY = root.position.y - agent.baseOffset;
            Vector3 origin = new Vector3(root.position.x, navY + 1.2f, root.position.z);
            int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, SharedHits, 2.4f, ~0, QueryTriggerInteraction.Ignore);

            bool found = false;
            float bestY = navY;
            float bestDistToNav = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = SharedHits[i];
                Collider col = hit.collider;
                if (col == null || !col.enabled || col.isTrigger) continue;
                if (col.transform == root || col.transform.IsChildOf(root)) continue;
                if (hit.normal.y < 0.65f) continue;

                if (col.CompareTag("Enemy") || col.CompareTag("Player") || col.CompareTag("Interactable") || col.CompareTag("Door")) continue;
                if (col.GetComponentInParent<NavMeshAgent>() != null ||
                    col.GetComponentInParent<HamsterController>() != null ||
                    col.GetComponentInParent<LootContainer>() != null)
                {
                    continue;
                }

                if (col.GetComponentInParent<BuildingData>() != null || col.GetComponentInParent<CitySocket>() != null)
                {
                    string n = col.gameObject.name;
                    if (!n.Contains("Pavement") && !n.Contains("Floor") && !n.Contains("Ground")) continue;
                }

                float y = hit.point.y;
                if (y < navY - 0.65f || y > navY + 0.35f) continue;

                float dist = Mathf.Abs(y - navY);
                if (dist < bestDistToNav)
                {
                    bestDistToNav = dist;
                    bestY = y;
                    found = true;
                }
            }

            if (!found) return agent.baseOffset;

            return Mathf.Clamp((bestY - navY) - initialFootClearance, -0.55f, 0.15f);
        }
    }
}
