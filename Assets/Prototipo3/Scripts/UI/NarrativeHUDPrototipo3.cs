using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Prototipo3.Narrative;

namespace Prototipo3.UI
{
    public class NarrativeHUDPrototipo3 : MonoBehaviour
    {
        [Header("UI Containers")]
        public Canvas targetCanvas;

        private GameObject panelMisionObj;
        private TextMeshProUGUI textoTituloMision;
        private TextMeshProUGUI textoDescripcionMision;
        private TextMeshProUGUI textoGrafoBreadcrumb;
        private TextMeshProUGUI textoInventarioNarrativo;
        private TextMeshProUGUI textoDialogoPopup;
        private GameObject panelDialogoObj;
        private GameObject panelVictoriaObj;

        private void Start()
        {
            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
                if (targetCanvas == null)
                {
                    GameObject canvasObj = new GameObject("Prototipo3_HUD_Canvas");
                    targetCanvas = canvasObj.AddComponent<Canvas>();
                    targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
                    cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    cs.referenceResolution = new Vector2(1920f, 1080f);
                    canvasObj.AddComponent<GraphicRaycaster>();
                }
            }

            ConstruirUI();

            if (StoryManagerPrototipo3.Instance != null)
            {
                StoryManagerPrototipo3.Instance.OnActiveNodeChanged += ActualizarMision;
                StoryManagerPrototipo3.Instance.OnDialogueTriggered += MostrarDialogo;
                StoryManagerPrototipo3.Instance.OnStoryCompleted += MostrarVictoria;

                if (StoryManagerPrototipo3.Instance.CurrentActiveNode != null)
                {
                    ActualizarMision(StoryManagerPrototipo3.Instance.CurrentActiveNode);
                }
            }
        }

        private void OnDestroy()
        {
            if (StoryManagerPrototipo3.Instance != null)
            {
                StoryManagerPrototipo3.Instance.OnActiveNodeChanged -= ActualizarMision;
                StoryManagerPrototipo3.Instance.OnDialogueTriggered -= MostrarDialogo;
                StoryManagerPrototipo3.Instance.OnStoryCompleted -= MostrarVictoria;
            }
        }

