#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace Prototipo3.Editor
{
    // Prueba de integración con frames reales, NavMesh y componentes de producción.
    // Se ejecuta en una escena vacía sin guardar ni modificar Prototipo3.unity.
    [InitializeOnLoad]
    public static class TrapEffectValidation
    {
        private const string SessionKey = "Feral.Prototipo3.TrapValidation";
        private static readonly StringBuilder Report = new StringBuilder();
        private static IEnumerator steps;
        private static float nextTime;
        private static int lastFrame = -1;
        private static double started;
        private static int checks;
        private static NavMeshData navData;
        private static NavMeshDataInstance navInstance;
        private static string runtimeError;

        static TrapEffectValidation()
        {
            if (SessionState.GetBool(SessionKey, false)) EditorApplication.update += Tick;
        }

        public static void Run()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Ejecutar con Unity -batchmode -executeMethod Prototipo3.Editor.TrapEffectValidation.Run");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(SessionKey, true);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
            try
            {
                if (steps == null)
                {
                    started = EditorApplication.timeSinceStartup;
                    Application.logMessageReceived += CaptureError;
                    steps = Validate();
                }
                if (runtimeError != null) throw new Exception(runtimeError);
                if (EditorApplication.timeSinceStartup - started > 100) throw new TimeoutException("Trap validation timeout");
                if (lastFrame == Time.frameCount || Time.time < nextTime) return;
                lastFrame = Time.frameCount;
                if (!steps.MoveNext()) { Finish(0); return; }
                nextTime = Time.time + (steps.Current is float delay ? delay : 0f);
            }
            catch (Exception error)
            {
                Report.AppendLine("FAIL " + error);
                Finish(1);
            }
        }

        private static void CaptureError(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                runtimeError = message + "\n" + stack;
        }

        private static void Check(bool value, string label)
        {
            if (!value) throw new Exception(label);
            checks++;
            Report.AppendLine("PASS " + label);
        }

        private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.03f;
        private static bool VisualActive(EnemyAIPrototipo3 enemy, string name)
        {
            Transform visual = enemy.transform.Find(name);
            return visual != null && visual.gameObject.activeSelf;
        }

        private static IEnumerator Validate()
        {
            Time.timeScale = 1f;
            var clock = new GameObject("TrapTestClock").AddComponent<TimeManager>();
            clock.pauseAutoAdvance = true;
            clock.SetExactHour(20f);
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.TRS(new Vector3(0, -0.5f, 0), Quaternion.identity, Vector3.one),
                size = new Vector3(100, 1, 100), area = 0
            };
            navData = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),
                new System.Collections.Generic.List<NavMeshBuildSource> { source },
                new Bounds(Vector3.zero, new Vector3(110, 10, 110)), Vector3.zero, Quaternion.identity);
            Check(navData != null, "NavMesh de prueba generado");
            navInstance = NavMesh.AddNavMeshData(navData);
            var target = new GameObject("TrapTestTarget") { tag = "Player" };
            target.transform.position = new Vector3(40, 0, 40);
            var go = new GameObject("TrapTestEnemy");
            var collider = go.AddComponent<CapsuleCollider>();
            collider.center = Vector3.up;
            var enemy = go.AddComponent<EnemyAIPrototipo3>();
            var agent = go.GetComponent<NavMeshAgent>();
            enemy.player = target.transform;
            enemy.visionRadiusNormal = 18f;
            enemy.visionRadiusCrouched = 7f;
            enemy.backDetectionRadius = 3.7f;
            enemy.SetPatrolWaypoints(new[] { new Vector3(20, 0, 0), new Vector3(-20, 0, 0) });
            var traps = TrapSystemPrototipo3.EnsureInstance();
            yield return 0.3f;
            Check(agent.isOnNavMesh, "Enemigo sobre NavMesh");

            enemy.ApplySpikeSlowAndSparks();
            enemy.ApplySpikeSlowAndSparks();
            Check(Near(agent.speed, enemy.wanderSpeed * 0.5f), "Púas repetidas no acumulan multiplicadores");
            yield return 0.3f;
            Check(Near(agent.speed, enemy.wanderSpeed * 0.5f), "Patrulla mantiene ralentización después de Update");
            target.transform.position = go.transform.position + Vector3.forward * 12f;
            enemy.ForceChasePlayer();
            yield return 0.15f;
            Check(enemy.CurrentState == EnemyAI.EnemyState.Chase && Near(agent.speed, enemy.chaseSpeed * 0.5f),
                "Cambio a persecución conserva ralentización");
            enemy.enabled = false;
            Check(!enemy.IsSpikeSlowed && !VisualActive(enemy, "SpikeElectricSparksRing"), "Desactivar limpia efecto y visual de púas");
            enemy.enabled = true;
            target.transform.position = new Vector3(40, 0, 40);
            yield return 0.15f;

            float appliedAt = Time.time;
            enemy.ApplySpikeSlowAndSparks();
            enemy.ApplyNetImmobilization();
            enemy.ApplyStaticParalysis(1f);
            Vector3 frozenAt = go.transform.position;
            Vector3 decoyPosition = frozenAt + Vector3.right * 10;
            while (Time.time - appliedAt < 4.6f)
            {
                enemy.AttractToAcousticDecoy(decoyPosition, Mathf.Max(0.1f, 8f - (Time.time - appliedAt)));
                yield return 0.2f;
                Check(agent.isStopped && Vector3.Distance(go.transform.position, frozenAt) < 0.08f,
                    "Red resiste pulso de señuelo sin desplazamiento");
                Check(enemy.CurrentState != EnemyAI.EnemyState.Attack && Near(enemy.backDetectionRadius, 0f),
                    "Red bloquea ataque y proximidad");
                if (Time.time - appliedAt > 1.2f)
                    Check(!enemy.IsParalyzedByStatic && !VisualActive(enemy, "StaticParalysisRing"), "Parálisis expira mientras la red sigue activa");
            }
            while (Time.time - appliedAt < 5.3f) yield return null;
            Check(!enemy.IsNetImmobilized && !agent.isStopped && !VisualActive(enemy, "NetTrapVisualRing"), "Red termina a los 5 s y libera navegación");
            Check(enemy.IsSpikeSlowed && Near(agent.speed, Mathf.Max(enemy.wanderSpeed * 1.2f, 4.5f) * 0.5f), "Señuelo respeta el 50% de púas");
            while (Time.time - appliedAt < 6.3f) yield return null;
            Check(!enemy.IsSpikeSlowed && !VisualActive(enemy, "SpikeElectricSparksRing"), "Púas terminan a los 6 s aunque hubo red");
            Check(enemy.IsDistractedByDecoy && Near(agent.speed, Mathf.Max(enemy.wanderSpeed * 1.2f, 4.5f)), "Señuelo recupera velocidad al terminar púas");
            while (Time.time - appliedAt < 8.4f) yield return null;
            Check(!enemy.IsDistractedByDecoy && Near(agent.speed, enemy.wanderSpeed), "Fin del señuelo restaura velocidad de patrulla");
            Check(Near(enemy.visionRadiusNormal, 18f) && Near(enemy.visionRadiusCrouched, 7f) && Near(enemy.backDetectionRadius, 3.7f),
                "Detección recupera los valores reales del enemigo");

            enemy.ApplyNetImmobilization(1f);
            yield return 0.5f;
            enemy.ApplyNetImmobilization(1f);
            yield return 0.65f;
            Check(enemy.IsNetImmobilized && agent.isStopped, "Reaplicar red renueva su duración");
            yield return 0.5f;
            Check(!enemy.IsNetImmobilized && !VisualActive(enemy, "NetTrapVisualRing"), "Red renovada termina y elimina su visual");

            var door = new GameObject("TrapTestDoor").AddComponent<PuertaTrampa>();
            var trigger = door.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = Vector3.one * 3;
            door.transform.position = go.transform.position + Vector3.right * 30;
            int notifications = 0;
            enemy.OnTrapEffectApplied += _ => notifications++;
            yield return null;
            door.InstalarTrampa(TipoTrampa.Red);
            door.transform.position = go.transform.position;
            yield return 0.25f;
            Check(door.trampaActual == TipoTrampa.Ninguna && enemy.IsNetImmobilized && notifications == 1,
                "Colisión real con puerta consume red y usa el efecto Prototipo3 una sola vez");
            enemy.ApplyWoodBarricadeRepath(go.transform.position);
            Check(agent.isStopped, "Recalcular ruta por barricada no libera una red");
            Object.Destroy(door.gameObject);
            enemy.enabled = false;
            Check(!enemy.IsNetImmobilized && !agent.isStopped && Near(enemy.visionRadiusNormal, 18f), "Desactivar enemigo atrapado restaura navegación y detección");
            enemy.enabled = true;
            ((EnemyAI)enemy).RecibirTrampa(TipoTrampa.Puas, go.transform.position);
            Check(enemy.IsSpikeSlowed && Near(agent.speed, enemy.wanderSpeed * 0.5f), "Púas recibidas mediante EnemyAI usan parámetros Prototipo3");
            enemy.enabled = false;
            enemy.enabled = true;

            Vector3 barrierGround = new Vector3(0, 0, -20);
            ((EnemyAI)enemy).RecibirTrampa(TipoTrampa.Madera, barrierGround);
            GameObject barricade = GameObject.Find("Barricada_Madera_Activa_10s");
            Check(barricade != null && agent.enabled && go.GetComponent<Rigidbody>().isKinematic,
                "Madera de puerta crea barricada sin desactivar NavMesh ni lanzar al enemigo");
            NavMeshObstacle obstacle = barricade.GetComponent<NavMeshObstacle>();
            Check(Vector3.Distance(Vector3.Scale(obstacle.size, barricade.transform.lossyScale),
                Vector3.Scale(barricade.GetComponent<BoxCollider>().size, barricade.transform.lossyScale)) < 0.01f,
                "Obstáculo y colisionador tienen las mismas dimensiones");
            yield return 0.5f;
            Check(!NavMesh.SamplePosition(barrierGround, out _, 0.1f, NavMesh.AllAreas), "Barricada recorta el NavMesh");
            var path = new NavMeshPath();
            Vector3 across = barricade.transform.forward * 4f;
            Check(NavMesh.CalculatePath(barrierGround - across, barrierGround + across, NavMesh.AllAreas, path) &&
                path.status == NavMeshPathStatus.PathComplete && path.corners.Length > 2, "Ruta completa rodea barricada");
            yield return 9.8f;
            Check(barricade == null, "Barricada desaparece a los 10 s");
            Check(NavMesh.SamplePosition(barrierGround, out _, 0.2f, NavMesh.AllAreas), "Al desaparecer se recupera el paso navegable");

            // Misma ruta de entrada en un enemigo del prototipo anterior.
            var legacy = new GameObject("TrapTestLegacy").AddComponent<EnemyAI>();
            legacy.transform.position = new Vector3(-20, 0, -20);
            yield return 0.15f;
            legacy.RecibirTrampa(TipoTrampa.Red, Vector3.zero);
            Check((bool)typeof(EnemyAI).GetField("isImmobilized", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(legacy),
                "EnemyAI original conserva su implementación de red");
            Object.Destroy(legacy.gameObject);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototipo3/Prefabs/Tiger_Prototipo3.prefab");
            Check(prefab != null, "Prefab real del tigre disponible");
            var tiger = Object.Instantiate(prefab, new Vector3(20, 0, 20), Quaternion.identity).GetComponent<EnemyAIPrototipo3>();
            Check(tiger != null, "Tigre utiliza EnemyAIPrototipo3");
            yield return 0.3f;
            tiger.ApplyNetImmobilization(0.5f);
            Check(tiger.IsNetImmobilized && tiger.GetComponent<NavMeshAgent>().isStopped, "Red funciona en el prefab real del tigre");
            yield return 0.7f;
            Check(!tiger.IsNetImmobilized && !VisualActive(tiger, "NetTrapVisualRing"), "Tigre sale de la red y limpia su visual");

            target.transform.position = tiger.transform.position + Vector3.right * 2f;
            float tigerVision = tiger.visionRadiusNormal;
            var inventory = new GameObject("TrapTestInventory").AddComponent<InventoryManager>();
            inventory.AddTrampa(TipoTrampa.Puas);
            Check(traps.TryDeployTrap(TipoTrampa.Puas, target.transform) && inventory.GetTrapCount(TipoTrampa.Puas) == 0,
                "Despliegue de tecla 1 consume una sola trampa");
            yield return 0.2f;
            Check(tiger.IsSpikeSlowed && tiger.IsDistractedByDecoy &&
                Near(tiger.GetComponent<NavMeshAgent>().speed, Mathf.Max(tiger.wanderSpeed * 1.2f, 4.5f) * 0.5f),
                "Despliegue real combina señuelo y ralentización en el tigre");
            inventory.AddTrampa(TipoTrampa.Red);
            target.transform.position = tiger.transform.position + Vector3.right * 2f;
            Check(traps.TryDeployTrap(TipoTrampa.Red, target.transform) && inventory.GetTrapCount(TipoTrampa.Red) == 0,
                "Despliegue de tecla 2 consume una sola trampa");
            yield return 0.2f;
            Check(tiger.IsNetImmobilized && tiger.GetComponent<NavMeshAgent>().isStopped, "Red desplegada prevalece sobre señuelo activo");
            Time.timeScale = 0f;
            double pausedAt = EditorApplication.timeSinceStartup;
            while (EditorApplication.timeSinceStartup - pausedAt < 0.5) yield return null;
            Check(tiger.IsNetImmobilized, "Pausa congela los temporizadores de trampas");
            Time.timeScale = 1f;
            yield return 8.3f;
            Check(!tiger.IsNetImmobilized && !tiger.IsDistractedByDecoy && TrapSystemPrototipo3.IsPollenCloudActive && Near(tiger.visionRadiusNormal, tigerVision * 0.5f),
                "Al terminar red y señuelo se conserva el polen todavía activo");
            yield return 7f;
            Check(!TrapSystemPrototipo3.IsPollenCloudActive && Near(tiger.visionRadiusNormal, tigerVision),
                "Al terminar polen también se recupera la detección original");
        }

        private static void Finish(int code)
        {
            Application.logMessageReceived -= CaptureError;
            SessionState.SetBool(SessionKey, false);
            EditorApplication.update -= Tick;
            Report.AppendLine($"RESULT: {(code == 0 ? "PASS" : "FAIL")} ({checks} checks)");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/prototipo3-trap-validation.txt", Report.ToString());
            if (navInstance.valid) navInstance.Remove();
            EditorApplication.Exit(code);
        }
    }
}
#endif
