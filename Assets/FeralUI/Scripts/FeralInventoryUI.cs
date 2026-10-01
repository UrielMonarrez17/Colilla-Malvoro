using System;
using UnityEngine;
using UnityEngine.UI;

namespace Feral.UI
{
    // Quantities are read from the existing gameplay inventory, never a parallel UI store.
    [DefaultExecutionOrder(100)]
    public sealed class FeralInventoryUI : MonoBehaviour
    {
        public bool openOnDirectPlay = true;
        static readonly string[] Descriptions = {
            "Tablas y fragmentos recuperados de la ciudad.\n\nMaterial requerido por las trampas 1 y 3.",
            "Piezas de metal recuperadas durante la exploración.\n\nMaterial requerido por la trampa 1.",
            "Restos que aún pueden aprovecharse.\n\nMaterial requerido por la trampa 2.",
            "Semillas reunidas durante la exploración.\n\nNo hay una receta con semillas en el prototipo actual.",
            "Fragmentos de goma reutilizables.\n\nMaterial requerido por la trampa 2."
        };
        InventoryManager inventory;
        HamsterController player;
        GameObject panel, confirmation, hud;
        Text total, detailTitle, detailBody, detailAmount, empty, status;
        Text[] amounts;
        Button[] cards;
        Image[] cardImages;
        Text[] recipes;
        ItemType selected;
        bool onlyAvailable, isOpen, wasPlayerEnabled, previousCursorVisible;
        CursorLockMode previousCursorLock;
        float previousTimeScale;

        void Start()
        {
            FeralUICommon.EnsureEventSystem();
            FeralUICommon.ApplyAudio();
            inventory = InventoryManager.Instance;
            if (inventory == null) inventory = new GameObject("Feral Inventory").AddComponent<InventoryManager>();
            player = FindAnyObjectByType<HamsterController>();
            panel = FeralUICommon.Find<RectTransform>(transform, "Inventory").gameObject;
            confirmation = FeralUICommon.Find<RectTransform>(transform, "ReturnConfirmation").gameObject;
            hud = FeralUICommon.Find<RectTransform>(transform, "InventoryHint").gameObject;
            total = FeralUICommon.Find<Text>(transform, "Total");
            detailTitle = FeralUICommon.Find<Text>(transform, "DetailTitle");
            detailBody = FeralUICommon.Find<Text>(transform, "DetailBody");
            detailAmount = FeralUICommon.Find<Text>(transform, "DetailAmount");
            empty = FeralUICommon.Find<Text>(transform, "EmptyState");
            status = FeralUICommon.Find<Text>(transform, "ReturnStatus");
            int count = Enum.GetValues(typeof(ItemType)).Length;
            amounts = new Text[count]; cards = new Button[count]; cardImages = new Image[count];
            for (int i = 0; i < count; i++)
            {
                var item = (ItemType)i;
                amounts[i] = FeralUICommon.Find<Text>(transform, "Count" + item);
                amounts[i].resizeTextForBestFit = true;
                amounts[i].resizeTextMinSize = 10;
                amounts[i].resizeTextMaxSize = 38;
                cards[i] = FeralUICommon.Find<Button>(transform, "Card" + item);
                cardImages[i] = cards[i].GetComponent<Image>();
                cards[i].onClick.AddListener(() => { selected = item; Refresh(); });
            }
            recipes = new Text[3];
            for (int i = 0; i < recipes.Length; i++) recipes[i] = FeralUICommon.Find<Text>(transform, "RecipeState" + i);
            FeralUICommon.Bind(transform, "CloseInventory", () => SetOpen(false));
            FeralUICommon.Bind(transform, "OpenInventory", () => SetOpen(true));
            FeralUICommon.Bind(transform, "FilterAll", () => Filter(false));
            FeralUICommon.Bind(transform, "FilterAvailable", () => Filter(true));
            FeralUICommon.Bind(transform, "ReturnMenu", () =>
            {
                SetConfirmation(true);
                FeralUICommon.Focus(confirmation.transform);
            });
            FeralUICommon.Bind(transform, "CancelReturn", CancelReturn);
            FeralUICommon.Bind(transform, "ConfirmReturn", ReturnMenu);
            SetConfirmation(false);
            inventory.Changed += Refresh;
            Refresh();
            bool initialOpen = openOnDirectPlay && !FeralUICommon.EnteringFromMenu;
            FeralUICommon.EnteringFromMenu = false;
            // The serialized panel is visible in the editor for direct visual review.
            panel.SetActive(false);
            SetOpen(initialOpen);
        }

