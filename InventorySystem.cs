using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// add your items here. Do not remove "noItem". It is needed for the code to work
public enum type
{
    apple,
    gem,
    sword,
    potionMana,
    potionHealth,
    noItem // do not remove
}

public enum tooltipAlignment
{
    Dynamic,
    BottomLeft,
    BottomRight,
    TopLeft,
    TopRight
}

namespace Inventory
{
    public class InventorySystem : MonoBehaviour
    {
        [Serializable]
        public struct Item
        {
            [Tooltip("Enum (set in inventorySystem file)")]
            public type type;
            [Tooltip("The image for the item")]
            public Sprite image;
            [Tooltip("Name of the item")]
            public string headline;
            [Tooltip("You can enter a description text here")]
            public string description;
            [Tooltip("Change the color of the headline")]
            public string headlineColorHEX;
            [Tooltip("Change the color of the description")]
            public string descriptionColorHEX;
            [Tooltip("Value of the Item (Currency)")]
            public float value;
        }

        [Serializable]
        public struct tooltips
        {
            [Tooltip("Enable or disable tooltips")]
            public bool Enable;
            [Tooltip("Changes the position of the tooltip. Dynamic will choose the corner opposite to the screen corner the item is closest to. This can prevent the tooltip from ranging outside the screen")]
            public tooltipAlignment TooltipPosition;
            [Tooltip("Time before the tooltip appears")]
            public float TimeBeforeTooltipAppears;
        }

        [Serializable]
        public struct dataStorage
        {
            [Tooltip("Enable or disable local storage of the items")]
            public bool SaveItems;
            [Tooltip("Name of the file the inventory items get saved to")]
            public string inventoryFileName;
            [Tooltip("Name of the file the storage items get saved to")]
            public string storageFileName;
        }

        public GameObject sellWindowPrefab;
        public GameObject lootWindowPrefab;
        public GameObject slotPrefab;
        public GameObject lootSlotPrefab;
        public GameObject currencyUI;

        [Tooltip("Set item specific information")]
        public Item[] Items;
        public tooltips Tooltips;
        public dataStorage DataStorage;

        Dictionary<type, int> inventory = new Dictionary<type, int>();
        [HideInInspector]
        public GameObject lootBox = null, sellBox = null, itemStorage = null;
        [HideInInspector]
        public Transform canvasOfInventory = null;
        InventoryUI inventoryUI = null;
        float currency = 0;
        [Tooltip("Enable or disable currency")]
        public bool EnableCurrency = false;
        Text currencyText = null;

        private void Awake()
        {
            Array itemList = Enum.GetValues(typeof(type));
            foreach (type item in itemList)
            {
                if (item != type.noItem)
                    inventory[item] = 0;
            }

            if (GameObject.FindGameObjectWithTag("ItemStorage"))
                itemStorage = GameObject.FindGameObjectWithTag("ItemStorage");
            else
                itemStorage = null;

            canvasOfInventory = transform.root.GetComponent<Canvas>().transform;
            inventoryUI = GetComponent<InventoryUI>();
            currencyText = transform.parent.Find("Currency").Find("Value").GetComponent<Text>();
            if (!EnableCurrency)
                currencyUI.SetActive(false);
        }

        private void Start()
        {
            if (DataStorage.SaveItems)
                load();
            inventoryUI.init();
        }

        public Item getItemFromType(type itemType)
        {
            Item information = new Item();
            foreach (Item item in Items)
            {
                if (item.type == itemType)
                    return item;
            }

            return information;
        }

        /************************** General **************************/

        /// <summary>
        /// Shows or hides the inventory
        /// </summary>
        public void toggleInventory()
        {
            transform.parent.GetComponent<Visibility>().toggleVisibility();
        }

        /// <summary>
        /// Save items
        /// </summary>
        public void save()
        {
            if (DataStorage.SaveItems)
            {
                SaveLoadManager.saveItems(GetComponent<InventoryUI>().occupiedSlots, DataStorage.inventoryFileName);
                if (itemStorage != null)
                    SaveLoadManager.saveItems(itemStorage.GetComponent<ItemBox>().occupied, DataStorage.storageFileName);
            }
        }

