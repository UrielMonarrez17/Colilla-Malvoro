using UnityEngine;

namespace Prototipo3
{
    /// <summary>
    /// Escucha el inicio de cada nuevo día en <see cref="TimeManager"/> para reiniciar los contenedores de loot.
    /// Desactivando este componente se deshabilita el reinicio diario del loot.
    /// </summary>
    public class DailyLootResetterPrototipo3 : MonoBehaviour
    {
        private TimeManager subscribedManager;

        private void OnEnable()
        {
            TryBindTimeManager();
        }

        private void Update()
        {
            if (subscribedManager != TimeManager.Instance)
            {
                TryBindTimeManager();
            }
        }

        private void OnDisable()
        {
            if (subscribedManager != null)
            {
                subscribedManager.OnDayStart -= HandleDayStart;
                subscribedManager = null;
            }
        }

        private void TryBindTimeManager()
        {
            if (subscribedManager != null)
            {
                subscribedManager.OnDayStart -= HandleDayStart;
                subscribedManager = null;
            }

            TimeManager current = TimeManager.Instance;
            if (current != null)
            {
                current.OnDayStart += HandleDayStart;
                subscribedManager = current;
            }
        }

        private void HandleDayStart()
        {
            if (!isActiveAndEnabled) return;
            LootContainer.ResetAllForNewDay();
        }
    }
}
