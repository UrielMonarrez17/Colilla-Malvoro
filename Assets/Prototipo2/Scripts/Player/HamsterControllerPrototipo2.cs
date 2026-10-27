using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo2
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class HamsterControllerPrototipo2 : HamsterController
    {
        [Header("Físicas Mejoradas y Gravedad Prototipo2")]
        [Tooltip("Multiplicador de gravedad cuando está en el aire (2.4x = aprox -23.5 m/s² para peso real a escala diminuta).")]
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

        [Header("Controles de Dash")]
        public KeyCode dashKeyPrimary = KeyCode.Q;
        public KeyCode dashKeySecondary = KeyCode.LeftControl;
        public bool rightClickDash = true;

        // Estado interno de físicas
        private bool isGrounded = true;
        private bool isJumping = false;
        private Vector3 groundNormal = Vector3.up;
        private float nextJumpTime = 0f;

        public bool IsGrounded => isGrounded;
        public bool IsJumping => isJumping;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();

            rb.freezeRotation = true;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            initialScale = transform.localScale;
            if (initialScale.sqrMagnitude < 0.000001f)
            {
                initialScale = new Vector3(0.0042f, 0.0042f, 0.0042f);
            }

            currentStamina = maxStamina;
            currentBaseSpeed = walkSpeed;

            if (staminaSlider != null)
            {
                staminaSlider.maxValue = maxStamina;
                staminaSlider.value = currentStamina;
            }
        }

        private void Update()
        {
            if (isDead) return;

            // 1. Capturar Input de movimiento (WASD/Joystick)
            float horizontal = FeralInput.Horizontal;
            float vertical = FeralInput.Vertical;
            moveInput = new Vector3(horizontal, 0f, vertical).normalized;

            // 2. Selección de trampas
            if (FeralInput.Pressed(KeyCode.Alpha1)) { trampaSeleccionada = TipoTrampa.Puas; }
            if (FeralInput.Pressed(KeyCode.Alpha2)) { trampaSeleccionada = TipoTrampa.Red; }
            if (FeralInput.Pressed(KeyCode.Alpha3)) { trampaSeleccionada = TipoTrampa.Madera; }

            // 3. Mecánicas
            HandleStealthAndSpeedManagementProto();
            HandleJumpAndDash();
            HandleInteractionProto();
        }

        private void FixedUpdate()
        {
            if (isDead) return;

            UpdateGroundStatus();

            if (!isDashing)
            {
                MoveProto();
                RotateProto();
            }

            ApplyCustomGravity();
        }

        private void UpdateGroundStatus()
        {
            // Origen ubicado justo en la base inferior del cuerpo (nivel de las patas)
            // En escala 0.0042, el centro de la cápsula está en Y=0.0075m y las patas en Y=-0.076m
            Vector3 origin = transform.position + Vector3.up * 0.02f;
            float radius = 0.06f;

            RaycastHit[] hits = Physics.SphereCastAll(origin, radius, Vector3.down, groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);
            bool foundGround = false;

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                // Excluir colisionadores del propio hámster o sus hijos
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
                // Excluir triggers (zonas de interacción, etc.)
                if (hit.collider.isTrigger) continue;

                foundGround = true;
                groundNormal = hit.normal;
                break;
            }

            // Si está subiendo por impulso de salto, no considerarlo en el suelo hasta que comience a descender
            if (isJumping && rb.linearVelocity.y > 0.1f)
            {
                isGrounded = false;
            }
            else
            {
                isGrounded = foundGround;
                // Solo restablecer isJumping a false cuando realmente haya tocado el suelo en caída
                if (isGrounded && rb.linearVelocity.y <= 0.15f)
                {
                    isJumping = false;
                }
            }
        }

        private void ApplyCustomGravity()
        {
            // Gravedad extra si está en el aire (para peso y caída contundente a escala miniatura)
            if (!isGrounded)
            {
                float extraG = Mathf.Abs(Physics.gravity.y) * (customGravityMultiplier - 1f);
                rb.AddForce(Vector3.down * extraG, ForceMode.Acceleration);
            }
            else if (enableGroundSnapping && !isDashing && !isJumping && rb.linearVelocity.y <= 0.1f)
            {
                // Ground Snapping: mantiene al personaje adherido a rampas y aceras al correr sin salir despegado
                if (moveInput != Vector3.zero)
                {
                    rb.AddForce(Vector3.down * groundSnapForce, ForceMode.Acceleration);
                }
            }
        }

        private void HandleStealthAndSpeedManagementProto()
        {
            // Sigilo (Crouch)
            bool crouching = FeralInput.Held(KeyCode.C);
            isCrouching = crouching;
            if (crouching)
            {
                transform.localScale = new Vector3(initialScale.x, initialScale.y * 0.5f, initialScale.z);
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
            bool spacePressed = FeralInput.Pressed(KeyCode.Space) || Input.GetKeyDown(KeyCode.Space);
            bool dashTriggered = IsDashInputTriggered();

            // SALTO: Únicamente 1 salto permitido. Solo si está en el suelo (isGrounded) y NO está ya saltando (isJumping == false)
            if (spacePressed && isGrounded && !isJumping && Time.time >= nextJumpTime && !FeralInput.Held(KeyCode.C))
            {
                PerformJump();
                return;
            }

            // DASH: Exclusivo con [Q], [Click Derecho] o [LeftControl] (nunca con Espacio)
            if (dashTriggered && Time.time >= nextDashTime && !FeralInput.Held(KeyCode.C))
            {
                StartCoroutine(DashRoutineProto());
            }
        }

        private bool IsDashInputTriggered()
        {
            if (FeralInput.Pressed(dashKeyPrimary) || Input.GetKeyDown(dashKeyPrimary)) return true;
            if (FeralInput.Pressed(dashKeySecondary) || Input.GetKeyDown(dashKeySecondary)) return true;

            if (rightClickDash && Input.GetMouseButtonDown(1)) return true;

#if ENABLE_INPUT_SYSTEM
            if (rightClickDash && UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame) return true;
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.qKey.wasPressedThisFrame) return true;
#endif

            return false;
        }

        private void PerformJump()
        {
            isGrounded = false;
            isJumping = true; // Bloquea saltos subsiguientes hasta aterrizar en el suelo
            nextJumpTime = Time.time + 0.25f;

            // Impulso vertical limpio hacia arriba
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

            // Mantener componente vertical sin congelar gravedad en seco
            float preservedY = Mathf.Max(rb.linearVelocity.y * 0.35f, 0.5f);
            rb.linearVelocity = new Vector3(0, preservedY, 0);
            rb.AddForce(dashDir * dashForce, ForceMode.Impulse);

            yield return new WaitForSeconds(dashDuration);

            // Al terminar el dash, estabilizar la velocidad horizontal a la velocidad normal sin frenar a cero absoluto
            Vector3 exitVel = dashDir * (currentBaseSpeed * currentSpeedModifier);
            rb.linearVelocity = new Vector3(exitVel.x, rb.linearVelocity.y, exitVel.z);
            isDashing = false;
        }

        private void MoveProto()
        {
            float finalSpeed = currentBaseSpeed * currentSpeedModifier;
            Vector3 targetVelocity = moveInput * finalSpeed;

            // Si está en el suelo sobre una pendiente o bordillo, proyectar sobre la normal
            if (isGrounded && groundNormal != Vector3.up)
            {
                targetVelocity = Vector3.ProjectOnPlane(targetVelocity, groundNormal);
            }

            // Mantenemos la velocidad vertical calculada por PhysX + Gravedad extra
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

        private void HandleInteractionProto()
        {
            if (currentInteractable != null && !currentInteractable.activeInHierarchy)
            {
                currentInteractable = null;
            }

            if (FeralInput.Pressed(KeyCode.F) && currentInteractable != null)
            {
                if (currentInteractable.TryGetComponent<CraftingTable>(out CraftingTable craftingTable))
                {
                    craftingTable.ToggleCraftingMenu();
                }
                else if (currentInteractable.TryGetComponent<LootContainer>(out LootContainer lootContainer))
                {
                    lootContainer.InteractuarConLoot();
                }
                else if (currentInteractable.TryGetComponent<LuluNPCPrototipo2>(out LuluNPCPrototipo2 luluProto))
                {
                    luluProto.InteractuarConLulu();
                }
                else if (currentInteractable.TryGetComponent<LuluNPC>(out LuluNPC lulu))
                {
                    lulu.InteractuarConLulu();
                }
                else if (currentInteractable.TryGetComponent<PuertaTrampa>(out PuertaTrampa puerta))
                {
                    if (trampaSeleccionada == TipoTrampa.Ninguna) return;

                    if (puerta.trampaActual == TipoTrampa.Ninguna)
                    {
                        if (InventoryManager.Instance != null && InventoryManager.Instance.HasTrampa(trampaSeleccionada))
                        {
                            puerta.InstalarTrampa(trampaSeleccionada);
                            InventoryManager.Instance.RemoveTrampa(trampaSeleccionada);
                            InvokeOnTrapInstalled(trampaSeleccionada, puerta);
                            Debug.Log($"[HamsterControllerPrototipo2] Trampa de {trampaSeleccionada} instalada.");
                        }
                    }
                }
            }
        }

        public override void RespawnOrHeal(Vector3? newPosition = null)
        {
            base.RespawnOrHeal(newPosition);
            isGrounded = true;
            isJumping = false;
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
