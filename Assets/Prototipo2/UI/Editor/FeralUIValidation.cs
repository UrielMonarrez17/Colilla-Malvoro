using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Prototipo2;
using Prototipo2.UI;
using Prototipo2.Narrative;
using Object = UnityEngine.Object;

/// <summary>Reproducible presentation/integration checks in a disposable scene, with real inventory/crafting components.</summary>
[InitializeOnLoad]
public static class FeralUIValidation
{
    const string Active = "Feral.UnifiedUI.Validation";
    const string ReturnScene = "Feral.UnifiedUI.ReturnScene";
    const string Flow = "Feral.UnifiedUI.Flow";
    static IEnumerator suite;
    static double next;
    static int lastFrame = -1;
    static FeralGameUI ui;
    static string report = "";
    public static void Batch()
    {
        var atlas = AssetImporter.GetAtPath("Assets/Resources/FeralUI/SurvivalAtlas.png") as TextureImporter;
        if (atlas) { atlas.alphaIsTransparency = true; atlas.mipmapEnabled = false; atlas.maxTextureSize = 2048; atlas.textureCompression = TextureImporterCompression.Uncompressed; atlas.SaveAndReimport(); }
        StyleMenu();
        Run();
    }
    static FeralUIValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Active, false))
            {
                SessionState.SetBool(Active, false); suite = null;
                string path = SessionState.GetString(ReturnScene, "");
                if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path);
            }
        };
    }
    [MenuItem("Feral/UI unificada/Aplicar estilo a submenús")]
    public static void StyleMenu()
    {
        var menuScene = EditorSceneManager.OpenScene("Assets/Prototipo2/Scenes/MainMenu_Prototipo2.unity", OpenSceneMode.Additive);
        try
        {
            var menu = menuScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MainMenuControllerPrototipo2>(true)).Single();
            FeralTheme.ApplyMenu(menu.transform); EditorSceneManager.SaveScene(menuScene);
            foreach (var pageName in new[] { "Home", "Sessions", "Settings", "Controls", "Exit" })
            {
                foreach (var t in menu.GetComponentsInChildren<RectTransform>(true))
                    if (new[] { "Home", "Sessions", "Settings", "Controls", "Exit" }.Contains(t.name)) t.gameObject.SetActive(t.name == pageName);
                Capture(menu.GetComponent<Canvas>(), "Menu-" + pageName);
            }
        }
        finally { EditorSceneManager.CloseScene(menuScene, true); }
        Debug.Log("FERAL_SUBMENUS_STYLED");
    }
    [MenuItem("Feral/UI unificada/Validar y capturar pantallas")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Sal de Play antes de ejecutar la validación aislada.");
        var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (current.isDirty) throw new InvalidOperationException("Guarda primero la escena abierta para conservar los cambios antes de validar.");
        SessionState.SetString(ReturnScene, current.path);
        SessionState.SetBool(Flow, false);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var player = new GameObject("Validation player", typeof(Rigidbody), typeof(CapsuleCollider), typeof(HamsterController));
        player.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        new GameObject("Inventory", typeof(InventoryManager));
        new GameObject("UI", typeof(FeralGameUI));
        new GameObject("Crafting table", typeof(CraftingTable));
        EditorSceneManager.SaveScene(scene, "Assets/Prototipo2/UI/Editor/UIValidation.unity");
        SessionState.SetBool(Active, true);
        EditorApplication.EnterPlaymode();
    }
    [MenuItem("Feral/UI unificada/Validar flujo de escenas")]
    public static void RunFlow()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("La escena debe estar guardada y fuera de Play.");
        SessionState.SetString(ReturnScene, UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        EditorSceneManager.OpenScene("Assets/Prototipo2/Scenes/MainMenu_Prototipo2.unity");
        SessionState.SetBool(Flow, true); SessionState.SetBool(Active, true);
        EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isPaused) return;
        Application.runInBackground = true;
        EditorApplication.QueuePlayerLoopUpdate();
        if (EditorApplication.timeSinceStartup < next) return;
        if (lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        next = EditorApplication.timeSinceStartup + .5;
        try
        {
            if (suite == null) { report = ""; suite = SessionState.GetBool(Flow, false) ? CheckFlow() : CheckScreens(); }
            if (!suite.MoveNext())
            {
                Directory.CreateDirectory("Docs/Previews/UnifiedUI");
                File.WriteAllText("Docs/Previews/UnifiedUI/" + (SessionState.GetBool(Flow, false) ? "flow-validation.txt" : "validation.txt"), report + "\nALL CHECKS PASSED\n");
                Debug.Log("FERAL_UNIFIED_UI_VALIDATION_OK\n" + report);
                if (Application.isBatchMode) { EditorApplication.Exit(0); return; }
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e); File.WriteAllText("Temp/FeralUI-validation-failure.txt", e.ToString());
            if (Application.isBatchMode) { EditorApplication.Exit(1); return; }
            EditorApplication.ExitPlaymode();
        }
    }
    static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); report += "PASS: " + message + "\n"; }
    static IEnumerator CheckFlow()
    {
        yield return null;
        var menu = Object.FindAnyObjectByType<MainMenuControllerPrototipo2>();
        Check(menu != null, "Main menu opens with its controller");
        menu.StartTutorial(); yield return null; yield return null;
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Tutorial", "Play loads the actual Tutorial scene");
        foreach (var cinema in Object.FindObjectsByType<CinematicaManager>(FindObjectsSortMode.None)) cinema.IniciarGameplay(null);
        yield return null; yield return null;
        ui = FeralGameUI.Instance;
        Check(ui && ui.Canvas.isActiveAndEnabled, "Tutorial presents the unified Canvas after the intro");
        Check(!string.IsNullOrWhiteSpace(TutorialPipelineManager.Instance.ObjectiveText), "Live tutorial has an objective");
        ScreenCapture.CaptureScreenshot("Docs/Previews/UnifiedUI/Tutorial-InGame.png"); yield return null;
        ui.Open(FeralGameUI.Page.Inventory); yield return null;
        Check(ui.CurrentPage == FeralGameUI.Page.Inventory && Time.timeScale == 0, "Inventory pauses the actual tutorial"); ui.Close();
        yield return null; yield return null;
        var tutorial = TutorialPipelineManager.Instance;
        tutorial.StartCoroutine((IEnumerator)typeof(TutorialPipelineManager).GetMethod("TransicionarAPrototipo2Routine", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(tutorial, null));
        double deadline = EditorApplication.timeSinceStartup + 45;
        while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Tutorial" && EditorApplication.timeSinceStartup < deadline) yield return null;
        yield return null; yield return null;
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Prototipo2", "Tutorial completion transition loads Prototipo2");
        ui = FeralGameUI.Instance; Check(ui && ui.Canvas.isActiveAndEnabled, "Prototype presents the unified Canvas");
        Check(!Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Any(t => t.name == "PanelMisionNarrativa" && t.gameObject.activeInHierarchy), "Legacy narrative panel does not overlap the new HUD");
        ScreenCapture.CaptureScreenshot("Docs/Previews/UnifiedUI/Prototype-InGame.png"); yield return null;
        ui.Open(FeralGameUI.Page.Missions); yield return null;
        Check(ui.GetComponentsInChildren<Text>().Any(t => t.name == "Name"), "Prototype journal displays generated missions");
        ScreenCapture.CaptureScreenshot("Docs/Previews/UnifiedUI/Prototype-Missions.png"); yield return null;
        ui.Close(); ui.ReturnToMenu(); yield return null;
        Check(Object.FindAnyObjectByType<MainMenuControllerPrototipo2>() != null && Time.timeScale == 1, "Return to main menu restores time");
    }
    static void Click(string name)
    {
        var b = ui.GetComponentsInChildren<Button>().Single(x => x.name == name);
        Check(b.interactable, name + " is interactable"); b.onClick.Invoke();
    }
    static IEnumerator CheckScreens()
    {
        yield return null;
        ui = FeralGameUI.Instance; var inv = InventoryManager.Instance;
        var player = Object.FindAnyObjectByType<HamsterController>();
        Check(ui != null && ui.Canvas != null, "A single uGUI presentation is created");
        Check(Object.FindObjectsByType<FeralGameUI>(FindObjectsSortMode.None).Length == 1, "No duplicate unified UI");
        int notifications = 0; Action changed = () => notifications++; inv.Changed += changed;
        inv.AddTrampa(TipoTrampa.Puas); inv.RemoveTrampa(TipoTrampa.Puas);
        Check(notifications == 2, "Trap additions and removals notify the UI");
        inv.AddTrampa(TipoTrampa.Red); inv.Clear();
        Check(inv.GetTrapCount(TipoTrampa.Red) == 0, "Starting a new session clears crafted traps");
        inv.Changed -= changed;
        Time.timeScale = .5f; player.enabled = false;
        ui.Open(FeralGameUI.Page.Inventory); ui.Open(FeralGameUI.Page.Missions); ui.Close();
        Check(Mathf.Approximately(Time.timeScale, .5f) && !player.enabled, "Modal switching restores the previous time scale and disabled player state");
        Check(FeralGameUI.BlocksGameplay, "Closing input does not leak into gameplay in the same frame");
        Time.timeScale = 1; player.enabled = true;
        yield return null;
        yield return null;
        Check(!FeralGameUI.BlocksGameplay, "Input suppression expires after closing");
        foreach (ItemType item in Enum.GetValues(typeof(ItemType))) inv.AddItem(item, item == ItemType.Madera ? 8 : 3);
        inv.AddTrampa(TipoTrampa.Puas); inv.AddTrampa(TipoTrampa.Red);
        player.currentStamina = 64;
        yield return null;
        Capture(ui.Canvas, "HUD");
        ui.Open(FeralGameUI.Page.Inventory); yield return null; Capture(ui.Canvas, "Inventory-Materials");
        Click("Traps"); yield return null; Capture(ui.Canvas, "Inventory-Traps");
        Click("Equip"); Check(player.trampaSeleccionada == TipoTrampa.Puas, "Owned traps can be equipped");
        ui.Close(); yield return null;
        ui.Open(FeralGameUI.Page.Pause); yield return null;
        Check(Time.timeScale == 0 && !player.enabled, "Pause freezes gameplay"); Capture(ui.Canvas, "Pause");
        ui.Close(); Check(Time.timeScale == 1 && player.enabled, "Resume restores gameplay");
        var table = Object.FindAnyObjectByType<CraftingTable>();
        table.ToggleCraftingMenu(); yield return null;
        Check(ui.CurrentPage == FeralGameUI.Page.Crafting, "The actual crafting-table event opens the shared crafting screen");
        Capture(ui.Canvas, "Crafting");
        int wood = inv.GetCount(ItemType.Madera), metal = inv.GetCount(ItemType.Metal), traps = inv.GetTrapCount(TipoTrampa.Puas);
        ui.Craft(TipoTrampa.Puas); yield return null;
        Check(inv.GetCount(ItemType.Madera) == wood - 1 && inv.GetCount(ItemType.Metal) == metal - 1 && inv.GetTrapCount(TipoTrampa.Puas) == traps + 1, "Crafting consumes the actual recipe and grants a trap");
        inv.Clear(); yield return null; ui.Craft(TipoTrampa.Puas);
        Check(inv.GetTrapCount(TipoTrampa.Puas) == 0, "Insufficient materials cannot produce traps");
        ui.Close(); Check(!table.IsMenuOpen && Time.timeScale == 1, "Closing crafting synchronizes table and pause state");
        yield return null;
        var storyGo = new GameObject("Validation narrative"); storyGo.SetActive(false);
        var story = storyGo.AddComponent<StoryManagerPrototipo2>(); storyGo.SetActive(true); story.enabled = false;
        var active = new StoryNode { id = "quest", title = "Una ayuda antes del anochecer", description = "Lulú necesita materiales para preparar el refugio.", hint = "Busca en los contenedores de la calle y vuelve con Lulú.", type = StoryNodeType.Quest, state = StoryNodeState.Active, requiredItemCount = 3, currentItemProgress = 1 };
        var graph = new StoryGraph(); graph.nodes.Add(new StoryNode { id = "first", title = "Encuentra a Lulú", state = StoryNodeState.Completed }); graph.nodes.Add(active);
        graph.nodes.Add(new StoryNode { id = "secret", title = "Hidden future objective", state = StoryNodeState.Locked });
        Field(story, "currentGraph", graph); Field(story, "currentActiveNode", active);
        yield return null;
        ui.Open(FeralGameUI.Page.Missions); yield return null;
        Check(!ui.GetComponentsInChildren<Text>().Any(t => t.text.Contains("Hidden future")), "Journal hides unrevealed mission spoilers");
        Capture(ui.Canvas, "Missions");
        Check(ui.GetComponentsInChildren<Text>().Any(t => t.text.Contains("1 / 3")), "Journal displays actual objective progress: " + string.Join(" | ", ui.GetComponentsInChildren<Text>().Select(t => t.text)));
        ui.Close(); yield return null;
        ui.ShowDialogue("Lulú", "¡Qué bueno verte fuera de la madriguera!\nAntes de que caiga el sol, busca tres desperdicios en los contenedores de la calle. Podemos usarlos para preparar el refugio.");
        yield return null; Capture(ui.Canvas, "Dialogue"); Click("Continue"); yield return null;
        Check(!ui.DialogueOpen && Time.timeScale == 1, "Dialogue confirmation releases pause");
        ui.Open(FeralGameUI.Page.Return); yield return null; Capture(ui.Canvas, "ReturnConfirmation"); ui.Close();
        yield return null;
        // A disabled tutorial fixture exposes real text bindings without generating a city or changing progression.
        var tutorialGo = new GameObject("Validation tutorial"); tutorialGo.SetActive(false);
        var tutorial = tutorialGo.AddComponent<TutorialPipelineManager>(); tutorial.enabled = false;
        tutorial.currentState = TutorialPipelineManager.TutorialState.RecolectarLoot;
        tutorialGo.SetActive(true);
        var texts = new GameObject("Tutorial text fixture");
        var objective = new GameObject("Objective", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>(); objective.transform.SetParent(texts.transform);
        objective.text = "Reúne 3 desperdicios para Lulú (1 / 3)";
        var guidance = new GameObject("Guidance", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>(); guidance.transform.SetParent(texts.transform);
        guidance.text = "Busca contenedores en la ciudad y pulsa [F] para recoger materiales.";
        Field(tutorial, "textoMisionBanner", objective); Field(tutorial, "textoSubtituloGuia", guidance);
        yield return null; Capture(ui.Canvas, "Tutorial-HUD");
        ui.Open(FeralGameUI.Page.Missions); yield return null; Capture(ui.Canvas, "Tutorial-Journal"); ui.Close();
        yield return null;
        Check(ui.GetComponentsInChildren<Text>().Any(t => t.name == "ObjetivoActual" && t.text.Contains("desperdicios")), "Tutorial HUD reads the live tutorial objective");
    }
    static void Field(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    public static void Capture(Canvas canvas, string name, int width = 1600, int height = 900)
    {
        Directory.CreateDirectory("Docs/Previews/UnifiedUI");
        var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; float oldPlane = canvas.planeDistance;
        var scaler = canvas.GetComponent<CanvasScaler>(); bool scalerEnabled = scaler && scaler.enabled;
        float oldScale = canvas.scaleFactor;
        if (scaler) scaler.enabled = false;
        canvas.scaleFactor = 1;
        var cam = new GameObject("UI validation camera", typeof(Camera)).GetComponent<Camera>();
        cam.transform.position = new Vector3(0, 0, -10); cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(.035f, .045f, .055f); cam.cullingMask = 1 << 5;
        foreach (var t in canvas.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;
        var target = new RenderTexture(width, height, 24); cam.targetTexture = target;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1;
        foreach (var label in canvas.GetComponentsInChildren<Text>(true)) label.SetAllDirty();
        Canvas.ForceUpdateCanvases(); cam.Render();
        var old = RenderTexture.active; RenderTexture.active = target;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
        File.WriteAllBytes("Docs/Previews/UnifiedUI/" + name + ".png", tex.EncodeToPNG());
        RenderTexture.active = old; canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane;
        canvas.scaleFactor = oldScale; if (scaler) scaler.enabled = scalerEnabled;
        Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(tex); target.Release(); Object.DestroyImmediate(target);
    }
}
