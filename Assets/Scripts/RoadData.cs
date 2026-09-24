using UnityEngine;

    public class RoadData : MonoBehaviour
    {
        [Header("Ajuste de Orientación")]
        [Tooltip("Offset de rotación en Y si el modelo base no apunta por defecto hacia el Norte")]
        public float rotationOffset = 0f;
    }