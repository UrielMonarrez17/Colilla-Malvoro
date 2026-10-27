using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo3.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class HamsterControllerPrototipo3 : HamsterController
    {
        public static HamsterControllerPrototipo3 Instance { get; private set; }
        public static bool IsInputLocked { get; private set; } = false;

        [Header("Físicas Mejoradas y Gravedad Prototipo 3")]
        [Tooltip("Multiplicador de gravedad en el aire para peso contundente a escala diminuta.")]
        public float customGravityMultiplier = 2.4f;

        [Tooltip("Fuerza de impulso para el salto.")]
        public float jumpForce = 7.5f;

        [Tooltip("Costo de estamina por salto.")]
        public float jumpStaminaCost = 10f;

        [Tooltip("Distancia de detección del suelo.")]
        public float groundCheckDistance = 0.14f;

        [Tooltip("Capas de suelo.")]
        public LayerMask groundLayers = ~0;

        [Tooltip("Ground Snapping para transiciones suaves.")]
        public bool enableGroundSnapping = true;
        public float groundSnapForce = 12f;

        [Header("Controles de Dash")]
        public KeyCode dashKeyPrimary = KeyCode.Q;
        public KeyCode dashKeySecondary = KeyCode.LeftControl;
        public bool rightClickDash = true;

        [Header("Inventario Narrativo")]
        public int filtrosCarmesiCount = 0;
        public bool hasKeycardTorreNorte = false;
        public bool hasLithiumBattery = false;

        // Estado interno
        private bool isGrounded = true;
        private bool isJumping = false;
        private Vector3 groundNormal = Vector3.up;
        private float nextJumpTime = 0f;

        public bool IsGrounded => isGrounded;
        public bool IsJumping => isJumping;

        public static void SetInputLocked(bool locked)
        {
            IsInputLocked = locked;
            if (Instance != null && Instance.rb != null)
            {
                Instance.rb.linearVelocity = new Vector3(0f, Instance.rb.linearVelocity.y, 0f);
            }
        }

        private void Awake()
        {
            Instance = this;
            this.enabled = true;
            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            this.enabled = true;
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
            if (isDead || IsInputLocked)
            {
                moveInput = Vector3.zero;
                return;
            }

            // 1. Capturar Movimiento
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
            if (isDead || IsInputLocked) return;

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

            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            if (staminaSlider != null) staminaSlider.value = currentStamina;
        }

        private void HandleJumpAndDash()
        {
            // Salto con [Espacio]
            if (FeralInput.Pressed(KeyCode.Space) && isGrounded && !isJumping && Time.time >= nextJumpTime)
            {
                if (currentStamina >= jumpStaminaCost)
                {
                    currentStamina -= jumpStaminaCost;
                    nextJumpTime = Time.time + 0.25f;
                    isJumping = true;
                    isGrounded = false;

                    Vector3 vel = rb.linearVelocity;
                    vel.y = jumpForce;
                    rb.linearVelocity = vel;
                }
            }

            // Dash con [Q], [Ctrl] o Click Derecho
            bool dashPressed = FeralInput.Pressed(dashKeyPrimary) || FeralInput.Pressed(dashKeySecondary);
            if (rightClickDash && Input.GetMouseButtonDown(1)) dashPressed = true;

            if (dashPressed && !isDashing && currentStamina >= 20f)
            {
                StartCoroutine(PerformDashProto());
            }
        }

        private IEnumerator PerformDashProto()
        {
            isDashing = true;
            currentStamina -= 20f;

            Vector3 dashDir = moveInput != Vector3.zero ? moveInput : transform.forward;
            rb.linearVelocity = dashDir * dashForce;

            yield return new WaitForSeconds(dashDuration);

            rb.linearVelocity = Vector3.zero;
            yield return new WaitForSeconds(dashCooldown);
            isDashing = false;
        }

        private void HandleInteractionProto()
        {
            if (FeralInput.Pressed(KeyCode.E) || FeralInput.Pressed(KeyCode.F))
            {
                if (currentInteractable != null)
                {
                    currentInteractable.SendMessage("Interactuar", this, SendMessageOptions.DontRequireReceiver);
                    currentInteractable.SendMessage("Interact", this, SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        private void MoveProto()
        {
            if (moveInput == Vector3.zero)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                return;
            }

            Vector3 targetVelocity = moveInput * (currentBaseSpeed * currentSpeedModifier);
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }

        private void RotateProto()
        {
            if (moveInput != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveInput, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
            }
        }

        public void AddFiltroCarmesi(int amount = 1)
        {
            filtrosCarmesiCount += amount;
            Debug.Log($"[HamsterControllerPrototipo3] Filtro Carmesí recolectado: {filtrosCarmesiCount}/3");
            Prototipo3.Narrative.StoryManagerPrototipo3.Instance?.OnItemCollected("Filtro Carmesí", filtrosCarmesiCount);
        }

        public void AcquireKeycard()
        {
            hasKeycardTorreNorte = true;
            Debug.Log("[HamsterControllerPrototipo3] ¡Keycard_TorreNorte adquirida!");
            Prototipo3.Narrative.StoryManagerPrototipo3.Instance?.OnItemCollected("Keycard_TorreNorte", 1);
        }

        public void AcquireLithiumBattery()
        {
            hasLithiumBattery = true;
            Debug.Log("[HamsterControllerPrototipo3] ¡Batería de Litio obtenida!");
            Prototipo3.Narrative.StoryManagerPrototipo3.Instance?.OnItemCollected("Batería de Litio", 1);
        }
    }
}
