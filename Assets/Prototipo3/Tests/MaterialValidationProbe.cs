#if UNITY_EDITOR || FERAL_MATERIAL_VALIDATION
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Prototipo3
{
    // Compiled into a player only when the validation build explicitly requests it.
    public class MaterialValidationProbe : MonoBehaviour
    {
        readonly StringBuilder report = new StringBuilder();
        readonly HashSet<string> failures = new HashSet<string>();
        float started;
        string output;
        bool finished;
        bool hadPendingSeed;
        int pendingSeed;
        readonly HashSet<string> renderedVariants = new HashSet<string>();
        Camera previewCamera;
        Renderer previewRenderer;
        RenderTexture previewTarget;
        Texture2D previewPixels;

#if FERAL_MATERIAL_VALIDATION && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() => new GameObject("MaterialValidationProbe").AddComponent<MaterialValidationProbe>();
#endif

        void Awake()
        {
            started = Time.realtimeSinceStartup;
            output = Path.GetFullPath("Logs/material-validation.txt");
            foreach (string arg in Environment.GetCommandLineArgs())
                if (arg.StartsWith("-materialReport=")) output = arg.Substring("-materialReport=".Length);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            hadPendingSeed = PlayerPrefs.HasKey("Feral_PendingSeed");
            pendingSeed = PlayerPrefs.GetInt("Feral_PendingSeed");
            Application.logMessageReceived += OnLog;
        }

        void OnLog(string message, string stack, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception) &&
                (message.IndexOf("shader", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 message.IndexOf("material", StringComparison.OrdinalIgnoreCase) >= 0))
                failures.Add("RENDER ERROR " + message);
        }

        IEnumerator Start()
        {
            Time.timeScale = 1;
            yield return new WaitForSecondsRealtime(5);
            Check(GraphicsSettings.currentRenderPipeline != null, "URP activa: " + GraphicsSettings.currentRenderPipeline?.name);
            var city = CityGeneratorPrototipo3.Instance;
            Check(city != null && city.Grid != null, "Ciudad real generada");
            if (city == null) { Finish(); yield break; }
            var clock = TimeManager.Instance;
            Check(clock != null, "Reloj disponible");
            if (clock != null) { clock.pauseAutoAdvance = true; clock.SetExactHour(12); }
            yield return new WaitForSecondsRealtime(2);
            Audit("dia");
            VerifyMechanismColors();
            if (clock != null) clock.SetExactHour(22);
            yield return new WaitForSecondsRealtime(2);
            Audit("noche");

            var player = city.hamsterTransform;
            if (player == null) player = FindAnyObjectByType<HamsterController>()?.transform;
            Check(player != null, "Jugador disponible");
            if (player != null)
            {
                var traps = TrapSystemPrototipo3.EnsureInstance();
                traps.DeploySpikesTrap(player);
                traps.DeployNetTrap(player);
                traps.DeployWoodBarricade(player);
                new GameObject("ValidationGas").AddComponent<AnestheticGasCloud>().transform.position = player.position;
                new GameObject("ValidationDischarge").AddComponent<StaticDischargeEffect>().transform.position = player.position;
                foreach (var enemy in FindObjectsByType<EnemyAIPrototipo3>(FindObjectsSortMode.None))
                {
                    enemy.ApplySpikeSlowAndSparks();
                    enemy.ApplyNetImmobilization();
                    enemy.ApplyStaticParalysis(5);
                }
                yield return null;
                yield return null;
                Audit("trampas");
            }

            city.RegenerateCityRuntime(24680);
            yield return new WaitForSecondsRealtime(4);
            Audit("regenerada");
            if (clock != null) clock.SetExactHour(12);
            yield return new WaitForSecondsRealtime(2);
            Audit("regenerada-dia");
            var lootSpawner = new GameObject("ValidationLootSpawner").AddComponent<CityLootSpawner>();
            var loot = lootSpawner.CreateLootContainerInstance(lootSpawner.transform, "ValidationFallbackLoot", city.PlayerSpawnPoint,
                new List<ItemType>(), null);
            Check(loot.GetComponent<Renderer>().sharedMaterial.shader.name == "Universal Render Pipeline/Lit", "Botín sin prefab usa URP");
#if !UNITY_EDITOR
            foreach (var lulu in FindObjectsByType<LuluNPCPrototipo3>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Destroy(lulu.gameObject);
            yield return null;
            var binder = new GameObject("ValidationFallbackBinder").AddComponent<StoryWorldBinder>();
            binder.EnsureLuluExists(city);
            Check(binder.SpawnedLuluInstance != null && binder.SpawnedLuluInstance.name == "Lulu_Prototipo3_Fallback" &&
                binder.SpawnedLuluInstance.GetComponent<Renderer>().sharedMaterial.shader.name == "Universal Render Pipeline/Lit",
                "Lulú sin prefab usa URP en el player");
#endif
            yield return null;
            Audit("modelos-respaldo");
            Finish();
        }

        void VerifyMechanismColors()
        {
            var mechanisms = FindObjectsByType<StoryMechanismPrototipo3>(FindObjectsSortMode.None);
            var models = new List<Renderer>();
            foreach (var mechanism in mechanisms)
            {
                var model = mechanism.transform.Find("VisualMesh_ReplaceMe/MechanismModel");
                if (model != null) models.Add(model.GetComponent<Renderer>());
            }
            Check(models.Count >= 6, "Se comprueban los seis mecanismos generados");
            if (models.Count < 2) return;
            var first = models[0].GetComponentInParent<StoryMechanismPrototipo3>();
            Color otherColor = models[1].sharedMaterial.color;
            first.isCompleted = true;
            first.UpdateVisualState();
            Check(models[0].sharedMaterial.color == new Color(0.2f, 0.95f, 0.45f, 1f), "Mecanismo completado conserva su color verde");
            Check(models[1].sharedMaterial.color == otherColor && models[0].sharedMaterial != models[1].sharedMaterial,
                "Cambiar un mecanismo no modifica los demás");
            Check(Resources.Load<Material>("Prototipo3Materials/GeneratedOpaque").color == Color.white,
                "La plantilla conserva su color original");
            first.isCompleted = false;
            first.UpdateVisualState();
        }

        void Check(bool value, string message)
        {
            report.AppendLine((value ? "PASS " : "FAIL ") + message);
            if (!value) failures.Add(message);
        }

        void Audit(string stage)
        {
            var shaders = new Dictionary<string, int>();
            int count = 0;
            foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!renderer.gameObject.scene.IsValid() || renderer == previewRenderer) continue;
                foreach (var material in renderer.sharedMaterials)
                {
                    count++;
                    Shader shader = material != null ? material.shader : null;
                    string name = shader != null ? shader.name : "NULL";
                    if (!shaders.ContainsKey(name)) shaders[name] = 0;
                    shaders[name]++;
                    RenderVariant(material, stage, renderer);
                    if (shader == null || !shader.isSupported || name == "Hidden/InternalErrorShader" ||
                        name == "Standard" || name.StartsWith("Legacy Shaders/"))
                        failures.Add(stage + " | " + Hierarchy(renderer.transform) + " | " + (material != null ? material.name : "NULL") + " | " + name);
                }
            }
            report.AppendLine("AUDIT " + stage + " materials=" + count);
            foreach (var entry in shaders) report.AppendLine("  " + entry.Key + " = " + entry.Value);
            File.WriteAllText(output, report.ToString() + string.Join("\n", failures));
        }

        void RenderVariant(Material material, string stage, Renderer source)
        {
            if (material == null || material.shader == null || source is ParticleSystemRenderer ||
                material.shader.name.Contains("TextMeshPro") || material.shader.name.Contains("CityDirectionalFog")) return;
            string key = material.shader.name + " | " + string.Join(" ", material.shaderKeywords);
            if (source.name == "MechanismModel") key += " | " + source.transform.parent.parent.name;
            if (!renderedVariants.Add(key)) return;
            if (previewCamera == null)
            {
                previewCamera = new GameObject("ValidationPreviewCamera").AddComponent<Camera>();
                previewCamera.enabled = false;
                previewCamera.transform.position = new Vector3(0, -1000, -3);
                previewCamera.orthographic = true;
                previewCamera.orthographicSize = 0.6f;
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = Color.gray;
                previewCamera.cullingMask = 1 << 31;
                previewCamera.farClipPlane = 10;
                var data = previewCamera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = false;
                data.renderShadows = false;
                data.volumeLayerMask = 0;
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "ValidationMaterialPreview";
                quad.layer = 31;
                quad.transform.position = new Vector3(0, -1000, 0);
                previewRenderer = quad.GetComponent<Renderer>();
                previewTarget = new RenderTexture(96, 96, 24, RenderTextureFormat.ARGB32);
                previewTarget.Create();
                previewPixels = new Texture2D(96, 96, TextureFormat.RGB24, false);
            }
            previewRenderer.sharedMaterial = material;
#if UNITY_EDITOR
            // Las capturas de QA deben esperar a la compilación bajo demanda del editor.
            bool previousAsync = UnityEditor.ShaderUtil.allowAsyncCompilation;
            UnityEditor.ShaderUtil.allowAsyncCompilation = false;
            int forwardPass = material.FindPass("ForwardLit");
            if (forwardPass >= 0) UnityEditor.ShaderUtil.CompilePass(material, forwardPass, true);
#endif
            try
            {
            RenderPipeline.SubmitRenderRequest(previewCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = previewTarget });
            }
            finally
            {
#if UNITY_EDITOR
                UnityEditor.ShaderUtil.allowAsyncCompilation = previousAsync;
#endif
            }
            var previous = RenderTexture.active;
            RenderTexture.active = previewTarget;
            previewPixels.ReadPixels(new Rect(0, 0, 96, 96), 0, 0);
            previewPixels.Apply();
            RenderTexture.active = previous;
            int magenta = 0;
            foreach (var pixel in previewPixels.GetPixels32())
                if (pixel.r > 200 && pixel.b > 200 && pixel.g < 70) magenta++;
            string name = Path.GetFileNameWithoutExtension(output) + "-sample-" + renderedVariants.Count + ".png";
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(output), name), previewPixels.EncodeToPNG());
            report.AppendLine("RENDER " + key + " magentaPixels=" + magenta + " image=" + name);
            if (magenta > 96) failures.Add("PINK " + stage + " | " + Hierarchy(source.transform) + " | " + material.name + " | " + key);
        }

        static string Hierarchy(Transform t) => t.parent == null ? t.name : Hierarchy(t.parent) + "/" + t.name;

        void Update()
        {
            if (!finished && Time.realtimeSinceStartup - started > 180)
            {
                failures.Add("Timeout");
                Finish();
            }
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            Application.logMessageReceived -= OnLog;
            if (hadPendingSeed) PlayerPrefs.SetInt("Feral_PendingSeed", pendingSeed);
            else PlayerPrefs.DeleteKey("Feral_PendingSeed");
            PlayerPrefs.Save();
            foreach (string error in failures) report.AppendLine("FAIL " + error);
            report.AppendLine("RESULT: " + (failures.Count == 0 ? "PASS" : "FAIL") + " issues=" + failures.Count);
            File.WriteAllText(output, report.ToString());
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
#else
            Application.Quit(failures.Count == 0 ? 0 : 1);
#endif
        }
    }
}
#endif
