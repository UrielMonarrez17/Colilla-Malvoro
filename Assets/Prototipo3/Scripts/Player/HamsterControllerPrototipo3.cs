using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo3
{
    [RequireComponent(typeof(Rigidbody))]
    public class HamsterControllerPrototipo3 : HamsterController
    {
        [Header("Físicas Mejoradas y Gravedad Prototipo3")]
        [Tooltip("Multiplicador de gravedad cuando está en el aire.")]
        public float customGravityMultiplier = 2.4f;

        [Tooltip("Fuerza de impulso vertical para el salto.")]
        public float jumpForce = 7.5f;

        [Tooltip("Costo de estamina por salto.")]
        public float jumpStaminaCost = 10f;

        [Tooltip("Distancia de detección del suelo desde las patas.")]
        public float groundCheckDistance = 0.14f;

        [Tooltip("Capas consideradas suelo.")]
        public LayerMask groundLayers = ~0;

        [Tooltip("Ajuste suave al suelo para no salir flotando sobre desniveles y bordillos.")]
        public bool enableGroundSnapping = true;
        public float groundSnapForce = 12f;

        [Header("Controles de Dash y Regeneración")]
        public KeyCode dashKeyPrimary = KeyCode.Q;
        public KeyCode dashKeySecondary = KeyCode.LeftControl;
        public KeyCode regenerateCityKey = KeyCode.G;
        public bool rightClickDash = true;

        [Header("Movimiento relativo a la cámara")]
        [Tooltip("Cámara que orienta WASD. Si no se asigna, se usa Main Camera.")]
        public Transform movementCamera;

        // Estado interno de físicas
        private bool isGrounded = true;
        private bool isJumping = false;
        private Vector3 groundNormal = Vector3.up;
        private float nextJumpTime = 0f;
        private Transform visualModelTransform;
        private Vector3 visualBaseLocalScale = Vector3.one;
        private bool visualScaleCached = false;

        // Ralentización externa temporal e invulnerabilidad post-golpe anti-atasco
        private float externalSlowMultiplier = 1f;
        private float externalSlowTimer = 0f;
        private float postHitInvulnTimer = 0f;
        private readonly System.Collections.Generic.List<Collider> ignoredEnemyColliders = new System.Collections.Generic.List<Collider>();

        public bool IsGrounded => isGrounded;
        public bool IsJumping => isJumping;

        private void Awake()
        {
            this.enabled = true;
            rb = GetComponent<Rigidbody>();
            if (!gameObject.CompareTag("Player"))
            {
                gameObject.tag = "Player";
            }
            NormalizePlayerCollidersAndVisual();
        }

        private void Start()
        {
            this.enabled = true;
            if (rb == null) rb = GetComponent<Rigidbody>();

            rb.freezeRotation = true;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            NormalizePlayerCollidersAndVisual();

            initialScale = transform.localScale;
            if (initialScale.sqrMagnitude < 0.000001f)
            {
                initialScale = Vector3.one;
                transform.localScale = Vector3.one;
            }

            currentStamina = maxStamina;
            currentBaseSpeed = walkSpeed;

            if (staminaSlider != null)
            {
                staminaSlider.maxValue = maxStamina;
                staminaSlider.value = currentStamina;
            }

            TrapSystemPrototipo3.EnsureInstance();
            RespawnManagerPrototipo3.EnsureInstance();
        }

        /// <summary>
        /// Corrige el BoxCollider automático de [RequireComponent(typeof(BoxCollider))] para que su base quede exactamente en y=0
        /// (evitando que el hámster flote 0.5m en el aire) y elimina colliders duplicados en la raíz.
        /// </summary>
        public void NormalizePlayerCollidersAndVisual()
        {
            float sx = Mathf.Max(0.0001f, Mathf.Abs(transform.localScale.x));
            float sy = Mathf.Max(0.0001f, Mathf.Abs(transform.localScale.y));
            float sz = Mathf.Max(0.0001f, Mathf.Abs(transform.localScale.z));

            // Si el hámster usa escala 1.0 con VisualModel hijo (Prototipo3 y Escuelas), configurar BoxCollider exacto al ras de y=0
            if (sy > 0.2f)
            {
                CapsuleCollider extraCap = GetComponent<CapsuleCollider>();
                if (extraCap != null) extraCap.enabled = false;

                BoxCollider box = GetComponent<BoxCollider>();
                if (box == null) box = gameObject.AddComponent<BoxCollider>();
                box.enabled = true;
                box.isTrigger = false;
                box.size = new Vector3(0.44f / sx, 0.56f / sy, 0.52f / sz);
                box.center = new Vector3(0f, 0.28f / sy, 0f);
            }

            Transform vis = transform.Find("VisualModel");
            if (vis != null)
            {
                visualModelTransform = vis;
                if (!visualScaleCached)
                {
                    visualBaseLocalScale = vis.localScale;
                    visualScaleCached = true;
                }

                // Asegurar que la base de las patas del modelo visual descanse exactamente en local y = 0
                MeshRenderer[] mrs = vis.GetComponentsInChildren<MeshRenderer>(true);
                if (mrs.Length > 0)
                {
                    float minWorldY = float.MaxValue;
                    for (int i = 0; i < mrs.Length; i++)
                    {
                        if (mrs[i] != null) minWorldY = Mathf.Min(minWorldY, mrs[i].bounds.min.y);
                    }
                    if (minWorldY < float.MaxValue)
                    {
                        float deltaWorldY = transform.position.y - minWorldY;
                        if (Mathf.Abs(deltaWorldY) > 0.005f && Mathf.Abs(deltaWorldY) < 2.5f)
                        {
                            vis.position += new Vector3(0f, deltaWorldY, 0f);
                        }
                    }
                }
            }
        }

        public void ApplyExternalSlow(float multiplier, float duration)
        {
            externalSlowMultiplier = Mathf.Clamp(multiplier, 0.25f, 1f);
            externalSlowTimer = Mathf.Max(externalSlowTimer, duration);
        }

        private void Update()
        {
            if (Prototipo3.UI.FeralGameUI.BlocksGameplay) return;
            if (PortraitDialogueUIPrototipo3.IsConversationActive || StoryDirectorPrototipo3.IsDemoVictoryOpen) return;
            if (RespawnManagerPrototipo3.Instance != null && RespawnManagerPrototipo3.Instance.IsRespawning) return;
            if (isDead) return;

            if (externalSlowTimer > 0f)
            {
                externalSlowTimer -= Time.deltaTime;
                if (externalSlowTimer <= 0f)
                {
                    externalSlowMultiplier = 1f;
                }
            }

            if (postHitInvulnTimer > 0f)
            {
                postHitInvulnTimer -= Time.deltaTime;
                if (visualModelTransform != null)
                {
                    // Parpadeo sutil de invulnerabilidad para indicar ventana de escape
                    bool blinkVis = Mathf.FloorToInt(postHitInvulnTimer * 16f) % 2 == 0;
                    Renderer[] rens = visualModelTransform.GetComponentsInChildren<Renderer>(true);
                    for (int i = 0; i < rens.Length; i++) if (rens[i] != null) rens[i].enabled = blinkVis;
                }
                if (postHitInvulnTimer <= 0f)
                {
                    RestoreEnemyCollisionsAndVisibility();
                }
            }

            // 1. Capturar Input de movimiento (WASD/Joystick)
            float horizontal = FeralInput.Horizontal;
            float vertical = FeralInput.Vertical;
            moveInput = GetCameraRelativeMovement(horizontal, vertical);

            // 2. Regenerar ciudad en tiempo real con [G]
            if (FeralInput.Pressed(regenerateCityKey) || Input.GetKeyDown(regenerateCityKey))
            {
                if (CityGeneratorPrototipo3.Instance != null)
                {
                    CityGeneratorPrototipo3.Instance.RegenerateCityRuntime();
                }
            }

            // 3. Selección y uso de Trampas Tácticas con teclas rápidas [1], [2], [3]
            if (FeralInput.Pressed(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha1))
            {
                SelectAndTryUseTrap(TipoTrampa.Puas);
            }
            if (FeralInput.Pressed(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Alpha2))
            {
                SelectAndTryUseTrap(TipoTrampa.Red);
            }
            if (FeralInput.Pressed(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Alpha3))
            {
                SelectAndTryUseTrap(TipoTrampa.Madera);
            }

            // 4. Mecánicas de movimiento, sigilo, dash e interacción
            RefreshProximityInteractable();
            HandleStealthAndSpeedManagementProto();
            HandleJumpAndDash();
            HandleInteractionProto();
        }

        private void SelectAndTryUseTrap(TipoTrampa tipo)
        {
            trampaSeleccionada = tipo;

            // Si no estamos frente a una PuertaTrampa tradicional, desplegar inmediatamente la herramienta táctica
            bool standingAtTrapDoor = currentInteractable != null && currentInteractable.GetComponent<PuertaTrampa>() != null;
            if (!standingAtTrapDoor)
            {
                TrapSystemPrototipo3.EnsureInstance().TryDeployTrap(tipo, transform);
            }
        }

        private Vector3 GetCameraRelativeMovement(float horizontal, float vertical)
        {
            Vector3 input = Vector3.ClampMagnitude(new Vector3(horizontal, 0f, vertical), 1f);
            Transform cameraTransform = movementCamera;
            if (cameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null) cameraTransform = mainCamera.transform;
            }

            // Only yaw affects movement: camera pitch must not push the player
            // into the floor. Match the isometric view even before a camera exists.
            float yaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 45f;
            return Quaternion.Euler(0f, yaw, 0f) * input;
        }

        private void FixedUpdate()
        {
            if (isDead) return;

            EnforceMinimumFloorSafety();
            UpdateGroundStatus();

            if (!isDashing)
            {
                MoveProto();
                RotateProto();
            }

            ApplyCustomGravity();
            EnforceMinimumFloorSafety();
        }

        private void LateUpdate()
        {
            if (!isDead) EnforceMinimumFloorSafety();
        }

        private void EnforceMinimumFloorSafety()
        {
            float minSurfaceY = CityGeneratorPrototipo3.Instance != null ? CityGeneratorPrototipo3.Instance.roadHeightY : 0f;
            if (transform.position.y < minSurfaceY - 0.04f)
            {
                Vector3 safePos = new Vector3(transform.position.x, minSurfaceY + 0.02f, transform.position.z);
                transform.position = safePos;
                if (rb != null && !rb.isKinematic)
                {
                    rb.position = safePos;
                    if (rb.linearVelocity.y < 0f)
                    {
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                    }
                }
                isGrounded = true;
                isJumping = false;
            }
        }

        private void UpdateGroundStatus()
        {
            Vector3 origin = transform.position + Vector3.up * 0.02f;
            float radius = 0.06f;

            RaycastHit[] hits = Physics.SphereCastAll(origin, radius, Vector3.down, groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);
            bool foundGround = false;

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.isTrigger) continue;

                foundGround = true;
                groundNormal = hit.normal;
                break;
            }

            if (isJumping && rb.linearVelocity.y > 0.1f)
            {
                isGrounded = false;
            }
            else
            {
                isGrounded = foundGround;
                if (isGrounded && rb.linearVelocity.y <= 0.15f)
                {
                    isJumping = false;
                }
            }
        }

        private void ApplyCustomGravity()
        {
            if (!isGrounded)
            {
                float extraG = Mathf.Abs(Physics.gravity.y) * (customGravityMultiplier - 1f);
                rb.AddForce(Vector3.down * extraG, ForceMode.Acceleration);
            }
            else if (enableGroundSnapping && !isDashing && !isJumping && rb.linearVelocity.y <= 0.1f)
            {
                if (moveInput != Vector3.zero)
                {
                    rb.AddForce(Vector3.down * groundSnapForce, ForceMode.Acceleration);
                }
            }
        }

        private void HandleStealthAndSpeedManagementProto()
        {
            bool crouching = FeralInput.Held(KeyCode.C);
            isCrouching = crouching;

            // Escalar únicamente el modelo visual al agacharse para no encoger ni hundir el Collider físico contra el suelo
            if (visualModelTransform != null && visualScaleCached)
            {
                visualModelTransform.localScale = crouching
                    ? new Vector3(visualBaseLocalScale.x * 1.08f, visualBaseLocalScale.y * 0.55f, visualBaseLocalScale.z * 1.08f)
                    : visualBaseLocalScale;
            }
            else
            {
                transform.localScale = initialScale;
            }

            if (currentStamina <= 0f) isExhausted = true;
            else if (currentStamina >= minStaminaToSprint) isExhausted = false;

            bool isTryingToSprint = FeralInput.Held(KeyCode.LeftShift) && moveInput != Vector3.zero && !isExhausted && !crouching;

            if (isTryingToSprint)
            {
                currentBaseSpeed = sprintSpeed;
                currentStamina -= staminaDrainRate * Time.deltaTime;
            }
            else if (crouching)
            {
                currentBaseSpeed = crouchSpeed;
                if (currentStamina < maxStamina) currentStamina += staminaRegenRate * Time.deltaTime;
            }
            else
            {
                currentBaseSpeed = walkSpeed;
                if (currentStamina < maxStamina) currentStamina += staminaRegenRate * Time.deltaTime;
            }

            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            if (staminaSlider != null) staminaSlider.value = currentStamina;
        }

        private void HandleJumpAndDash()
        {
            if (isDashing || FeralInput.Held(KeyCode.C)) return;

            // Space is reserved for jumping, including with old Inspector bindings.
            if (FeralInput.Pressed(KeyCode.Space))
            {
                if (isGrounded && !isJumping && Time.time >= nextJumpTime) PerformJump();
                return;
            }

            if (IsDashInputTriggered() && Time.time >= nextDashTime)
            {
                StartCoroutine(DashRoutineProto());
            }
        }

        private bool IsDashInputTriggered()
        {
            KeyCode primary = dashKeyPrimary == KeyCode.Space ? KeyCode.Q : dashKeyPrimary;
            KeyCode secondary = dashKeySecondary == KeyCode.Space ? KeyCode.LeftControl : dashKeySecondary;
            if (FeralInput.Pressed(primary) || FeralInput.Pressed(secondary)) return true;

#if ENABLE_INPUT_SYSTEM
            if (rightClickDash && UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame) return true;
#else
            if (rightClickDash && Input.GetMouseButtonDown(1)) return true;
#endif
            return false;
        }

        private void PerformJump()
        {
            isGrounded = false;
            isJumping = true;
            nextJumpTime = Time.time + 0.25f;

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            currentStamina = Mathf.Max(0, currentStamina - jumpStaminaCost);
            if (staminaSlider != null) staminaSlider.value = currentStamina;
        }

        private IEnumerator DashRoutineProto()
        {
            isDashing = true;
            nextDashTime = Time.time + dashCooldown;
            InvokeOnDashUsed();

            Vector3 dashDir = moveInput != Vector3.zero ? moveInput : transform.forward;

            float preservedY = Mathf.Max(rb.linearVelocity.y * 0.35f, 0.5f);
            rb.linearVelocity = new Vector3(0, preservedY, 0);
            rb.AddForce(dashDir * dashForce, ForceMode.Impulse);

            yield return new WaitForSeconds(dashDuration);

            Vector3 exitVel = dashDir * (currentBaseSpeed * currentSpeedModifier * externalSlowMultiplier);
            rb.linearVelocity = new Vector3(exitVel.x, rb.linearVelocity.y, exitVel.z);
            isDashing = false;
        }

        private void MoveProto()
        {
            float finalSpeed = currentBaseSpeed * currentSpeedModifier * externalSlowMultiplier;
            Vector3 targetVelocity = moveInput * finalSpeed;

            if (isGrounded && groundNormal != Vector3.up)
            {
                targetVelocity = Vector3.ProjectOnPlane(targetVelocity, groundNormal);
            }

            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }

        private void RotateProto()
        {
            if (moveInput != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveInput);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
            }
        }

        private void RefreshProximityInteractable()
        {
            if (currentInteractable != null)
            {
                Vector3 interactablePos = currentInteractable.transform.position;
                if (!currentInteractable.activeInHierarchy ||
                    Vector3.Distance(transform.position, interactablePos) > 4.2f)
                {
                    if (currentInteractable.TryGetComponent<CraftingTable>(out CraftingTable table))
                    {
                        table.CloseMenu();
                    }
                    currentInteractable = null;
                }
            }

            if (currentInteractable != null) return;

            float bestDist = 3.8f;
            GameObject bestCandidate = null;

            // Colliders cercanos con tag Interactable o Door
            Collider[] nearby = Physics.OverlapSphere(transform.position, 3.4f, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < nearby.Length; i++)
            {
                Collider col = nearby[i];
                if (col == null) continue;
                if (col.CompareTag("Interactable") || col.CompareTag("Door"))
                {
                    float d = Vector3.Distance(transform.position, col.transform.position);
                    if (d <= bestDist)
                    {
                        bestDist = d;
                        bestCandidate = col.gameObject;
                    }
                }
            }

            if (bestCandidate != null)
            {
                currentInteractable = bestCandidate;
            }
        }

        private void HandleInteractionProto()
        {
            if (currentInteractable != null && !currentInteractable.activeInHierarchy)
            {
                currentInteractable = null;
            }

            if ((FeralInput.Pressed(KeyCode.F) || Input.GetKeyDown(KeyCode.F)) && currentInteractable != null)
            {
                if (currentInteractable.GetComponentInParent<StoryInteractablePrototipo3>() is StoryInteractablePrototipo3 storyInter)
                {
                    storyInter.Interact(this);
                }
                else if (currentInteractable.GetComponentInParent<CraftingTable>() is CraftingTable craftingTable)
                {
                    craftingTable.ToggleCraftingMenu();
                }
                else if (currentInteractable.GetComponentInParent<LootContainer>() is LootContainer lootContainer)
                {
                    lootContainer.InteractuarConLoot();
                }
                else if (currentInteractable.GetComponentInParent<LuluNPC>() is LuluNPC lulu)
                {
                    lulu.InteractuarConLulu();
                }
                else if (currentInteractable.GetComponentInParent<PuertaTrampa>() is PuertaTrampa puerta)
                {
                    if (trampaSeleccionada == TipoTrampa.Ninguna) return;

                    if (puerta.trampaActual == TipoTrampa.Ninguna)
                    {
                        if (InventoryManager.Instance != null && InventoryManager.Instance.HasTrampa(trampaSeleccionada))
                        {
                            puerta.InstalarTrampa(trampaSeleccionada);
                            InventoryManager.Instance.RemoveTrampa(trampaSeleccionada);
                            InvokeOnTrapInstalled(trampaSeleccionada, puerta);
                            Debug.Log($"[HamsterControllerPrototipo3] Trampa de {trampaSeleccionada} instalada.");
                        }
                    }
                }
            }
        }

        public override void ReceiveDamage()
        {
            if (isDead || postHitInvulnTimer > 0f) return;

            base.ReceiveDamage();

            if (!isDead)
            {
                // Activar 1.4s de invulnerabilidad + atravesar colliders enemigos + impulso de escape para jamás quedar trabado!
                postHitInvulnTimer = 1.4f;
                externalSlowMultiplier = 1f;
                externalSlowTimer = 0f;
                isDashing = false;

                Collider myCol = GetComponent<Collider>();
                Vector3 pushAway = Vector3.zero;

                Collider[] nearby = Physics.OverlapSphere(transform.position, 4.5f, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < nearby.Length; i++)
                {
                    Collider c = nearby[i];
                    if (c == null || c == myCol) continue;
                    bool isEnemy = c.CompareTag("Enemy") || c.GetComponentInParent<EnemyAI>() != null ||
                                   c.GetComponentInParent<MarcoFeralBossAI>() != null || c.GetComponentInParent<OwlRectorBossAI>() != null;
                    if (!isEnemy) continue;

                    if (myCol != null && !c.isTrigger)
                    {
                        Physics.IgnoreCollision(myCol, c, true);
                        ignoredEnemyColliders.Add(c);
                    }

                    Vector3 diff = transform.position - c.transform.position;
                    diff.y = 0f;
                    if (diff.sqrMagnitude > 0.001f) pushAway += diff.normalized;
                }

                if (rb != null && !rb.isKinematic)
                {
                    Vector3 escapeDir = pushAway.sqrMagnitude > 0.01f ? pushAway.normalized : -transform.forward;
                    rb.linearVelocity = new Vector3(escapeDir.x * 9.5f, 2.2f, escapeDir.z * 9.5f);
                }
            }
        }

        private void RestoreEnemyCollisionsAndVisibility()
        {
            Collider myCol = GetComponent<Collider>();
            if (myCol != null)
            {
                for (int i = 0; i < ignoredEnemyColliders.Count; i++)
                {
                    if (ignoredEnemyColliders[i] != null)
                    {
                        Physics.IgnoreCollision(myCol, ignoredEnemyColliders[i], false);
                    }
                }
            }
            ignoredEnemyColliders.Clear();

            if (visualModelTransform != null)
            {
                Renderer[] rens = visualModelTransform.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < rens.Length; i++) if (rens[i] != null) rens[i].enabled = true;
            }
        }

        public override void RespawnOrHeal(Vector3? newPosition = null)
        {
            postHitInvulnTimer = 0f;
            RestoreEnemyCollisionsAndVisibility();
            base.RespawnOrHeal(newPosition);
            isGrounded = true;
            isJumping = false;
            externalSlowMultiplier = 1f;
            externalSlowTimer = 0f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Interactable") || other.CompareTag("Door"))
            {
                currentInteractable = other.gameObject;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (currentInteractable == null && (other.CompareTag("Interactable") || other.CompareTag("Door")))
            {
                currentInteractable = other.gameObject;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if ((other.CompareTag("Interactable") || other.CompareTag("Door")) && other.gameObject == currentInteractable)
            {
                if (currentInteractable.TryGetComponent<CraftingTable>(out CraftingTable table))
                {
                    table.CloseMenu();
                }
                currentInteractable = null;
            }
        }
    }
}
