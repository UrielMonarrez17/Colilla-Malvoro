using UnityEngine;
using UnityEngine.UI; 
using System;
using System.Collections; 

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))] 
public class HamsterController : MonoBehaviour
{
    // ===================================================================================
    //  VARIABLES Y CONFIGURACIÓN
    // ===================================================================================

    [Header("Ajustes de Movimiento")]
    public float walkSpeed = 8f;
    public float sprintSpeed = 13f;
    public float crouchSpeed = 4f;
    public float turnSpeed = 15f;

    [Header("Estamina (UI)")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 25f; 
    public float staminaRegenRate = 15f;  
    public float minStaminaToSprint = 20f; 
    public Slider staminaSlider; 

    [Header("Dash")]
    public float dashForce = 25f; 
    public float dashCooldown = 1.5f;
    public float dashDuration = 0.15f; 

    [Header("Salud y Efectos de Daño")]
    public int maxHitsAllowed = 3;
    public float adrenalineBoostModifier = 1.3f; 
    public float adrenalineDuration = 3f; 
    public float fatiguePenaltyPerHit = 0.1f; 

    [Header("Trampas")]
    public TipoTrampa trampaSeleccionada = TipoTrampa.Puas; // Trampa seleccionada actualmente

    // --- Variables de Estado ---
    protected float currentBaseSpeed;
    protected float currentSpeedModifier = 1f; 
    protected int hitsReceived = 0;
    protected bool isExhausted = false; 
    protected bool isDashing = false;
    protected bool isDead = false;
    protected bool isAdrenalineActive = false;
    protected Vector3 initialScale;

    public bool IsDead => isDead;
    public bool IsDashing => isDashing;
    public bool IsExhausted => isExhausted;
    public bool IsAdrenalineActive => isAdrenalineActive;
    public int HitsReceived => hitsReceived;
    public float CurrentSpeedModifier => currentSpeedModifier;
    public bool IsMoving => moveInput != Vector3.zero;
    public bool IsSprinting => FeralInput.Held(KeyCode.LeftShift) && moveInput != Vector3.zero && !isExhausted && !isCrouching;
    public GameObject CurrentInteractable => currentInteractable;
    
    public bool isCrouching { get; protected set; } 

    // Eventos para integración con Tutorial y HUD
    public event Action OnDashUsed;
    public event Action OnAdrenalineTriggered;
    public event Action<int, int> OnDamageReceived;
    public event Action OnPlayerDied;
    public event Action<TipoTrampa, PuertaTrampa> OnTrapInstalled;

    protected float nextDashTime = 0f;
    protected KeyCode interactKey = KeyCode.F;
    protected GameObject currentInteractable; 

    protected Rigidbody rb;
    protected Vector3 moveInput;

    protected void InvokeOnDashUsed() => OnDashUsed?.Invoke();
    protected void InvokeOnAdrenalineTriggered() => OnAdrenalineTriggered?.Invoke();
    protected void InvokeOnDamageReceived(int hits, int max) => OnDamageReceived?.Invoke(hits, max);
    protected void InvokeOnPlayerDied() => OnPlayerDied?.Invoke();

    // ===================================================================================
    //  INICIALIZACIÓN (Start)
    // ===================================================================================

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; 
        rb.useGravity = true;

        // Evita el tunneling a velocidades altas o durante el dash
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Suaviza el movimiento entre ticks

        initialScale = transform.localScale;
        if (initialScale.sqrMagnitude < 0.000001f)
        {
            initialScale = new Vector3(0.019348267f, 0.019348267f, 0.019348267f);
        }

        currentStamina = maxStamina;
        currentBaseSpeed = walkSpeed;

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
    }

    // ===================================================================================
    //  BUCLE DE ACTUALIZACIÓN (Update)
    // ===================================================================================

