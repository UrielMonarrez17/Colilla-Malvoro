using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Prototipo2.Narrative;

namespace Prototipo2.UI
{
    /// <summary>One Canvas and one modal owner for the tutorial and the playable prototype.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class FeralGameUI : MonoBehaviour
    {
        public enum Page { None, Inventory, Pause, Missions, Crafting, Dialogue, Return, Defeat, Victory }
        public static FeralGameUI Instance { get; private set; }
        public static bool BlocksGameplay => Instance != null && (Instance.CurrentPage != Page.None || Time.frameCount <= Instance.suppressInputUntilFrame);
        public static bool Supports(string scene) => scene == "Tutorial" || scene == "Prototipo2" || scene == "Prototipo1" || scene == "Game";
        public Page CurrentPage { get; private set; }
        public Canvas Canvas { get; private set; }
        public bool DialogueOpen => CurrentPage == Page.Dialogue;
        RectTransform hud, modal, content;
        Text health, staminaText, clock, objective, guide, phase, interaction, quickTraps, status;
        MenuBrushGraphic staminaFill, healthFill;
        Text[] trapAmounts = new Text[3];
        MenuBrushGraphic[] trapFrames = new MenuBrushGraphic[3];
        HamsterController player;
        InventoryManager inventory;
        StoryManagerPrototipo2 story;
        CraftingTable table;
        bool ownsPause, previousPlayerEnabled, previousCursorVisible, dirty, lastCinematic;
        CursorLockMode previousCursor;
        float previousScale, nextRefresh;
        int inventoryTab, selectedItem;
        int suppressInputUntilFrame = -1;
        string selectedMission, speaker, dialogue;
        float dialogueReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; SceneManager.sceneLoaded -= SceneLoaded; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { SceneManager.sceneLoaded -= SceneLoaded; SceneManager.sceneLoaded += SceneLoaded; }
        static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Supports(scene.name) || Instance != null) return;
            new GameObject("Feral • UI del juego").AddComponent<FeralGameUI>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            var root = new GameObject("Feral Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false); Canvas = root.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay; Canvas.sortingOrder = 150;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            BuildHud();
            modal = FeralTheme.Rect("Modal", root.transform, 0, 0, 1600, 900); FeralTheme.Stretch(modal);
            var shade = modal.gameObject.AddComponent<Image>(); shade.color = new Color(.015f, .012f, .01f, .82f);
            modal.gameObject.SetActive(false);
            CraftingTable.OnMenuToggled += CraftingToggled;
            CraftingTable.OnTrapCrafted += Crafted;
            SuppressLegacy();
        }
        IEnumerator Start()
        {
            MainMenuControllerPrototipo2.EnsureEventSystem(); MainMenuControllerPrototipo2.ApplySavedAudio();
            yield return null; // Let the existing scene create its player, tutorial and inventory.
            BindData(); SuppressLegacy(); RefreshHud();
        }
        void BindData()
        {
            if (!player) player = FindAnyObjectByType<HamsterController>();
            if (inventory == null)
            {
                inventory = InventoryManager.Instance;
                if (inventory == null) inventory = new GameObject("InventoryManager").AddComponent<InventoryManager>();
                inventory.Changed += OnInventoryChanged;
            }
            if (story == null && StoryManagerPrototipo2.Instance != null)
            {
                story = StoryManagerPrototipo2.Instance;
                story.OnDialogueTriggered += ShowDialogue;
                story.OnDemoCompleted += Won;
                story.OnActiveNodeChanged += MissionChanged;
            }
        }
        void OnInventoryChanged() { dirty = true; }
        void MissionChanged(StoryNode node) { dirty = true; }
        void Crafted(TipoTrampa type) { dirty = true; }
        void Won() { Open(Page.Victory); }
        bool Cinematic => (TutorialPipelineManager.Instance != null && TutorialPipelineManager.Instance.enCinematica) ||
            FindObjectsByType<CinematicaManager>(FindObjectsSortMode.None).Any(c => c.isActiveAndEnabled && !c.CinematicaTerminada);

