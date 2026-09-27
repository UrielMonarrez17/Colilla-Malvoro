using UnityEngine;
using UnityEngine.UI; // Necesario para la UI (Slider de estamina)
using System.Collections; // Necesario para las Corrutinas (Dash y Adrenalina)

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))] // Aseguramos que tenga colisionador para los triggers
public class HamsterController : MonoBehaviour
{
    // ===================================================================================
    //  VARIABLES Y CONFIGURACIÓN
    // ===================================================================================

    [Header("Ajustes de Movimiento")]
    [Tooltip("Velocidad normal al caminar.")]
    public float walkSpeed = 8f;
    [Tooltip("Velocidad al esprintar.")]
    public float sprintSpeed = 13f;
    [Tooltip("Velocidad al estar agachado (sigilo).")]
    public float crouchSpeed = 4f;
    [Tooltip("Velocidad a la que el hámster gira.")]
    public float turnSpeed = 15f;

    [Header("Estamina (UI)")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 25f; // Pérdida por segundo
    public float staminaRegenRate = 15f;  // Regeneración por segundo
    public float minStaminaToSprint = 20f; // Mínimo para salir del agotamiento
    public Slider staminaSlider; // Arrastra tu Slider aquí en el Inspector

    [Header("Dash")]
    public float dashForce = 25f; // Fuerza del impulso
    public float dashCooldown = 1.5f;
    public float dashDuration = 0.15f; // Tiempo que toma control físico

    [Header("Salud y Efectos de Daño")]
    public int maxHitsAllowed = 3;
    public float adrenalineBoostModifier = 1.3f; // +30% velocidad
    public float adrenalineDuration = 3f; // Segundos de duración
    public float fatiguePenaltyPerHit = 0.1f; // -10% velocidad por golpe sucesivo

    // --- Variables de Estado (Privadas/Públicas para lectura de IA) ---
    private float currentBaseSpeed;
    private float currentSpeedModifier = 1f; // 1 = 100% (Normal)
    private int hitsReceived = 0;
    private bool isExhausted = false; // Fix del bug de sprint infinito
    private bool isDashing = false;
    private bool isDead = false;
    
    // Propiedad pública para que el EnemyAI lea si estamos agachados
    public bool isCrouching { get; private set; } 

    private float nextDashTime = 0f;
    private KeyCode interactKey = KeyCode.F;
    private GameObject currentInteractable; // Objeto cercano con tag "Interactable"

    // Componentes
    private Rigidbody rb;
    private Vector3 moveInput;

    // ===================================================================================
    //  INICIALIZACIÓN (Start)
    // ===================================================================================

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Configuraciones vitales del Rigidbody para el hámster
        rb.freezeRotation = true; // No queremos que ruede como pelota
        rb.useGravity = true; // Para que caiga si hay desnivel
        
        // Inicializar datos
        currentStamina = maxStamina;
        currentBaseSpeed = walkSpeed;

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
    }

    // ===================================================================================
    //  BUCLE DE ACTUALIZACIÓN (Update - Input y Lógica de tiempo)
    // ===================================================================================

    void Update()
    {
        if (isDead || isDashing) return; // Si estamos muertos o dasheando, no hacemos nada más.

        // 1. Capturar Input de movimiento (WASD/Joystick)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        // Normalizado para evitar mayor velocidad en diagonal
        moveInput = new Vector3(horizontal, 0f, vertical).normalized;

        // 2. Ejecutar Lógicas de mecánicas
        HandleStealthAndSpeedManagement(); // Gestiona sigilo, sprint y modificadores
        HandleDash(); // Gestiona trigger de Dash
        HandleInteraction(); // Gestiona trigger de Interacción ('F')
    }

    // ===================================================================================
    //  BUCLE DE FÍSICAS (FixedUpdate - Movimiento y Rotación Rigidbody)
    // ===================================================================================

