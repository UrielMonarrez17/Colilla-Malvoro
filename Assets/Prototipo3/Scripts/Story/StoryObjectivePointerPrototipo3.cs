using UnityEngine;

namespace Prototipo3
{
    /// <summary>
    /// Flecha indicadora 3D sobre Kilo + baliza luminosa vertical sobre el objetivo actual de la historia lineal.
    /// </summary>
    public class StoryObjectivePointerPrototipo3 : MonoBehaviour
    {
        private Transform targetTransform;
        private Vector3? fallbackWorldPoint;
        private HamsterController trackedPlayer;
        private GameObject playerCompassArrow;
        private GameObject targetBeaconPillar;

        public void SetTarget(Transform target, Vector3? worldPointFallback = null)
        {
            targetTransform = target;
            fallbackWorldPoint = worldPointFallback;
        }

        public void ClearTarget()
        {
            targetTransform = null;
            fallbackWorldPoint = null;
            if (playerCompassArrow != null) playerCompassArrow.SetActive(false);
            if (targetBeaconPillar != null) targetBeaconPillar.SetActive(false);
        }

        private void EnsureVisuals()
        {
            if (playerCompassArrow == null)
            {
                playerCompassArrow = new GameObject("StoryCompassArrow");
                playerCompassArrow.transform.SetParent(transform, false);

                GameObject arrowFbx = Resources.Load<GameObject>("Props/SM_MissionArrow");
                if (arrowFbx != null)
                {
                    GameObject vis = Instantiate(arrowFbx, playerCompassArrow.transform, false);
                    vis.transform.localScale = Vector3.one * 0.65f;
                }
                else
                {
                    Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                    Material goldMat = new Material(sh);
                    Color c = new Color(1f, 0.78f, 0.16f, 0.92f);
                    goldMat.color = c;
                    if (goldMat.HasProperty("_BaseColor")) goldMat.SetColor("_BaseColor", c);

                    GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(shaft.GetComponent<Collider>());
                    shaft.transform.SetParent(playerCompassArrow.transform, false);
                    shaft.transform.localPosition = new Vector3(0f, 0f, 0.15f);
                    shaft.transform.localScale = new Vector3(0.16f, 0.08f, 0.55f);
                    shaft.GetComponent<Renderer>().material = goldMat;

                    GameObject tipL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(tipL.GetComponent<Collider>());
                    tipL.transform.SetParent(playerCompassArrow.transform, false);
                    tipL.transform.localPosition = new Vector3(-0.14f, 0f, 0.36f);
                    tipL.transform.localRotation = Quaternion.Euler(0f, 38f, 0f);
                    tipL.transform.localScale = new Vector3(0.12f, 0.08f, 0.36f);
                    tipL.GetComponent<Renderer>().material = goldMat;

                    GameObject tipR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(tipR.GetComponent<Collider>());
                    tipR.transform.SetParent(playerCompassArrow.transform, false);
                    tipR.transform.localPosition = new Vector3(0.14f, 0f, 0.36f);
                    tipR.transform.localRotation = Quaternion.Euler(0f, -38f, 0f);
                    tipR.transform.localScale = new Vector3(0.12f, 0.08f, 0.36f);
                    tipR.GetComponent<Renderer>().material = goldMat;
                }
            }

            if (targetBeaconPillar == null)
            {
                targetBeaconPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                targetBeaconPillar.name = "StoryObjectiveBeacon";
                targetBeaconPillar.transform.SetParent(transform, false);
                Destroy(targetBeaconPillar.GetComponent<Collider>());
                targetBeaconPillar.transform.localScale = new Vector3(1.1f, 6.5f, 1.1f);

                Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                Material beamMat = new Material(sh);
                Color col = new Color(1f, 0.78f, 0.22f, 0.24f);
                beamMat.color = col;
                if (beamMat.HasProperty("_BaseColor")) beamMat.SetColor("_BaseColor", col);
                if (beamMat.HasProperty("_Surface"))
                {
                    beamMat.SetFloat("_Surface", 1f);
                    beamMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    beamMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    beamMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    beamMat.SetInt("_ZWrite", 0);
                    beamMat.renderQueue = 3550;
                }
                Renderer ren = targetBeaconPillar.GetComponent<Renderer>();
                ren.material = beamMat;
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private void LateUpdate()
        {
            Vector3? dest = null;
            if (targetTransform != null && targetTransform.gameObject.activeInHierarchy)
            {
                dest = targetTransform.position;
            }
            else if (fallbackWorldPoint.HasValue)
            {
                dest = fallbackWorldPoint.Value;
            }

            if (!dest.HasValue || PortraitDialogueUIPrototipo3.IsConversationActive)
            {
                if (playerCompassArrow != null) playerCompassArrow.SetActive(false);
                if (targetBeaconPillar != null) targetBeaconPillar.SetActive(false);
                return;
            }

            if (trackedPlayer == null)
            {
                trackedPlayer = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
                if (trackedPlayer == null) return;
            }

            EnsureVisuals();

            Vector3 targetPos = dest.Value;
            Vector3 playerPos = trackedPlayer.transform.position;
            Vector3 flatDelta = new Vector3(targetPos.x - playerPos.x, 0f, targetPos.z - playerPos.z);
            float dist = flatDelta.magnitude;

            if (dist > 2.6f)
            {
                playerCompassArrow.SetActive(true);
                float bob = Mathf.Sin(Time.time * 4.2f) * 0.08f;
                playerCompassArrow.transform.position = playerPos + Vector3.up * (1.25f + bob) + flatDelta.normalized * 0.85f;
                playerCompassArrow.transform.rotation = Quaternion.LookRotation(flatDelta.normalized, Vector3.up);
            }
            else
            {
                playerCompassArrow.SetActive(false);
            }

            targetBeaconPillar.SetActive(dist > 3.2f);
            float pulse = 0.92f + 0.16f * Mathf.Sin(Time.time * 3.5f);
            targetBeaconPillar.transform.position = new Vector3(targetPos.x, targetPos.y + 6.5f, targetPos.z);
            targetBeaconPillar.transform.localScale = new Vector3(0.95f * pulse, 6.5f, 0.95f * pulse);
        }
    }
}
