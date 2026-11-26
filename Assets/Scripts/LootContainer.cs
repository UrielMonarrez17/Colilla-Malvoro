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
    public bool isLooted => yaFueSaqueado;

    [Header("Inyección de Gramática / Misiones")]
    public string containerTagFilter = "recurso";
    public string injectedNoteId = "";
    public string injectedNoteText = "";
    public string injectedKeyItemId = "";

    public static event Action<LootContainer, List<ItemType>> OnContainerLooted;
    public static event Action<LootContainer> OnContainerAlreadyLooted;
    public static event Action<LootContainer, string, string> OnNoteFound;

    public static string FormatLootSummary(List<ItemType> items)
    {
        if (items == null || items.Count == 0) return "Nada";
        var counts = new Dictionary<ItemType, int>();
        foreach (var item in items)
        {
            if (!counts.ContainsKey(item)) counts[item] = 0;
            counts[item]++;
        }
        var parts = new List<string>();
        foreach (var kvp in counts)
        {
            parts.Add($"+{kvp.Value} {kvp.Key}");
        }
        return string.Join(", ", parts);
    }

    private Renderer[] renderers;
    private readonly List<Material> cachedMaterials = new List<Material>();
    private Light glowLight;
    private Color colorBaseOriginal = new Color(0.85f, 0.68f, 0.15f);
    private static AudioClip cachedPickupClip;

    void Start()
    {
        CacheRenderMaterials();
        if (cachedMaterials.Count > 0 && cachedMaterials[0] != null)
        {
            colorBaseOriginal = cachedMaterials[0].HasProperty("_BaseColor")
                ? cachedMaterials[0].GetColor("_BaseColor")
                : cachedMaterials[0].color;
        }

        if (esContenedorResplandeciente && !yaFueSaqueado)
        {
            ConfigurarEfectoResplandor();
        }
    }

    private void CacheRenderMaterials()
    {
        cachedMaterials.Clear();
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || r is ParticleSystemRenderer || r is SpriteRenderer) continue;
            if (r.GetComponent<TMPro.TMP_Text>() != null || r.GetComponent<TextMesh>() != null) continue;
            Material[] mats = r.materials;
            if (mats == null) continue;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] != null) cachedMaterials.Add(mats[j]);
            }
        }
    }

    private void ApplyColorToCachedMaterials(Color col)
    {
        for (int i = 0; i < cachedMaterials.Count; i++)
        {
            Material m = cachedMaterials[i];
            if (m == null) continue;
            m.color = col;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            if (m.HasProperty("_Color")) m.SetColor("_Color", col);
        }
    }

    void Update()
    {
        if (esContenedorResplandeciente && !yaFueSaqueado)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
            Color glowColor = Color.Lerp(new Color(0.95f, 0.78f, 0.25f), new Color(1f, 0.96f, 0.55f), pulse);
            ApplyColorToCachedMaterials(glowColor);

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
            OnContainerAlreadyLooted?.Invoke(this);
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
        if (cachedMaterials.Count == 0)
        {
            CacheRenderMaterials();
        }
        ApplyColorToCachedMaterials(new Color(0.45f, 0.45f, 0.45f, 1f));

        // Retroalimentación clara al recoger objetos: texto flotante 3D + partículas + sonido
        SpawnPickupFeedback(FormatLootSummary(obtenidos));

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

        if (!string.IsNullOrEmpty(injectedNoteText))
        {
            OnNoteFound?.Invoke(this, injectedNoteId, injectedNoteText);
            if (Prototipo3.UI.FeralGameUI.Instance != null)
            {
                Prototipo3.UI.FeralGameUI.Instance.ShowDialogue("Nota Encontrada", injectedNoteText);
            }
        }

        OnContainerLooted?.Invoke(this, obtenidos);
    }

    private void SpawnPickupFeedback(string summary)
    {
        SpawnFloatingText3D(transform.position + Vector3.up * 1.15f, summary);
        SpawnPickupParticles(transform.position + Vector3.up * 0.55f);
        PlayPickupChime(transform.position);
    }

    private static void SpawnFloatingText3D(Vector3 worldPos, string text)
    {
        GameObject go = new GameObject("LootPopupText3D");
        go.transform.position = worldPos;
        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 36;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = new Color(1f, 0.92f, 0.35f, 1f);
        go.AddComponent<LootFloatingPopupBehavior>();
    }

    private static void SpawnPickupParticles(Vector3 worldPos)
    {
        GameObject fx = new GameObject("LootPickupSparkles");
        fx.transform.position = worldPos;
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.65f;
        main.loop = false;
        main.startLifetime = 0.65f;
        main.startSpeed = 3.2f;
        main.startSize = 0.16f;
        main.gravityModifier = 0.35f;
        main.startColor = new Color(1f, 0.88f, 0.28f, 1f);
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        ParticleSystemRenderer pr = ps.GetComponent<ParticleSystemRenderer>();
        if (pr != null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (s != null)
            {
                Material m = new Material(s);
                m.color = new Color(1f, 0.9f, 0.35f, 1f);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(1f, 0.9f, 0.35f, 1f));
                pr.sharedMaterial = m;
            }
        }

        ps.Play();
        Destroy(fx, 1.5f);
    }

    private static void PlayPickupChime(Vector3 worldPos)
    {
        if (cachedPickupClip == null)
        {
            const int sampleRate = 22050;
            int samples = sampleRate / 4;
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = i < samples / 2 ? 660f : 880f;
                float env = 1f - (float)i / samples;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.22f;
            }
            cachedPickupClip = AudioClip.Create("LootPickupChime", samples, 1, sampleRate, false);
            cachedPickupClip.SetData(data, 0);
        }

        GameObject audioGo = new GameObject("LootPickupAudio");
        audioGo.transform.position = worldPos;
        AudioSource src = audioGo.AddComponent<AudioSource>();
        src.clip = cachedPickupClip;
        src.spatialBlend = 0f;
        src.volume = 0.45f;
        src.Play();
        Destroy(audioGo, 0.4f);
    }

    private sealed class LootFloatingPopupBehavior : MonoBehaviour
    {
        private float elapsed = 0f;
        private TextMesh tm;

        private void Awake()
        {
            tm = GetComponent<TextMesh>();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            transform.position += Vector3.up * (1.15f * Time.deltaTime);
            Camera cam = Camera.main;
            if (cam != null)
            {
                transform.rotation = cam.transform.rotation;
            }
            if (tm != null)
            {
                Color c = tm.color;
                c.a = Mathf.Clamp01(1f - (elapsed / 1.4f));
                tm.color = c;
            }
            if (elapsed >= 1.4f)
            {
                Destroy(gameObject);
            }
        }
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
