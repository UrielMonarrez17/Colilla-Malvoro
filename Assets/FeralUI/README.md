# Feral: menú e inventario para revisión

## Abrir y probar

Unity **6000.5.9f1**. Abrir `Assets/Scenes/UIReview/Feral_MainMenu.unity` y pulsar Play.
También hay accesos en **Feral > UI** en la barra de Unity.

- **Jugar / Partidas > Nueva partida** abre `Game` con inventario vacío.
- **Configuración de audio** controla `AudioListener.volume`, permite silenciar y restablecer. Las preferencias se conservan con PlayerPrefs. El proyecto todavía no contiene una mezcla musical para separar música y efectos.
- **Cómo jugar** explica las teclas reales del controlador.
- **Salir del juego** pide confirmación; en el Editor detiene Play y en un ejecutable cierra la aplicación.
- **I / Tab** abre o cierra la mochila; **Esc** vuelve o alterna la mochila. También hay botones para usar el ratón y navegación con flechas y Enter.
- Al abrir directamente `Game`, la mochila aparece abierta para facilitar su revisión. Entrando desde el menú aparece cerrada.
- La mochila pausa el tiempo, bloquea el controlador y libera el cursor. Al cerrar, restaura los estados anteriores.
- **Volver al menú** advierte que se descarta la sesión. Cancelar conserva los materiales.

Para revisar cantidades sin buscar contenedores: durante Play, usar **Feral > UI > Añadir materiales de prueba (Play)**. Añade tres unidades de cada material al inventario real. No se regalan materiales automáticamente al jugador. Las recetas se consultan en la mochila y se ejecutan en la mesa original.

## Qué se conserva

`Game.unity` es una copia de `Prototipo1.unity` con un Canvas adicional. Conserva todos los objetos, componentes, referencias y UI anteriores, incluida la mesa y la barra de estamina. Ninguna escena original se modifica ni elimina. La copia anterior `RodrigoScene_UIReview` también se conserva, fuera del flujo del menú y de la lista del build. `Feral_MainMenu` parte de la configuración de cámara de SampleScene y contiene exclusivamente el menú como contenido jugable.

La jerarquía uGUI está guardada en cada `.unity`: se pueden editar posiciones, colores, textos y tamaños en el Inspector sin ejecutar código generador. El Canvas escala desde 1600 × 900 con Expand para mantener todo visible en otras proporciones. Los paneles secundarios del menú están desactivados; activar uno para editarlo y desactivar Home. En ejecución el controlador gestiona su visibilidad.

## Relación con el GDD y el código

Documentos revisados: **Game Design Document Feral.pdf**, especialmente páginas impresas 7, 10 y 11; **One Pager Feral.pdf**. Son referencias de diseño, no instrucciones para cambiar otras partes del proyecto.

El juego combina exploración diurna, preparación y evasión nocturna. Los scripts propios contienen: movimiento físico y estamina (`HamsterController`), cámara de seguimiento, detección y persecución con NavMesh (`EnemyAI`), tiempo e iluminación (`TimeManager`, `DayNightCycle`), ciudad procedural (`CityGenerator`), contenedores con loot ponderado, inventario persistente entre escenas y recetas de prueba.

El inventario visual usa exactamente el catálogo implementado: madera, metal, desperdicios, semillas y goma. No se inventan límites de capacidad, peso, equipo, armas ni efectos de consumo. Muestra cantidades reales, selección de material, filtros Todos/Disponibles, estado vacío y los costos actuales de las tres trampas. No agrega una fabricación portátil.

El GDD deja pendiente el diseño visual final. La propuesta usa verde azulado oscuro, ámbar, iconos geométricos y una ciudad nocturna con un hámster pequeño. El arte de esta entrega es original y está en `Art`; no requiere paquetes externos.

## Integración

