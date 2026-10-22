#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.AI;

[InitializeOnLoad]
public static class TutorialPlayValidation
{
    const string Key = "FeralTutorialValidation";
    static double start;
    static int step;
    static StringBuilder report = new StringBuilder();
    static TutorialPlayValidation()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Tutorial.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }
    static void Check(bool value, string label)
    {
        report.AppendLine((value ? "PASS " : "FAIL ") + label);
        if (!value) throw new Exception(label);
    }
    static void Invoke(TutorialPipelineManager manager, string method)
    {
        typeof(TutorialPipelineManager).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        if (start == 0) start = EditorApplication.timeSinceStartup;
        double elapsed = EditorApplication.timeSinceStartup - start;
        try
        {
            if (step == 0 && elapsed > 2)
            {
                var intro = UnityEngine.Object.FindAnyObjectByType<CinematicaManager>();
                Check(intro != null && intro.reproductorVideo.clip.name == "TestInitialVideo", "Initial video reference");
                intro.IniciarGameplay(intro.reproductorVideo);
                step++;
            }
            else if (step == 1 && elapsed > 5)
            {
                var m = TutorialPipelineManager.Instance;
                Check(m != null && m.hamster != null && m.hamster.enabled, "Tutorial and player start after intro");
                Check(m.cinematicaAnochecer != null, "Night MP4 serialized");
                Check(m.mesaCrafteoRefugio != null && m.mesaCrafteoRefugio.craftingPanelUI != null, "Crafting UI assigned");
                Check(m.luluNPC != null, "Lulu assigned");
                CaptureOverview();
                var gate = GameObject.Find("PuertaControl_Ciudad");
                Check(gate != null, "Gate exists before looting");
                Invoke(m, "IniciarPasoSaquearContenedorControl");
                GameObject.Find("GarbageCollector").GetComponent<LootContainer>().InteractuarConLoot();
                Check(!gate.activeSelf, "Loot opens gate");
                Check(InventoryManager.Instance.HasEnoughItem(ItemType.Madera, 1) && InventoryManager.Instance.HasEnoughItem(ItemType.Metal, 1), "Guaranteed crafting resources");
                Check(m.currentState == TutorialPipelineManager.TutorialState.BuscarLulu, "Loot advances mission");
                m.NotificarDialogoInicialLulu();
                for (int i = 1; i <= 3; i++) GameObject.Find("ContenedorCiudad_" + i).GetComponent<LootContainer>().InteractuarConLoot();
                Check(InventoryManager.Instance.GetItemCount(ItemType.Desperdicios) >= 3, "City loot available");
                m.NotificarEntregaLuluCompletada();
                step++;
            }
            else if (step == 2 && elapsed > 19)
            {
                var m = TutorialPipelineManager.Instance;
                Check(m.currentState == TutorialPipelineManager.TutorialState.EscapeDash, "Night video releases controls into chase");
                var enemy = UnityEngine.Object.FindAnyObjectByType<EnemyAI>();
                Check(enemy != null && enemy.GetComponent<NavMeshAgent>().isOnNavMesh, "Feral on baked NavMesh");
                m.hamster.RespawnOrHeal();
                m.hamster.ReceiveDamage();
                m.hamster.ReceiveDamage();
                Check(m.hamster.HitsReceived == 1 && m.hamster.IsAdrenalineActive, "Adrenaline protects escape window");
                enemy.gameObject.SetActive(false);
                TimeManager.Instance.SetExactHour(8);
                Invoke(m, "IniciarFase3Crafting");
                m.mesaCrafteoRefugio.ToggleCraftingMenu();
                Check(m.currentState == TutorialPipelineManager.TutorialState.CrearPrimeraTrampa, "Workbench starts recipe step");
                m.mesaCrafteoRefugio.CraftTrap1();
                Check(InventoryManager.Instance.HasTrampa(TipoTrampa.Puas), "Recipe creates trap");
                Check(m.currentState == TutorialPipelineManager.TutorialState.ColocacionTacticaTrampa, "Crafting starts placement step");
                m.puertaTrampaRefugio.InstalarTrampa(TipoTrampa.Puas);
                step++;
            }
            else if (step == 3 && elapsed > 24)
            {
                Check(TutorialPipelineManager.Instance.currentState == TutorialPipelineManager.TutorialState.FinTutorial, "Placement completes tutorial");
                Finish(0);
            }
            if (elapsed > 40) throw new Exception("Validation timeout");
        }
        catch (Exception error)
        {
            report.AppendLine(error.ToString());
            Finish(1);
        }
    }
    static void Finish(int code)
    {
        File.WriteAllText("Logs/tutorial-play-validation.txt", report.ToString());
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        EditorApplication.Exit(code);
    }
    static void CaptureOverview()
    {
        var go = new GameObject("ReviewCamera");
        var camera = go.AddComponent<Camera>();
        camera.transform.position = new Vector3(30, 70, 135);
        camera.transform.rotation = Quaternion.Euler(90, 0, 0);
        camera.orthographic = true;
        camera.orthographicSize = 23;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.06f, .08f, .10f);
        var target = new RenderTexture(1600, 1000, 24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
        image.Apply();
        File.WriteAllBytes("Logs/tutorial-overview.png", image.EncodeToPNG());
        RenderTexture.active = null;
        camera.targetTexture = null;
        target.Release();
        UnityEngine.Object.Destroy(go);
        UnityEngine.Object.Destroy(target);
        UnityEngine.Object.Destroy(image);
    }
}
#endif
