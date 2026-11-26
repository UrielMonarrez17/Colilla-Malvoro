using UnityEngine;
using UnityEngine.UI;

namespace Prototipo3
{
    /// <summary>
    /// Sistema doble de guía de misión a prueba de fallos:
    /// 1) Flecha 3D dorada brillante (malla procedural orientada matemáticamente a +Z) flotando sobre Kilo.
    /// 2) Baliza luminosa vertical sobre el objetivo en el mundo.
    /// 3) Indicador direccional en pantalla (HUD Compass Badge) que muestra rumbo y distancia en metros incluso fuera de cámara.
    /// </summary>
    public class StoryObjectivePointerPrototipo3 : MonoBehaviour
    {
        private Transform targetTransform;
        private Vector3? fallbackWorldPoint;
        private HamsterController trackedPlayer;

        private GameObject playerCompassArrow;
        private GameObject targetBeaconPillar;

        private Canvas pointerHudCanvas;
        private RectTransform hudBadgeRt;
        private RectTransform hudArrowIconRt;
        private Text hudDistanceLabel;

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
            if (hudBadgeRt != null) hudBadgeRt.gameObject.SetActive(false);
        }

        private static Mesh BuildForwardChevronArrowMesh()
        {
            // Punta exacta en +Z (+0.68), alas en (-0.42, -0.10) y (+0.42, -0.10), cola en -0.52 -> 100% +Z forward!
            Mesh m = new Mesh { name = "ProceduralMissionChevron" };
            float h = 0.09f;
            Vector2[] poly =
            {
                new Vector2(0f, 0.68f),       // 0: Punta frontal (+Z)
                new Vector2(0.44f, -0.05f),   // 1: Ala derecha exterior
                new Vector2(0.18f, -0.05f),   // 2: Ala derecha interior
                new Vector2(0.18f, -0.52f),   // 3: Cola derecha
                new Vector2(-0.18f, -0.52f),  // 4: Cola izquierda
                new Vector2(-0.18f, -0.05f),  // 5: Ala izquierda interior
                new Vector2(-0.44f, -0.05f),  // 6: Ala izquierda exterior
            };

            int n = poly.Length;
            Vector3[] verts = new Vector3[n * 2];
            for (int i = 0; i < n; i++)
            {
                verts[i] = new Vector3(poly[i].x, h, poly[i].y);
                verts[i + n] = new Vector3(poly[i].x, -h, poly[i].y);
            }

            int[] topTris =
            {
                0, 1, 2,
                0, 2, 5,
                0, 5, 6,
                5, 2, 3,
                5, 3, 4
            };

            var tris = new System.Collections.Generic.List<int>();
            // Cara superior
            tris.AddRange(topTris);
            // Cara inferior
            for (int i = 0; i < topTris.Length; i += 3)
            {
                tris.Add(topTris[i] + n);
                tris.Add(topTris[i + 2] + n);
                tris.Add(topTris[i + 1] + n);
            }
            // Bordes laterales para volumen sólido
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                tris.Add(i); tris.Add(i + n); tris.Add(next);
                tris.Add(next); tris.Add(i + n); tris.Add(next + n);
            }

