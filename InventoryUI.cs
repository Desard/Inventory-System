using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        public GameObject tooltipPrefab;
        public GameObject itemDividerPrefab;
        public GameObject itemDestructionPrefab;
        public readonly List<ItemSlot> freeSlots = new List<ItemSlot>();
        public readonly List<ItemSlot> occupiedSlots = new List<ItemSlot>();
        Dictionary<type, int> currentInventory = new Dictionary<type, int>();
        Dictionary<type, int> previousInventory = new Dictionary<type, int>();

        void Awake()
        {
            ItemSlot[] startingSlotArray = GetComponentsInChildren<ItemSlot>(true);
            for (int i = 0; i < startingSlotArray.Length; i++)
            {
                freeSlots.Add(startingSlotArray[i]);
                startingSlotArray[i].inventoryPosition = i;
            }

            currentInventory = GetComponent<InventorySystem>().getInventory();
        }

        public void init()
        {
            updatePreviousInventory();
            updateUI();
        }

        public void swapSlots(ItemSlot oldSlot, ItemSlot newSlot)
        {
            List<ItemSlot> freeSlotsOld;
            List<ItemSlot> occupiedSlotsOld;
            bool oldSlotIsNotInventory = false;

            List<ItemSlot> freeSlotsNew;
            List<ItemSlot> occupiedSlotsNew;
            bool newSlotIsNotInventory = false;

            // Assign the the right slots
            if (oldSlot.transform.parent.GetComponent<ItemBox>())
            {
                freeSlotsOld = oldSlot.transform.parent.GetComponent<ItemBox>().freeSlots;
                occupiedSlotsOld = oldSlot.transform.parent.GetComponent<ItemBox>().occupied;
                oldSlotIsNotInventory = true;
            }
            else
            {
                freeSlotsOld = freeSlots;
                occupiedSlotsOld = occupiedSlots;
            }

            if (newSlot.transform.parent.GetComponent<ItemBox>())
            {
                freeSlotsNew = newSlot.transform.parent.GetComponent<ItemBox>().freeSlots;
                occupiedSlotsNew = newSlot.transform.parent.GetComponent<ItemBox>().occupied;
                newSlotIsNotInventory = true;
            }
            else
            {
                freeSlotsNew = freeSlots;
                occupiedSlotsNew = occupiedSlots;
            }

            // Merge the slots if the item from the old slot matches the item from the new slot
            if (oldSlot.item == newSlot.item && oldSlot != newSlot)
            {
                if (newSlotIsNotInventory && !oldSlotIsNotInventory)
                {
                    newSlot.transform.parent.GetComponent<ItemBox>().itemDroppedIntoBox(newSlot.item, oldSlot.numberOfItmes);
                    GetComponent<InventorySystem>().removeItemWithoutUpdatingUI(newSlot.item, oldSlot.numberOfItmes);
                }
                else if (!newSlotIsNotInventory && oldSlotIsNotInventory)
                {
                    oldSlot.transform.parent.GetComponent<ItemBox>().itemTakenFromBox(newSlot.item, oldSlot.numberOfItmes);
                    GetComponent<InventorySystem>().addItemWithoutUpdatingUI(newSlot.item, oldSlot.numberOfItmes);
                }

                newSlot.updateSlot(newSlot.numberOfItmes + oldSlot.numberOfItmes);
                freeSlotsOld.Add(oldSlot);
                occupiedSlotsOld.Remove(oldSlot);
                oldSlot.clearSlot();

            }
            else
            {
                type item = oldSlot.item;
                int numberOfItmes = oldSlot.numberOfItmes;

                // Swap slots
                oldSlot.updateSlot(newSlot.item, newSlot.numberOfItmes);
                newSlot.updateSlot(item, numberOfItmes);
                newSlot.transform.GetChild(0).GetChild(0).GetComponent<ToolTip>().itemsSwapped();

                // Remove the slots form the lists
                if (oldSlot.item == type.noItem && occupiedSlotsOld.Contains(oldSlot))
                {
                    occupiedSlotsOld.Remove(oldSlot);
                    freeSlotsOld.Add(oldSlot);
                }
                else if (!(oldSlot.item == type.noItem) && freeSlotsOld.Contains(oldSlot))
                {
                    freeSlotsOld.Remove(oldSlot);
                    occupiedSlotsOld.Add(oldSlot);
                }

                if (newSlot.item == type.noItem && occupiedSlotsNew.Contains(newSlot))
                {
                    occupiedSlotsNew.Remove(newSlot);
                    freeSlotsNew.Add(newSlot);
                }
                else if (!(newSlot.item == type.noItem) && freeSlotsNew.Contains(newSlot))
                {
                    freeSlotsNew.Remove(newSlot);
                    occupiedSlotsNew.Add(newSlot);
                }

                // move items from inventoy to box or vice versa
                if (oldSlotIsNotInventory && !newSlotIsNotInventory)
                {
                    oldSlot.transform.parent.GetComponent<ItemBox>().itemTakenFromBox(item, numberOfItmes);
                    GetComponent<InventorySystem>().addItemWithoutUpdatingUI(item, numberOfItmes);
                }
                else if (newSlotIsNotInventory && !oldSlotIsNotInventory)
                {
                    newSlot.transform.parent.GetComponent<ItemBox>().itemDroppedIntoBox(item, numberOfItmes);
                    GetComponent<InventorySystem>().removeItemWithoutUpdatingUI(item, numberOfItmes);
                }
            }

            // Sort the lists
            freeSlotsNew.Sort((x, y) => x.inventoryPosition.CompareTo(y.inventoryPosition));
            freeSlotsOld.Sort((x, y) => x.inventoryPosition.CompareTo(y.inventoryPosition));
        }

        public void itemDestructionRequest(ItemSlot slot)
        {
            GameObject itemDestruction = Instantiate(itemDestructionPrefab) as GameObject;
            itemDestruction.transform.SetParent(GameObject.FindGameObjectWithTag("Inventory").GetComponent<InventorySystem>().canvasOfInventory);
            itemDestruction.transform.localPosition = new Vector3(0, 0, 0);

            itemDestruction.transform.Find("ItemName").GetComponent<Text>().text = GetComponent<InventorySystem>().getItemFromType(slot.item).headline;
            itemDestruction.GetComponent<ItemDestruction>().setSlot(slot);
        }

        /************ Divide Stack ****************/

        public void itemRightClicked(GameObject obj)
        {
            ItemSlot slot = obj.transform.parent.parent.GetComponent<ItemSlot>();

            if (slot.numberOfItmes > 1 && freeSlots.Count > 0)
            {
                GameObject itemsDivider = Instantiate(itemDividerPrefab) as GameObject;
                itemsDivider.transform.SetParent(GameObject.FindGameObjectWithTag("Inventory").GetComponent<InventorySystem>().canvasOfInventory);
                itemsDivider.transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                itemsDivider.GetComponent<DivideItemStack>().setSlotToDivideItemsFrom(slot);
            }
        }

        public void divideStack(ItemSlot slot, int numberOfItems)
        {
            freeSlots.Sort((x, y) => x.inventoryPosition.CompareTo(y.inventoryPosition));
            slot.updateSlot(slot.numberOfItmes - numberOfItems);
            freeSlots.First().updateSlot(slot.item, numberOfItems);

            occupiedSlots.Add(freeSlots.First());
            freeSlots.Remove(freeSlots.First());
        }

        /*****************************************/

        public void updateUI()
        {
            // If there are occupied slots in the list
            if (occupiedSlots.Count > 0)
            {
                // Sort list
                occupiedSlots.Sort((x, y) => x.inventoryPosition.CompareTo(y.inventoryPosition));
                foreach (ItemSlot occupiedSlot in occupiedSlots)
                {
                    occupiedSlot.dirty = false;
                    // Iterate all items
                    foreach (KeyValuePair<type, int> item in currentInventory)
                    {
                        // If item matches slot item
                        if (occupiedSlot.item == item.Key)
                        {
                            if (occupiedSlot.numberOfItmes == 0)
                                occupiedSlot.dirty = true;

                            // If inventory is holding items of this type
                            if (item.Value > 0)
                            {
                                // Count all items of this type among all slots
                                int itemCountAmongAllSlots = 0;
                                foreach (ItemSlot slot in occupiedSlots)
                                {
                                    if (slot.item == occupiedSlot.item)
                                        itemCountAmongAllSlots += slot.numberOfItmes;
                                }

                                // Inventory is holding more items than all slots together, so items need to me added
                                if (itemCountAmongAllSlots < item.Value)
                                    occupiedSlot.updateSlot(occupiedSlot.numberOfItmes + (item.Value - itemCountAmongAllSlots));

                                // Inventory is holding less items than all slots together, so items need to me removed
                                else if (itemCountAmongAllSlots > item.Value)
                                {
                                    // If this slot is not holding enough items to correct the difference between the ui and the inventory set its value to 0
                                    if (occupiedSlot.numberOfItmes < itemCountAmongAllSlots - item.Value)
                                        occupiedSlot.dirty = true;
                                    // The slot has enough items to correct the ui
                                    else
                                    {
                                        occupiedSlot.updateSlot(occupiedSlot.numberOfItmes - (itemCountAmongAllSlots - item.Value));

                                        if (occupiedSlot.numberOfItmes == 0)
                                            occupiedSlot.dirty = true;
                                    }
                                }
                            }
                            else
                                occupiedSlot.dirty = true;
                            break;
                        }
                    }
                }

                for (int i = occupiedSlots.Count - 1; i >= 0; i--)
                {
                    if (occupiedSlots[i].dirty)
                    {
                        occupiedSlots[i].clearSlot();
                        freeSlots.Add(occupiedSlots[i]);
                        occupiedSlots.Remove(occupiedSlots[i]);
                    }
                }
            }

            foreach (KeyValuePair<type, int> item in currentInventory)
            {
                if (!previousInventory.ContainsKey(item.Key) || (previousInventory[item.Key] == 0 && currentInventory[item.Key] > 0))
                {
                    freeSlots.Sort((x, y) => x.inventoryPosition.CompareTo(y.inventoryPosition));
                    freeSlots.First().updateSlot(item.Key, item.Value);
                    occupiedSlots.Add(freeSlots.First());
                    freeSlots.Remove(freeSlots.First());
                }
            }

            updatePreviousInventory();
        }

        public void updatePreviousInventory()
        {
            Array inventoryArray = currentInventory.ToArray();
            previousInventory.Clear();
            foreach (KeyValuePair<type, int> item in inventoryArray)
            {
                previousInventory[item.Key] = item.Value;
            }
        }

        public int getFreeInventorySlots()
        {
            return freeSlots.Count;
        }
    }
}