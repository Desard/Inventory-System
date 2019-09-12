using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Inventory
{
    public static class SaveLoadManager
    {
        public static void saveItems(List<ItemSlot> occupiedSlots, string name)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(Application.persistentDataPath + "/" + name + ".sav", FileMode.Create);

            SavedItems items = new SavedItems(occupiedSlots);
            formatter.Serialize(stream, items);
            stream.Close();
        }

        public static List<ItemData> loadItems(string name)
        {
            FileStream stream = null;
            BinaryFormatter formatter = new BinaryFormatter();
            if (File.Exists(Application.persistentDataPath + "/" + name + ".sav"))
                stream = new FileStream(Application.persistentDataPath + "/" + name + ".sav", FileMode.Open);
            else
                return null;

            SavedItems box = formatter.Deserialize(stream) as SavedItems;
            stream.Close();
            return box.items;
        }
    }

    [Serializable]
    public class SavedItems
    {
        public List<ItemData> items = new List<ItemData>();

        public SavedItems(List<ItemSlot> occupiedSlots)
        {
            foreach (ItemSlot slot in occupiedSlots)
            {
                ItemData itemData = new ItemData(slot.item, slot.numberOfItmes, slot.slotNumber);
                items.Add(itemData);
            }
        }
    }

    [Serializable]
    public struct ItemData
    {
        public type item;
        public int quantity;
        public int itemSlot;

        public ItemData(type itemType, int itemQuantity, int slotNumber)
        {
            item = itemType;
            quantity = itemQuantity;
            itemSlot = slotNumber;
        }
    }
}