    void Update()
    {
        if (isDead || isDashing) return; 

        // 1. Capturar Input de movimiento (WASD/Joystick)
        float horizontal = FeralInput.Horizontal;
        float vertical = FeralInput.Vertical;
        // Normalizado para evitar mayor velocidad en diagonal
        moveInput = new Vector3(horizontal, 0f, vertical).normalized;

        // --- SELECCIÓN DE TRAMPAS CON DEBUGS ---
        if (FeralInput.Pressed(KeyCode.Alpha1)) 
        { 
            trampaSeleccionada = TipoTrampa.Puas; 
            Debug.Log("👉 Trampa armada en las manos: PÚAS"); 
        }
        if (FeralInput.Pressed(KeyCode.Alpha2)) 
        { 
            trampaSeleccionada = TipoTrampa.Red; 
            Debug.Log("👉 Trampa armada en las manos: RED"); 
        }
        if (FeralInput.Pressed(KeyCode.Alpha3)) 
        { 
            trampaSeleccionada = TipoTrampa.Madera; 
            Debug.Log("👉 Trampa armada en las manos: MADERA"); 
        }

        HandleStealthAndSpeedManagement(); 
        HandleDash(); 
        HandleInteraction(); 
    }

    void FixedUpdate()
    {
        if (isDead || isDashing) return; 

        Move();
        Rotate();
    }

    // ===================================================================================
    //  LÓGICA DETALLADA DE MECÁNICAS
    // ===================================================================================

    private void HandleStealthAndSpeedManagement()
    {
        // --- Mecánica de Sigilo (Crouch) ---
        isCrouching = FeralInput.Held(KeyCode.C);
        
        if (isCrouching) 
        {
            // Reducimos visualmente la escala en Y conservando la escala original del modelo
            transform.localScale = new Vector3(initialScale.x, initialScale.y * 0.5f, initialScale.z); 
        }
        else
        {
            // Escala normal original
            transform.localScale = initialScale;
        }

        if (currentStamina <= 0f) isExhausted = true;
        else if (currentStamina >= minStaminaToSprint) isExhausted = false;

        // Reglas para sprintar: Mantener Shift, moverse, NO estar agotado, NO estar agachado
        bool isTryingToSprint = FeralInput.Held(KeyCode.LeftShift) && moveInput != Vector3.zero && !isExhausted && !isCrouching;

        if (isTryingToSprint)
        {
            currentBaseSpeed = sprintSpeed;
            currentStamina -= staminaDrainRate * Time.deltaTime;
        }
        else if (isCrouching)
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

    private void HandleDash()
    {
        if (FeralInput.Pressed(KeyCode.Space) && Time.time >= nextDashTime && moveInput != Vector3.zero && !isCrouching)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;
        OnDashUsed?.Invoke();

        rb.linearVelocity = Vector3.zero; 
        rb.AddForce(moveInput * dashForce, ForceMode.Impulse);

        yield return new WaitForSeconds(dashDuration);
        
        rb.linearVelocity = Vector3.zero; 
        isDashing = false;
    }

    private void HandleInteraction()
    {
        // Si el objeto interactuable actual se desactivó o destruyó, limpiarlo
        if (currentInteractable != null && !currentInteractable.activeInHierarchy)
        {
            currentInteractable = null;
        }

        if (FeralInput.Pressed(interactKey) && currentInteractable != null)
        {
            if (currentInteractable.TryGetComponent<CraftingTable>(out CraftingTable craftingTable))
            {
                craftingTable.ToggleCraftingMenu();
            }
            else if (currentInteractable.TryGetComponent<LootContainer>(out LootContainer lootContainer))
            {
                lootContainer.InteractuarConLoot();
            }
            else if (currentInteractable.TryGetComponent<LuluNPC>(out LuluNPC lulu))
            {
                lulu.InteractuarConLulu();
            }
            // --- LÓGICA DE PUERTAS CON DEBUGS ---
            else if (currentInteractable.TryGetComponent<PuertaTrampa>(out PuertaTrampa puerta))
            {
                // Verificamos si no has seleccionado ninguna trampa por accidente
                if (trampaSeleccionada == TipoTrampa.Ninguna)
                {
                    Debug.LogWarning("⚠️ No tienes ninguna trampa seleccionada. Presiona 1, 2 o 3.");
                    return;
                }

                // Verificamos si la puerta está libre
                if (puerta.trampaActual == TipoTrampa.Ninguna)
                {
                    // Verificamos si realmente la tienes en el inventario (¿la crafteaste?)
                    if (InventoryManager.Instance != null && InventoryManager.Instance.HasTrampa(trampaSeleccionada))
                    {
                        puerta.InstalarTrampa(trampaSeleccionada);
                        InventoryManager.Instance.RemoveTrampa(trampaSeleccionada);
                        OnTrapInstalled?.Invoke(trampaSeleccionada, puerta);
                        Debug.Log($"✅ ¡Trampa de {trampaSeleccionada} instalada en la puerta con éxito!");
                    }
                    else
                    {
                        Debug.LogWarning($"❌ Intentaste poner una trampa de {trampaSeleccionada}, pero NO TIENES en el inventario. ¡Ve a la mesa de crafteo!");
                    }
                }
                else
                {
                    Debug.Log("⚠️ Ya hay una trampa instalada en esta puerta.");
                }
            }
        }
    }

    private void Move()
    {
        float finalSpeed = currentBaseSpeed * currentSpeedModifier;
        Vector3 targetVelocity = moveInput * finalSpeed;

        // Mantenemos la velocidad vertical actual (gravedad/caída) intacta
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }

    private void Rotate()
    {
        if (moveInput != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveInput);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }
    }

