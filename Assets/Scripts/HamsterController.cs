using UnityEngine;
using UnityEngine.UI; // <-- Necesario para el Slider de UI
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class HamsterController : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkSpeed = 8f;
    public float sprintSpeed = 13f;
    public float turnSpeed = 15f;
    private float currentSpeed;

    [Header("Estamina y UI")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 25f;
    public float staminaRegenRate = 15f;
    public float minStaminaToSprint = 20f; // Cuánta estamina necesita para salir del agotamiento
    public Slider staminaSlider; // Arrastra tu Slider aquí en el Inspector

    private bool isExhausted = false; // Variable para arreglar el bug de correr infinito

    [Header("Dash")]
    public float dashForce = 15f;
    public float dashCooldown = 1.5f;
    public float dashDuration = 0.2f;
    private float nextDashTime = 0f;
    private bool isDashing = false;

    [Header("Interacción")]
    public KeyCode interactKey = KeyCode.F;
    private GameObject currentInteractable;

    private Rigidbody rb;
    private Vector3 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        
        currentStamina = maxStamina;
        currentSpeed = walkSpeed;

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
    }

    void Update()
    {
        if (isDashing) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(horizontal, 0f, vertical).normalized;

        HandleStaminaAndSprint();
        HandleDash();
        HandleInteraction();
    }

    void FixedUpdate()
    {
        if (!isDashing)
        {
            Move();
            Rotate();
        }
    }

    private void HandleStaminaAndSprint()
    {
        // ARREGLO DEL BUG: Si llega a 0, se agota.
        if (currentStamina <= 0f)
        {
            isExhausted = true;
        }
        // Solo se quita el agotamiento si supera el mínimo requerido
        else if (currentStamina >= minStaminaToSprint)
        {
            isExhausted = false;
        }

        // Ya no puedes correr si estás agotado
        bool isTryingToSprint = Input.GetKey(KeyCode.LeftShift) && moveInput != Vector3.zero && !isExhausted;

        if (isTryingToSprint)
        {
            currentSpeed = sprintSpeed;
            currentStamina -= staminaDrainRate * Time.deltaTime;
        }
        else
        {
            currentSpeed = walkSpeed;
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
            }
        }

        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        // Actualizar la UI
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }
    }

    private void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextDashTime && moveInput != Vector3.zero)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;
        rb.AddForce(moveInput * dashForce, ForceMode.Impulse);
        yield return new WaitForSeconds(dashDuration);
        rb.linearVelocity = Vector3.zero; 
        isDashing = false;
    }

    private void Move()
    {
        Vector3 newPosition = rb.position + moveInput * currentSpeed * Time.fixedDeltaTime;
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

    private void HandleInteraction()
    {
        if (Input.GetKeyDown(interactKey) && currentInteractable != null)
        {
            // Primero intentamos ver si es una mesa de crafteo
            CraftingTable craftingTable = currentInteractable.GetComponent<CraftingTable>();
            if (craftingTable != null)
            {
                craftingTable.ToggleCraftingMenu();
                return; // Salimos para no seguir ejecutando código
            }

            // Si no fue mesa, intentamos ver si es un LootContainer (Basurero, arbusto, etc.)
            LootContainer lootContainer = currentInteractable.GetComponent<LootContainer>();
            if (lootContainer != null)
            {
                lootContainer.InteractuarConLoot();
                return;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            currentInteractable = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Interactable") && other.gameObject == currentInteractable)
        {
            // Si nos alejamos, cerramos el menú automáticamente si estaba abierto
            CraftingTable craftingTable = currentInteractable.GetComponent<CraftingTable>();
            if (craftingTable != null)
            {
                craftingTable.CloseMenu();
            }
            currentInteractable = null;
        }
    }
}