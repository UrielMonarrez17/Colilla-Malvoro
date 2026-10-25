using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Prototipo2.Narrative;

namespace Prototipo2
{
    public class NarrativeHUDPrototipo2 : MonoBehaviour
    {
        [Header("UI Containers")]
        public Canvas targetCanvas;

        private GameObject panelMisionObj;
        private TextMeshProUGUI textoTituloMision;
        private TextMeshProUGUI textoDescripcionMision;
        private TextMeshProUGUI textoGrafoBreadcrumb;
        private TextMeshProUGUI textoDialogoPopup;
        private GameObject panelDialogoObj;
        private GameObject panelVictoriaObj;

        private void Start()
        {
            if (targetCanvas == null)
            {
                targetCanvas = FindAnyObjectByType<Canvas>();
            }

            ConstruirUI();

            StoryManagerPrototipo2 sm = FindAnyObjectByType<StoryManagerPrototipo2>();
            if (sm != null)
            {
                sm.OnActiveNodeChanged += ActualizarMision;
                sm.OnDialogueTriggered += MostrarDialogo;
                sm.OnDemoCompleted += MostrarVictoria;

                if (sm.CurrentActiveNode != null)
                {
                    ActualizarMision(sm.CurrentActiveNode);
                }
            }
        }

        private void ConstruirUI()
        {
            if (targetCanvas == null) return;

            // Panel de Misión (Esquina Superior Izquierda)
            panelMisionObj = new GameObject("PanelMisionNarrativa");
            panelMisionObj.transform.SetParent(targetCanvas.transform, false);

            RectTransform rtPanel = panelMisionObj.AddComponent<RectTransform>();
            rtPanel.anchorMin = new Vector2(0, 1);
            rtPanel.anchorMax = new Vector2(0, 1);
            rtPanel.pivot = new Vector2(0, 1);
            rtPanel.anchoredPosition = new Vector2(25, -25);
            rtPanel.sizeDelta = new Vector2(460, 160);

            Image bg = panelMisionObj.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

            // Título de la Misión
            GameObject objTitulo = new GameObject("TextoTitulo");
            objTitulo.transform.SetParent(panelMisionObj.transform, false);
            RectTransform rtTitulo = objTitulo.AddComponent<RectTransform>();
            rtTitulo.anchorMin = new Vector2(0, 1);
            rtTitulo.anchorMax = new Vector2(1, 1);
            rtTitulo.pivot = new Vector2(0, 1);
            rtTitulo.anchoredPosition = new Vector2(15, -10);
            rtTitulo.sizeDelta = new Vector2(-30, 32);

            textoTituloMision = objTitulo.AddComponent<TextMeshProUGUI>();
            textoTituloMision.fontSize = 20;
            textoTituloMision.fontStyle = FontStyles.Bold;
            textoTituloMision.color = new Color(1f, 0.85f, 0.25f);
            textoTituloMision.text = "OBJETIVO NARRATIVO";

            // Descripción y Pista
            GameObject objDesc = new GameObject("TextoDescripcion");
            objDesc.transform.SetParent(panelMisionObj.transform, false);
            RectTransform rtDesc = objDesc.AddComponent<RectTransform>();
            rtDesc.anchorMin = new Vector2(0, 0);
            rtDesc.anchorMax = new Vector2(1, 1);
            rtDesc.pivot = new Vector2(0, 1);
            rtDesc.anchoredPosition = new Vector2(15, -45);
            rtDesc.sizeDelta = new Vector2(-30, 65);

            textoDescripcionMision = objDesc.AddComponent<TextMeshProUGUI>();
            textoDescripcionMision.fontSize = 14;
            textoDescripcionMision.color = Color.white;
            textoDescripcionMision.text = "Cargando grafo narrativo procedimental...";

            // Breadcrumb del Grafo DAG
            GameObject objGrafo = new GameObject("TextoGrafoDAG");
            objGrafo.transform.SetParent(panelMisionObj.transform, false);
            RectTransform rtGrafo = objGrafo.AddComponent<RectTransform>();
            rtGrafo.anchorMin = new Vector2(0, 0);
            rtGrafo.anchorMax = new Vector2(1, 0);
            rtGrafo.pivot = new Vector2(0, 0);
            rtGrafo.anchoredPosition = new Vector2(15, 8);
            rtGrafo.sizeDelta = new Vector2(-30, 26);

            textoGrafoBreadcrumb = objGrafo.AddComponent<TextMeshProUGUI>();
            textoGrafoBreadcrumb.fontSize = 12;
            textoGrafoBreadcrumb.fontStyle = FontStyles.Italic;
            textoGrafoBreadcrumb.color = new Color(0.7f, 0.85f, 1f);
            textoGrafoBreadcrumb.text = "[Grafo DAG Activo]";

            // Diálogo Emergente (Parte Inferior Central)
            panelDialogoObj = new GameObject("PanelDialogoPopup");
            panelDialogoObj.transform.SetParent(targetCanvas.transform, false);
            RectTransform rtDia = panelDialogoObj.AddComponent<RectTransform>();
            rtDia.anchorMin = new Vector2(0.5f, 0);
            rtDia.anchorMax = new Vector2(0.5f, 0);
            rtDia.pivot = new Vector2(0.5f, 0);
            rtDia.anchoredPosition = new Vector2(0, 60);
            rtDia.sizeDelta = new Vector2(600, 90);

            Image bgDia = panelDialogoObj.AddComponent<Image>();
            bgDia.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

            GameObject objTextoDia = new GameObject("TextoDialogo");
            objTextoDia.transform.SetParent(panelDialogoObj.transform, false);
            RectTransform rtTextoDia = objTextoDia.AddComponent<RectTransform>();
            rtTextoDia.anchorMin = new Vector2(0, 0);
            rtTextoDia.anchorMax = new Vector2(1, 1);
            rtTextoDia.offsetMin = new Vector2(15, 10);
            rtTextoDia.offsetMax = new Vector2(-15, -10);

            textoDialogoPopup = objTextoDia.AddComponent<TextMeshProUGUI>();
            textoDialogoPopup.fontSize = 16;
            textoDialogoPopup.color = new Color(1f, 0.95f, 0.75f);
            textoDialogoPopup.alignment = TextAlignmentOptions.Center;

            panelDialogoObj.SetActive(false);

            // Banner Victoria
            panelVictoriaObj = new GameObject("PanelVictoria");
            panelVictoriaObj.transform.SetParent(targetCanvas.transform, false);
            RectTransform rtVic = panelVictoriaObj.AddComponent<RectTransform>();
            rtVic.anchorMin = new Vector2(0.5f, 0.5f);
            rtVic.anchorMax = new Vector2(0.5f, 0.5f);
            rtVic.anchoredPosition = Vector2.zero;
            rtVic.sizeDelta = new Vector2(700, 220);

            Image bgVic = panelVictoriaObj.AddComponent<Image>();
            bgVic.color = new Color(0.04f, 0.12f, 0.06f, 0.95f);

            GameObject objVicTxt = new GameObject("TextoVictoria");
            objVicTxt.transform.SetParent(panelVictoriaObj.transform, false);
            RectTransform rtVicTxt = objVicTxt.AddComponent<RectTransform>();
            rtVicTxt.anchorMin = Vector2.zero;
            rtVicTxt.anchorMax = Vector2.one;
            rtVicTxt.offsetMin = new Vector2(20, 20);
            rtVicTxt.offsetMax = new Vector2(-20, -20);

            TextMeshProUGUI vicTmp = objVicTxt.AddComponent<TextMeshProUGUI>();
            vicTmp.fontSize = 24;
            vicTmp.fontStyle = FontStyles.Bold;
            vicTmp.alignment = TextAlignmentOptions.Center;
            vicTmp.color = Color.green;
            vicTmp.text = "¡DEMO COMPLETADA!\n\nHas superado la amenaza de los animales ferales y alcanzado la colonia segura.\n¡Gracias por jugar!";

            panelVictoriaObj.SetActive(false);
        }

