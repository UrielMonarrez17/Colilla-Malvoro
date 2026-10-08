using UnityEngine;
using UnityEngine.UI; 
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
    private float currentBaseSpeed;
    private float currentSpeedModifier = 1f; 
    private int hitsReceived = 0;
    private bool isExhausted = false; 
    private bool isDashing = false;
    private bool isDead = false;
    public bool IsDead => isDead;
    
    public bool isCrouching { get; private set; } 

    private float nextDashTime = 0f;
    private KeyCode interactKey = KeyCode.F;
    private GameObject currentInteractable; 

    private Rigidbody rb;
    private Vector3 moveInput;

    // ===================================================================================
    //  INICIALIZACIÓN (Start)
    // ===================================================================================

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; 
        rb.useGravity = true; 
        
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
        if (Input.GetKeyDown(KeyCode.Alpha1)) 
        { 
            trampaSeleccionada = TipoTrampa.Puas; 
            Debug.Log("👉 Trampa armada en las manos: PÚAS"); 
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) 
        { 
            trampaSeleccionada = TipoTrampa.Red; 
            Debug.Log("👉 Trampa armada en las manos: RED"); 
        }
        if (Input.GetKeyDown(KeyCode.Alpha3)) 
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
            // Reducimos visualmente la escala en Y para que se vea agachado
            transform.localScale = new Vector3(0.01f, 0.01f*0.5f, 0.01f); 
        }
        else
        {
            // Escala normal
            transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
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

        rb.linearVelocity = Vector3.zero; 
        rb.AddForce(moveInput * dashForce, ForceMode.Impulse);

        yield return new WaitForSeconds(dashDuration);
        
        rb.linearVelocity = Vector3.zero; 
        isDashing = false;
    }

    private void HandleInteraction()
    {
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
                    if (InventoryManager.Instance.HasTrampa(trampaSeleccionada))
                    {
                        puerta.InstalarTrampa(trampaSeleccionada);
                        InventoryManager.Instance.RemoveTrampa(trampaSeleccionada);
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
        Vector3 newPosition = rb.position + moveInput * finalSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
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

    public void ReceiveDamage()
    {
        if (isDead) return;

        hitsReceived++;
        Debug.Log("¡Un enemigo feral te ha tocado! Golpes: " + hitsReceived + "/" + maxHitsAllowed);

        if (hitsReceived == 1)
        {
            StartCoroutine(AdrenalineRoutine());
            currentStamina = maxStamina; 
            if (staminaSlider != null) staminaSlider.value = currentStamina;
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
        currentSpeedModifier = adrenalineBoostModifier;
        Debug.Log("¡ADRENALINA! Velocidad aumentada temporalmente.");
        
        yield return new WaitForSeconds(adrenalineDuration);
        
        if (!isDead)
        {
            currentSpeedModifier = 1f;
            Debug.Log("La adrenalina se ha disipado.");
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.LogError("¡HAS MUERTO! Game Over. (Aquí implementarás el reinicio de escena).");
        
        rb.linearVelocity = Vector3.zero;
        GetComponent<Collider>().enabled = false;
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
