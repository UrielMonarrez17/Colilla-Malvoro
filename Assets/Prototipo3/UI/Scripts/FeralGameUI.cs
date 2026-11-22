using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prototipo3.UI
{
    /// <summary>One Canvas and one modal owner for Prototipo 3 survival sandbox.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class FeralGameUI : MonoBehaviour
    {
        public enum Page { None, Inventory, Pause, CitySetup, Crafting, Dialogue, Return, Defeat }

        public static FeralGameUI Instance { get; private set; }
        public static bool BlocksGameplay => Instance != null && (Instance.CurrentPage != Page.None || Time.frameCount <= Instance.suppressInputUntilFrame);
        public static bool Supports(string scene) => !string.IsNullOrEmpty(scene) && (scene == "Prototipo3" || scene.IndexOf("Prototipo3", StringComparison.OrdinalIgnoreCase) >= 0);

        public Page CurrentPage { get; private set; }
        public Canvas Canvas { get; private set; }
        public bool DialogueOpen => CurrentPage == Page.Dialogue;

        RectTransform hud, modal, content;
        Text health, staminaText, clock, objective, guide, phase, interaction, status, lootFeedback;
        GameObject lootFeedbackBox;
        float lootFeedbackExpiryTime;
        MenuBrushGraphic staminaFill, healthFill;
        Text[] trapAmounts = new Text[3];
        MenuBrushGraphic[] trapFrames = new MenuBrushGraphic[3];
        HamsterController player;
        InventoryManager inventory;
        CraftingTable table;
        bool ownsPause, previousPlayerEnabled, previousCursorVisible, dirty, lastCinematic;
        CursorLockMode previousCursor;
        float previousScale, nextRefresh;
        int inventoryTab, selectedItem;
        int suppressInputUntilFrame = -1;
        string speaker, dialogue;
        float dialogueReady;

        InputField citySeedInput, cityWidthInput, cityLengthInput;
        Text citySetupStatus;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; SceneManager.sceneLoaded -= SceneLoaded; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { SceneManager.sceneLoaded -= SceneLoaded; SceneManager.sceneLoaded += SceneLoaded; }

        static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Supports(scene.name) || Instance != null) return;
            new GameObject("Feral • UI Prototipo3").AddComponent<FeralGameUI>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            foreach (Transform child in transform)
            {
                if (child.name != "Feral Canvas" || !child.GetComponent<Canvas>()) continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            var root = new GameObject("Feral Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            Canvas = root.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 150;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            BuildHud();

            modal = FeralTheme.Rect("Modal", root.transform, 0, 0, 1600, 900);
            FeralTheme.Stretch(modal);
            var shade = modal.gameObject.AddComponent<Image>();
            shade.color = new Color(.015f, .012f, .01f, .82f);
            modal.gameObject.SetActive(false);

            CraftingTable.OnMenuToggled += CraftingToggled;
            CraftingTable.OnTrapCrafted += Crafted;
            TrapSystemPrototipo3.OnProto3TrapCrafted += Crafted;
            TrapSystemPrototipo3.OnProto3TrapDeployed += Crafted;
            LootContainer.OnContainerLooted += HandleContainerLooted;
            LootContainer.OnContainerAlreadyLooted += HandleContainerAlreadyLooted;
            SuppressLegacy();
        }

        IEnumerator Start()
        {
            MainMenuControllerPrototipo3.EnsureEventSystem();
            MainMenuControllerPrototipo3.ApplySavedAudio();
            yield return null;
            BindData();
            SuppressLegacy();
            RefreshHud();
        }

        void BindData()
        {
            if (!player)
            {
                player = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
            }
            if (inventory == null)
            {
                inventory = InventoryManager.Instance;
                if (inventory == null) inventory = new GameObject("InventoryManager").AddComponent<InventoryManager>();
                inventory.Changed += OnInventoryChanged;
            }
        }

        void OnInventoryChanged() { dirty = true; }
        void Crafted(TipoTrampa type) { dirty = true; }

        bool Cinematic =>
            (RespawnManagerPrototipo3.Instance != null && RespawnManagerPrototipo3.Instance.IsRespawning) ||
            (TutorialPipelineManager.Instance != null && TutorialPipelineManager.Instance.enCinematica) ||
            FindObjectsByType<CinematicaManager>(FindObjectsSortMode.None).Any(c => c.isActiveAndEnabled && !c.CinematicaTerminada);

        void Update()
        {
            bool cinematic = Cinematic;
            Canvas.enabled = !cinematic;
            if (cinematic || lastCinematic) { lastCinematic = cinematic; return; }

            if (CurrentPage == Page.Dialogue)
            {
                if (Time.unscaledTime >= dialogueReady && (FeralInput.Pressed(KeyCode.F) || FeralInput.Pressed(KeyCode.Space) || FeralInput.Pressed(KeyCode.Return) || FeralInput.Pressed(KeyCode.Escape)))
                    Close();
            }
            else if (FeralInput.Pressed(KeyCode.Escape))
            {
                if (CurrentPage == Page.Return || CurrentPage == Page.CitySetup) Open(Page.Pause);
                else if (CurrentPage != Page.Defeat)
                {
                    if (CurrentPage == Page.None) Open(Page.Pause); else Close();
                }
            }
            else if (CurrentPage == Page.None || CurrentPage == Page.Inventory)
            {
                if (FeralInput.Pressed(KeyCode.Tab)) { if (CurrentPage == Page.Inventory) Close(); else Open(Page.Inventory); }
            }

            if (dirty)
            {
                dirty = false;
                if (CurrentPage == Page.Inventory || CurrentPage == Page.Crafting) RenderPage();
            }

            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + .2f;
                BindData();
                SuppressLegacy();
                RefreshHud();

                if (player && player.IsDead && RespawnManagerPrototipo3.Instance == null && TutorialPipelineManager.Instance == null && CurrentPage == Page.None)
                {
                    Open(Page.Defeat);
                }
            }

            if (lootFeedbackBox != null && lootFeedbackBox.activeSelf && Time.unscaledTime >= lootFeedbackExpiryTime)
            {
                lootFeedbackBox.SetActive(false);
                if (lootFeedback != null) lootFeedback.text = "";
            }
        }

        public void Open(Page page)
        {
            BindData();
            if (page == Page.None) { Close(); return; }
            if (Cinematic) return;
            if (CurrentPage == Page.Crafting && page != Page.Crafting && table != null)
            {
                var old = table; table = null; old.CloseMenu();
            }
            if (!ownsPause)
            {
                previousScale = Time.timeScale;
                previousCursor = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                previousPlayerEnabled = player && player.enabled;
                ownsPause = true;
            }
            if (player) player.enabled = false;
            Time.timeScale = 0;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            CurrentPage = page;
            modal.gameObject.SetActive(true);
            hud.gameObject.SetActive(false);
            RenderPage();
        }

        public void Close()
        {
            suppressInputUntilFrame = Time.frameCount + 1;
            CurrentPage = Page.None;
            if (table != null) { var old = table; table = null; old.CloseMenu(); }
            modal.gameObject.SetActive(false);
            hud.gameObject.SetActive(true);
            ReleasePause();
            PlayerPrefs.Save();
        }

        void ReleasePause()
        {
            if (!ownsPause) return;
            Time.timeScale = previousScale;
            if (player && !player.IsDead) player.enabled = previousPlayerEnabled;
            Cursor.visible = previousCursorVisible;
            Cursor.lockState = previousCursor;
            ownsPause = false;
        }

        void OnDestroy()
        {
            if (inventory != null) inventory.Changed -= OnInventoryChanged;
            CraftingTable.OnMenuToggled -= CraftingToggled;
            CraftingTable.OnTrapCrafted -= Crafted;
            TrapSystemPrototipo3.OnProto3TrapCrafted -= Crafted;
            TrapSystemPrototipo3.OnProto3TrapDeployed -= Crafted;
            LootContainer.OnContainerLooted -= HandleContainerLooted;
            LootContainer.OnContainerAlreadyLooted -= HandleContainerAlreadyLooted;
            ReleasePause();
            if (Instance == this) Instance = null;
        }

        void HandleContainerLooted(LootContainer container, System.Collections.Generic.List<ItemType> items)
        {
            string summary = LootContainer.FormatLootSummary(items);
            ShowLootFeedback($"Recogido:  {summary}", 3.8f);
        }

        void HandleContainerAlreadyLooted(LootContainer container)
        {
            ShowLootFeedback("Contenedor vacío (ya saqueado)", 2.5f);
        }

        public void ShowLootFeedback(string msg, float duration)
        {
            if (lootFeedback != null && lootFeedbackBox != null)
            {
                lootFeedback.text = msg;
                lootFeedbackBox.SetActive(true);
                lootFeedbackExpiryTime = Time.unscaledTime + duration;
            }
        }

        void CraftingToggled(CraftingTable source, bool opened)
        {
            if (opened)
            {
                if (!source.IsMenuOpen) return;
                if (source.craftingPanelUI != null) source.craftingPanelUI.SetActive(false);
                table = source;
                Open(Page.Crafting);
            }
            else if (table == source)
            {
                table = null;
                if (CurrentPage == Page.Crafting) Close();
            }
        }

        public void ShowDialogue(string who, string text)
        {
            if (Cinematic) return;
            speaker = who;
            dialogue = text;
            dialogueReady = Time.unscaledTime + .35f;
            Open(Page.Dialogue);
        }

        void BuildHud()
        {
            hud = FeralTheme.Rect("HUD", Canvas.transform, 0, 0, 1600, 900);
            FeralTheme.Stretch(hud);

            var vitals = FeralTheme.Brush("Vitales", hud, 26, 26, 445, 130, FeralTheme.Ink).rectTransform;
            FeralTheme.Illustration("Hamster", vitals, 0, 0, 0, 125);
            health = FeralTheme.Label("Vida", vitals, "VIDA", 132, 14, 290, 24, 18);
            FeralTheme.Brush("HealthTrack", vitals, 132, 40, 290, 26, FeralTheme.Card);
            healthFill = FeralTheme.Brush("HealthFill", vitals, 132, 40, 290, 26, FeralTheme.Red);
            staminaText = FeralTheme.Label("Estamina", vitals, "ESTAMINA", 132, 76, 290, 22, 14, FeralTheme.Muted);
            FeralTheme.Brush("Track", vitals, 132, 98, 290, 20, FeralTheme.Card);
            staminaFill = FeralTheme.Brush("StaminaFill", vitals, 132, 98, 290, 20, FeralTheme.Gold);

            var survivalPanel = FeralTheme.Brush("Supervivencia", hud, 26, 174, 460, 215, FeralTheme.Ink).rectTransform;
            FeralTheme.Brush("Paper", survivalPanel, 16, 14, 428, 38, FeralTheme.Cream);
            phase = FeralTheme.Label("Fase", survivalPanel, "CIUDAD PROCEDURAL", 28, 22, 404, 24, 15, FeralTheme.Ink);
            objective = FeralTheme.Label("ObjetivoActual", survivalPanel, "Reúne materiales con [F] y prepara tus trampas.", 24, 64, 412, 82, 17);
            guide = FeralTheme.Label("Guia", survivalPanel, "[G] Nueva semilla  ·  [ESC] Opciones", 24, 154, 412, 46, 14, FeralTheme.Gold);

            var bottom = FeralTheme.Brush("Atajos", hud, 32, 0, 320, 104, FeralTheme.Ink).rectTransform;
            bottom.anchorMin = bottom.anchorMax = bottom.pivot = Vector2.zero;
            bottom.anchoredPosition = new Vector2(32, 28);
            FeralTheme.Label("Teclas", bottom, "TAB  Inventario     ESC  Pausa\n1/2/3  Trampas      F  Interactuar", 20, 25, 285, 70, 16, FeralTheme.Cream);

            var hotbar = FeralTheme.Rect("TrampasRapidas", hud, 0, 0, 330, 108);
            hotbar.anchorMin = hotbar.anchorMax = hotbar.pivot = new Vector2(.5f, 0);
            hotbar.anchoredPosition = new Vector2(0, 28);
            for (int i = 0; i < 3; i++)
            {
                trapFrames[i] = FeralTheme.Brush("Slot " + i, hotbar, i * 112, 0, 104, 104, FeralTheme.Gold);
                FeralTheme.Brush("SlotInset", trapFrames[i].transform, 4, 4, 96, 96, FeralTheme.Ink);
                FeralTheme.Illustration("TrapIcon", trapFrames[i].transform, i + 1, 9, 9, 82);
                FeralTheme.Label("Key", trapFrames[i].transform, (i + 1).ToString(), 8, 6, 24, 24, 18);
                trapAmounts[i] = FeralTheme.Label("Amount", trapFrames[i].transform, "0", 72, 76, 28, 24, 18);
            }

            var clockPanel = FeralTheme.Brush("ClockPaper", hud, 0, 0, 220, 66, FeralTheme.Cream).rectTransform;
            clockPanel.anchorMin = clockPanel.anchorMax = clockPanel.pivot = Vector2.one;
            clockPanel.anchoredPosition = new Vector2(-32, -32);
            clock = FeralTheme.Label("Reloj", clockPanel, "", 20, 18, 180, 32, 22, FeralTheme.Ink);
            clock.alignment = TextAnchor.MiddleCenter;

            interaction = FeralTheme.Label("Interaccion", hud, "", 0, 0, 760, 65, 22);
            interaction.rectTransform.anchorMin = interaction.rectTransform.anchorMax = interaction.rectTransform.pivot = new Vector2(.5f, 0);
            interaction.rectTransform.anchoredPosition = new Vector2(0, 140);
            interaction.alignment = TextAnchor.MiddleCenter;
            interaction.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);

            var lootPanel = FeralTheme.Brush("LootFeedbackBox", hud, 0, 0, 560, 54, FeralTheme.Ink).rectTransform;
            lootPanel.anchorMin = lootPanel.anchorMax = lootPanel.pivot = new Vector2(.5f, 0);
            lootPanel.anchoredPosition = new Vector2(0, 212);
            lootFeedbackBox = lootPanel.gameObject;
            lootFeedback = FeralTheme.Label("LootFeedback", lootPanel, "", 16, 8, 528, 38, 20, FeralTheme.Gold);
            lootFeedback.alignment = TextAnchor.MiddleCenter;
            lootFeedbackBox.SetActive(false);
        }

        void RefreshHud()
        {
            if (player)
            {
                int hp = Mathf.Max(0, player.maxHitsAllowed - player.HitsReceived);
                health.text = "VIDA   " + hp + " / " + player.maxHitsAllowed + (hp == 1 ? "   ¡CUIDADO!" : "");
                healthFill.rectTransform.sizeDelta = new Vector2(290 * hp / Mathf.Max(1f, player.maxHitsAllowed), 26);
                health.color = hp <= 1 ? new Color(1, .48f, .35f) : FeralTheme.Cream;
                staminaFill.rectTransform.sizeDelta = new Vector2(290 * Mathf.Clamp01(player.currentStamina / Mathf.Max(1, player.maxStamina)), 20);
                staminaText.text = (player.IsExhausted ? "AGOTADO" : player.IsAdrenalineActive ? "ADRENALINA" : player.isCrouching ? "SIGILO" : "ESTAMINA") + "   " + Mathf.RoundToInt(player.currentStamina) + " / " + Mathf.RoundToInt(player.maxStamina);

                var target = player.CurrentInteractable;
                if (target == null)
                {
                    interaction.text = "";
                }
                else if (target.GetComponentInParent<CraftingTable>() != null)
                {
                    interaction.text = "[ F ]  Usar mesa de fabricación";
                }
                else if (target.GetComponentInParent<LootContainer>() is LootContainer lc)
                {
                    interaction.text = lc.YaFueSaqueado ? "Contenedor ya saqueado (Vacío)" : "[ F ]  Saquear materiales";
                }
                else if (target.GetComponentInParent<PuertaTrampa>() != null)
                {
                    interaction.text = "[ F ]  Colocar trampa";
                }
                else if (target.GetComponentInParent<LuluNPC>() != null)
                {
                    interaction.text = "[ F ]  Hablar con Lulú";
                }
                else
                {
                    interaction.text = "[ F ]  Interactuar";
                }
            }

            for (int i = 0; i < 3; i++)
            {
                trapAmounts[i].text = TrapCount(Traps[i]).ToString();
                trapFrames[i].color = player && player.trampaSeleccionada == Traps[i] ? FeralTheme.Gold : FeralTheme.Card;
            }

            var tm = TimeManager.Instance;
            bool isNight = tm != null && tm.CurrentCycleState == TimeManager.CycleState.Night;
            clock.text = tm == null ? "" : $"{(isNight ? "NOCHE" : "DÍA")}   {tm.CurrentHour:00}:{tm.CurrentMinute:00}";

            var city = CityGeneratorPrototipo3.Instance;
            string seedInfo = city != null ? $"#{city.seed} ({city.mapWidth}x{city.mapLength})" : "ACTIVA";
            phase.text = $"CIUDAD PROCEDURAL · SEMILLA {seedInfo}";
            objective.text = isNight
                ? "¡Cae la noche feral! Evade a los depredadores con sigilo [C], dash o trampas [1][2][3]."
                : "Explora la ciudad: recoge recursos con [F] y fabrica trampas en tu mesa central.";
            guide.text = "[G] Ciudad aleatoria  ·  [ESC] Configurar semilla y tamaño";
        }

        void SuppressLegacy()
        {
            foreach (var old in FindObjectsByType<Prototipo2.UI.InGameInventoryUIPrototipo2>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                old.gameObject.SetActive(false);
                Destroy(old.gameObject);
            }

            foreach (var old in FindObjectsByType<Feral.UI.FeralInventoryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                old.enabled = false;
                foreach (var t in old.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Inventory" || t.name == "InventoryHint" || t.name == "ReturnConfirmation") t.gameObject.SetActive(false);
            }
            if (player && player.staminaSlider) player.staminaSlider.gameObject.SetActive(false);
        }

        void RenderPage()
        {
            if (content != null) { content.gameObject.SetActive(false); Destroy(content.gameObject); }
            bool small = CurrentPage == Page.Pause || CurrentPage == Page.Return || CurrentPage == Page.Defeat || CurrentPage == Page.CitySetup;
            float width = small ? 640 : 1320, height = CurrentPage == Page.Dialogue ? 330 : 740;
            if (CurrentPage == Page.Dialogue) width = 1100;
            content = FeralTheme.Brush("Contenido " + CurrentPage, modal, 0, 0, width, height, FeralTheme.Ink).rectTransform;
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, .5f);
            content.anchoredPosition = Vector2.zero;
            FeralTheme.Panel("Accent", content, 0, 0, width, 5, FeralTheme.Red);
            FeralTheme.Label("Kicker", content, "FERAL  /  SUPERVIVENCIA EN LA CIUDAD", 36, 28, width - 72, 28, 16, FeralTheme.Gold);
            if (CurrentPage != Page.Dialogue && CurrentPage != Page.Defeat)
                FeralTheme.Button("Close", content, "Volver  [ESC]", width - 236, 68, 200, 48, () =>
                {
                    if (CurrentPage == Page.CitySetup || CurrentPage == Page.Return) Open(Page.Pause);
                    else Close();
                });

            switch (CurrentPage)
            {
                case Page.Inventory: InventoryPage(); break;
                case Page.Pause: PausePage(); break;
                case Page.CitySetup: CitySetupPage(); break;
                case Page.Crafting: CraftingPage(); break;
                case Page.Dialogue: DialoguePage(); break;
                case Page.Return: ReturnPage(); break;
                case Page.Defeat: EndPage(); break;
            }
            var first = content.GetComponentsInChildren<Button>().FirstOrDefault(b => b.interactable && b.name != "Close");
            if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        void Heading(string title, string subtitle)
        {
            FeralTheme.Label("Title", content, title, 36, 70, 930, 54, 40);
            FeralTheme.Label("Subtitle", content, subtitle, 36, 132, 1200, 38, 19, FeralTheme.Muted);
        }

        static readonly TipoTrampa[] Traps = { TipoTrampa.Puas, TipoTrampa.Red, TipoTrampa.Madera };
        public static string TrapName(TipoTrampa t) => TrapSystemPrototipo3.GetTrapDisplayName(t);
        int TrapCount(TipoTrampa t) => inventory != null ? inventory.GetTrapCount(t) : 0;
        int Count(ItemType item) => inventory != null ? inventory.GetCount(item) : 0;
        static string Recipe(TipoTrampa t) => TrapSystemPrototipo3.GetTrapRecipeText(t);
        static string TrapDescription(TipoTrampa t) => TrapSystemPrototipo3.GetTrapDescription(t);
        bool CanCraft(TipoTrampa t) => TrapSystemPrototipo3.CanCraftTrap(t);

        void Icon(Transform parent, ItemType item, float x, float y, float size)
        {
            var texture = Resources.Load<Texture2D>("FeralUI/" + item);
            if (!texture) return;
            var image = FeralTheme.Rect("Icon " + item, parent, x, y, size, size).gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
        }

        void InventoryPage()
        {
            Heading("INVENTARIO", "Materiales recolectados y trampas tácticas [1/2/3] para defenderte en la noche.");
            FeralTheme.Button("Materials", content, "Materiales", 302, 190, 250, 52, () => { inventoryTab = 0; selectedItem = 0; RenderPage(); }, inventoryTab == 0);
            FeralTheme.Button("Traps", content, "Trampas [1/2/3]", 566, 190, 250, 52, () => { inventoryTab = 1; selectedItem = 0; RenderPage(); }, inventoryTab == 1);
            FeralTheme.Button("InventoryNav", content, "Inventario", 30, 270, 240, 64, () => { }, true);
            var craftNav = FeralTheme.Button("CraftNav", content, "Fabricación", 30, 348, 240, 64, () => Open(Page.Crafting));
            craftNav.interactable = table && table.IsMenuOpen;
            FeralTheme.Button("CityNav", content, "Nueva Ciudad", 30, 426, 240, 64, () => Open(Page.CitySetup));
            FeralTheme.Illustration("HamsterInventory", content, 0, 48, 490, 190);

            FeralTheme.Brush("MaterialHeading", content, 302, 268, 250, 36, FeralTheme.Cream);
            FeralTheme.Label("MaterialHeadingText", content, "MATERIALES", 316, 276, 230, 24, 16, FeralTheme.Ink);
            var items = (ItemType[])Enum.GetValues(typeof(ItemType));
            for (int i = 0; i < items.Length; i++)
            {
                int id = i;
                float x = 302 + (i % 3) * 174, y = 316 + (i / 3) * 126;
                var b = FeralTheme.Button("Material " + items[i], content, "", x, y, 162, 114, () => { inventoryTab = 0; selectedItem = id; RenderPage(); });
                SlotStyle(b, inventoryTab == 0 && i == selectedItem);
                Icon(b.transform, items[i], 16, 9, 70);
                FeralTheme.Label("Amount", b.transform, Count(items[i]).ToString(), 112, 36, 42, 34, 27);
                FeralTheme.Label("Name", b.transform, items[i].ToString(), 12, 84, 144, 24, 16);
            }

            FeralTheme.Brush("TrapHeading", content, 302, 568, 350, 36, FeralTheme.Cream);
            FeralTheme.Label("TrapHeadingText", content, "HERRAMIENTAS Y TRAMPAS", 316, 576, 330, 24, 16, FeralTheme.Ink);
            for (int i = 0; i < 3; i++)
            {
                int id = i;
                var b = FeralTheme.Button("Trampa " + Traps[i], content, "", 302 + i * 174, 610, 162, 100, () => { inventoryTab = 1; selectedItem = id; RenderPage(); });
                SlotStyle(b, inventoryTab == 1 && i == selectedItem);
                FeralTheme.Illustration("TrapIcon", b.transform, i + 1, 14, 0, 96);
                FeralTheme.Label("Count", b.transform, "×" + TrapCount(Traps[i]), 111, 62, 45, 32, 23);
            }

            selectedItem = Mathf.Clamp(selectedItem, 0, inventoryTab == 0 ? items.Length - 1 : 2);
            var pane = FeralTheme.Brush("Detalle", content, 862, 190, 422, 520, FeralTheme.Card).rectTransform;
            string title = inventoryTab == 0 ? items[selectedItem].ToString() : TrapName(Traps[selectedItem]);
            FeralTheme.Brush("Paper", pane, 18, 16, 386, 52, FeralTheme.Cream);
            FeralTheme.Label("Name", pane, title, 34, 30, 356, 38, 25, FeralTheme.Ink);
            if (inventoryTab == 0) Icon(pane, items[selectedItem], 108, 78, 200);
            else FeralTheme.Illustration("TrapPreview", pane, selectedItem + 1, 90, 66, 240);
            string body = inventoryTab == 0 ? MaterialDescription(items[selectedItem]) : TrapDescription(Traps[selectedItem]);
            FeralTheme.Label("Description", pane, body, 26, 294, 370, 130, 20);
            if (inventoryTab == 1)
            {
                var trap = Traps[selectedItem];
                FeralTheme.Label("Recipe", pane, "Receta: " + Recipe(trap), 26, 425, 370, 30, 16, FeralTheme.Gold);
                var equip = FeralTheme.Button("Equip", pane, player && player.trampaSeleccionada == trap ? "Seleccionada [" + (selectedItem + 1) + "]" : "Seleccionar [" + (selectedItem + 1) + "]", 26, 460, 370, 52, () => { if (player) player.trampaSeleccionada = trap; RenderPage(); }, true);
                equip.interactable = player && TrapCount(trap) > 0;
            }
            else
            {
                FeralTheme.Label("Owned", pane, "EN TU MOCHILA   " + Count(items[selectedItem]), 26, 466, 370, 32, 20, FeralTheme.Gold);
            }
        }

        static void SlotStyle(Button button, bool selected)
        {
            var colors = button.colors;
            colors.normalColor = selected ? FeralTheme.Gold : FeralTheme.Card;
            colors.highlightedColor = colors.selectedColor = FeralTheme.Gold;
            button.colors = colors;
            var inset = FeralTheme.Brush("Inset", button.transform, 4, 4, button.GetComponent<RectTransform>().sizeDelta.x - 8, button.GetComponent<RectTransform>().sizeDelta.y - 8, FeralTheme.Ink);
            inset.transform.SetAsFirstSibling();
        }

        static string MaterialDescription(ItemType item)
        {
            switch (item)
            {
                case ItemType.Madera: return "Tablas recuperadas de la ciudad.\n\nSirven para fabricar trampas de púas, barricadas de madera y bombas de polen.";
                case ItemType.Metal: return "Piezas metálicas conductoras.\n\nRequeridas para fabricar el señuelo acústico (Trampa 1) y el descargador estático.";
                case ItemType.Desperdicios: return "Latas y restos resonantes.\n\nCombínalos con Metal para armar trampas de distracción o redes.";
                case ItemType.Goma: return "Fragmentos de goma aislante.\n\nÚtiles para fabricar trampas eléctricas y de contención.";
                default: return "Semillas silvestres cargadas de polen.\n\nCombínalas con Madera para fabricar la Bomba de Polen (Trampa 2).";
            }
        }

        void PausePage()
        {
            FeralTheme.Label("Title", content, "En pausa", 36, 80, 340, 60, 42);
            FeralTheme.Label("Subtitle", content, "Respira. La ciudad puede esperar.", 36, 146, 560, 36, 21, FeralTheme.Muted);
            FeralTheme.Button("Resume", content, "Continuar", 36, 210, 568, 58, Close, true);
            FeralTheme.Button("Inventory", content, "Mochila   [TAB]", 36, 282, 568, 58, () => Open(Page.Inventory));
            FeralTheme.Button("NewCity", content, "Nueva Ciudad (Semilla)", 36, 354, 568, 58, () => Open(Page.CitySetup));
            var volumeText = FeralTheme.Label("VolumeLabel", content, "Volumen", 36, 444, 568, 30, 21);
            var volume = FeralTheme.Slider("Volume", content, 36, 486, 380, PlayerPrefs.GetFloat("Feral.UI.MasterVolume", .8f), v =>
            {
                PlayerPrefs.SetFloat("Feral.UI.MasterVolume", v);
                PlayerPrefs.SetInt("Feral.UI.Muted", 0);
                MainMenuControllerPrototipo3.ApplySavedAudio();
                volumeText.text = "Volumen   " + Mathf.RoundToInt(v * 100) + "%";
            });
            volumeText.text = "Volumen   " + Mathf.RoundToInt(volume.value * 100) + "%";
            FeralTheme.Button("ReturnMenu", content, "Volver al menú principal", 36, 568, 568, 58, () => Open(Page.Return));
        }

        void CitySetupPage()
        {
            var city = CityGeneratorPrototipo3.Instance;
            string currentSeed = city != null ? city.seed.ToString() : "12345";
            string currentW = city != null ? city.mapWidth.ToString() : "30";
            string currentL = city != null ? city.mapLength.ToString() : "30";

            FeralTheme.Label("Title", content, "Nueva Ciudad", 36, 80, 540, 54, 38);
            FeralTheme.Label("Subtitle", content, "Genera una ciudad aleatoria o personaliza semilla y dimensiones (20 a 60).", 36, 140, 568, 52, 18, FeralTheme.Muted);

            FeralTheme.Button("BtnRandom", content, "Ciudad Aleatoria (Generar ahora)", 36, 206, 568, 58, () =>
            {
                int rSeed = UnityEngine.Random.Range(100000, 999999);
                if (CityGeneratorPrototipo3.Instance != null)
                {
                    CityGeneratorPrototipo3.Instance.RegenerateCityRuntime(rSeed, 30, 30);
                }
                Close();
            }, true);

            var card = FeralTheme.Panel("CustomBox", content, 36, 286, 568, 326, FeralTheme.Card).rectTransform;
            FeralTheme.Label("CustomHead", card, "Personalizar Semilla y Tamaño", 24, 18, 520, 32, 22, FeralTheme.Gold);

            FeralTheme.Label("SeedLbl", card, "Semilla numérica:", 24, 70, 200, 32, 18);
            citySeedInput = CreateInputField(card, "SeedInput", currentSeed, 232, 66, 190, 40);
            FeralTheme.Button("BtnRollSeed", card, "Azar", 434, 66, 110, 40, () =>
            {
                if (citySeedInput != null) citySeedInput.text = UnityEngine.Random.Range(100000, 999999).ToString();
            });

            FeralTheme.Label("WidthLbl", card, "Ancho (celdas):", 24, 128, 200, 32, 18);
            cityWidthInput = CreateInputField(card, "WidthInput", currentW, 232, 124, 120, 40);

            FeralTheme.Label("LengthLbl", card, "Largo (celdas):", 24, 184, 200, 32, 18);
            cityLengthInput = CreateInputField(card, "LengthInput", currentL, 232, 180, 120, 40);

            FeralTheme.Button("BtnApplyCustom", card, "Generar con estos datos", 24, 246, 520, 56, ApplyCustomCityFromInputs, true);
            citySetupStatus = FeralTheme.Label("CityStatus", content, "", 36, 626, 568, 42, 16, FeralTheme.Gold);
        }

        void ApplyCustomCityFromInputs()
        {
            if (CityGeneratorPrototipo3.Instance == null)
            {
                if (citySetupStatus != null) citySetupStatus.text = "No se encontró CityGeneratorPrototipo3 en la escena.";
                return;
            }

            int targetSeed = UnityEngine.Random.Range(100000, 999999);
            if (citySeedInput != null && int.TryParse(citySeedInput.text.Trim(), out int s)) targetSeed = Mathf.Abs(s);

            int targetW = 30;
            if (cityWidthInput != null && int.TryParse(cityWidthInput.text.Trim(), out int w)) targetW = Mathf.Clamp(w, 20, 60);

            int targetL = 30;
            if (cityLengthInput != null && int.TryParse(cityLengthInput.text.Trim(), out int l)) targetL = Mathf.Clamp(l, 20, 60);

            CityGeneratorPrototipo3.Instance.RegenerateCityRuntime(targetSeed, targetW, targetL);
            Close();
        }

        static InputField CreateInputField(Transform parent, string name, string initialValue, float x, float y, float w, float h)
        {
            var bg = FeralTheme.Panel(name, parent, x, y, w, h, FeralTheme.Ink);
            var text = FeralTheme.Label("Text", bg.transform, "", 10, 4, w - 20, h - 8, 18, FeralTheme.Cream);
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            var input = bg.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.text = initialValue;
            return input;
        }

        void ReturnPage()
        {
            FeralTheme.Label("Title", content, "¿Volver al menú?", 36, 150, 568, 70, 38);
            FeralTheme.Label("Body", content, "Se cerrará tu sesión actual en la ciudad procedural.", 36, 258, 568, 150, 25);
            FeralTheme.Button("Cancel", content, "Seguir jugando", 36, 470, 568, 64, Close, true);
            FeralTheme.Button("Confirm", content, "Volver al menú", 36, 552, 568, 64, ReturnToMenu);
            status = FeralTheme.Label("Status", content, "", 36, 645, 568, 60, 18, FeralTheme.Muted);
        }

        public void ReturnToMenu()
        {
            const string scene = "Assets/Prototipo3/Scenes/MainMenu_Prototipo3.unity";
            const string fallbackScene = "MainMenu_Prototipo3";
            const string legacyScene = "Assets/Prototipo2/Scenes/MainMenu_Prototipo2.unity";
            string target = Application.CanStreamedLevelBeLoaded(scene) ? scene
                : (Application.CanStreamedLevelBeLoaded(fallbackScene) ? fallbackScene
                : (Application.CanStreamedLevelBeLoaded(legacyScene) ? legacyScene : ""));
            if (string.IsNullOrEmpty(target))
            {
                if (status) status.text = "El menú no está incluido en las escenas del build.";
                return;
            }
            Close();
            PlayerPrefs.Save();
            if (inventory) inventory.Clear();
            Time.timeScale = 1;
            SceneManager.LoadScene(target);
        }

        void EndPage()
        {
            FeralTheme.Label("Title", content, "La noche te alcanzó", 36, 170, 568, 100, 40);
            FeralTheme.Label("Body", content, "Fuiste atrapado en la oscuridad. Reúne materiales y prepara trampas antes del próximo anochecer.", 36, 300, 568, 140, 25);
            FeralTheme.Button("Restart", content, "Despertar en la Base", 36, 490, 568, 64, () =>
            {
                Close();
                Time.timeScale = 1;
            }, true);
            FeralTheme.Button("Menu", content, "Menú principal", 36, 578, 568, 64, ReturnToMenu);
            status = FeralTheme.Label("Status", content, "", 36, 660, 568, 54, 18);
        }

        void DialoguePage()
        {
            FeralTheme.Label("Speaker", content, speaker, 36, 72, 1028, 45, 30, FeralTheme.Gold);
            var scroll = FeralTheme.Scroll("DialogueScroll", content, 36, 126, 1028, 110, 180);
            var body = FeralTheme.Label("DialogueText", scroll, dialogue, 0, 0, 996, 180, 24);
            float height = Mathf.Max(110, body.preferredHeight + 10);
            body.rectTransform.sizeDelta = new Vector2(996, height);
            scroll.sizeDelta = new Vector2(scroll.sizeDelta.x, height);
            FeralTheme.Button("Continue", content, "Continuar   [F / ESPACIO]", 680, 254, 384, 54, () => { if (Time.unscaledTime >= dialogueReady) Close(); }, true);
        }

        void CraftingPage()
        {
            Heading("Mesa de fabricación", "Fabrica tus 3 trampas tácticas [Teclas 1, 2 y 3]. Los materiales se descuentan al fabricar.");
            for (int i = 0; i < 3; i++)
            {
                var t = Traps[i];
                float x = 36 + i * 424;
                var card = FeralTheme.Panel("Receta " + t, content, x, 210, 400, 420, FeralTheme.Card).rectTransform;
                FeralTheme.Label("Number", card, "TECLA [" + (i + 1) + "]  /  TRAMPA " + (i + 1), 26, 24, 348, 28, 16, FeralTheme.Gold);
                FeralTheme.Label("Name", card, TrapName(t), 26, 56, 348, 48, 30);
                FeralTheme.Illustration("RecipeIcon", card, i + 1, 134, 100, 124);
                FeralTheme.Label("Description", card, TrapDescription(t), 26, 222, 348, 64, 17);
                FeralTheme.Label("Cost", card, Recipe(t), 26, 286, 348, 28, 19, FeralTheme.Gold);
                bool ready = CanCraft(t);
                FeralTheme.Label("Availability", card, ready ? "Materiales disponibles  ·  Tienes " + TrapCount(t) : "Faltan materiales  ·  Tienes " + TrapCount(t), 26, 318, 348, 34, 16, ready ? FeralTheme.Ready : FeralTheme.Muted);
                var b = FeralTheme.Button("Craft " + t, card, "Fabricar", 26, 354, 348, 52, () => Craft(t), true);
                b.interactable = table && ready;
            }
            FeralTheme.Label("Materials", content, string.Join("     ", ((ItemType[])Enum.GetValues(typeof(ItemType))).Select(t => t + "  " + Count(t))), 36, 660, 1248, 46, 21, FeralTheme.Muted);
        }

        public void Craft(TipoTrampa type)
        {
            if (!table || !table.IsMenuOpen || !CanCraft(type)) return;
            TrapSystemPrototipo3.EnsureInstance();
            TrapSystemPrototipo3.CraftTrap(type);
            dirty = true;
        }
    }
}