        public void ActualizarMision(StoryNode node)
        {
            if (node == null) return;

            if (textoTituloMision != null)
            {
                textoTituloMision.text = $"MISIÓN: {node.title.ToUpper()}";
            }

            if (textoDescripcionMision != null)
            {
                string progressInfo = "";
                if (node.type == StoryNodeType.Quest)
                {
                    progressInfo = $"\n[Progreso: {node.currentItemProgress}/{node.requiredItemCount}]";
                }
                textoDescripcionMision.text = $"{node.description}\n<color=#FFD700>Pista: {node.hint}</color>{progressInfo}";
            }

            ActualizarBreadcrumb();
        }

        private void ActualizarBreadcrumb()
        {
            StoryManagerPrototipo2 sm = StoryManagerPrototipo2.Instance;
            if (sm == null || sm.CurrentGraph == null || textoGrafoBreadcrumb == null) return;

            StringBuilder sb = new StringBuilder();
            foreach (var n in sm.CurrentGraph.nodes)
            {
                string tag = n.type.ToString();
                switch (n.state)
                {
                    case StoryNodeState.Completed:
                        sb.Append($"<color=#32CD32>[✓ {tag}]</color> ");
                        break;
                    case StoryNodeState.Active:
                        sb.Append($"<color=#FFD700>[▶ {tag}]</color> ");
                        break;
                    default:
                        sb.Append($"<color=#808080>[{tag}]</color> ");
                        break;
                }
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
            textoDialogoPopup.text = $"<color=#00FFFF><b>{speaker}:</b></color> \"{text}\"";
            yield return new WaitForSeconds(6f);
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
