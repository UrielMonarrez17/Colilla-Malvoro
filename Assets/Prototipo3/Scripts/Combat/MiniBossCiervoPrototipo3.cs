using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace Prototipo3.Combat
{
    [SelectionBase]
    public class MiniBossCiervoPrototipo3 : MonoBehaviour
    {
        public enum BossState
        {
            Idle,
            AlertBellow,
            Charging,
            Stunned,
            Defeated
        }

        [Header("Configuración de Combate")]
        public BossState currentState = BossState.Idle;
        public float detectionRadius = 16f;
        public float chargeSpeed = 14f;
        public float stunDuration = 3.5f;
        public int health = 3;
        public int maxHealth = 3;

        [Header("Referencias")]
        public Transform playerTransform;
        private Rigidbody rb;
        private Animator animator;

        private Vector3 chargeDirection;
        private float stateTimer = 0f;
        private bool hasPlayedIntroCinematic = false;
        private Vector3 spawnPosition;

        private GameObject promptRootObj;
        private TextMeshPro statusTextMesh;

        public event Action OnDefeated;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            spawnPosition = transform.position;

            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            var scripts = GetComponents<MonoBehaviour>();
            for (int i = 0; i < scripts.Length; i++)
            {
                if (scripts[i] == null || scripts[i] == this) continue;
                string tName = scripts[i].GetType().Name;
                if (tName.Contains("EnemyAI") || tName.Contains("Creature") || tName.Contains("Mover") || tName.Contains("AnimationShowcase"))
                {
                    scripts[i].enabled = false;
                }
            }

            CreateStatusUI();
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                var p = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (p != null) playerTransform = p.transform;
            }
        }

        private void Update()
        {
            if (currentState == BossState.Defeated) return;

            if (playerTransform == null)
            {
                var p = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (p != null) playerTransform = p.transform;
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);

            if (statusTextMesh != null && Camera.main != null)
            {
                promptRootObj.transform.rotation = Camera.main.transform.rotation;
            }

            switch (currentState)
            {
                case BossState.Idle:
                    if (dist <= detectionRadius)
                    {
                        TriggerAlertBellow();
                    }
                    break;

                case BossState.AlertBellow:
                    stateTimer -= Time.deltaTime;
                    // Girar hacia el jugador
                    Vector3 lookDir = (playerTransform.position - transform.position).normalized;
                    lookDir.y = 0;
                    if (lookDir != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
                    }

                    if (stateTimer <= 0f)
                    {
                        StartCharge();
                    }
                    break;

                case BossState.Charging:
                    stateTimer -= Time.deltaTime;
                    transform.position += chargeDirection * (chargeSpeed * Time.deltaTime);

                    // Si pasa demasiado tiempo de carga, volver a Idle
                    if (stateTimer <= 0f)
                    {
                        currentState = BossState.Idle;
                        UpdateStatusText("Acechando...");
                    }
                    break;

                case BossState.Stunned:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                    {
                        currentState = BossState.Idle;
                        UpdateStatusText("¡Alerta!");
                    }
                    break;
            }
        }

        private void TriggerAlertBellow()
        {
            currentState = BossState.AlertBellow;
            stateTimer = 2.0f;
            UpdateStatusText("¡BRAMIDO FERAL!");

            if (!hasPlayedIntroCinematic)
            {
                hasPlayedIntroCinematic = true;
                if (Prototipo3.Cinematics.CinematicsManagerPrototipo3.Instance != null)
                {
                    Prototipo3.Cinematics.CinematicsManagerPrototipo3.Instance.PlayCinematic("Alert_CiervoFeral_Prototipo3");
                }
            }
        }

        private void StartCharge()
        {
            currentState = BossState.Charging;
            stateTimer = 2.5f;
            chargeDirection = (playerTransform.position - transform.position).normalized;
            chargeDirection.y = 0f;
            if (chargeDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(chargeDirection);
            }
            UpdateStatusText("¡EMBESTIDA!");
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (currentState == BossState.Charging)
            {
                // Si choca con el jugador
                var player = collision.collider.GetComponent<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (player != null)
                {
                    player.ReceiveDamage();
                    Stun();
                    return;
                }

                // Si choca contra un muro o farola
                if (!collision.collider.isTrigger)
                {
                    Stun();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Trampas colocadas por el hámster
            if (other.CompareTag("Trampa") || other.name.ToLower().Contains("tramp"))
            {
                TakeDamage(1);
                Destroy(other.gameObject);
            }
        }

        public void TakeDamage(int dmg)
        {
            health -= dmg;
            Debug.Log($"[MiniBossCiervoPrototipo3] Vida restante: {health}/{maxHealth}");

            if (health <= 0)
            {
                Defeat();
            }
            else
            {
                Stun();
            }
        }

        public void Stun()
        {
            currentState = BossState.Stunned;
            stateTimer = stunDuration;
            UpdateStatusText($"¡ATURDIDO! ({stunDuration:F1}s)");
        }

        private void Defeat()
        {
            currentState = BossState.Defeated;
            UpdateStatusText("¡DERROTADO!");
            Debug.Log("[MiniBossCiervoPrototipo3] ¡Ciervo Feral Alfa ha sido derrotado!");

            OnDefeated?.Invoke();

            if (Prototipo3.Narrative.StoryManagerPrototipo3.Instance != null)
            {
                Prototipo3.Narrative.StoryManagerPrototipo3.Instance.OnMiniBossDefeated();
            }

            StartCoroutine(CollapseRoutine());
        }

        private IEnumerator CollapseRoutine()
        {
            float t = 0f;
            Vector3 startPos = transform.position;
            while (t < 2.0f)
            {
                t += Time.deltaTime;
                transform.position = startPos - Vector3.up * (t * 0.8f);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        private void CreateStatusUI()
        {
            promptRootObj = new GameObject("CiervoStatusUI");
            promptRootObj.transform.SetParent(this.transform);
            promptRootObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);

            statusTextMesh = promptRootObj.AddComponent<TextMeshPro>();
            statusTextMesh.text = "Ciervo Feral Alfa";
            statusTextMesh.fontSize = 4.2f;
            statusTextMesh.alignment = TextAlignmentOptions.Center;
            statusTextMesh.color = new Color(1f, 0.2f, 0.2f, 1f);
        }

        private void UpdateStatusText(string msg)
        {
            if (statusTextMesh != null)
            {
                statusTextMesh.text = $"Ciervo Feral Alfa\n<size=70%>{msg}</size>";
            }
        }
    }
}
