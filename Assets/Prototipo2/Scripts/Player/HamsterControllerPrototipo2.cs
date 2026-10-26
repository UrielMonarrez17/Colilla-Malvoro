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
        private Rigidbody protoRb;
        private Vector3 protoMoveInput;
        private float protoCurrentBaseSpeed;
        private float protoCurrentSpeedModifier = 1f;
        private int protoHitsReceived = 0;
        private bool protoIsExhausted = false;
        private bool protoIsDashing = false;
        private bool protoIsDead = false;
        private bool protoIsAdrenalineActive = false;
        private Vector3 protoInitialScale;
        private float protoNextDashTime = 0f;
        private float nextJumpTime = 0f;
        private GameObject protoCurrentInteractable;

        public bool IsGrounded => isGrounded;
        public bool IsJumping => isJumping;

        private void Awake()
        {
            protoRb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            if (protoRb == null) protoRb = GetComponent<Rigidbody>();

            protoRb.freezeRotation = true;
            protoRb.useGravity = true;
            protoRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            protoRb.interpolation = RigidbodyInterpolation.Interpolate;

            protoInitialScale = transform.localScale;
            if (protoInitialScale.sqrMagnitude < 0.000001f)
            {
                protoInitialScale = new Vector3(0.0042f, 0.0042f, 0.0042f);
            }

            currentStamina = maxStamina;
            protoCurrentBaseSpeed = walkSpeed;

            if (staminaSlider != null)
            {
                staminaSlider.maxValue = maxStamina;
                staminaSlider.value = currentStamina;
            }
        }

        private void Update()
        {
            if (protoIsDead) return;

            // 1. Capturar Input de movimiento (WASD/Joystick)
            float horizontal = FeralInput.Horizontal;
            float vertical = FeralInput.Vertical;
            protoMoveInput = new Vector3(horizontal, 0f, vertical).normalized;

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
            if (protoIsDead) return;

            UpdateGroundStatus();

            if (!protoIsDashing)
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
            if (isJumping && protoRb.linearVelocity.y > 0.1f)
            {
                isGrounded = false;
            }
            else
            {
                isGrounded = foundGround;
                // Solo restablecer isJumping a false cuando realmente haya tocado el suelo en caída
                if (isGrounded && protoRb.linearVelocity.y <= 0.15f)
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
                protoRb.AddForce(Vector3.down * extraG, ForceMode.Acceleration);
            }
            else if (enableGroundSnapping && !protoIsDashing && !isJumping && protoRb.linearVelocity.y <= 0.1f)
            {
                // Ground Snapping: mantiene al personaje adherido a rampas y aceras al correr sin salir despegado
                if (protoMoveInput != Vector3.zero)
                {
                    protoRb.AddForce(Vector3.down * groundSnapForce, ForceMode.Acceleration);
                }
            }
        }

        private void HandleStealthAndSpeedManagementProto()
        {
            // Sigilo (Crouch)
            bool crouching = FeralInput.Held(KeyCode.C);
            if (crouching)
            {
                transform.localScale = new Vector3(protoInitialScale.x, protoInitialScale.y * 0.5f, protoInitialScale.z);
            }
            else
            {
                transform.localScale = protoInitialScale;
            }

            if (currentStamina <= 0f) protoIsExhausted = true;
            else if (currentStamina >= minStaminaToSprint) protoIsExhausted = false;

            bool isTryingToSprint = FeralInput.Held(KeyCode.LeftShift) && protoMoveInput != Vector3.zero && !protoIsExhausted && !crouching;

            if (isTryingToSprint)
            {
                protoCurrentBaseSpeed = sprintSpeed;
                currentStamina -= staminaDrainRate * Time.deltaTime;
            }
            else if (crouching)
            {
                protoCurrentBaseSpeed = crouchSpeed;
                if (currentStamina < maxStamina) currentStamina += staminaRegenRate * Time.deltaTime;
            }
            else
            {
                protoCurrentBaseSpeed = walkSpeed;
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
            if (dashTriggered && Time.time >= protoNextDashTime && protoMoveInput != Vector3.zero && !FeralInput.Held(KeyCode.C))
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
            protoRb.linearVelocity = new Vector3(protoRb.linearVelocity.x, jumpForce, protoRb.linearVelocity.z);

            currentStamina = Mathf.Max(0, currentStamina - jumpStaminaCost);
            if (staminaSlider != null) staminaSlider.value = currentStamina;
        }

        private IEnumerator DashRoutineProto()
        {
            protoIsDashing = true;
            protoNextDashTime = Time.time + dashCooldown;

            // Mantener componente vertical sin congelar gravedad en seco
            float preservedY = Mathf.Max(protoRb.linearVelocity.y * 0.35f, 0.5f);
            protoRb.linearVelocity = new Vector3(0, preservedY, 0);
            protoRb.AddForce(protoMoveInput * dashForce, ForceMode.Impulse);

            yield return new WaitForSeconds(dashDuration);

            // Al terminar el dash, estabilizar la velocidad horizontal a la velocidad normal sin frenar a cero absoluto
            Vector3 exitVel = protoMoveInput * (protoCurrentBaseSpeed * protoCurrentSpeedModifier);
            protoRb.linearVelocity = new Vector3(exitVel.x, protoRb.linearVelocity.y, exitVel.z);
            protoIsDashing = false;
        }

        private void MoveProto()
        {
            float finalSpeed = protoCurrentBaseSpeed * protoCurrentSpeedModifier;
            Vector3 targetVelocity = protoMoveInput * finalSpeed;

            // Si está en el suelo sobre una pendiente o bordillo, proyectar sobre la normal
            if (isGrounded && groundNormal != Vector3.up)
            {
                targetVelocity = Vector3.ProjectOnPlane(targetVelocity, groundNormal);
            }

            // Mantenemos la velocidad vertical calculada por PhysX + Gravedad extra
            protoRb.linearVelocity = new Vector3(targetVelocity.x, protoRb.linearVelocity.y, targetVelocity.z);
        }

        private void RotateProto()
        {
            if (protoMoveInput != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(protoMoveInput);
                protoRb.MoveRotation(Quaternion.Slerp(protoRb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
            }
        }

        private void HandleInteractionProto()
        {
            if (protoCurrentInteractable != null && !protoCurrentInteractable.activeInHierarchy)
            {
                protoCurrentInteractable = null;
            }

            if (FeralInput.Pressed(KeyCode.F) && protoCurrentInteractable != null)
            {
                if (protoCurrentInteractable.TryGetComponent<CraftingTable>(out CraftingTable craftingTable))
                {
                    craftingTable.ToggleCraftingMenu();
                }
                else if (protoCurrentInteractable.TryGetComponent<LootContainer>(out LootContainer lootContainer))
                {
                    lootContainer.InteractuarConLoot();
                }
                else if (protoCurrentInteractable.TryGetComponent<LuluNPCPrototipo2>(out LuluNPCPrototipo2 luluProto))
                {
                    luluProto.InteractuarConLulu();
                }
                else if (protoCurrentInteractable.TryGetComponent<LuluNPC>(out LuluNPC lulu))
                {
                    lulu.InteractuarConLulu();
                }
                else if (protoCurrentInteractable.TryGetComponent<PuertaTrampa>(out PuertaTrampa puerta))
                {
                    if (trampaSeleccionada == TipoTrampa.Ninguna) return;

                    if (puerta.trampaActual == TipoTrampa.Ninguna)
                    {
                        if (InventoryManager.Instance != null && InventoryManager.Instance.HasTrampa(trampaSeleccionada))
                        {
                            puerta.InstalarTrampa(trampaSeleccionada);
                            InventoryManager.Instance.RemoveTrampa(trampaSeleccionada);
                            Debug.Log($"[HamsterControllerPrototipo2] Trampa de {trampaSeleccionada} instalada.");
                        }
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Interactable") || other.CompareTag("Door"))
            {
                protoCurrentInteractable = other.gameObject;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (protoCurrentInteractable == null && (other.CompareTag("Interactable") || other.CompareTag("Door")))
            {
                protoCurrentInteractable = other.gameObject;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if ((other.CompareTag("Interactable") || other.CompareTag("Door")) && other.gameObject == protoCurrentInteractable)
            {
                if (protoCurrentInteractable.TryGetComponent<CraftingTable>(out CraftingTable table))
                {
                    table.CloseMenu();
                }
                protoCurrentInteractable = null;
            }
        }
    }
}