    // ===================================================================================
    //  SISTEMA PÚBLICO DE SALUD Y DAÑO
    // ===================================================================================

    public virtual void ReceiveDamage()
    {
        if (isDead) return;
        // The first hit teaches adrenaline; give the player its full escape window.
        if (TutorialPipelineManager.Instance != null && isAdrenalineActive) return;

        hitsReceived++;
        Debug.Log("¡Un enemigo feral te ha tocado! Golpes: " + hitsReceived + "/" + maxHitsAllowed);
        OnDamageReceived?.Invoke(hitsReceived, maxHitsAllowed);

        if (hitsReceived == 1)
        {
            StartCoroutine(AdrenalineRoutine());
            currentStamina = maxStamina; 
            if (staminaSlider != null) staminaSlider.value = currentStamina;
            OnAdrenalineTriggered?.Invoke();
        }
        else
        {
            currentSpeedModifier -= fatiguePenaltyPerHit;
            currentSpeedModifier = Mathf.Max(currentSpeedModifier, 0.5f); 
            Debug.Log("Pena de velocidad aplicada. Modificador permanente actual: " + (currentSpeedModifier * 100) + "%");
        }

        if (hitsReceived >= maxHitsAllowed)
        {
            Die();
        }
    }

    private IEnumerator AdrenalineRoutine()
    {
        isAdrenalineActive = true;
        currentSpeedModifier = adrenalineBoostModifier;
        Debug.Log("¡ADRENALINA! Velocidad aumentada temporalmente.");
        
        yield return new WaitForSeconds(adrenalineDuration);
        
        isAdrenalineActive = false;
        if (!isDead)
        {
            currentSpeedModifier = 1f;
            Debug.Log("La adrenalina se ha disipado.");
        }
    }

    protected virtual void Die()
    {
        isDead = true;
        Debug.LogError("¡HAS MUERTO! Game Over.");
        
        if (rb != null) rb.linearVelocity = Vector3.zero;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        OnPlayerDied?.Invoke();
    }

    /// <summary>
    /// Restaura la salud, estamina y posición del hámster (usado al dormir en refugio o reaparecer en checkpoint).
    /// </summary>
    public virtual void RespawnOrHeal(Vector3? newPosition = null)
    {
        StopAllCoroutines();
        isDead = false;
        isDashing = false;
        isExhausted = false;
        isAdrenalineActive = false;
        hitsReceived = 0;
        currentSpeedModifier = 1f;
        currentStamina = maxStamina;
        if (staminaSlider != null) staminaSlider.value = currentStamina;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        if (newPosition.HasValue)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.position = newPosition.Value;
            }
            transform.position = newPosition.Value;
        }
    }

    // ===================================================================================
    //  DETECCIÓN DE PROXIMIDAD (TRIGGERS)
    // ===================================================================================

    private void OnTriggerEnter(Collider other)
    {
        // Añadimos la detección del Tag "Door" para interactuar con las puertas
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