        /// <summary>
        /// Load items
        /// </summary>
        void load()
        {
            loadInventory();
            if (itemStorage != null)
                loadStorage();
        }

        /************************************************************************/

        /**************** Loot window - sell window - stroage *******************/

        /// <summary>
        /// Creates a sell window
        /// </summary>
        public void createSellWindow()
        {
            if (sellBox == null)
            {
                sellBox = Instantiate(sellWindowPrefab) as GameObject;
                sellBox.transform.SetParent(canvasOfInventory);
                sellBox.transform.localPosition = new Vector3(0, 0, 0);
            }
        }

        /// <summary>
        /// Creates a loot box
        /// </summary>
        public void createLootBox(Dictionary<type, int> loot)
        {
            if (lootBox == null)
            {
                lootBox = Instantiate(lootWindowPrefab) as GameObject;
                lootBox.transform.SetParent(canvasOfInventory);
                lootBox.transform.localPosition = new Vector3(0, 0, 0);

                GameObject slot = null;
                foreach (KeyValuePair<type, int> item in loot)
                {
                    GameObject slots = GameObject.FindGameObjectWithTag("LootItems");
                    slot = Instantiate(lootSlotPrefab) as GameObject;
                    slot.transform.SetParent(slots.transform);

                    slots.GetComponent<LootBox>().loot.Add(item.Key, item.Value);
                    slot.GetComponent<LootSlot>().updateSlot(item.Key, item.Value);
                }
            }
        }

        /// <summary>
        /// Toggle visibility of the storage
        /// </summary>
        public void toggleStorage()
        {
            itemStorage.transform.parent.gameObject.GetComponent<Visibility>().toggleVisibility();
        }

        /**********************************************************************/


        /************************ Add or remove items ************************/
        /// <summary>
        /// Add an item to the inventory
        /// </summary>
        public void addItem(type item, int quantity)
        {
            if (numberOfFreeSlots() > 0 || getQuantityOfItem(item) > 0)
            {
                inventory[item] += quantity;
                inventoryUI.updateUI();
            }
            else
                Debug.LogWarning("Could not add " + quantity + " " + item.ToString() + " because the inventory was full");
        }

        /// <summary>
        /// Remove an item form the inventory. An error occurs if you try to remove more items than available
        /// </summary>
        public void removeItem(type item, int quantity)
        {
            if (inventory[item] >= quantity)
                inventory[item] -= quantity;
            else
            {
                inventory[item] = 0;
                Debug.LogWarning("Could not remove " + quantity + " " + item.ToString() + " because there were only " + inventory[item] + " in the inventory, quantity set to 0");
            }

            inventoryUI.updateUI();
        }

        /// <summary>
        /// Add some items to the inventory
        /// </summary>
        public void addDictionary(Dictionary<type, int> otherInventory)
        {
            foreach (KeyValuePair<type, int> item in otherInventory)
            {
                if (getQuantityOfItem(item.Key) > 0)
                    inventory[item.Key] += item.Value;
            }

            int freeSlots = numberOfFreeSlots();
            foreach (KeyValuePair<type, int> item in otherInventory)
            {
                if (getQuantityOfItem(item.Key) <= 0)
                {
                    if (freeSlots > 0)
                    {
                        inventory[item.Key] += item.Value;
                        freeSlots--;
                    }
                    else
                        Debug.LogWarning("Could not add " + item.Value + " " + item.Key.ToString() + " because the inventory was full");
                }
            }

            inventoryUI.updateUI();
        }

        /// <summary>
        /// Remove some items from the inventory
        /// </summary>
        public void subtractDictionary(Dictionary<type, int> otherInventory)
        {
            foreach (KeyValuePair<type, int> item in otherInventory)
            {
                if (inventory[item.Key] >= item.Value)
                    inventory[item.Key] -= item.Value;
                else
                {
                    inventory[item.Key] = 0;
                    Debug.LogWarning("Could not remove " + item.Value + " " + item.Key.ToString() + " because there are only " + getQuantityOfItem(item.Key) + " in the inventory, quantity set to 0");
                }
            }
            inventoryUI.updateUI();
        }

