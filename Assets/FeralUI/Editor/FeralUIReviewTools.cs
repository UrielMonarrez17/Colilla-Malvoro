using System;
using System.Linq;
using Feral.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FeralUIReviewTools
{
    const string BatchKey = "Feral.UI.BatchRuntime";
    static int runtimeStage;
    static double nextCheck;

    [InitializeOnLoadMethod]
    static void ResumeBatchValidation()
    {
        EditorApplication.update -= RuntimeTick;
        EditorApplication.update += RuntimeTick;
    }

    [MenuItem("Feral/UI/Abrir menú principal")]
    static void OpenMenu() { Open(FeralUICommon.MenuScene); }

    [MenuItem("Feral/UI/Abrir Game (Prototipo1 con inventario)")]
    static void OpenInventory() { Open(FeralUICommon.GameScene); }

    static void Open(string path)
    {
        if (!EditorApplication.isPlaying && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(path);
    }

    [MenuItem("Feral/UI/Preparar escenas para build de revisión")]
    public static void ConfigureBuild()
    {
        var paths = new[] { FeralUICommon.MenuScene, FeralUICommon.GameScene };
        var existing = EditorBuildSettings.scenes.Where(s => !paths.Contains(s.path));
        EditorBuildSettings.scenes = paths.Select(p => new EditorBuildSettingsScene(p, true)).Concat(existing).ToArray();
        Debug.Log("Feral: menú y copia de juego agregados al inicio del build; se conservaron las escenas anteriores.");
    }

    [MenuItem("Feral/UI/Añadir materiales de prueba (Play)")]
    static void SeedInventory()
    {
        foreach (ItemType item in Enum.GetValues(typeof(ItemType))) InventoryManager.Instance.AddItem(item, 3);
    }

    [MenuItem("Feral/UI/Añadir materiales de prueba (Play)", true)]
    static bool CanSeed() { return EditorApplication.isPlaying && InventoryManager.Instance != null; }

    [MenuItem("Feral/UI/Validar escenas")]
    public static void ValidateScenes()
    {
        ValidateScene(FeralUICommon.MenuScene, typeof(FeralMainMenu));
        ValidateScene(FeralUICommon.GameScene, typeof(FeralInventoryUI));
        Debug.Log("FERAL_UI_VALIDATION_OK: escenas, referencias, botones y texto verificados.");
    }

    static void ValidateScene(string path, Type controller)
    {
        var scene = EditorSceneManager.OpenPreviewScene(path);
        try
        {
            var roots = scene.GetRootGameObjects();
            var ui = roots.Single(go => go.GetComponent(controller) != null);
            var canvas = ui.GetComponent<Canvas>();
            if (canvas == null || canvas.GetComponent<CanvasScaler>() == null) throw new Exception("Canvas incompleto: " + path);
            foreach (var rect in ui.GetComponentsInChildren<RectTransform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(rect.gameObject) > 0)
                    throw new Exception("Script perdido: " + rect.name);
            foreach (var text in ui.GetComponentsInChildren<Text>(true))
            {
                if (text.font == null) throw new Exception("Fuente perdida: " + text.name);
                if (text.preferredHeight > text.rectTransform.rect.height + 2)
                    throw new Exception("Texto recortado: " + text.name + " requiere " + text.preferredHeight + ", tiene " + text.rectTransform.rect.height);
            }
            foreach (var image in ui.GetComponentsInChildren<RawImage>(true))
                if (image.texture == null) throw new Exception("Textura perdida: " + image.name);
            if (ui.GetComponentsInChildren<Button>(true).Length < 8) throw new Exception("Faltan botones: " + path);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    // Intended for a disposable validation copy, not the user's current editor session.
    public static void ValidateBatch()
    {
        try { ValidateScenes(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    public static void ValidateRuntimeBatch()
    {
        ValidateScenes();
        SessionState.SetBool(BatchKey, true);
        EditorSceneManager.OpenScene(FeralUICommon.MenuScene);
        EditorApplication.EnterPlaymode();
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Feral runtime: " + message);
    }

    static void Click(Transform root, string name)
    {
        FeralUICommon.Find<Button>(root, name).onClick.Invoke();
    }

    static void CheckAudio(Transform root)
    {
        const string volumeKey = "Feral.UI.MasterVolume", muteKey = "Feral.UI.Muted";
        bool hadVolume = PlayerPrefs.HasKey(volumeKey), hadMute = PlayerPrefs.HasKey(muteKey);
        float oldVolume = PlayerPrefs.GetFloat(volumeKey, .8f);
        int oldMute = PlayerPrefs.GetInt(muteKey, 0);
        var slider = FeralUICommon.Find<Slider>(root, "MasterVolume");
        try
        {
            PlayerPrefs.SetInt(muteKey, 0);
            slider.value = .25f;
            Check(Mathf.Approximately(AudioListener.volume, .25f), "volumen no se aplica");
            Check(FeralUICommon.Find<Text>(root, "VolumeValue").text == "25%", "porcentaje incorrecto");
            Click(root, "Mute");
            Check(AudioListener.volume == 0f, "silenciar no funciona");
            Click(root, "Mute");
            Check(Mathf.Approximately(AudioListener.volume, .25f), "activar no restaura volumen");
        }
        finally
        {
            slider.SetValueWithoutNotify(oldVolume);
            if (hadVolume) PlayerPrefs.SetFloat(volumeKey, oldVolume); else PlayerPrefs.DeleteKey(volumeKey);
            if (hadMute) PlayerPrefs.SetInt(muteKey, oldMute); else PlayerPrefs.DeleteKey(muteKey);
            PlayerPrefs.Save();
            FeralUICommon.ApplyAudio();
        }
    }

    static void RuntimeTick()
    {
        if (!SessionState.GetBool(BatchKey, false) || !EditorApplication.isPlaying) return;
        if (nextCheck == 0) { nextCheck = EditorApplication.timeSinceStartup + 3; return; }
        if (EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 2;
        try
        {
            if (runtimeStage == 0)
            {
                var menu = UnityEngine.Object.FindAnyObjectByType<FeralMainMenu>();
                Check(menu != null, "no arrancó el menú");
                Click(menu.transform, "Play");
                Check(FeralUICommon.Find<RectTransform>(menu.transform, "Sessions").gameObject.activeSelf, "Jugar no abre partidas");
                Click(menu.transform, "BackSessions");
                Click(menu.transform, "AudioSettings");
                Check(FeralUICommon.Find<RectTransform>(menu.transform, "Settings").gameObject.activeSelf, "audio no abre");
                CheckAudio(menu.transform);
                Click(menu.transform, "BackSettings");
                Click(menu.transform, "Play");
                Click(menu.transform, "NewGame");
            }
            else if (runtimeStage == 1)
            {
                var ui = UnityEngine.Object.FindAnyObjectByType<FeralInventoryUI>();
                Check(ui != null, "no cargó el inventario");
                var panel = FeralUICommon.Find<RectTransform>(ui.transform, "Inventory").gameObject;
                Check(!panel.activeSelf, "inicio desde menú debe permitir jugar");
                Click(ui.transform, "OpenInventory");
                Check(panel.activeSelf && Time.timeScale == 0, "mochila no pausa");
                Check(!UnityEngine.Object.FindAnyObjectByType<HamsterController>().enabled, "jugador sigue recibiendo controles");
                var inv = InventoryManager.Instance;
                inv.AddItem(ItemType.Madera, 2);
                inv.AddItem(ItemType.Metal, 1);
                Check(FeralUICommon.Find<Text>(ui.transform, "CountMadera").text == "02", "cantidad no se actualiza");
                Check(FeralUICommon.Find<Text>(ui.transform, "RecipeState0").text == "Materiales completos", "receta 1 no detecta materiales");
                inv.RemoveItem(ItemType.Madera, 2);
                Check(FeralUICommon.Find<Text>(ui.transform, "CountMadera").text == "00", "consumo no se actualiza");
                Click(ui.transform, "FilterAvailable");
                Check(!FeralUICommon.Find<Button>(ui.transform, "CardMadera").gameObject.activeSelf, "filtro no oculta ceros");
                Check(FeralUICommon.Find<Button>(ui.transform, "CardMetal").gameObject.activeSelf, "filtro oculta disponibles");
                Click(ui.transform, "CloseInventory");
                Check(Time.timeScale == 1 && UnityEngine.Object.FindAnyObjectByType<HamsterController>().enabled, "cerrar no restaura controles");
                Click(ui.transform, "OpenInventory");
                Click(ui.transform, "ReturnMenu");
                Check(!FeralUICommon.Find<Button>(ui.transform, "CloseInventory").interactable, "modal no bloquea controles del fondo");
                Click(ui.transform, "CancelReturn");
                Check(FeralUICommon.Find<Button>(ui.transform, "CloseInventory").interactable, "cancelar no restaura los botones");
                Check(inv.GetCount(ItemType.Metal) == 1, "cancelar pierde inventario");
                Click(ui.transform, "ReturnMenu");
                Click(ui.transform, "ConfirmReturn");
            }
            else if (runtimeStage == 2)
            {
                Check(UnityEngine.Object.FindAnyObjectByType<FeralMainMenu>() != null, "no vuelve al menú");
                Check(InventoryManager.Instance.GetCount(ItemType.Metal) == 0, "nueva sesión conserva materiales");
                Check(Time.timeScale == 1, "menú queda pausado");
                FeralUICommon.Load(FeralUICommon.GameScene);
            }
            else
            {
                var ui = UnityEngine.Object.FindAnyObjectByType<FeralInventoryUI>();
                Check(ui != null && Time.timeScale == 0, "la revisión directa no abre la mochila");
                Check(FeralUICommon.Find<RectTransform>(ui.transform, "Inventory").gameObject.activeSelf, "mochila de revisión oculta");
                Click(ui.transform, "CloseInventory");
                Check(Time.timeScale == 1, "revisión directa no permite reanudar");
                SessionState.SetBool(BatchKey, false);
                Debug.Log("FERAL_UI_RUNTIME_OK: navegación, audio, carga de escena, inventario, recetas, filtros, pausa, retorno y revisión directa.");
                EditorApplication.Exit(0);
            }
            runtimeStage++;
        }
        catch (Exception e)
        {
            SessionState.SetBool(BatchKey, false);
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }
}
