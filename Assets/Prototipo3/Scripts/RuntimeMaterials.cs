using UnityEngine;

namespace Prototipo3
{
    /// <summary>Material explícito para modelos que se crean sin prefab en tiempo de ejecución.</summary>
    public static class RuntimeMaterials
    {
        private static Material opaqueTemplate;

        public static Material CreateOpaque(Color color)
        {
            // Resources mantiene la referencia al material y al shader URP en el player.
            // CreatePrimitive no garantiza un material predeterminado válido fuera del editor.
            if (opaqueTemplate == null)
                opaqueTemplate = Resources.Load<Material>("Prototipo3Materials/GeneratedOpaque");
            if (opaqueTemplate == null)
                throw new System.InvalidOperationException("Falta Resources/Prototipo3Materials/GeneratedOpaque.mat");

            // Cada mecanismo puede cambiar de color sin modificar los demás ni el asset.
            return new Material(opaqueTemplate) { name = "GeneratedOpaque (Runtime)", color = color };
        }
    }
}