        /**********************************************************************/

        /// <summary>
        /// Add Currency
        /// </summary>
        public void addCurrency(float value)
        {
            currency += value;
            currencyText.text = currency.ToString();
        }

        /// <summary>
        /// Remove Currency
        /// </summary>
        public void removeCurrency(float value)
        {
            if (currency >= value)
                currency += value;
            else
            {
                currency = 0;
                Debug.LogWarning("Could not remove " + value + " coins because there are only " + currency + ", currency set to 0");
            }

            currencyText.text = currency.ToString();
        }

        /// <summary>
        /// Returns the quantity of an item
        /// </summary>
        public int getQuantityOfItem(type item)
        {
            return inventory[item];
        }

        /// <summary>
        /// Returns a shallow copy of the inventory, you are better of not altering it directly
        /// </summary>
        public Dictionary<type, int> getInventory()
        {
            return inventory;
        }

        internal void moveItemsBackToInventory(GameObject objHoldingItemBoxScript)
        {
            addDictionary(objHoldingItemBoxScript.GetComponent<ItemBox>().items);
        }

        /// <summary>
        /// Returns the number of free inbentory slots
        /// </summary>
        public int numberOfFreeSlots()
        {
            return inventoryUI.getFreeInventorySlots();
        }

        /// <summary>
        /// Adds items to the inventory without updating the UI, needed for the code to work. Its not meant to be called from outside. Calling this fromm outside could produce bugs
        /// </summary>
        public void addItemWithoutUpdatingUI(type item, int quantity)
        {
            if (numberOfFreeSlots() > 0 || getQuantityOfItem(item) > 0)
            {
                inventory[item] += quantity;
                inventoryUI.updatePreviousInventory();
            }
            else
                Debug.LogWarning("Could not add " + quantity + " " + item.ToString() + " because the inventory was full");
        }

        /// <summary>
        /// Removes items form the inventory without updating the UI. Its not meant to be called from outside. Calling this fromm outside could produce bugs
        /// </summary>
        public void removeItemWithoutUpdatingUI(type item, int quantity)
        {
            if (inventory[item] >= quantity)
            {
                inventory[item] -= quantity;
                inventoryUI.updatePreviousInventory();
            }
            else
                Debug.LogWarning("Could not remove " + quantity + " " + item.ToString() + " because there were only " + inventory[item] + " in the inventory");
        }

        // Gets called when an intem gets used
        public void itemUsed(Item item)
        {
            Debug.Log(item.headline + " used");

            //switch (item.type)
            //{
            //    case type.potionHealth:

            //        break;
            //    case type.potionMana:

            //        break;
            //}
        }

        void OnApplicationQuit()
        {
            save();
        }

        void loadInventory()
        {
            List<ItemData> items = SaveLoadManager.loadItems(DataStorage.inventoryFileName);
            if (items != null)
            {
                int i = 0;
                foreach (ItemData itemData in items)
                {
                    ItemSlot slot = transform.GetChild(itemData.itemSlot).gameObject.GetComponent<ItemSlot>();
                    slot.updateSlot(itemData.item, itemData.quantity);
                    GetComponent<InventoryUI>().freeSlots.Remove(slot);
                    GetComponent<InventoryUI>().occupiedSlots.Add(slot);
                    addItemWithoutUpdatingUI(itemData.item, itemData.quantity);
                    i++;
                }
            }
        }

        void loadStorage()
        {
            List<ItemData> items = SaveLoadManager.loadItems(DataStorage.storageFileName);
            if (items != null)
            {
                int i = 0;
                ItemBox storage = itemStorage.GetComponent<ItemBox>();
                foreach (ItemData itemData in items)
                {
                    ItemSlot slot = itemStorage.transform.GetChild(itemData.itemSlot).gameObject.GetComponent<ItemSlot>();
                    slot.updateSlot(itemData.item, itemData.quantity);
                    storage.freeSlots.Remove(slot);
                    storage.occupied.Add(slot);
                    storage.itemDroppedIntoBox(itemData.item, itemData.quantity);
                    i++;
                }
            }
        }
    }
}