        private void ConstruirUI()
        {
            if (targetCanvas == null) return;

            // Panel de Misión (Esquina Superior Izquierda)
            panelMisionObj = new GameObject("Panel_Mision_Prototipo3");
            panelMisionObj.transform.SetParent(targetCanvas.transform, false);
            RectTransform rtMision = panelMisionObj.AddComponent<RectTransform>();
            rtMision.anchorMin = new Vector2(0f, 1f);
            rtMision.anchorMax = new Vector2(0f, 1f);
            rtMision.pivot = new Vector2(0f, 1f);
            rtMision.anchoredPosition = new Vector2(25f, -25f);
            rtMision.sizeDelta = new Vector2(480f, 210f);

            Image bgMision = panelMisionObj.AddComponent<Image>();
            bgMision.color = new Color(0.06f, 0.05f, 0.12f, 0.88f);

            // Título de la Misión
            GameObject tituloObj = new GameObject("Texto_TituloMision");
            tituloObj.transform.SetParent(panelMisionObj.transform, false);
            RectTransform rtTitulo = tituloObj.AddComponent<RectTransform>();
            rtTitulo.anchorMin = new Vector2(0f, 1f);
            rtTitulo.anchorMax = new Vector2(1f, 1f);
            rtTitulo.pivot = new Vector2(0.5f, 1f);
            rtTitulo.anchoredPosition = new Vector2(0f, -12f);
            rtTitulo.sizeDelta = new Vector2(-24f, 32f);

            textoTituloMision = tituloObj.AddComponent<TextMeshProUGUI>();
            textoTituloMision.fontSize = 20f;
            textoTituloMision.fontStyle = FontStyles.Bold;
            textoTituloMision.color = new Color(1f, 0.88f, 0.35f, 1f);
            textoTituloMision.text = "PROTOTIPO 3: HISTORIA PROCEDURAL";

            // Descripción y Pistas
            GameObject descObj = new GameObject("Texto_DescMision");
            descObj.transform.SetParent(panelMisionObj.transform, false);
            RectTransform rtDesc = descObj.AddComponent<RectTransform>();
            rtDesc.anchorMin = new Vector2(0f, 1f);
            rtDesc.anchorMax = new Vector2(1f, 1f);
            rtDesc.pivot = new Vector2(0.5f, 1f);
            rtDesc.anchoredPosition = new Vector2(0f, -48f);
            rtDesc.sizeDelta = new Vector2(-24f, 75f);

            textoDescripcionMision = descObj.AddComponent<TextMeshProUGUI>();
            textoDescripcionMision.fontSize = 15f;
            textoDescripcionMision.color = new Color(0.92f, 0.94f, 0.98f, 0.95f);
            textoDescripcionMision.text = "Iniciando secuencia de escape...";

            // Contador de Items Narrativos (Filtros Carmesí / Keycard)
            GameObject invObj = new GameObject("Texto_InventarioNarrativo");
            invObj.transform.SetParent(panelMisionObj.transform, false);
            RectTransform rtInv = invObj.AddComponent<RectTransform>();
            rtInv.anchorMin = new Vector2(0f, 0f);
            rtInv.anchorMax = new Vector2(1f, 0f);
            rtInv.pivot = new Vector2(0.5f, 0f);
            rtInv.anchoredPosition = new Vector2(0f, 34f);
            rtInv.sizeDelta = new Vector2(-24f, 26f);

            textoInventarioNarrativo = invObj.AddComponent<TextMeshProUGUI>();
            textoInventarioNarrativo.fontSize = 13f;
            textoInventarioNarrativo.fontStyle = FontStyles.Bold;
            textoInventarioNarrativo.color = new Color(1f, 0.35f, 0.35f, 1f);
            textoInventarioNarrativo.text = "Filtros Carmesí: 0/3 | Tarjeta Torre: NO";

            // Breadcrumb del Grafo Narrativo
            GameObject breadcrumbObj = new GameObject("Texto_GrafoBreadcrumb");
            breadcrumbObj.transform.SetParent(panelMisionObj.transform, false);
            RectTransform rtBreadcrumb = breadcrumbObj.AddComponent<RectTransform>();
            rtBreadcrumb.anchorMin = new Vector2(0f, 0f);
            rtBreadcrumb.anchorMax = new Vector2(1f, 0f);
            rtBreadcrumb.pivot = new Vector2(0.5f, 0f);
            rtBreadcrumb.anchoredPosition = new Vector2(0f, 8f);
            rtBreadcrumb.sizeDelta = new Vector2(-24f, 22f);

            textoGrafoBreadcrumb = breadcrumbObj.AddComponent<TextMeshProUGUI>();
            textoGrafoBreadcrumb.fontSize = 11f;
            textoGrafoBreadcrumb.color = new Color(0.6f, 0.7f, 0.85f, 0.8f);
            textoGrafoBreadcrumb.text = "[Madriguera] -> [Lulú] -> [Filtros] -> [Ciervo] -> [Barón] -> [Torre] -> [Reactor]";

            // Popup de Diálogo Narrativo (Inferior Central)
            panelDialogoObj = new GameObject("Panel_Dialogo_Prototipo3");
            panelDialogoObj.transform.SetParent(targetCanvas.transform, false);
            RectTransform rtDialogo = panelDialogoObj.AddComponent<RectTransform>();
            rtDialogo.anchorMin = new Vector2(0.5f, 0f);
            rtDialogo.anchorMax = new Vector2(0.5f, 0f);
            rtDialogo.pivot = new Vector2(0.5f, 0f);
            rtDialogo.anchoredPosition = new Vector2(0f, 50f);
            rtDialogo.sizeDelta = new Vector2(850f, 130f);

            Image bgDialogo = panelDialogoObj.AddComponent<Image>();
            bgDialogo.color = new Color(0.04f, 0.03f, 0.08f, 0.94f);

            GameObject textoDialogoChild = new GameObject("Texto_DialogoPopup");
            textoDialogoChild.transform.SetParent(panelDialogoObj.transform, false);
            RectTransform rtTextoDialogo = textoDialogoChild.AddComponent<RectTransform>();
            rtTextoDialogo.anchorMin = new Vector2(0f, 0f);
            rtTextoDialogo.anchorMax = new Vector2(1f, 1f);
            rtTextoDialogo.sizeDelta = new Vector2(-30f, -20f);

            textoDialogoPopup = textoDialogoChild.AddComponent<TextMeshProUGUI>();
            textoDialogoPopup.fontSize = 18f;
            textoDialogoPopup.alignment = TextAlignmentOptions.Center;
            textoDialogoPopup.color = new Color(0.95f, 0.95f, 1f, 1f);

            panelDialogoObj.SetActive(false);

            // Panel de Victoria (Centro)
            panelVictoriaObj = new GameObject("Panel_Victoria_Prototipo3");
            panelVictoriaObj.transform.SetParent(targetCanvas.transform, false);
            RectTransform rtVic = panelVictoriaObj.AddComponent<RectTransform>();
            rtVic.anchorMin = new Vector2(0.5f, 0.5f);
            rtVic.anchorMax = new Vector2(0.5f, 0.5f);
            rtVic.pivot = new Vector2(0.5f, 0.5f);
            rtVic.anchoredPosition = Vector2.zero;
            rtVic.sizeDelta = new Vector2(700f, 320f);

            Image bgVic = panelVictoriaObj.AddComponent<Image>();
            bgVic.color = new Color(0.05f, 0.08f, 0.15f, 0.96f);

            GameObject txtVicTitle = new GameObject("VicTitle");
            txtVicTitle.transform.SetParent(panelVictoriaObj.transform, false);
            RectTransform rtVT = txtVicTitle.AddComponent<RectTransform>();
            rtVT.anchoredPosition = new Vector2(0, 90);
            rtVT.sizeDelta = new Vector2(650, 60);
            var tmVT = txtVicTitle.AddComponent<TextMeshProUGUI>();
            tmVT.text = "¡MISIÓN CUMPLIDA!";
            tmVT.fontSize = 36f;
            tmVT.fontStyle = FontStyles.Bold;
            tmVT.alignment = TextAlignmentOptions.Center;
            tmVT.color = new Color(0.2f, 1f, 0.4f, 1f);

            GameObject txtVicDesc = new GameObject("VicDesc");
            txtVicDesc.transform.SetParent(panelVictoriaObj.transform, false);
            RectTransform rtVD = txtVicDesc.AddComponent<RectTransform>();
            rtVD.anchoredPosition = new Vector2(0, 0);
            rtVD.sizeDelta = new Vector2(650, 100);
            var tmVD = txtVicDesc.AddComponent<TextMeshProUGUI>();
            tmVD.text = "El Reactor Carmesí ha sido destruido y el Aditivo Cero neutralizado.\nBarnaby contempla el amanecer sobre las cenizas de Malvoro Tobacco Corp.";
            tmVD.fontSize = 18f;
            tmVD.alignment = TextAlignmentOptions.Center;
            tmVD.color = Color.white;

            panelVictoriaObj.SetActive(false);
        }