        void Update()
        {
            bool cinematic = Cinematic;
            Canvas.enabled = !cinematic;
            if (cinematic || lastCinematic) { lastCinematic = cinematic; return; }
            if (CurrentPage == Page.Dialogue)
            {
                if (Time.unscaledTime >= dialogueReady && (FeralInput.Pressed(KeyCode.F) || FeralInput.Pressed(KeyCode.Space) || FeralInput.Pressed(KeyCode.Return) || FeralInput.Pressed(KeyCode.Escape))) Close();
            }
            else if (FeralInput.Pressed(KeyCode.Escape))
            {
                if (CurrentPage == Page.Return) Open(Page.Pause);
                else if (CurrentPage != Page.Defeat && CurrentPage != Page.Victory) { if (CurrentPage == Page.None) Open(Page.Pause); else Close(); }
            }
            else if (CurrentPage != Page.Return && CurrentPage != Page.Defeat && CurrentPage != Page.Victory)
            {
                if (FeralInput.Pressed(KeyCode.Tab)) { if (CurrentPage == Page.Inventory) Close(); else Open(Page.Inventory); }
                else if (FeralInput.Pressed(KeyCode.M)) { if (CurrentPage == Page.Missions) Close(); else Open(Page.Missions); }
            }
            if (dirty)
            {
                dirty = false;
                if (CurrentPage == Page.Inventory || CurrentPage == Page.Crafting || CurrentPage == Page.Missions) RenderPage();
            }
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + .2f; BindData(); SuppressLegacy(); RefreshHud();
                if (player && player.IsDead && TutorialPipelineManager.Instance == null && CurrentPage == Page.None) Open(Page.Defeat);
            }
        }
        public void Open(Page page)
        {
            BindData();
            if (page == Page.None) { Close(); return; }
            if (Cinematic) return;
            if (CurrentPage == Page.Crafting && page != Page.Crafting && table != null)
            { var old = table; table = null; old.CloseMenu(); }
            if (!ownsPause)
            {
                previousScale = Time.timeScale; previousCursor = Cursor.lockState; previousCursorVisible = Cursor.visible;
                previousPlayerEnabled = player && player.enabled; ownsPause = true;
            }
            if (player) player.enabled = false;
            Time.timeScale = 0; Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
            CurrentPage = page; modal.gameObject.SetActive(true); hud.gameObject.SetActive(false); RenderPage();
        }
        public void Close()
        {
            suppressInputUntilFrame = Time.frameCount + 1;
            CurrentPage = Page.None;
            if (table != null) { var old = table; table = null; old.CloseMenu(); }
            modal.gameObject.SetActive(false); hud.gameObject.SetActive(true); ReleasePause();
            PlayerPrefs.Save();
        }
        void ReleasePause()
        {
            if (!ownsPause) return;
            Time.timeScale = previousScale;
            if (player && !player.IsDead) player.enabled = previousPlayerEnabled;
            Cursor.visible = previousCursorVisible; Cursor.lockState = previousCursor; ownsPause = false;
        }
        void OnDestroy()
        {
            if (inventory != null) inventory.Changed -= OnInventoryChanged;
            if (story != null)
            {
                story.OnDialogueTriggered -= ShowDialogue; story.OnDemoCompleted -= Won; story.OnActiveNodeChanged -= MissionChanged;
            }
            CraftingTable.OnMenuToggled -= CraftingToggled; CraftingTable.OnTrapCrafted -= Crafted;
            ReleasePause(); if (Instance == this) Instance = null;
        }
        void CraftingToggled(CraftingTable source, bool opened)
        {
            if (opened)
            {
                if (!source.IsMenuOpen) return; // Tutorial may reject this interaction before our listener runs.
                if (source.craftingPanelUI != null) source.craftingPanelUI.SetActive(false);
                table = source; Open(Page.Crafting);
            }
            else if (table == source) { table = null; if (CurrentPage == Page.Crafting) Close(); }
        }
        public void ShowDialogue(string who, string text)
        {
            speaker = who; dialogue = text; dialogueReady = Time.unscaledTime + .35f; Open(Page.Dialogue);
        }

        void BuildHud()
        {
            hud = FeralTheme.Rect("HUD", Canvas.transform, 0, 0, 1600, 900); FeralTheme.Stretch(hud);
            var vitals = FeralTheme.Brush("Vitales", hud, 26, 26, 445, 130, FeralTheme.Ink).rectTransform;
            FeralTheme.Illustration("Hamster", vitals, 0, 0, 0, 125);
            health = FeralTheme.Label("Vida", vitals, "VIDA", 132, 14, 290, 24, 18);
            FeralTheme.Brush("HealthTrack", vitals, 132, 40, 290, 26, FeralTheme.Card);
            healthFill = FeralTheme.Brush("HealthFill", vitals, 132, 40, 290, 26, FeralTheme.Red);
            staminaText = FeralTheme.Label("Estamina", vitals, "ESTAMINA", 132, 76, 290, 22, 14, FeralTheme.Muted);
            FeralTheme.Brush("Track", vitals, 132, 98, 290, 20, FeralTheme.Card);
            staminaFill = FeralTheme.Brush("StaminaFill", vitals, 132, 98, 290, 20, FeralTheme.Gold);
            var mission = FeralTheme.Brush("Objetivo", hud, 26, 180, 445, 222, FeralTheme.Ink).rectTransform;
            FeralTheme.Brush("Paper", mission, 16, 14, 340, 40, FeralTheme.Cream);
            phase = FeralTheme.Label("Fase", mission, "MISIÓN ACTUAL", 30, 24, 320, 24, 15, FeralTheme.Ink);
            objective = FeralTheme.Label("ObjetivoActual", mission, "Explora la ciudad", 24, 70, 397, 68, 21);
            guide = FeralTheme.Label("Guia", mission, "", 24, 144, 397, 66, 16, FeralTheme.Muted);
            var bottom = FeralTheme.Brush("Atajos", hud, 32, 0, 280, 104, FeralTheme.Ink).rectTransform;
            bottom.anchorMin = bottom.anchorMax = bottom.pivot = Vector2.zero; bottom.anchoredPosition = new Vector2(32, 28);
            FeralTheme.Label("Teclas", bottom, "TAB  Inventario     M  Misiones\nESC  Pausa            F  Interactuar", 20, 25, 250, 70, 17, FeralTheme.Cream);
            var hotbar = FeralTheme.Rect("TrampasRapidas", hud, 0, 0, 330, 108);
            hotbar.anchorMin = hotbar.anchorMax = hotbar.pivot = new Vector2(.5f, 0); hotbar.anchoredPosition = new Vector2(0, 28);
            for (int i = 0; i < 3; i++)
            {
                trapFrames[i] = FeralTheme.Brush("Slot " + i, hotbar, i * 112, 0, 104, 104, FeralTheme.Gold);
                FeralTheme.Brush("SlotInset", trapFrames[i].transform, 4, 4, 96, 96, FeralTheme.Ink);
                FeralTheme.Illustration("TrapIcon", trapFrames[i].transform, i + 1, 9, 9, 82);
                FeralTheme.Label("Key", trapFrames[i].transform, (i + 1).ToString(), 8, 6, 24, 24, 18);
                trapAmounts[i] = FeralTheme.Label("Amount", trapFrames[i].transform, "0", 72, 76, 28, 24, 18);
            }
            var clockPanel = FeralTheme.Brush("ClockPaper", hud, 0, 0, 220, 66, FeralTheme.Cream).rectTransform;
            clockPanel.anchorMin = clockPanel.anchorMax = clockPanel.pivot = Vector2.one; clockPanel.anchoredPosition = new Vector2(-32, -32);
            clock = FeralTheme.Label("Reloj", clockPanel, "", 20, 18, 180, 32, 22, FeralTheme.Ink); clock.alignment = TextAnchor.MiddleCenter;
            interaction = FeralTheme.Label("Interaccion", hud, "", 0, 0, 700, 65, 23);
            interaction.rectTransform.anchorMin = interaction.rectTransform.anchorMax = interaction.rectTransform.pivot = new Vector2(.5f, 0);
            interaction.rectTransform.anchoredPosition = new Vector2(0, 140); interaction.alignment = TextAnchor.MiddleCenter;
            interaction.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
        }
        static string Clean(string text) => System.Text.RegularExpressions.Regex.Replace(text ?? "", "<.*?>", "");
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
                interaction.text = target == null ? "" : target.GetComponent<CraftingTable>() ? "[ F ]  Usar mesa de fabricación" : target.GetComponent<LootContainer>() ? "[ F ]  Buscar materiales" : target.GetComponent<PuertaTrampa>() ? "[ F ]  Colocar trampa" : "[ F ]  Hablar";
            }
            for (int i = 0; i < 3; i++) { trapAmounts[i].text = TrapCount(Traps[i]).ToString(); trapFrames[i].color = player && player.trampaSeleccionada == Traps[i] ? FeralTheme.Gold : FeralTheme.Card; }
            var tm = TimeManager.Instance;
            clock.text = tm == null ? "" : $"{(tm.CurrentCycleState == TimeManager.CycleState.Day ? "DÍA" : "NOCHE")}   {tm.CurrentHour:00}:{tm.CurrentMinute:00}";
            var tutorial = TutorialPipelineManager.Instance;
            if (tutorial != null)
            {
                phase.text = "TUTORIAL   /   PASO " + ((int)tutorial.currentState + 1) + " DE 16";
                objective.text = Clean(tutorial.ObjectiveText); guide.text = Clean(tutorial.GuidanceText);
                if (!string.IsNullOrWhiteSpace(tutorial.ContextPrompt)) interaction.text = Clean(tutorial.ContextPrompt);
            }
            else if (story != null && story.CurrentActiveNode != null)
            {
                var n = story.CurrentActiveNode; phase.text = "MISIÓN ACTIVA   /   [ M ] DIARIO";
                objective.text = n.title; guide.text = n.hint;
            }
        }

        void SuppressLegacy()
        {
            foreach (var old in FindObjectsByType<NarrativeHUDPrototipo2>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                old.UseUnifiedPresentation();
            foreach (var old in FindObjectsByType<InGameInventoryUIPrototipo2>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                old.enabled = false;
                foreach (var t in old.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Inventory" || t.name == "InventoryHint" || t.name == "ReturnConfirmation") t.gameObject.SetActive(false);
            }
            foreach (var old in FindObjectsByType<Feral.UI.FeralInventoryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                old.enabled = false;
                foreach (var t in old.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Inventory" || t.name == "InventoryHint" || t.name == "ReturnConfirmation") t.gameObject.SetActive(false);
            }
            if (player && player.staminaSlider) player.staminaSlider.gameObject.SetActive(false);
            var tutorial = TutorialPipelineManager.Instance;
            if (tutorial != null) tutorial.UseUnifiedPresentation();
        }

        void RenderPage()
        {
            if (content != null) { content.gameObject.SetActive(false); Destroy(content.gameObject); }
            bool small = CurrentPage == Page.Pause || CurrentPage == Page.Return || CurrentPage == Page.Defeat || CurrentPage == Page.Victory;
            float width = small ? 620 : 1320, height = CurrentPage == Page.Dialogue ? 330 : 740;
            if (CurrentPage == Page.Dialogue) width = 1100;
            content = FeralTheme.Brush("Contenido " + CurrentPage, modal, 0, 0, width, height, FeralTheme.Ink).rectTransform;
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, .5f); content.anchoredPosition = Vector2.zero;
            FeralTheme.Panel("Accent", content, 0, 0, width, 5, FeralTheme.Red);
            FeralTheme.Label("Kicker", content, "FERAL  /  PREPÁRATE PARA LA NOCHE", 36, 28, width - 72, 28, 16, FeralTheme.Gold);
            if (CurrentPage != Page.Dialogue && CurrentPage != Page.Defeat && CurrentPage != Page.Victory)
                FeralTheme.Button("Close", content, "Volver  [ESC]", width - 236, 68, 200, 48, Close);
            switch (CurrentPage)
            {
                case Page.Inventory: InventoryPage(); break;
                case Page.Pause: PausePage(); break;
                case Page.Missions: MissionsPage(); break;
                case Page.Crafting: CraftingPage(); break;
                case Page.Dialogue: DialoguePage(); break;
                case Page.Return: ReturnPage(); break;
                case Page.Defeat: EndPage(false); break;
                case Page.Victory: EndPage(true); break;
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
        public static string TrapName(TipoTrampa t) => t == TipoTrampa.Puas ? "Púas" : t == TipoTrampa.Red ? "Red" : "Madera";
        int TrapCount(TipoTrampa t) => inventory != null ? inventory.GetTrapCount(t) : 0;
        int Count(ItemType item) => inventory != null ? inventory.GetCount(item) : 0;
        static string Recipe(TipoTrampa t) => t == TipoTrampa.Puas ? "1 Madera + 1 Metal" : t == TipoTrampa.Red ? "1 Goma + 1 Desperdicios" : "2 Madera";
        static string TrapDescription(TipoTrampa t) => t == TipoTrampa.Puas ? "Prepara una entrada con púas para frenar a los animales ferales." : t == TipoTrampa.Red ? "Una red para atrapar a quien cruce la puerta protegida." : "Una trampa hecha de madera recuperada para proteger el refugio.";
        bool CanCraft(TipoTrampa t) => inventory != null && (t == TipoTrampa.Puas ? Count(ItemType.Madera) >= 1 && Count(ItemType.Metal) >= 1 : t == TipoTrampa.Red ? Count(ItemType.Goma) >= 1 && Count(ItemType.Desperdicios) >= 1 : Count(ItemType.Madera) >= 2);
        bool RecipeUnlocked(TipoTrampa t) => t == TipoTrampa.Puas || TutorialPipelineManager.Instance == null || TutorialPipelineManager.Instance.currentState == TutorialPipelineManager.TutorialState.FinTutorial;
        void Icon(Transform parent, ItemType item, float x, float y, float size)
        {
            var texture = Resources.Load<Texture2D>("FeralUI/" + item);
            if (!texture) return;
            var image = FeralTheme.Rect("Icon " + item, parent, x, y, size, size).gameObject.AddComponent<RawImage>();
            image.texture = texture; image.raycastTarget = false;
        }
        void InventoryPage()
        {
            Heading("INVENTARIO", "Todo lo que has recuperado. Todo lo que te ayudará a sobrevivir.");
            FeralTheme.Button("Materials", content, "Objetos", 302, 190, 250, 52, () => { inventoryTab = 0; selectedItem = 0; RenderPage(); }, inventoryTab == 0);
            FeralTheme.Button("Traps", content, "Trampas", 566, 190, 250, 52, () => { inventoryTab = 1; selectedItem = 0; RenderPage(); }, inventoryTab == 1);
            FeralTheme.Button("InventoryNav", content, "Inventario", 30, 270, 240, 64, () => {}, true);
            var craftNav = FeralTheme.Button("CraftNav", content, "Fabricación", 30, 348, 240, 64, () => Open(Page.Crafting));
            craftNav.interactable = table && table.IsMenuOpen;
            FeralTheme.Button("JournalNav", content, "Objetivos   [M]", 30, 426, 240, 64, () => Open(Page.Missions));
            FeralTheme.Illustration("HamsterInventory", content, 0, 48, 490, 190);
            FeralTheme.Label("CraftHint", content, "Fabrica en una mesa de trabajo", 36, 700, 230, 24, 14, FeralTheme.Muted);
            FeralTheme.Brush("MaterialHeading", content, 302, 268, 250, 36, FeralTheme.Cream);
            FeralTheme.Label("MaterialHeadingText", content, "MATERIALES", 316, 276, 230, 24, 16, FeralTheme.Ink);
            var items = (ItemType[])Enum.GetValues(typeof(ItemType));
            for (int i = 0; i < items.Length; i++)
            {
                int id = i; float x = 302 + (i % 3) * 174, y = 316 + (i / 3) * 126;
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
                int id = i; var b = FeralTheme.Button("Trampa " + Traps[i], content, "", 302 + i * 174, 610, 162, 100, () => { inventoryTab = 1; selectedItem = id; RenderPage(); });
                SlotStyle(b, inventoryTab == 1 && i == selectedItem);
                FeralTheme.Illustration("TrapIcon", b.transform, i + 1, 14, 0, 96);
                FeralTheme.Label("Count", b.transform, "×" + TrapCount(Traps[i]), 111, 62, 45, 32, 23);
            }
            selectedItem = Mathf.Clamp(selectedItem, 0, inventoryTab == 0 ? items.Length - 1 : 2);
            var pane = FeralTheme.Brush("Detalle", content, 862, 190, 422, 520, FeralTheme.Card).rectTransform;
            string title = inventoryTab == 0 ? items[selectedItem].ToString() : "Trampa de " + TrapName(Traps[selectedItem]);
            FeralTheme.Brush("Paper", pane, 18, 16, 386, 52, FeralTheme.Cream);
            FeralTheme.Label("Name", pane, title, 34, 30, 356, 38, 27, FeralTheme.Ink);
            if (inventoryTab == 0) Icon(pane, items[selectedItem], 108, 78, 200);
            else FeralTheme.Illustration("TrapPreview", pane, selectedItem + 1, 90, 66, 240);
            string body = inventoryTab == 0 ? MaterialDescription(items[selectedItem]) : TrapDescription(Traps[selectedItem]);
            FeralTheme.Label("Description", pane, body, 26, 294, 370, 130, 21);
            if (inventoryTab == 1)
            {
                var trap = Traps[selectedItem];
                FeralTheme.Label("Recipe", pane, Recipe(trap), 26, 425, 370, 30, 17, FeralTheme.Gold);
                var equip = FeralTheme.Button("Equip", pane, player && player.trampaSeleccionada == trap ? "Equipada" : "Equipar trampa", 26, 460, 370, 52, () => { if (player) player.trampaSeleccionada = trap; RenderPage(); }, true);
                equip.interactable = player && TrapCount(trap) > 0;
            }
            else FeralTheme.Label("Owned", pane, "EN TU MOCHILA   " + Count(items[selectedItem]), 26, 466, 370, 32, 20, FeralTheme.Gold);
        }
        static void SlotStyle(Button button, bool selected)
        {
            var colors = button.colors; colors.normalColor = selected ? FeralTheme.Gold : FeralTheme.Card;
            colors.highlightedColor = colors.selectedColor = FeralTheme.Gold; button.colors = colors;
            var inset = FeralTheme.Brush("Inset", button.transform, 4, 4, button.GetComponent<RectTransform>().sizeDelta.x - 8, button.GetComponent<RectTransform>().sizeDelta.y - 8, FeralTheme.Ink);
            inset.transform.SetAsFirstSibling();
        }
        static string MaterialDescription(ItemType item)
        {
            switch (item)
            {
                case ItemType.Madera: return "Tablas recuperadas de la ciudad.\n\nSe usan para fabricar trampas de púas y de madera.";
                case ItemType.Metal: return "Piezas resistentes recuperadas.\n\nCombínalas con madera para fabricar una trampa de púas.";
                case ItemType.Desperdicios: return "Restos que todavía pueden aprovecharse.\n\nLulú los necesita. También se usan en las trampas de red.";
                case ItemType.Goma: return "Fragmentos de goma reutilizable.\n\nCombínalos con desperdicios para fabricar una red.";
                default: return "Semillas reunidas durante la exploración. Guárdalas para los objetivos que las requieran.";
            }
        }
        void Detail(string title, string count, string body, string hint)
        {
            var panel = FeralTheme.Panel("Detalle", content, 780, 270, 504, 386, FeralTheme.Card).rectTransform;
            FeralTheme.Label("Name", panel, title, 32, 26, 440, 48, 30);
            FeralTheme.Label("Count", panel, count, 32, 84, 440, 30, 17, FeralTheme.Gold);
            FeralTheme.Label("Description", panel, body, 32, 140, 440, 130, 23);
            FeralTheme.Label("Hint", panel, hint, 32, 282, 440, 90, 18, FeralTheme.Muted);
        }
        void PausePage()
        {
            FeralTheme.Label("Title", content, "En pausa", 36, 80, 320, 60, 42);
            FeralTheme.Label("Subtitle", content, "Respira. La ciudad puede esperar.", 36, 148, 550, 36, 21, FeralTheme.Muted);
            FeralTheme.Button("Resume", content, "Continuar", 36, 215, 548, 60, Close, true);
            FeralTheme.Button("Inventory", content, "Mochila   [TAB]", 36, 292, 548, 60, () => Open(Page.Inventory));
            FeralTheme.Button("Missions", content, "Misiones   [M]", 36, 369, 548, 60, () => Open(Page.Missions));
            var volumeText = FeralTheme.Label("VolumeLabel", content, "Volumen", 36, 474, 548, 30, 22);
            var volume = FeralTheme.Slider("Volume", content, 36, 519, 548, PlayerPrefs.GetFloat("Feral.UI.MasterVolume", .8f), v =>
            {
                PlayerPrefs.SetFloat("Feral.UI.MasterVolume", v); PlayerPrefs.SetInt("Feral.UI.Muted", 0);
                MainMenuControllerPrototipo2.ApplySavedAudio(); volumeText.text = "Volumen   " + Mathf.RoundToInt(v * 100) + "%";
            });
            volumeText.text = "Volumen   " + Mathf.RoundToInt(volume.value * 100) + "%";
            FeralTheme.Button("ReturnMenu", content, "Volver al menú principal", 36, 620, 548, 60, () => Open(Page.Return));
        }
        void ReturnPage()
        {
            FeralTheme.Label("Title", content, "¿Volver al menú?", 36, 150, 548, 70, 38);
            FeralTheme.Label("Body", content, "La partida actual no se guarda. Se perderán los materiales, las trampas y el progreso de esta sesión.", 36, 258, 548, 150, 25);
            FeralTheme.Button("Cancel", content, "Seguir jugando", 36, 470, 548, 64, Close, true);
            FeralTheme.Button("Confirm", content, "Volver al menú", 36, 552, 548, 64, ReturnToMenu);
            status = FeralTheme.Label("Status", content, "", 36, 645, 548, 60, 18, FeralTheme.Muted);
        }
        public void ReturnToMenu()
        {
            const string scene = "Assets/Prototipo2/Scenes/MainMenu_Prototipo2.unity";
            if (!Application.CanStreamedLevelBeLoaded(scene)) { if (status) status.text = "El menú no está incluido en las escenas del build."; return; }
            Close(); PlayerPrefs.Save(); if (inventory) inventory.Clear(); Time.timeScale = 1; SceneManager.LoadScene(scene);
        }
        void EndPage(bool won)
        {
            FeralTheme.Label("Title", content, won ? "A salvo" : "La noche te alcanzó", 36, 170, 548, 100, 40);
            FeralTheme.Label("Body", content, won ? "Llegaste a la colonia segura. Has completado esta aventura." : "Busca otra ruta, reúne materiales y prepara tus trampas antes de volver a salir.", 36, 300, 548, 140, 26);
            FeralTheme.Button("Restart", content, "Volver a intentarlo", 36, 490, 548, 64, () => { var path = SceneManager.GetActiveScene().path; Close(); inventory.Clear(); Time.timeScale = 1; SceneManager.LoadScene(path); }, true);
            FeralTheme.Button("Menu", content, "Menú principal", 36, 578, 548, 64, ReturnToMenu);
            status = FeralTheme.Label("Status", content, "", 36, 660, 548, 54, 18);
        }
        void DialoguePage()
        {
            FeralTheme.Label("Speaker", content, speaker, 36, 72, 1028, 45, 30, FeralTheme.Gold);
            var scroll = FeralTheme.Scroll("DialogueScroll", content, 36, 126, 1028, 110, 180);
            var body = FeralTheme.Label("DialogueText", scroll, dialogue, 0, 0, 996, 180, 25);
            float height = Mathf.Max(110, body.preferredHeight + 10); body.rectTransform.sizeDelta = new Vector2(996, height); scroll.sizeDelta = new Vector2(scroll.sizeDelta.x, height);
            FeralTheme.Button("Continue", content, "Continuar   [F / ESPACIO]", 680, 254, 384, 54, () => { if (Time.unscaledTime >= dialogueReady) Close(); }, true);
        }
        void CraftingPage()
        {
            Heading("Mesa de fabricación", "Elige una receta. Los materiales se descuentan al fabricar la trampa.");
            for (int i = 0; i < 3; i++)
            {
                var t = Traps[i]; float x = 36 + i * 424;
                var card = FeralTheme.Panel("Receta " + t, content, x, 210, 400, 420, FeralTheme.Card).rectTransform;
                FeralTheme.Label("Number", card, "0" + (i + 1) + "  /  TRAMPA", 26, 24, 348, 28, 16, FeralTheme.Gold);
                FeralTheme.Label("Name", card, TrapName(t), 26, 60, 348, 48, 34);
                FeralTheme.Illustration("RecipeIcon", card, i + 1, 134, 100, 132);
                FeralTheme.Label("Description", card, TrapDescription(t), 26, 226, 348, 60, 19);
                FeralTheme.Label("Cost", card, Recipe(t), 26, 282, 348, 28, 20);
                bool unlocked = RecipeUnlocked(t), ready = CanCraft(t);
                FeralTheme.Label("Availability", card, !unlocked ? "Se desbloquea al terminar el tutorial" : ready ? "Materiales disponibles  ·  Tienes " + TrapCount(t) : "Faltan materiales  ·  Tienes " + TrapCount(t), 26, 316, 348, 36, 16, ready && unlocked ? FeralTheme.Ready : FeralTheme.Muted);
                var b = FeralTheme.Button("Craft " + t, card, "Fabricar", 26, 354, 348, 52, () => Craft(t), true);
                b.interactable = table && unlocked && ready;
            }
            FeralTheme.Label("Materials", content, string.Join("     ", ((ItemType[])Enum.GetValues(typeof(ItemType))).Select(t => t + "  " + Count(t))), 36, 660, 1248, 46, 21, FeralTheme.Muted);
        }
        public void Craft(TipoTrampa type)
        {
            if (!table || !table.IsMenuOpen || !CanCraft(type) || !RecipeUnlocked(type)) return;
            if (type == TipoTrampa.Puas) table.CraftTrap1(); else if (type == TipoTrampa.Red) table.CraftTrap2(); else table.CraftTrap3();
            dirty = true;
        }
        void MissionsPage()
        {
            Heading("Diario de misiones", "Tu siguiente paso, las pistas que has reunido y los objetivos completados.");
            var tutorial = TutorialPipelineManager.Instance;
            if (tutorial != null)
            {
                var list = FeralTheme.Scroll("TutorialSteps", content, 36, 208, 550, 456, 16 * 70);
                var steps = (TutorialPipelineManager.TutorialState[])Enum.GetValues(typeof(TutorialPipelineManager.TutorialState));
                for (int i = 0; i < steps.Length; i++)
                {
                    string state = i < (int)tutorial.currentState ? "COMPLETADO" : i == (int)tutorial.currentState ? "EN CURSO" : "PENDIENTE";
                    FeralTheme.Label("Step " + i, list, (i + 1).ToString("00") + "  " + StepName(i) + "\n" + state, 16, i * 70, 500, 64, 20, i == (int)tutorial.currentState ? FeralTheme.Gold : FeralTheme.Muted);
                }
                MissionDetail("TUTORIAL / EN CURSO", Clean(tutorial.ObjectiveText), Clean(tutorial.GuidanceText));
                return;
            }
            var nodes = story?.CurrentGraph?.nodes.Where(n => n.state != StoryNodeState.Locked).ToArray();
            if (nodes == null || nodes.Length == 0)
            { FeralTheme.Label("Empty", content, "Aún no tienes misiones. Explora la ciudad y habla con sus habitantes.", 36, 240, 1000, 130, 28); return; }
            var selected = nodes.FirstOrDefault(n => n.id == selectedMission) ?? nodes.FirstOrDefault(n => n.state == StoryNodeState.Active) ?? nodes[0];
            var rows = FeralTheme.Scroll("MissionList", content, 36, 208, 550, 456, nodes.Length * 108);
            for (int i = 0; i < nodes.Length; i++)
            {
                var n = nodes[i]; var b = FeralTheme.Button("Mission " + n.id, rows, "", 0, i * 108, 530, 96, () => { selectedMission = n.id; RenderPage(); }, selected == n);
                FeralTheme.Label("Name", b.transform, n.title, 22, 12, 486, 42, 23);
                FeralTheme.Label("State", b.transform, n.state == StoryNodeState.Completed ? "COMPLETADA" : n.state == StoryNodeState.Active ? "EN CURSO" : "DISPONIBLE", 22, 63, 486, 24, 16, FeralTheme.Gold);
            }
            string progress = selected.type == StoryNodeType.Quest ? $"\n\nProgreso: {selected.currentItemProgress} / {selected.requiredItemCount}" : "";
            MissionDetail(selected.state == StoryNodeState.Completed ? "COMPLETADA" : "OBJETIVO", selected.title, selected.description + "\n\nPista: " + selected.hint + progress);
        }
        void MissionDetail(string state, string title, string description)
        {
            var pane = FeralTheme.Panel("MissionDetail", content, 624, 208, 660, 456, FeralTheme.Card).rectTransform;
            FeralTheme.Label("State", pane, state, 28, 24, 604, 32, 17, FeralTheme.Gold);
            var scroll = FeralTheme.Scroll("DetailScroll", pane, 28, 78, 604, 350, 500);
            var text = FeralTheme.Label("Content", scroll, title + "\n\n" + description, 10, 10, 570, 500, 25);
            float h = Mathf.Max(350, text.preferredHeight + 30); text.rectTransform.sizeDelta = new Vector2(570, h); scroll.sizeDelta = new Vector2(scroll.sizeDelta.x, h + 20);
        }
        static string StepName(int i)
        {
            string[] names = { "Primeros pasos", "Aprende a correr", "Busca materiales", "Encuentra a Lulú", "Reúne desperdicios", "Regresa con Lulú", "Cae la noche", "La amenaza feral", "Escapa", "Muévete en sigilo", "Vuelve al refugio", "La mesa de trabajo", "Tu primera trampa", "Protege la entrada", "La trampa en acción", "Listo para la ciudad" };
            return names[Mathf.Clamp(i, 0, names.Length - 1)];
        }
    }
}
