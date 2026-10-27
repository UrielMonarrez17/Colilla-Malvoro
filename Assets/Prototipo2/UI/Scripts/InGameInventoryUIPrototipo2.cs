using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Prototipo2.UI
{
    [DefaultExecutionOrder(100)]
    public class InGameInventoryUIPrototipo2 : MonoBehaviour
    {
        [Header("Configuración")]
        public bool openOnDirectPlay = false;
        public string mainMenuSceneName = "Assets/Prototipo2/Scenes/MainMenu_Prototipo2.unity";

        private static readonly string[] Descriptions = {
            "Tablas y fragmentos recuperados de la ciudad.\n\nMaterial requerido por las trampas 1 (Púas) y 3 (Madera).",
            "Piezas de metal recuperadas durante la exploración.\n\nMaterial requerido por la trampa 1 (Púas).",
            "Restos que aún pueden aprovecharse.\n\nMaterial requerido por la trampa 2 (Red).",
            "Semillas reunidas durante la exploración urbana.\n\nAlimento y distracción animal.",
            "Fragmentos de goma reutilizables.\n\nMaterial requerido por la trampa 2 (Red)."
        };

        private InventoryManager inventory;
        private HamsterController player;
        private GameObject panel;
        private GameObject confirmation;
        private GameObject hud;
        private Text total;
        private Text detailTitle;
        private Text detailBody;
        private Text detailAmount;
        private Text empty;
        private Text status;
        private Text[] amounts;
        private Button[] cards;
        private Image[] cardImages;
        private Text[] recipes;
        private ItemType selected = ItemType.Madera;
        private bool onlyAvailable = false;
        private bool isOpen = false;
        private bool wasPlayerEnabled = true;
        private bool previousCursorVisible;
        private CursorLockMode previousCursorLock;
        private float previousTimeScale = 1f;

        private void Start()
        {
            MainMenuControllerPrototipo2.ApplySavedAudio();
            inventory = InventoryManager.Instance;
            if (inventory == null)
            {
                inventory = new GameObject("InventoryManager").AddComponent<InventoryManager>();
            }

            player = FindAnyObjectByType<HamsterController>();

            Transform panelTrans = FindChildRecursive(transform, "Inventory");
            if (panelTrans != null) panel = panelTrans.gameObject;

            Transform confirmTrans = FindChildRecursive(transform, "ReturnConfirmation");
            if (confirmTrans != null) confirmation = confirmTrans.gameObject;

            Transform hudTrans = FindChildRecursive(transform, "InventoryHint");
            if (hudTrans != null) hud = hudTrans.gameObject;

            total = FindComponentRecursive<Text>(transform, "Total");
            detailTitle = FindComponentRecursive<Text>(transform, "DetailTitle");
            detailBody = FindComponentRecursive<Text>(transform, "DetailBody");
            detailAmount = FindComponentRecursive<Text>(transform, "DetailAmount");
            empty = FindComponentRecursive<Text>(transform, "EmptyState");
            status = FindComponentRecursive<Text>(transform, "ReturnStatus");

            int count = Enum.GetValues(typeof(ItemType)).Length;
            amounts = new Text[count];
            cards = new Button[count];
            cardImages = new Image[count];

            for (int i = 0; i < count; i++)
            {
                var item = (ItemType)i;
                amounts[i] = FindComponentRecursive<Text>(transform, "Count" + item);
                if (amounts[i] != null)
                {
                    amounts[i].resizeTextForBestFit = true;
                    amounts[i].resizeTextMinSize = 10;
                    amounts[i].resizeTextMaxSize = 38;
                }

                cards[i] = FindComponentRecursive<Button>(transform, "Card" + item);
                if (cards[i] != null)
                {
                    cardImages[i] = cards[i].GetComponent<Image>();
                    cards[i].onClick.AddListener(() => { selected = item; RefreshUI(); });
                }
            }

            recipes = new Text[3];
            for (int i = 0; i < recipes.Length; i++)
            {
                recipes[i] = FindComponentRecursive<Text>(transform, "RecipeState" + i);
            }

            BindButton("CloseInventory", () => SetOpen(false));
            BindButton("OpenInventory", () => SetOpen(true));
            BindButton("FilterAll", () => Filter(false));
            BindButton("FilterAvailable", () => Filter(true));
            BindButton("ReturnMenu", () => SetConfirmation(true));
            BindButton("CancelReturn", () => SetConfirmation(false));
            BindButton("ConfirmReturn", ReturnToMainMenu);

            SetConfirmation(false);

            if (inventory != null)
            {
                inventory.Changed += RefreshUI;
            }

            RefreshUI();

            if (panel != null)
            {
                panel.SetActive(false);
            }
            SetOpen(openOnDirectPlay);
        }

        private void Filter(bool available)
        {
            onlyAvailable = available;
            if (available && inventory != null && inventory.GetCount(selected) == 0)
            {
                foreach (ItemType item in Enum.GetValues(typeof(ItemType)))
                {
                    if (inventory.GetCount(item) > 0) { selected = item; break; }
                }
            }
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (inventory == null) return;

            if (onlyAvailable && inventory.GetCount(selected) == 0)
            {
                foreach (ItemType item in Enum.GetValues(typeof(ItemType)))
                {
                    if (inventory.GetCount(item) > 0) { selected = item; break; }
                }
            }

            int sum = 0;
            for (int i = 0; i < amounts.Length; i++)
            {
                int c = inventory.GetCount((ItemType)i);
                sum += c;
                if (amounts[i] != null) amounts[i].text = c.ToString("00");
                if (cards[i] != null)
                {
                    cards[i].gameObject.SetActive(!onlyAvailable || c > 0);
                    if (cardImages[i] != null)
                    {
                        cardImages[i].color = selected == (ItemType)i ? new Color32(74, 80, 66, 255) : new Color32(30, 43, 44, 255);
                    }
                }
            }

            if (total != null) total.text = sum + (sum == 1 ? " MATERIAL REUNIDO" : " MATERIALES REUNIDOS");
            if (empty != null)
            {
                empty.gameObject.SetActive(sum == 0);
                empty.text = onlyAvailable ? "Todavía no tienes materiales.\nExplora y usa [F] cerca de un contenedor." : "Tu mochila está vacía. Busca contenedores en la ciudad.";
            }

            bool showDetail = !onlyAvailable || sum > 0;
            if (detailTitle != null) detailTitle.text = showDetail ? selected.ToString() : "Sin materiales";
            if (detailBody != null)
            {
                int idx = (int)selected;
                detailBody.text = (showDetail && idx >= 0 && idx < Descriptions.Length) ? Descriptions[idx] : "Los materiales que reúnas aparecerán aquí.";
            }
            if (detailAmount != null)
            {
                detailAmount.text = showDetail ? "EN TU MOCHILA  /  " + inventory.GetCount(selected) : "EN TU MOCHILA  /  0";
            }

            SetRecipe(0, inventory.HasEnoughItem(ItemType.Madera, 1) && inventory.HasEnoughItem(ItemType.Metal, 1));
            SetRecipe(1, inventory.HasEnoughItem(ItemType.Goma, 1) && inventory.HasEnoughItem(ItemType.Desperdicios, 1));
            SetRecipe(2, inventory.HasEnoughItem(ItemType.Madera, 2));

            Text allLabel = FindComponentRecursive<Text>(transform, "AllLabel");
            if (allLabel != null) allLabel.text = onlyAvailable ? "Todos" : "[ Todos ]";

            Text availLabel = FindComponentRecursive<Text>(transform, "AvailableLabel");
            if (availLabel != null) availLabel.text = onlyAvailable ? "[ Disponibles ]" : "Disponibles";
        }

        private void SetRecipe(int index, bool ready)
        {
            if (index < 0 || index >= recipes.Length || recipes[index] == null) return;
            recipes[index].text = ready ? "Materiales listos" : "Faltan materiales";
            recipes[index].color = ready ? new Color32(156, 197, 145, 255) : new Color32(194, 170, 132, 255);
        }

        public void SetOpen(bool value)
        {
            if (player == null) player = FindAnyObjectByType<HamsterController>();

            if (value && !isOpen)
            {
                previousTimeScale = Time.timeScale;
                previousCursorVisible = Cursor.visible;
                previousCursorLock = Cursor.lockState;
                wasPlayerEnabled = player != null && player.enabled;
                if (player != null) player.enabled = false;
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (!value && isOpen)
            {
                RestoreGameplay();
            }

            isOpen = value;
            if (panel != null) panel.SetActive(value);
            if (hud != null) hud.SetActive(!value);
            SetConfirmation(false);

            if (value)
            {
                RefreshUI();
            }
        }

        private void RestoreGameplay()
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            if (player != null) player.enabled = wasPlayerEnabled;
            Cursor.visible = previousCursorVisible;
            Cursor.lockState = previousCursorLock;
        }

        private void SetConfirmation(bool visible)
        {
            if (confirmation != null) confirmation.SetActive(visible);
            if (panel != null)
            {
                foreach (var button in panel.GetComponentsInChildren<Button>(true))
                {
                    if (confirmation != null && !button.transform.IsChildOf(confirmation.transform))
                    {
                        button.interactable = !visible;
                    }
                }
            }
        }

        public void ReturnToMainMenu()
        {
            SetOpen(false);
            if (inventory != null) inventory.Clear();
            Time.timeScale = 1f;

            if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else if (Application.CanStreamedLevelBeLoaded("MainMenu_Prototipo2"))
            {
                SceneManager.LoadScene("MainMenu_Prototipo2");
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }

        private void Update()
        {
            if (panel == null) return;

            bool escapePressed = false;
            bool invPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                escapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;
                invPressed = Keyboard.current.iKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame;
            }
#else
            escapePressed = Input.GetKeyDown(KeyCode.Escape);
            invPressed = Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab);
#endif

            if (escapePressed)
            {
                if (confirmation != null && confirmation.activeSelf)
                {
                    SetConfirmation(false);
                }
                else
                {
                    SetOpen(!isOpen);
                }
            }
            else if (invPressed && (confirmation == null || !confirmation.activeSelf))
            {
                SetOpen(!isOpen);
            }
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Changed -= RefreshUI;
            if (isOpen)
            {
                RestoreGameplay();
                isOpen = false;
            }
        }

        private void BindButton(string name, Action action)
        {
            Button btn = FindComponentRecursive<Button>(transform, name);
            if (btn != null)
            {
                btn.onClick.AddListener(() => action());
            }
        }

        private Transform FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                    return child;
            }
            return null;
        }

        private T FindComponentRecursive<T>(Transform parent, string objectName) where T : Component
        {
            foreach (T comp in parent.GetComponentsInChildren<T>(true))
            {
                if (comp.name.Equals(objectName, StringComparison.OrdinalIgnoreCase))
                    return comp;
            }
            return null;
        }
    }
}
