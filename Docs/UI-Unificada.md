# UI unificada de FERAL

La presentación usa Canvas de Unity (uGUI), con fondos carbón, texto crema, acentos rojos y botones con borde de pincel compartidos con el menú principal. El acabado sigue las referencias de inventario y HUD: etiquetas de papel, selección dorada, navegación lateral y ranuras ilustradas.

## Pantallas y controles

- Menú principal: configuración de audio, partidas, ayuda y confirmación de salida comparten el tema. El hámster frontal y el seguimiento del cursor se conservan.
- **Tab:** mochila con materiales y trampas visibles juntos, cantidades, descripciones, navegación lateral y una vista del objeto seleccionado. Permite equipar una trampa disponible.
- **Esc:** pausa con continuar, mochila, misiones, volumen y retorno al menú con confirmación.
- HUD: retrato ilustrado, barras de vida y estamina reales, agotamiento/adrenalina/sigilo, tres ranuras ilustradas de trampas, reloj, objetivo y acción contextual.
- Tutorial: objetivo, instrucciones, fase y diario de pasos conectados al tutorial existente.
- Diálogos de Lulú: panel común con texto desplazable y confirmación mediante F, Espacio, Enter o botón.
- Mesa: recetas, costes, disponibilidad y cantidades. Usa el inventario y los eventos reales del juego; respeta los bloqueos del tutorial.
- **M:** diario de misiones, estados y progreso. Las misiones aún bloqueadas no se revelan.

Los menús detienen el tiempo y restauran el estado anterior del personaje y el cursor. La entrada que cierra una ventana no se propaga inmediatamente al personaje. El fin del tutorial espera si hay una ventana abierta.

## Edición

`Assets/Prototipo2/UI/Scripts/FeralTheme.cs` centraliza colores, tipografía y controles. `FeralGameUI.cs` construye el Canvas de juego al cargar Tutorial y Prototipo2. Los elementos se pueden inspeccionar en la jerarquía durante Play. Las pantallas están enlazadas a los componentes de inventario, crafteo, historia y tutorial existentes.

El menú permanece guardado en `Assets/Prototipo2/Scenes/MainMenu_Prototipo2.unity`. La opción **Feral > UI unificada > Aplicar estilo a submenús** actualiza su presentación y guarda capturas.

Las ilustraciones se guardan en `Assets/Resources/FeralUI/SurvivalAtlas.png`, generado con imagegen para esta interfaz. Las cuatro regiones del atlas contienen hámster, púas, red y trampa de madera; son imágenes decorativas y no alteran las recetas ni los modelos del juego.

## Verificación reproducible

Con la escena guardada y fuera de Play:

1. **Feral > UI unificada > Validar y capturar pantallas:** ejecuta comprobaciones aisladas con los componentes reales de inventario y mesa, más datos de muestra de historia/tutorial. Verifica recursos, equipamiento, pausa, diálogo y diario.
2. **Feral > UI unificada > Validar flujo de escenas:** entra al menú, carga Tutorial, omite la cinemática con su método normal, verifica el Canvas y ejecuta la transición final del tutorial hacia Prototipo2. No sustituye una partida manual completa de los 16 pasos.

Ambas pruebas restauran la escena abierta. Resultados y capturas: `Docs/Previews/UnifiedUI/`.

La sección Partidas conserva la funcionalidad existente; este cambio no implementa persistencia de partidas nuevas. La sesión se descarta al confirmar el regreso al menú.