    void FixedUpdate()
    {
        if (isDead || isDashing) return; // Las físicas no actúan si estamos muertos o dasheando.

        Move();
        Rotate();
    }

    // ===================================================================================
    //  LÓGICA DETALLADA DE MECÁNICAS
    // ===================================================================================

    /// <summary>
    /// Gestiona sigilo (C), Sprint (Shift), Agotamiento y calcula la velocidad base final.
    /// </summary>
    private void HandleStealthAndSpeedManagement()
    {
        // --- Mecánica de Sigilo (Crouch) ---
        isCrouching = Input.GetKey(KeyCode.C);
        
        if (isCrouching) 
        {
            // Reducimos visualmente la escala en Y para que se vea agachado
            transform.localScale = new Vector3(1f, 0.5f, 1f); 
        }
        else
        {
            // Escala normal
            transform.localScale = new Vector3(1f, 1f, 1f);
        }

        // --- Gestión de Estamina y Sprint (Fix Bug Infinito incluido) ---

        // Fix: Si llega a 0, entra en agotamiento total.
        if (currentStamina <= 0f) isExhausted = true;
        // Solo sale del agotamiento si recupera estamina suficiente
        else if (currentStamina >= minStaminaToSprint) isExhausted = false;

        // Reglas para sprintar: Mantener Shift, moverse, NO estar agotado, NO estar agachado
        bool isTryingToSprint = Input.GetKey(KeyCode.LeftShift) && moveInput != Vector3.zero && !isExhausted && !isCrouching;

        // Decidir velocidad base y consumo/regen de estamina
        if (isTryingToSprint)
        {
            currentBaseSpeed = sprintSpeed;
            currentStamina -= staminaDrainRate * Time.deltaTime;
        }
        else if (isCrouching)
        {
            currentBaseSpeed = crouchSpeed;
            // Regeneración normal al agacharse (Opcional: podrías hacerlo más lento si quisieras)
            if (currentStamina < maxStamina) currentStamina += staminaRegenRate * Time.deltaTime;
        }
        else
        {
            currentBaseSpeed = walkSpeed;
            // Regeneración normal al caminar o estar quieto
            if (currentStamina < maxStamina) currentStamina += staminaRegenRate * Time.deltaTime;
        }

        // Limitar estamina entre 0 y MAX, y actualizar UI
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        if (staminaSlider != null) staminaSlider.value = currentStamina;
    }

