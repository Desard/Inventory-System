using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Inventory
{
    public class InventoryItemDragger : MonoBehaviour
    {
        bool dragging = false;

        Transform objectToDrag;
        Image objectToDragImage;
        GameObject dragClone;

        List<RaycastResult> hitObjects = new List<RaycastResult>();

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                objectToDrag = getTransformFromItemUnderMouse();

                if (objectToDrag != null)
                {
                    if (GameObject.FindGameObjectWithTag("ItemStackDivider"))
                        Destroy(GameObject.FindGameObjectWithTag("ItemStackDivider"));

                    dragClone = new GameObject();
                    dragClone.name = "CurrentlyDraggedItem";
                    dragClone.AddComponent<CanvasRenderer>();
                    dragClone.AddComponent<RectTransform>().sizeDelta = new Vector2(objectToDrag.GetComponent<RectTransform>().rect.width, objectToDrag.GetComponent<RectTransform>().rect.height);
                    dragClone.AddComponent<Image>().sprite = objectToDrag.GetComponent<Image>().sprite;
                    dragClone.transform.SetParent(GameObject.FindGameObjectWithTag("Inventory").GetComponent<InventorySystem>().canvasOfInventory);
                    dragClone.transform.position = objectToDrag.transform.position;

                    objectToDragImage = dragClone.GetComponent<Image>();

                    dragClone.transform.SetAsLastSibling();
                    objectToDragImage = dragClone.transform.GetComponent<Image>();
                    objectToDragImage.raycastTarget = false;
                    dragging = true;
                }
            }

            if (dragging)
                dragClone.transform.position = Input.mousePosition;

            if (Input.GetMouseButtonUp(0))
            {
                if (objectToDrag != null)
                {
                    GameObject objectUnderMouse = getItemUnderMouse();
                    ItemSlot oldSlot = objectToDrag.parent.parent.GetComponent<ItemSlot>();

                    if (objectUnderMouse != null)
                    {
                        if (objectUnderMouse.tag == "Item")
                            GetComponent<InventoryUI>().swapSlots(oldSlot, objectUnderMouse.transform.parent.parent.GetComponent<ItemSlot>());
                        else if (objectUnderMouse.tag == "InventroyItemButton")
                            GetComponent<InventoryUI>().swapSlots(oldSlot, objectUnderMouse.transform.parent.GetComponent<ItemSlot>());
                    }
                    else
                        GetComponent<InventoryUI>().itemDestructionRequest(oldSlot);

                    Destroy(dragClone);
                    dragging = false;
                }
            }
        }

        GameObject getItemUnderMouse()
        {
            var pointer = new PointerEventData(EventSystem.current);

            pointer.position = Input.mousePosition;
            EventSystem.current.RaycastAll(pointer, hitObjects);

            if (hitObjects.Count <= 0)
                return null;

            return hitObjects.First().gameObject;
        }

        Transform getTransformFromItemUnderMouse()
        {
            GameObject clickedItem = getItemUnderMouse();

            if (clickedItem != null && clickedItem.tag == "Item")
                return clickedItem.transform;

            return null;
        }
    }
}