        public void ActualizarMision(StoryNodePrototipo3 node)
        {
            if (node == null) return;

            if (textoTituloMision != null)
            {
                textoTituloMision.text = node.title;
            }

            if (textoDescripcionMision != null)
            {
                textoDescripcionMision.text = $"{node.description}\n<color=#FFD700><size=85%>{node.hint}</size></color>";
            }

            ActualizarInventario();
            ActualizarBreadcrumb(node.id);
        }

        public void ActualizarInventario()
        {
            if (textoInventarioNarrativo == null) return;

            var player = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
            int filtros = player != null ? player.filtrosCarmesiCount : 0;
            string keycard = (player != null && player.hasKeycardTorreNorte) ? "<color=#00FF66>SÍ</color>" : "<color=#FF4444>NO</color>";

            textoInventarioNarrativo.text = $"Filtros Carmesí: {filtros}/3 | Tarjeta Torre: {keycard}";
        }

        private void ActualizarBreadcrumb(string activeNodeId)
        {
            if (textoGrafoBreadcrumb == null) return;

            var sb = new StringBuilder();
            string[] flow = { "node_start", "node_lulu", "node_loot_filters", "node_miniboss", "node_baron", "node_key", "node_lock", "node_final_boss", "node_exit" };
            string[] labels = { "Drenaje", "Lulú", "Filtros", "Ciervo Alfa", "Barón", "Llave", "Torre", "Reactor", "Victoria" };

            for (int i = 0; i < flow.Length; i++)
            {
                if (flow[i] == activeNodeId)
                {
                    sb.Append($"<color=#FFD700><b>[{labels[i]}]</b></color>");
                }
                else
                {
                    sb.Append($"[{labels[i]}]");
                }

                if (i < flow.Length - 1) sb.Append(" -> ");
            }

            textoGrafoBreadcrumb.text = sb.ToString();
        }

        public void MostrarDialogo(string speaker, string text)
        {
            if (panelDialogoObj == null || textoDialogoPopup == null) return;
            StopAllCoroutines();
            StartCoroutine(DialogoRoutine(speaker, text));
        }

        private IEnumerator DialogoRoutine(string speaker, string text)
        {
            panelDialogoObj.SetActive(true);
            textoDialogoPopup.text = $"<color=#FFD700><b>{speaker}</b></color>\n«{text}»";
            yield return new WaitForSeconds(6.5f);
            panelDialogoObj.SetActive(false);
        }

        public void MostrarVictoria()
        {
            if (panelVictoriaObj != null)
            {
                panelVictoriaObj.SetActive(true);
            }
        }
    }
}