    /// <summary>
    /// Gestiona el trigger del Dash (Space) y cooldown.
    /// </summary>
    private void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextDashTime && moveInput != Vector3.zero && !isCrouching)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;

        // Aplicamos impulso físico instantáneo (AddForce en ForceMode.Impulse)
        rb.linearVelocity = Vector3.zero; // Frenamos inercia actual antes del impulso
        rb.AddForce(moveInput * dashForce, ForceMode.Impulse);

        yield return new WaitForSeconds(dashDuration);
        
        // Al terminar la duración, cortamos la velocidad para que no resbale interminablemente
        rb.linearVelocity = Vector3.zero; 
        isDashing = false;
    }

    /// <summary>
    /// Gestiona la interacción ('F') detectando qué componente tiene el interactuable cercano.
    /// </summary>
    private void HandleInteraction()
    {
        if (Input.GetKeyDown(interactKey) && currentInteractable != null)
        {
            // Usamos TryGetComponent para rendimiento y limpieza
            
            // Caso 1: Es una mesa de crafteo
            if (currentInteractable.TryGetComponent<CraftingTable>(out CraftingTable craftingTable))
            {
                craftingTable.ToggleCraftingMenu();
            }
            // Caso 2: Es un contenedor de Loot (basurero, etc.)
            else if (currentInteractable.TryGetComponent<LootContainer>(out LootContainer lootContainer))
            {
                lootContainer.InteractuarConLoot();
            }
        }
    }

    /// <summary>
    /// Aplica el movimiento físico calculado (Base Speed * Modificador de Adrenalina/Fatiga).
    /// </summary>
    private void Move()
    {
        // ¡Magia de la velocidad! Multiplicamos la base (walk, sprint o crouch) por el modificador activo.
        float finalSpeed = currentBaseSpeed * currentSpeedModifier;
        Vector3 newPosition = rb.position + moveInput * finalSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    /// <summary>
    /// Rota suavemente el hámster hacia donde moveInput apunte.
    /// </summary>
    private void Rotate()
    {
        if (moveInput != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveInput);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }
    }

    // ===================================================================================
    //  SISTEMA PÚBLICO DE SALUD Y DAÑO (RecibirDaño - Llamado por Enemigos)
    // ===================================================================================

    /// <summary>
    /// Función pública llamada por un enemigo cuando colisiona con el hámster.
    /// Maneja Adrenalina, Fatiga y Muerte.
    /// </summary>
    public void ReceiveDamage()
    {
        if (isDead) return;

        hitsReceived++;
        Debug.Log("¡Un enemigo feral te ha tocado! Golpes: " + hitsReceived + "/" + maxHitsAllowed);

        // Lógica según número de golpes
        if (hitsReceived == 1)
        {
            // PRIMER GOLPE: Boost de Adrenalina y Estamina al Máximo
            StartCoroutine(AdrenalineRoutine());
            currentStamina = maxStamina; 
            if (staminaSlider != null) staminaSlider.value = currentStamina;
        }
        else
        {
            // GOLPES SUCESIVOS: Penalización de fatiga (velocidad permanente)
            currentSpeedModifier -= fatiguePenaltyPerHit;
            // Límite de seguridad: Que no sea 0 (mínimo 50% de velocidad)
            currentSpeedModifier = Mathf.Max(currentSpeedModifier, 0.5f); 
            Debug.Log("Pena de velocidad aplicada. Modificador permanente actual: " + (currentSpeedModifier * 100) + "%");
        }

        // Comprobar Condición de Muerte
        if (hitsReceived >= maxHitsAllowed)
        {
            Die();
        }
    }

    private IEnumerator AdrenalineRoutine()
    {
        // Aplicamos el boost (ej: +30%) temporalmente
        currentSpeedModifier = adrenalineBoostModifier;
        Debug.Log("¡ADRENALINA! Velocidad aumentada temporalmente.");
        
        yield return new WaitForSeconds(adrenalineDuration);
        
        // Volvemos a la velocidad normal (100%) si no hemos muerto.
        // Nota: Si recibió el 2do golpe durante la adrenalina, al terminar volverá a 100%, 
        // y el 2do golpe luego aplicará su -10% permanentemente. 
        // Para simplificar, asumimos que tras Adrenalina vuelve a 1f si está vivo.
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
        
        // Congelamos al personaje
        rb.linearVelocity = Vector3.zero;
        // Desactivamos su colisionador para que el enemigo lo ignore
        GetComponent<Collider>().enabled = false;
        // Opcional: Podrías reproducir animación de muerte aquí
    }

    // ===================================================================================
    //  DETECCIÓN DE PROXIMIDAD (TRIGGERS)
    // ===================================================================================

    private void OnTriggerEnter(Collider other)
    {
        // Cuando entramos en el Trigger de un objeto con tag "Interactable"
        if (other.CompareTag("Interactable"))
        {
            currentInteractable = other.gameObject;
            // Opcional: Mostrar UI "Presiona F"
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Cuando nos alejamos
        if (other.CompareTag("Interactable") && other.gameObject == currentInteractable)
        {
            // Si alejamos de la mesa, forzamos cerrar menú si estaba abierto
            if (currentInteractable.TryGetComponent<CraftingTable>(out CraftingTable table))
            {
                table.CloseMenu();
            }
            
            currentInteractable = null;
            // Opcional: Ocultar UI "Presiona F"
        }
    }
}