        void Filter(bool available)
        {
            onlyAvailable = available;
            if (available && inventory.GetCount(selected) == 0)
                foreach (ItemType item in Enum.GetValues(typeof(ItemType)))
                    if (inventory.GetCount(item) > 0) { selected = item; break; }
            Refresh();
        }

        void Refresh()
        {
            if (onlyAvailable && inventory.GetCount(selected) == 0)
                foreach (ItemType item in Enum.GetValues(typeof(ItemType)))
                    if (inventory.GetCount(item) > 0) { selected = item; break; }
            int sum = 0;
            for (int i = 0; i < amounts.Length; i++)
            {
                int count = inventory.GetCount((ItemType)i);
                sum += count;
                amounts[i].text = count.ToString("00");
                cards[i].gameObject.SetActive(!onlyAvailable || count > 0);
                cardImages[i].color = selected == (ItemType)i ? new Color32(74, 80, 66, 255) : new Color32(30, 43, 44, 255);
            }
            total.text = sum + (sum == 1 ? " MATERIAL REUNIDO" : " MATERIALES REUNIDOS");
            empty.gameObject.SetActive(sum == 0);
            empty.text = onlyAvailable ? "Todavía no tienes materiales.\nExplora y usa F cerca de un contenedor." : "Tu mochila está vacía. Busca contenedores en la ciudad.";
            bool showDetail = !onlyAvailable || sum > 0;
            detailTitle.text = showDetail ? selected.ToString() : "Sin materiales";
            detailBody.text = showDetail ? Descriptions[(int)selected] : "Los materiales que reúnas aparecerán aquí.\n\nSelecciona Todos para consultar el catálogo.";
            detailAmount.text = showDetail ? "EN TU MOCHILA  /  " + inventory.GetCount(selected) : "EN TU MOCHILA  /  0";
            SetRecipe(0, inventory.HasEnoughItem(ItemType.Madera, 1) && inventory.HasEnoughItem(ItemType.Metal, 1));
            SetRecipe(1, inventory.HasEnoughItem(ItemType.Goma, 1) && inventory.HasEnoughItem(ItemType.Desperdicios, 1));
            SetRecipe(2, inventory.HasEnoughItem(ItemType.Madera, 2));
            FeralUICommon.Find<Text>(transform, "AllLabel").text = onlyAvailable ? "Todos" : "[ Todos ]";
            FeralUICommon.Find<Text>(transform, "AvailableLabel").text = onlyAvailable ? "[ Disponibles ]" : "Disponibles";
        }

        void SetRecipe(int index, bool ready)
        {
            recipes[index].text = ready ? "Materiales completos" : "Faltan materiales";
            recipes[index].color = ready ? new Color32(156, 197, 145, 255) : new Color32(194, 170, 132, 255);
        }

        void SetOpen(bool value)
        {
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
            else if (!value && isOpen) RestoreGameplay();
            isOpen = value;
            panel.SetActive(value);
            hud.SetActive(!value);
            SetConfirmation(false);
            if (value) { Refresh(); FeralUICommon.Focus(panel.transform); }
        }

        void RestoreGameplay()
        {
            Time.timeScale = previousTimeScale;
            if (player != null) player.enabled = wasPlayerEnabled;
            Cursor.visible = previousCursorVisible;
            Cursor.lockState = previousCursorLock;
        }

        void CancelReturn()
        {
            SetConfirmation(false);
            FeralUICommon.Focus(panel.transform);
        }

        void SetConfirmation(bool visible)
        {
            confirmation.SetActive(visible);
            // A modal must also block keyboard navigation to controls behind it.
            foreach (var button in panel.GetComponentsInChildren<Button>(true))
                if (!button.transform.IsChildOf(confirmation.transform)) button.interactable = !visible;
        }

        void ReturnMenu()
        {
            if (!FeralUICommon.CanLoad(FeralUICommon.MenuScene))
            {
                status.text = "No se encontró el menú. Revisa las escenas del build.";
                return;
            }
            SetOpen(false);
            inventory.Clear();
            FeralUICommon.Load(FeralUICommon.MenuScene);
        }

        void Update()
        {
            if (panel == null) return;
            if (FeralUICommon.EscapePressed())
            {
                if (confirmation.activeSelf) CancelReturn();
                else SetOpen(!isOpen);
            }
            else if (FeralUICommon.InventoryPressed() && !confirmation.activeSelf) SetOpen(!isOpen);
        }

        void OnDisable()
        {
            if (inventory != null) inventory.Changed -= Refresh;
            if (isOpen) { RestoreGameplay(); isOpen = false; }
        }
    }
}
