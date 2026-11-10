using UnityEngine;

public class PunteroMision : MonoBehaviour
{
    public Transform objetivo;
    public float velocidadRotacion = 10f;

    void Update()
    {
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