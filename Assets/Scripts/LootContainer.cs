using System;
using System.Collections.Generic;
using UnityEngine;

public class LootContainer : MonoBehaviour
{
    [Header("Configuración del Loot")]
    [Tooltip("Añade aquí los ítems y qué porcentaje tienen de salir.")]
    public List<LootDrop> posiblesLoot = new List<LootDrop>();

    [Header("Modo Tutorial / Drops Garantizados")]
    [Tooltip("Si tiene elementos, entregará TODOS estos ítems garantizados al saquear (ej. Madera + Metal en el tutorial).")]
    public List<ItemType> itemsGarantizados = new List<ItemType>();

    [Tooltip("Si está activo, el contenedor pulsa con un resplandor dorado hasta ser saqueado.")]
    public bool esContenedorResplandeciente = false;

    [Tooltip("Puerta o barrera opcional que se abrirá/desactivará al saquear este contenedor.")]
    public GameObject puertaDesbloqueable;

    private bool yaFueSaqueado = false;
    public bool YaFueSaqueado => yaFueSaqueado;

    public static event Action<LootContainer, List<ItemType>> OnContainerLooted;

    private Renderer[] renderers;
    private Light glowLight;
    private Color colorBaseOriginal = new Color(0.85f, 0.68f, 0.15f);

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0 && renderers[0].material != null)
        {
            colorBaseOriginal = renderers[0].material.color;
        }

        if (esContenedorResplandeciente && !yaFueSaqueado)
        {
            ConfigurarEfectoResplandor();
        }
    }

    void Update()
    {
        if (esContenedorResplandeciente && !yaFueSaqueado)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
            Color glowColor = Color.Lerp(new Color(0.95f, 0.75f, 0.15f), new Color(1f, 0.95f, 0.45f), pulse);

            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null && renderers[i].material != null)
                    {
                        renderers[i].material.color = glowColor;
                    }
                }
            }

            if (glowLight != null)
            {
                glowLight.intensity = Mathf.Lerp(1.8f, 4.2f, pulse);
            }
        }
    }

    public void ConfigurarEfectoResplandor()
    {
        esContenedorResplandeciente = true;
        if (glowLight == null)
        {
            GameObject lightObj = new GameObject("GlowLight_Loot");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            glowLight = lightObj.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(1f, 0.85f, 0.25f);
            glowLight.range = 6f;
            glowLight.intensity = 3f;
        }
    }

    // Esta función la llamaremos desde tu HamsterController cuando presiones la 'F'
    public void InteractuarConLoot()
    {
        if (yaFueSaqueado)
        {
            Debug.Log("Este contenedor ya está vacío.");
            return;
        }

        List<ItemType> obtenidos = new List<ItemType>();

        if (itemsGarantizados != null && itemsGarantizados.Count > 0)
        {
            foreach (ItemType item in itemsGarantizados)
            {
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.AddItem(item, 1);
                }
                obtenidos.Add(item);
            }
        }
        else
        {
            ItemType itemObtenido = CalcularDrop();
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(itemObtenido, 1);
            }
            obtenidos.Add(itemObtenido);
        }

        yaFueSaqueado = true;

        // Apagamos la luz de resplandor si existía
        if (glowLight != null)
        {
            glowLight.enabled = false;
        }

        // Cambiamos el color a gris para dar retroalimentación visual de que ya fue saqueado
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }
        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    renderers[i].material.color = Color.gray;
                }
            }
        }

        // Si este contenedor desbloquea una puerta o zona de control, abrirla
        if (puertaDesbloqueable != null)
        {
            DoorScript.Door doorComp = puertaDesbloqueable.GetComponentInChildren<DoorScript.Door>();
            if (doorComp != null && !doorComp.open)
            {
                doorComp.OpenDoor();
            }
            else
            {
                puertaDesbloqueable.SetActive(false);
            }
        }

        OnContainerLooted?.Invoke(this, obtenidos);
    }

    private ItemType CalcularDrop()
    {
        if (posiblesLoot == null || posiblesLoot.Count == 0)
        {
            return ItemType.Desperdicios;
        }

        // 1. Sumamos todos los porcentajes (Idealmente deberían sumar 100, pero esto evita errores si suman 90 o 110)
        float totalPeso = 0f;
        foreach (LootDrop loot in posiblesLoot)
        {
            totalPeso += loot.dropChance;
        }

        // 2. Tiramos un dado virtual entre 0 y el peso total
        float numeroAleatorio = UnityEngine.Random.Range(0f, totalPeso);

        // 3. Revisamos en qué "rango" cayó el dado
        foreach (LootDrop loot in posiblesLoot)
        {
            if (numeroAleatorio <= loot.dropChance)
            {
                return loot.item; // ¡Encontramos el ítem ganador!
            }
            // Si no fue este, le restamos su probabilidad al número y pasamos al siguiente
            numeroAleatorio -= loot.dropChance;
        }

        // Por seguridad, si algo falla (que no debería), regresamos basura por defecto
        return ItemType.Desperdicios;
    }
}
