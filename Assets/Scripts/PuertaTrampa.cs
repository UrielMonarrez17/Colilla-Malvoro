using System;
using UnityEngine;

public class PuertaTrampa : MonoBehaviour
{
    public TipoTrampa trampaActual = TipoTrampa.Ninguna;
    private Renderer[] renderers;
    private Color colorOriginal = Color.white;
    private GameObject trampaVisualObj;

    public static event Action<PuertaTrampa, TipoTrampa> OnTrapInstalledOnDoor;
    public static event Action<PuertaTrampa, TipoTrampa, EnemyAI> OnTrapTriggeredByEnemy;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();

        if (renderers != null && renderers.Length > 0 && renderers[0].material != null)
        {
            colorOriginal = renderers[0].material.color;
        }
        else
        {
            Debug.LogWarning($"El objeto {gameObject.name} no tiene un Renderer. Las trampas funcionarán, pero no cambiará de color.");
        }

        ActualizarVisualTrampa();
    }

    // Llamado por el hámster para instalar la trampa
    public void InstalarTrampa(TipoTrampa nuevaTrampa)
    {
        if (trampaActual != TipoTrampa.Ninguna && nuevaTrampa != TipoTrampa.Ninguna) return;

        trampaActual = nuevaTrampa;
        ActualizarVisualTrampa();

        if (trampaActual != TipoTrampa.Ninguna)
        {
            OnTrapInstalledOnDoor?.Invoke(this, trampaActual);
        }
    }

    public void LimpiarTrampa()
    {
        trampaActual = TipoTrampa.Ninguna;
        ActualizarVisualTrampa();
    }

    private void ActualizarVisualTrampa()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        Color targetColor = colorOriginal;
        if (trampaActual == TipoTrampa.Puas) targetColor = new Color(0.9f, 0.2f, 0.2f);
        else if (trampaActual == TipoTrampa.Red) targetColor = new Color(0.2f, 0.55f, 0.95f);
        else if (trampaActual == TipoTrampa.Madera) targetColor = new Color(0.6f, 0.35f, 0.1f);

        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    renderers[i].material.color = targetColor;
                }
            }
        }

        if (trampaVisualObj != null)
        {
            Destroy(trampaVisualObj);
            trampaVisualObj = null;
        }

        if (trampaActual != TipoTrampa.Ninguna)
        {
            CrearModelo3DTrampa(trampaActual);
        }
    }

    private void CrearModelo3DTrampa(TipoTrampa tipo)
    {
        trampaVisualObj = new GameObject("VisualTrampa_" + tipo);
        trampaVisualObj.transform.SetParent(transform, false);
        trampaVisualObj.transform.localPosition = Vector3.zero;

        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");

        if (tipo == TipoTrampa.Puas)
        {
            // Alfombra de púas metálicas en el piso de la entrada
            Material spikeMat = new Material(sh);
            spikeMat.color = new Color(0.75f, 0.15f, 0.15f);

            GameObject basePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(basePlate.GetComponent<Collider>());
            basePlate.transform.SetParent(trampaVisualObj.transform, false);
            basePlate.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            basePlate.transform.localScale = new Vector3(1.6f, 0.06f, 1.0f);
            basePlate.GetComponent<Renderer>().material = spikeMat;

            for (int x = -2; x <= 2; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Destroy(spike.GetComponent<Collider>());
                    spike.transform.SetParent(trampaVisualObj.transform, false);
                    spike.transform.localPosition = new Vector3(x * 0.3f, 0.22f, z * 0.3f);
                    spike.transform.localScale = new Vector3(0.1f, 0.18f, 0.1f);
                    spike.GetComponent<Renderer>().material = spikeMat;
                }
            }
        }
        else if (tipo == TipoTrampa.Red)
        {
            Material netMat = new Material(sh);
            netMat.color = new Color(0.2f, 0.65f, 0.95f);

            for (int i = -2; i <= 2; i++)
            {
                GameObject barH = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(barH.GetComponent<Collider>());
                barH.transform.SetParent(trampaVisualObj.transform, false);
                barH.transform.localPosition = new Vector3(0f, 0.8f + i * 0.25f, 0f);
                barH.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                barH.transform.localScale = new Vector3(0.05f, 0.8f, 0.05f);
                barH.GetComponent<Renderer>().material = netMat;
            }
        }
        else if (tipo == TipoTrampa.Madera)
        {
            Material woodMat = new Material(sh);
            woodMat.color = new Color(0.55f, 0.32f, 0.12f);

            for (int i = 0; i < 3; i++)
            {
                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(plank.GetComponent<Collider>());
                plank.transform.SetParent(trampaVisualObj.transform, false);
                plank.transform.localPosition = new Vector3(0f, 0.35f + i * 0.45f, 0f);
                plank.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 8f : -8f);
                plank.transform.localScale = new Vector3(1.6f, 0.18f, 0.12f);
                plank.GetComponent<Renderer>().material = woodMat;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (trampaActual == TipoTrampa.Ninguna) return;

        // Comprobamos tanto por Tag "Enemy" como por componente EnemyAI en el objeto o sus padres
        EnemyAI enemigo = other.GetComponentInParent<EnemyAI>();
        if (enemigo != null || other.CompareTag("Enemy"))
        {
            if (enemigo == null) enemigo = other.GetComponent<EnemyAI>();
            if (enemigo != null)
            {
                TipoTrampa trampaActivada = trampaActual;

                // Aplicamos el efecto al enemigo
                enemigo.RecibirTrampa(trampaActivada, transform.position);
                
                // Consumir la trampa
                trampaActual = TipoTrampa.Ninguna;
                ActualizarVisualTrampa();

                OnTrapTriggeredByEnemy?.Invoke(this, trampaActivada, enemigo);
            }
        }
    }
}