- `InventoryManager`: añade `GetCount`, `Changed` y `Clear`; notifica al recolectar/gastar, rechaza cantidades negativas y evita continuar Awake después de detectar un singleton duplicado. Conserva las APIs anteriores para loot y crafting.
- `FeralInventoryUI`: suscribe el evento del inventario existente; no mantiene una segunda copia de los materiales.
- `FeralInput`: adapta las lecturas del hámster y el atajo de inventario al nuevo Input System que ya estaba seleccionado. Mantiene fallback al sistema antiguo; no cambia ProjectSettings de entrada.
- `FeralMainMenu`: navegación, audio, comienzo de partida y salida.
- `FeralUICommon`: carga las escenas por ruta, crea un EventSystem si hace falta y configura el foco inicial.
- `FeralUIReviewTools`: accesos de Editor, materiales de prueba, preparación del build y validación.

## Límites actuales

El guardado/carga de partidas está pendiente en el GDD y en el juego. Partidas muestra ese estado con claridad y permite iniciar una nueva sesión; **no simula partidas guardadas**. Los materiales de una sesión no se guardan al cerrar la aplicación. PlayerPrefs solo guarda audio.

Las recetas originales descuentan materiales y escriben en consola, pero no crean trampas ni efectos. La mochila muestra requisitos, sin presentar esos objetos como implementados. La navegación de enemigos, la ciudad, la derrota y el ciclo día/noche conservan el comportamiento del prototipo; la entrega no resuelve sus pendientes previos.

En `RodrigoScene` ya hay un componente perdido en el objeto **CityGenerator**: referencia el GUID `9d60997c78695c14599e6b6d86dc76ee`, mientras el script actual tiene GUID `a90ab16ad59e4bf3a0704cab97dc6e3d` y otros campos serializados. La copia anterior RodrigoScene_UIReview conserva ese componente para revisión; ya no es el destino del menú. Game parte de Prototipo1, que referencia el generador actual. No basta con cambiar el GUID: los prefabs antiguos están vacíos y el esquema cambió. Este hallazgo corresponde a la escena de Rodrigo y no se trasladó a Game.

## Build y entrega al equipo

Las escenas de revisión se agregan al principio de `EditorBuildSettings`; la escena RodrigoScene anterior se conserva. Si un Build Profile tiene una lista de escenas propia, incluir primero Feral_MainMenu y después Game. **Feral > UI > Preparar escenas para build de revisión** permite reconstruir la lista compartida conservando las demás entradas.

Subir juntos `Assets/FeralUI` y su meta, `Assets/Scenes/UIReview` y su meta, `Assets/Scripts/FeralInput.cs` y su meta, los cambios de `HamsterController.cs` e `InventoryManager.cs`, `ProjectSettings/EditorBuildSettings.asset` y `Tools`. No subir Library, Temp, Logs ni preferencias locales. Ya había cambios locales en otros ajustes al comenzar: revisarlos aparte antes del commit. No se hizo commit ni push.

## Comprobación

**Feral > UI > Validar escenas** verifica los scripts, fuentes, texturas, botones y altura disponible de los textos en escenas de previsualización sin reemplazar la escena abierta. `ValidateRuntimeBatch` está pensado únicamente para una copia temporal: entra en Play, ejercita navegación, carga, cantidades, consumo, recetas, filtros, pausa y retorno, y cierra el Editor de esa copia.

Revisión manual recomendada: 1920 × 1080, 1280 × 720 y 1024 × 768; probar ratón y teclado; ajustar audio y reabrir; abrir/cerrar la mochila caminando; añadir materiales y consumirlos en la mesa; cancelar y confirmar el retorno al menú. El registro de la comprobación realizada acompaña esta entrega en `VALIDACION.md`.

`Tools/build_feral_ui.py` reconstruye las dos escenas y su arte usando Python/Pillow. **Sobrescribe las escenas de revisión**: no ejecutarlo después de hacer cambios manuales que se quieran conservar. No es necesario para abrir el proyecto o hacer un build. Las previsualizaciones de distribución se escriben en Temp/FeralUI; no sustituyen una captura renderizada por Unity.
