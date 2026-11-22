using UnityEngine;

public class PunteroMision : MonoBehaviour
{
    public Transform objetivo;
    public float velocidadRotacion = 10f;

    [Header("Flotación / Balanceo")]
    public bool flotar = true;
    public float velocidadFlotacion = 3.5f;
    public float amplitudFlotacion = 3.0f;

    protected Vector3 posicionInicialLocal;

    protected virtual void Start()
    {
        posicionInicialLocal = transform.localPosition;
    }

    protected virtual void Update()
    {
        if (flotar)
        {
            float bob = Mathf.Sin(Time.time * velocidadFlotacion) * amplitudFlotacion;
            transform.localPosition = posicionInicialLocal + Vector3.up * bob;
        }

        if (objetivo == null) return;

        // Calculamos la dirección ignorando la altura (Y) para que la flecha no apunte al piso
        Vector3 direccion = objetivo.position - transform.position;
        direccion.y = 0; 

        if (direccion != Vector3.zero)
        {
            Quaternion rotacionDeseada = Quaternion.LookRotation(direccion);
            // Interpolación suave para que la flecha gire bonito
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, Time.deltaTime * velocidadRotacion);
        }
    }
}