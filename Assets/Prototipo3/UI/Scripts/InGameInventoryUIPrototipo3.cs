using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Prototipo3.UI
{
    [DefaultExecutionOrder(100)]
    public class InGameInventoryUIPrototipo3 : MonoBehaviour
    {
        [Header("Configuración")]
        public bool openOnDirectPlay = false;
        public string mainMenuSceneName = "Assets/Prototipo3/Scenes/MainMenu_Prototipo3.unity";

        private InventoryManager inventory;
        private Prototipo3.Player.HamsterControllerPrototipo3 player;
        private GameObject panel;
        private bool isOpen = false;

        private void Start()
        {
            inventory = FindFirstObjectByType<InventoryManager>();
            player = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleInventory();
            }
        }

        public void ToggleInventory()
        {
            isOpen = !isOpen;
            if (panel != null)
            {
                panel.SetActive(isOpen);
            }
        }
    }
}
