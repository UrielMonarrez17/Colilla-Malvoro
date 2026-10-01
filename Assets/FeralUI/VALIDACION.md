# Validación de la UI de Feral

Validación funcional inicial: 27 de septiembre de 2026, sobre la copia de RodrigoScene. Unity 6000.5.9f1, Windows.

## Comprobaciones realizadas

- Compilación de los scripts con las referencias reales del proyecto y el compilador incluido con Unity.
- Importación y compilación en Unity en una copia temporal completa del proyecto, sin cerrar ni modificar la sesión abierta del usuario.
- Verificación en escenas de previsualización de los componentes de la UI, fuentes, texturas, botones y altura requerida por los textos. Resultado: `FERAL_UI_VALIDATION_OK`.
- Prueba automatizada en Play: Jugar abre Partidas; Nueva partida carga la copia; audio cambia el volumen real y permite silenciar/restaurar; las preferencias previas a la prueba se restauran.
- Prueba de inventario: añadir madera/metal actualiza cantidades y requisitos de recetas; consumir materiales actualiza ambas cosas; Disponibles oculta materiales en cero.
- Prueba de pausa: abrir bloquea el controlador y pausa el tiempo; cerrar restaura ambos; cancelar el retorno conserva materiales; confirmar vuelve al menú y vacía la sesión.
- La confirmación bloquea también los botones de fondo para la navegación por teclado, y cancelar los vuelve a habilitar.
- Apertura directa de la copia: la mochila empieza abierta, se puede cerrar y se restaura la simulación.
- Verificación estructural mediante `Tools/check_feral_ui.py`: los bloques originales y sus raíces se conservan íntegros; no hay IDs ni nombres nuevos duplicados; existen las referencias y enlaces de botones; las dos escenas están en el build y la anterior permanece.
- Revisión visual de las previsualizaciones de distribución del menú, audio e inventario, generadas desde las mismas coordenadas y recursos de la UI.

Resultado final de la prueba en Play: `FERAL_UI_RUNTIME_OK`.

## Alcance y limitaciones

Las pruebas de Unity se ejecutaron en batch con `-nographics`: verifican carga y comportamiento, pero **no son una comprobación visual por GPU ni una prueba manual del ratón/teclado**. Los eventos de botones se invocaron desde el verificador. No se creó ni probó un ejecutable standalone. Revisar el aspecto final en Game View y el build del equipo, especialmente en las resoluciones indicadas en README.

El proyecto de prueba mostró una excepción del indexador interno `UnityEditor.Search.SearchDatabase` durante el arranque. No interrumpió las comprobaciones de UI. La importación inicial de paquetes locales también reportó rutas largas de algunos recursos del Editor.

Al cargar la copia aparece el componente perdido de CityGenerator heredado de RodrigoScene. Su GUID y esquema antiguo se documentan en README. No pertenece a la nueva UI y se conserva para revisión. Esto limita la integración del escenario procedural, aunque los recorridos de UI se completaron.

Los logs completos están en `Temp/FeralUI/unity-validation.log` y `Temp/FeralUI/unity-runtime-final.log` (ignorados por Git). El verificador integrado permite repetir las comprobaciones; `ValidateRuntimeBatch` solo debe ejecutarse en una copia temporal, porque termina cerrando ese Editor.

## Cambio de escena del 1 de octubre de 2026

El destino de Nueva partida ahora es Assets/Scenes/UIReview/Game.unity, una copia de Prototipo1.unity con la jerarquía de inventario ya existente. Se conservaron íntegros los bloques y raíces originales de Prototipo1 y los objetos de la nueva UI. El menú, el build, el generador y el verificador apuntan a Game. La copia anterior de Rodrigo se conserva fuera del flujo activo.

Las pruebas en Play descritas arriba corresponden a la entrega anterior; no equivalen a una nueva prueba de Game en Play. Para este cambio se verifican estructura, referencias y configuración del destino mediante Tools/check_feral_ui.py.
