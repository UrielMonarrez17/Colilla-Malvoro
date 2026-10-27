using UnityEngine;

namespace Prototipo3.Narrative
{
    [SelectionBase]
    public class FiltroCarmesiItemPrototipo3 : MonoBehaviour
    {
        private bool collected = false;

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;

            var player = other.GetComponent<Prototipo3.Player.HamsterControllerPrototipo3>();
            if (player != null)
            {
                Collect(player);
            }
        }

        public void Collect(Prototipo3.Player.HamsterControllerPrototipo3 player)
        {
            if (collected) return;
            collected = true;

            player.AddFiltroCarmesi(1);
            Destroy(gameObject);
        }
    }
}
