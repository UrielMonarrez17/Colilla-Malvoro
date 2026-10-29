using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo3
{
    /// <summary>
    /// PASO 5.2: Sistema de Reinicio tras Muerte (3 Golpes).
    /// Muestra "Fuiste atrapado en la oscuridad", conserva el estado del StoryManager, la ciudad generada,
    /// los ítems clave y las trampas fabricadas, reduce al 50% los materiales crudos, avanza el reloj al
    /// siguiente Amanecer (OnDayStart), restaura la salud a 0 golpes y teletransporta a Kilo a su Base.
    /// </summary>
    public class RespawnManagerPrototipo3 : MonoBehaviour
    {
        public static RespawnManagerPrototipo3 Instance { get; private set; }

        [Header("Estado de Reinicio")]
        [SerializeField] private bool isRespawning = false;
        public bool IsRespawning => isRespawning;

        private HamsterController boundPlayer;
        private Canvas respawnCanvas;
        private GameObject overlayPanel;
        private Text detailsText;
        private Button wakeUpButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildRespawnOverlayUI();
        }

        public static RespawnManagerPrototipo3 EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("RespawnManagerPrototipo3");
            return go.AddComponent<RespawnManagerPrototipo3>();
        }

        private void Start()
        {
            BindToPlayer();
        }

        private void Update()
        {
            if (boundPlayer == null)
            {
                BindToPlayer();
            }

            if (isRespawning)
            {
                if (FeralInput.Pressed(KeyCode.Space) || FeralInput.Pressed(KeyCode.Return) ||
                    Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    CompleteRespawnAtSunrise();
                }
            }
        }

        private void BindToPlayer()
        {
            HamsterController hc = FindAnyObjectByType<HamsterController>();
            if (hc != null && hc != boundPlayer)
            {
                if (boundPlayer != null)
                {
                    boundPlayer.OnPlayerDied -= HandlePlayerDied;
                }
                boundPlayer = hc;
                boundPlayer.OnPlayerDied -= HandlePlayerDied;
                boundPlayer.OnPlayerDied += HandlePlayerDied;
            }
        }

        private void OnDestroy()
        {
            if (boundPlayer != null)
            {
                boundPlayer.OnPlayerDied -= HandlePlayerDied;
            }
            if (Instance == this) Instance = null;
        }

        private void BuildRespawnOverlayUI()
        {
            if (respawnCanvas != null) return;

            GameObject canvasGo = new GameObject("RespawnCanvas_Prototipo3", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            respawnCanvas = canvasGo.GetComponent<Canvas>();
            respawnCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            respawnCanvas.sortingOrder = 450;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            overlayPanel = new GameObject("DarknessCaughtPanel", typeof(RectTransform), typeof(Image));
            overlayPanel.transform.SetParent(canvasGo.transform, false);
            RectTransform rtPanel = overlayPanel.GetComponent<RectTransform>();
            rtPanel.anchorMin = Vector2.zero;
            rtPanel.anchorMax = Vector2.one;
            rtPanel.offsetMin = Vector2.zero;
            rtPanel.offsetMax = Vector2.zero;
            overlayPanel.GetComponent<Image>().color = new Color(0.02f, 0.01f, 0.01f, 0.94f);

            GameObject cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(overlayPanel.transform, false);
            RectTransform rtCard = cardGo.GetComponent<RectTransform>();
            rtCard.anchorMin = new Vector2(0.5f, 0.5f);
            rtCard.anchorMax = new Vector2(0.5f, 0.5f);
            rtCard.pivot = new Vector2(0.5f, 0.5f);
            rtCard.sizeDelta = new Vector2(880f, 480f);
            cardGo.GetComponent<Image>().color = new Color(0.10f, 0.08f, 0.07f, 0.98f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(cardGo.transform, false);
            RectTransform rtTitle = titleGo.GetComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0.05f, 0.72f);
            rtTitle.anchorMax = new Vector2(0.95f, 0.92f);
            rtTitle.offsetMin = Vector2.zero;
            rtTitle.offsetMax = Vector2.zero;
            Text titleTxt = titleGo.GetComponent<Text>();
            titleTxt.font = font;
            titleTxt.fontSize = 42;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = new Color(0.92f, 0.25f, 0.18f, 1f);
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.text = "FUISTE ATRAPADO EN LA OSCURIDAD";

            GameObject bodyGo = new GameObject("Details", typeof(RectTransform), typeof(Text));
            bodyGo.transform.SetParent(cardGo.transform, false);
            RectTransform rtBody = bodyGo.GetComponent<RectTransform>();
            rtBody.anchorMin = new Vector2(0.08f, 0.28f);
            rtBody.anchorMax = new Vector2(0.92f, 0.70f);
            rtBody.offsetMin = Vector2.zero;
            rtBody.offsetMax = Vector2.zero;
            detailsText = bodyGo.GetComponent<Text>();
            detailsText.font = font;
            detailsText.fontSize = 24;
            detailsText.color = new Color(0.92f, 0.86f, 0.76f, 1f);
            detailsText.alignment = TextAnchor.MiddleCenter;

            GameObject btnGo = new GameObject("WakeUpButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(cardGo.transform, false);
            RectTransform rtBtn = btnGo.GetComponent<RectTransform>();
            rtBtn.anchorMin = new Vector2(0.5f, 0.10f);
            rtBtn.anchorMax = new Vector2(0.5f, 0.10f);
            rtBtn.pivot = new Vector2(0.5f, 0.5f);
            rtBtn.sizeDelta = new Vector2(520f, 64f);
            btnGo.GetComponent<Image>().color = new Color(0.65f, 0.22f, 0.14f, 1f);

            wakeUpButton = btnGo.GetComponent<Button>();
            wakeUpButton.onClick.AddListener(CompleteRespawnAtSunrise);

            GameObject btnTxtGo = new GameObject("BtnLabel", typeof(RectTransform), typeof(Text));
            btnTxtGo.transform.SetParent(btnGo.transform, false);
            RectTransform rtBtnTxt = btnTxtGo.GetComponent<RectTransform>();
            rtBtnTxt.anchorMin = Vector2.zero;
            rtBtnTxt.anchorMax = Vector2.one;
            rtBtnTxt.offsetMin = Vector2.zero;
            rtBtnTxt.offsetMax = Vector2.zero;
            Text btnTxt = btnTxtGo.GetComponent<Text>();
            btnTxt.font = font;
            btnTxt.fontSize = 24;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.color = Color.white;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.text = "Despertar al Amanecer en la Base [ESPACIO]";

            overlayPanel.SetActive(false);
        }

        private void HandlePlayerDied()
        {
            if (isRespawning) return;
            isRespawning = true;

            // 1. Reducir al 50% las unidades de materiales crudos (Madera, Metal, Desperdicios, Semillas, Goma)
            string penaltyReport = ApplyFiftyPercentRawMaterialPenalty();

            // 2. Mostrar interfaz "Fuiste atrapado en la oscuridad"
            BuildRespawnOverlayUI();
            if (detailsText != null)
            {
                detailsText.text =
                    "Lograste arrastrarte de regreso a tu madriguera antes del alba.\n\n" +
                    "• Estado de la historia, ciudad generada, ítems clave y trampas fabricadas: CONSERVADOS.\n" +
                    $"• Penalización de materiales crudos (-50%): {penaltyReport}\n" +
                    "• El reloj avanza al siguiente Amanecer (06:00 AM).";
            }

            overlayPanel.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public string ApplyFiftyPercentRawMaterialPenalty()
        {
            InventoryManager inv = InventoryManager.Instance;
            if (inv == null) return "Sin inventario";

            ItemType[] rawTypes = (ItemType[])Enum.GetValues(typeof(ItemType));
            int totalLost = 0;

            for (int i = 0; i < rawTypes.Length; i++)
            {
                ItemType t = rawTypes[i];
                int current = inv.GetCount(t);
                if (current > 0)
                {
                    int keep = current / 2; // Reduce al 50%
                    int toRemove = current - keep;
                    if (toRemove > 0)
                    {
                        inv.RemoveItem(t, toRemove);
                        totalLost += toRemove;
                    }
                }
            }

            return totalLost > 0 ? $"-{totalLost} unidades perdidas en la huida" : "Sin pérdida (mochila vacía)";
        }

        public void CompleteRespawnAtSunrise()
        {
            if (!isRespawning) return;
            isRespawning = false;

            if (overlayPanel != null)
            {
                overlayPanel.SetActive(false);
            }

            // Cerrar pantalla modal si FeralGameUI la abrió
            if (Prototipo3.UI.FeralGameUI.Instance != null &&
                Prototipo3.UI.FeralGameUI.Instance.CurrentPage == Prototipo3.UI.FeralGameUI.Page.Defeat)
            {
                Prototipo3.UI.FeralGameUI.Instance.Close();
            }

            Time.timeScale = 1f;

            // 3. Avanzar el reloj al siguiente Amanecer (6:00 AM -> dispara OnDayStart en DualRoleCharacter y EnemyAI)
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SetExactHour(6.0f);
            }

            foreach (DualRoleCharacter drc in DualRoleCharacter.ActiveCharacters)
            {
                if (drc != null) drc.OnDayStart();
            }

            // 4. Restaurar salud a 0 golpes y teletransportar a Kilo a su Base
            Vector3 basePos = CityGeneratorPrototipo3.Instance != null
                ? CityGeneratorPrototipo3.Instance.PlayerSpawnPoint
                : Vector3.zero;

            if (boundPlayer == null)
            {
                boundPlayer = FindAnyObjectByType<HamsterController>();
            }

            if (boundPlayer != null)
            {
                boundPlayer.RespawnOrHeal(basePos);
                boundPlayer.enabled = true;
            }

            Debug.Log("[RespawnManagerPrototipo3] Kilo ha despertado en la Base al Amanecer (06:00 AM) con 0 golpes.");
        }
    }
}