            m.vertices = verts;
            m.triangles = tris.ToArray();
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private void EnsureVisuals()
        {
            if (playerCompassArrow == null)
            {
                playerCompassArrow = new GameObject("StoryCompassArrow_3D");
                playerCompassArrow.layer = LayerMask.NameToLayer("Ignore Raycast") >= 0 ? LayerMask.NameToLayer("Ignore Raycast") : 0;
                playerCompassArrow.transform.SetParent(transform, false);

                Mesh arrowMesh = BuildForwardChevronArrowMesh();
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

                // Borde exterior oscuro para alto contraste sobre pavimento claro u oscuro
                GameObject outlineGo = new GameObject("ArrowOutline");
                outlineGo.transform.SetParent(playerCompassArrow.transform, false);
                outlineGo.transform.localPosition = new Vector3(0f, -0.02f, 0f);
                outlineGo.transform.localScale = new Vector3(1.22f, 0.85f, 1.18f);
                outlineGo.AddComponent<MeshFilter>().sharedMesh = arrowMesh;
                Material darkMat = new Material(sh);
                Color darkCol = new Color(0.08f, 0.05f, 0.03f, 1f);
                darkMat.color = darkCol;
                if (darkMat.HasProperty("_BaseColor")) darkMat.SetColor("_BaseColor", darkCol);
                MeshRenderer darkMr = outlineGo.AddComponent<MeshRenderer>();
                darkMr.sharedMaterial = darkMat;
                darkMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                // Núcleo dorado brillante
                GameObject coreGo = new GameObject("ArrowGoldCore");
                coreGo.transform.SetParent(playerCompassArrow.transform, false);
                coreGo.transform.localScale = Vector3.one;
                coreGo.AddComponent<MeshFilter>().sharedMesh = arrowMesh;
                Material goldMat = new Material(sh);
                Color goldCol = new Color(1f, 0.82f, 0.14f, 1f);
                goldMat.color = goldCol;
                if (goldMat.HasProperty("_BaseColor")) goldMat.SetColor("_BaseColor", goldCol);
                MeshRenderer goldMr = coreGo.AddComponent<MeshRenderer>();
                goldMr.sharedMaterial = goldMat;
                goldMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                playerCompassArrow.transform.localScale = Vector3.one * 1.15f;
            }

            if (targetBeaconPillar == null)
            {
                targetBeaconPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                targetBeaconPillar.name = "StoryObjectiveBeacon";
                targetBeaconPillar.transform.SetParent(transform, false);
                Destroy(targetBeaconPillar.GetComponent<Collider>());

                Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                Material beamMat = new Material(sh);
                Color col = new Color(1f, 0.80f, 0.18f, 0.30f);
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

            if (pointerHudCanvas == null)
            {
                GameObject cGo = new GameObject("StoryCompassHudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                cGo.transform.SetParent(transform, false);
                pointerHudCanvas = cGo.GetComponent<Canvas>();
                pointerHudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                pointerHudCanvas.sortingOrder = 148;

                CanvasScaler sc = cGo.GetComponent<CanvasScaler>();
                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                sc.referenceResolution = new Vector2(1920f, 1080f);
                sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                sc.matchWidthOrHeight = 0.5f;

                GameObject badge = new GameObject("ObjectiveMarkerBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(AngularComicBoxGraphic));
                badge.transform.SetParent(cGo.transform, false);
                hudBadgeRt = badge.GetComponent<RectTransform>();
                hudBadgeRt.anchorMin = hudBadgeRt.anchorMax = hudBadgeRt.pivot = new Vector2(0.5f, 0.5f);
                hudBadgeRt.sizeDelta = new Vector2(178f, 44f);
                AngularComicBoxGraphic bg = badge.GetComponent<AngularComicBoxGraphic>();
                bg.color = new Color(0.08f, 0.07f, 0.06f, 0.90f);
                bg.raycastTarget = false;

                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                GameObject iconGo = new GameObject("DirArrow", typeof(RectTransform), typeof(Text));
                iconGo.transform.SetParent(hudBadgeRt, false);
                hudArrowIconRt = iconGo.GetComponent<RectTransform>();
                hudArrowIconRt.anchorMin = hudArrowIconRt.anchorMax = hudArrowIconRt.pivot = new Vector2(0.5f, 0.5f);
                hudArrowIconRt.anchoredPosition = new Vector2(-58f, 0f);
                hudArrowIconRt.sizeDelta = new Vector2(36f, 36f);
                Text iconTxt = iconGo.GetComponent<Text>();
                iconTxt.font = font;
                iconTxt.fontSize = 24;
                iconTxt.fontStyle = FontStyle.Bold;
                iconTxt.color = new Color(1f, 0.80f, 0.18f, 1f);
                iconTxt.alignment = TextAnchor.MiddleCenter;
                iconTxt.text = "▲";
                iconTxt.raycastTarget = false;

                GameObject distGo = new GameObject("DistText", typeof(RectTransform), typeof(Text));
                distGo.transform.SetParent(hudBadgeRt, false);
                RectTransform dRt = distGo.GetComponent<RectTransform>();
                dRt.anchorMin = Vector2.zero;
                dRt.anchorMax = Vector2.one;
                dRt.offsetMin = new Vector2(44f, 0f);
                dRt.offsetMax = new Vector2(-10f, 0f);
                hudDistanceLabel = distGo.GetComponent<Text>();
                hudDistanceLabel.font = font;
                hudDistanceLabel.fontSize = 18;
                hudDistanceLabel.fontStyle = FontStyle.Bold;
                hudDistanceLabel.color = new Color(0.98f, 0.92f, 0.78f, 1f);
                hudDistanceLabel.alignment = TextAnchor.MiddleLeft;
                hudDistanceLabel.raycastTarget = false;

                hudBadgeRt.gameObject.SetActive(false);
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

            if (!dest.HasValue || PortraitDialogueUIPrototipo3.IsConversationActive || UI.FeralGameUI.BlocksGameplay || StoryDirectorPrototipo3.IsDemoVictoryOpen)
            {
                if (playerCompassArrow != null) playerCompassArrow.SetActive(false);
                if (targetBeaconPillar != null) targetBeaconPillar.SetActive(false);
                if (hudBadgeRt != null) hudBadgeRt.gameObject.SetActive(false);
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

            // 1. Flecha 3D dorada flotando directamente encima de Kilo apuntando con interpolación suave hacia el objetivo
            if (dist > 2.2f)
            {
                playerCompassArrow.SetActive(true);
                float bob = Mathf.Sin(Time.time * 4.5f) * 0.12f;
                Vector3 arrowPos = playerPos + Vector3.up * (1.58f + bob) + flatDelta.normalized * 0.65f;
                playerCompassArrow.transform.position = arrowPos;
                Quaternion desiredRot = Quaternion.LookRotation(flatDelta.normalized, Vector3.up);
                playerCompassArrow.transform.rotation = Quaternion.Slerp(playerCompassArrow.transform.rotation, desiredRot, Time.deltaTime * 14f);
            }
            else
            {
                playerCompassArrow.SetActive(false);
            }

            // 2. Columna de luz en el objetivo
            targetBeaconPillar.SetActive(dist > 2.8f);
            float pulse = 0.90f + 0.18f * Mathf.Sin(Time.time * 3.8f);
            targetBeaconPillar.transform.position = new Vector3(targetPos.x, targetPos.y + 5.5f, targetPos.z);
            targetBeaconPillar.transform.localScale = new Vector3(1.1f * pulse, 5.5f, 1.1f * pulse);

            // 3. Indicador HUD en pantalla (siempre visible tanto cerca como a través de toda la ciudad)
            UpdateScreenCompassBadge(targetPos + Vector3.up * 1.8f, dist);
        }

        private void UpdateScreenCompassBadge(Vector3 worldTarget, float distMeters)
        {
            Camera cam = Camera.main;
            if (cam == null || hudBadgeRt == null) return;

            hudBadgeRt.gameObject.SetActive(true);
            hudDistanceLabel.text = $"{Mathf.Max(1, Mathf.RoundToInt(distMeters))} m";

            Vector3 vp = cam.WorldToViewportPoint(worldTarget);
            bool behind = vp.z < 0f;
            if (behind)
            {
                vp.x = 1f - vp.x;
                vp.y = 1f - vp.y;
            }

            Vector2 centerOffset = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            bool onScreen = !behind && vp.x >= 0.08f && vp.x <= 0.92f && vp.y >= 0.14f && vp.y <= 0.86f;

            RectTransform canvasRt = pointerHudCanvas.GetComponent<RectTransform>();
            Vector2 halfCanvas = canvasRt.sizeDelta * 0.5f;

            if (onScreen)
            {
                hudBadgeRt.anchoredPosition = new Vector2(centerOffset.x * canvasRt.sizeDelta.x, centerOffset.y * canvasRt.sizeDelta.y + 28f);
                hudArrowIconRt.localRotation = Quaternion.Euler(0f, 0f, 180f); // Apunta hacia abajo sobre el objetivo
            }
            else
            {
                Vector2 dir = centerOffset.sqrMagnitude > 0.0001f ? centerOffset.normalized : Vector2.up;
                Vector2 edgePos = new Vector2(dir.x * (halfCanvas.x - 140f), dir.y * (halfCanvas.y - 115f));
                hudBadgeRt.anchoredPosition = edgePos;
                float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                hudArrowIconRt.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
            }
        }
    }
}
