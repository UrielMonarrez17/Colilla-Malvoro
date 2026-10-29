using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Prototipo2.UI;
using Object = UnityEngine.Object;

public static class MainMenuSceneBuilder
{
    const string Menu = "Assets/Prototipo2/Scenes/MainMenu_Prototipo2.unity";
    const string Art = "Assets/Prototipo2/UI/Art/";
    static Transform ui;
    static T Find<T>(string name) where T : Component => ui.GetComponentsInChildren<T>(true).First(x => x.name == name);
    static void Place(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
    }
    static RawImage ArtImage(string name, string file, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(ui, false);
        var image = go.GetComponent<RawImage>(); image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + file);
        image.raycastTarget = false; Place(image.rectTransform, x, y, w, h); return image;
    }

    [MenuItem("Feral/Menú principal/Reconstruir presentación %&m")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AssetDatabase.Refresh();
        foreach (string file in new[] { "MainMenuCity.png", "FeralLogo.png" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art + file);
            importer.alphaIsTransparency = true; importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        // Read the actual prototype without saving or altering it.
        var source = EditorSceneManager.OpenScene("Assets/Prototipo2/Prototipo2.unity");
        var player = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HamsterController>(true)).First();
        var model = Object.Instantiate(player.gameObject);
        model.name = "Hamster_Menu_Visual"; model.SetActive(false);
        foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
        foreach (var component in model.GetComponentsInChildren<Component>(true).Reverse())
        {
            if (component is Transform || component is Renderer || component is MeshFilter || component is Animator) continue;
            Object.DestroyImmediate(component);
        }
        foreach (var a in model.GetComponentsInChildren<Animator>(true)) a.enabled = false;
        foreach (var t in model.GetComponentsInChildren<Transform>(true)) { t.gameObject.tag = "Untagged"; t.gameObject.layer = 30; }
        model.transform.position = Vector3.zero; model.transform.rotation = Quaternion.identity;
        model.SetActive(true);
        PrefabUtility.SaveAsPrefabAsset(model, "Assets/Prototipo2/UI/HamsterMenuVisual.prefab");
        var scene = EditorSceneManager.OpenScene(Menu);
        ui = Object.FindAnyObjectByType<MainMenuControllerPrototipo2>().transform;
        foreach (string name in new[] { "Brand", "Edition", "Footer", "NavigationHint", "Overline", "Title", "Tagline", "ArtCaption", "BackdropFill" })
        {
            var item = ui.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            if (item != null) item.gameObject.SetActive(false);
        }
        foreach (string name in new[] { "Feral Logo", "Hamster Portrait" })
        {
            var old = ui.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject);
        }
        var oldStage = GameObject.Find("Hamster portrait studio"); if (oldStage) Object.DestroyImmediate(oldStage);
        var bg = Find<RawImage>("NightCity"); bg.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "MainMenuCity.png");
        bg.color = Color.white; bg.raycastTarget = false;
        bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one;
        bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;
        bg.transform.SetAsFirstSibling();
        var scaler = ui.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var logo = ArtImage("Feral Logo", "FeralLogo.png", 65, 60, 565, 262); logo.transform.SetSiblingIndex(1);
        var home = Find<RectTransform>("Home"); Place(home, 0, 0, 1600, 900);
        string[] names = { "Play", "SavedGames", "AudioSettings", "Help", "Quit" };
        string[] labels = { "Jugar", "Partidas", "Configuración", "Cómo jugar", "Salir" };
        string[] icons = { "▶", "▣", "⚙", "▤", "⇥" };
        for (int i = 0; i < names.Length; i++)
        {
            var b = Find<Button>(names[i]); Place((RectTransform)b.transform, 96, 365 + i * 77, 430, 70);
            var old = b.GetComponent<Image>(); if (old) Object.DestroyImmediate(old);
            var brush = b.GetComponent<MenuBrushGraphic>() ?? b.gameObject.AddComponent<MenuBrushGraphic>();
            brush.color = Color.white; b.targetGraphic = brush;
            var colors = b.colors;
            colors.normalColor = i == 0 ? new Color(.49f, .105f, .07f, .97f) : new Color(.12f, .105f, .09f, .94f);
            colors.highlightedColor = colors.selectedColor = new Color(.63f, .17f, .11f, 1);
            colors.pressedColor = new Color(.36f, .065f, .04f, 1); colors.fadeDuration = .12f; b.colors = colors;
            var label = Find<Text>(names[i] + "Label"); label.text = labels[i]; label.fontSize = 28;
            label.fontStyle = FontStyle.Normal; label.alignment = TextAnchor.MiddleLeft; label.color = new Color(.87f, .77f, .64f);
            Place(label.rectTransform, 106, 0, 315, 70); label.raycastTarget = false;
            var icon = b.transform.Find("Icon"); if (icon) Object.DestroyImmediate(icon.gameObject);
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Text)); iconGo.transform.SetParent(b.transform, false);
            var text = iconGo.GetComponent<Text>(); text.font = label.font; text.text = icons[i]; text.fontSize = 34;
            text.alignment = TextAnchor.MiddleCenter; text.color = label.color; text.raycastTarget = false;
            Place(text.rectTransform, 22, 0, 62, 70);
        }
        var portrait = ArtImage("Hamster Portrait", "MainMenuCity.png", 825, 340, 610, 550);
        portrait.texture = null; portrait.transform.SetSiblingIndex(2);
        var stage = new GameObject("Hamster portrait studio"); stage.transform.position = new Vector3(0, -100, 0);
        var pivot = new GameObject("Mouse look pivot").transform; pivot.SetParent(stage.transform, false);
        var hamster = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototipo2/UI/HamsterMenuVisual.prefab"));
        hamster.transform.SetParent(pivot, false); hamster.transform.localPosition = Vector3.zero;
        var renderers = hamster.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        hamster.transform.position += stage.transform.position - bounds.center;
        var cam = new GameObject("Hamster portrait camera", typeof(Camera)).GetComponent<Camera>();
        cam.transform.SetParent(stage.transform, false);
        cam.transform.localPosition = new Vector3(0, bounds.size.y * .12f, bounds.size.magnitude * 2);
        cam.transform.LookAt(stage.transform.position);
        cam.orthographic = true; cam.orthographicSize = Mathf.Max(bounds.size.y, bounds.size.x) * .66f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.clear; cam.cullingMask = 1 << 30;
        cam.nearClipPlane = .01f; cam.farClipPlane = 100; cam.depth = -10;
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(Art + "HamsterPortrait.renderTexture");
        if (rt == null) { rt = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32); AssetDatabase.CreateAsset(rt, Art + "HamsterPortrait.renderTexture"); }
        cam.targetTexture = rt; portrait.texture = rt;
        foreach (var camera in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>()))
            if (camera != cam) camera.cullingMask &= ~(1 << 30);
        var look = stage.AddComponent<MenuHamsterLook>(); look.model = pivot; look.portraitCamera = cam; look.portrait = portrait;
        AddLight(stage.transform, "Warm key", new Vector3(30, -30, 0), new Color(1, .76f, .52f), 1.8f);
        AddLight(stage.transform, "Moon rim", new Vector3(15, 145, 0), new Color(.42f, .63f, 1), 1.4f);
        MainMenuControllerPrototipo2.EnsureEventSystem();
        for (int i = 0; i < 5; i++) Find<RectTransform>(new[] { "Home", "Sessions", "Settings", "Controls", "Exit" }[i]).gameObject.SetActive(i == 0);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("FERAL_MENU_BUILD_OK: Canvas + prototype hamster + tutorial routing.");
        Capture();
    }

    static void AddLight(Transform stage, string name, Vector3 angles, Color color, float intensity)
    {
        var light = new GameObject(name, typeof(Light)).GetComponent<Light>(); light.transform.SetParent(stage, false);
        light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(angles); light.color = color;
        light.intensity = intensity; light.cullingMask = 1 << 30;
    }

    [MenuItem("Feral/Menú principal/Capturar vista previa")]
    public static void Capture()
    {
        ui = Object.FindAnyObjectByType<MainMenuControllerPrototipo2>().transform;
        var look = Object.FindAnyObjectByType<MenuHamsterLook>(); look.portraitCamera.Render();
        var canvas = ui.GetComponent<Canvas>();
        var previousMode = canvas.renderMode; var previousCamera = canvas.worldCamera; var previousDistance = canvas.planeDistance;
        var camera = new GameObject("Preview capture", typeof(Camera)).GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.cullingMask = 1 << 5;
        camera.transform.position = new Vector3(0, 0, -10);
        var target = new RenderTexture(1600, 900, 24); camera.targetTexture = target;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        Canvas.ForceUpdateCanvases(); camera.Render();
        var old = RenderTexture.active; RenderTexture.active = target;
        var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); texture.Apply();
        Directory.CreateDirectory("Temp/FeralMenu"); File.WriteAllBytes("Temp/FeralMenu/MainMenu.png", texture.EncodeToPNG());
        RenderTexture.active = old; canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousDistance;
        Object.DestroyImmediate(camera.gameObject); target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(texture);
    }
}
