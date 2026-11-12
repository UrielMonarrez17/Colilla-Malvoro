# Prototipo3: efectos de trampas

La corrección se limita a los efectos sobre enemigos y a la navegación de la barricada.
No modifica el grafo narrativo, las recetas, los materiales del proyecto ni la escena guardada.

## Comportamiento

- Púas: velocidad al 50 % durante 6 segundos. Patrulla, persecución y señuelo respetan el multiplicador; repetir la aplicación renueva la duración sin multiplicar otra vez la velocidad.
- Red: inmoviliza durante 5 segundos. Los pulsos del señuelo y el recálculo de rutas no la cancelan.
- Parálisis y red tienen prioridad sobre el desplazamiento. Los temporizadores de todos los efectos avanzan simultáneamente con el tiempo del juego, incluso cuando otro efecto tiene prioridad.
- Al terminar un efecto se retira su visual y se recuperan los valores que correspondan a los efectos que aún estén activos. Al desactivar al enemigo se limpian los efectos y se restablecen sus valores.
- Madera: barricada de 2.4 × 1.2 × 0.45 unidades durante 10 segundos. Colisionador y obstáculo de navegación comparten dimensiones. Las rutas se actualizan después de crear y retirar el obstáculo.
- Las puertas aplican los efectos especializados de Prototipo3. `EnemyAI` conserva sus efectos anteriores para los demás prototipos mediante un método virtual de aplicación.

El despliegue táctico conserva el señuelo secundario de las púas y el polen secundario de la red, utilizados por la lógica actual de misiones. El polen continúa 15 segundos y reduce la detección; no prolonga la inmovilización de la red.

## Validación reproducible

Desde PowerShell, en la raíz del proyecto:

```powershell
& .\Tools\RunTrapValidation.ps1
```

El script utiliza Unity 6000.5.9f1 por defecto (se puede indicar `-UnityExecutable`). Prepara una copia temporal con los scripts de producción, los recursos del tigre y paquetes locales en `Logs/TrapValidationProject`. No cierra el editor abierto ni guarda escenas del proyecto original.

La prueba `Prototipo3.Editor.TrapEffectValidation.Run` usa Play Mode, un NavMesh generado para la prueba, colisiones reales de puerta y el prefab `Tiger_Prototipo3`. Verifica cambios de estado, duración, reaplicaciones, efectos simultáneos, limpieza de visuales, restauración de detección, pausa, consumo de inventario y apertura de rutas al desaparecer la barricada.

Resultados: `Logs/prototipo3-trap-validation.txt`. Registro de Unity: `Logs/prototipo3-trap-validation-unity.log`.

Última ejecución: 5 de noviembre de 2026, Unity 6000.5.9f1. Compilación correcta, salida de Unity 0 y **100 comprobaciones aprobadas** (incluyen comprobaciones repetidas durante los efectos). La ejecución final no registró excepciones. En el primer arranque de la copia temporal falló la inicialización del índice de búsqueda del editor; la repetición con la copia inicializada completó todas las pruebas sin omitir errores del juego.

Estas pruebas verifican comportamiento y activación de visuales; una partida en `Prototipo3.unity` permite comprobar su legibilidad con la cámara y la iluminación del juego.
