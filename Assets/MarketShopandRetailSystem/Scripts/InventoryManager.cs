using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.InputSystem;

namespace MarketShopandRetailSystem
{
    public class InventoryManager : MonoBehaviour
    {
        public List<PhysicalEquipmentDetails> Equipments;
        [HideInInspector]
        public List<EquipmentScript> Inventories;
        public List<SellableObject> SellableObjects;

        public OrderBox CurrentOrderBox;
        public GameObject EquipmentUIPrefab;
        public GameObject Container_Decoration;
        public GameObject Container_Poster;
        public GameObject Container_Sellables;

        public static InventoryManager Instance;
        public LayerMask layermask;

        [HideInInspector]
        public GameObject CreatedObject;

        public GameObject Panel_LocatingInfo;
        private EquipmentScript currentEquipment;
        public GameObject shop;
        [HideInInspector]
        public List<GameObject> CurrentEquipmentList;
        public InventoryMode inventoryMode;

        private void Awake()
        {
            Instance = this;
            CurrentEquipmentList = new List<GameObject>();
            LoadAllObjects();
        }

        public void Buy(EquipmentScript equipment)
        {
            GameCanvas.Instance.Hide_Warning();
            AdvancedGameManager.Instance.UpdatePlayerValues();
            if (AdvancedGameManager.Instance.playerValues.Money < equipment.EquipmentDetail.Price)
            {
                GameCanvas.Instance.Show_Warning_Not("Insufficent Money!", false);
                return;
            }
            if (AdvancedGameManager.Instance.playerValues.Experience < equipment.EquipmentDetail.Experience)
            {
                GameCanvas.Instance.Show_Warning_Not("Insufficent Experience!", false);
                return;
            }
            AdvancedGameManager.Instance.Spend(equipment.EquipmentDetail.Price);
            equipment.CurrentInStorage = equipment.CurrentInStorage + 1;
            SaveLoadManager.Instance.SetInventoryItemAmount(equipment.EquipmentDetail.Name, equipment.CurrentInStorage);
            GameCanvas.Instance.Show_Warning_Not("Added to Inventory!", true);
        }

        public void Recover(string name)
        {
            EquipmentScript equipment = Inventories.Where(x => x.EquipmentDetail.Name == name).FirstOrDefault();
            equipment.CurrentInStorage = equipment.CurrentInStorage + 1;
            SaveLoadManager.Instance.SetInventoryItemAmount(equipment.EquipmentDetail.Name, equipment.CurrentInStorage);
            GameCanvas.Instance.Show_Warning_Not("Added to Inventory!", true);
        }

        public void CheckCurrentEquipmentList()
        {
            for (int i = 0; i < InventoryManager.Instance.CurrentEquipmentList.Count; i++)
            {
                if (InventoryManager.Instance.CurrentEquipmentList[i] == null)
                {
                    InventoryManager.Instance.CurrentEquipmentList.RemoveAt(i);
                    i--;
                }
            }
        }

        public void Use(EquipmentScript equipment)
        {
            if (equipment.CurrentInStorage > 0)
            {
                if (equipment.EquipmentDetail.equipmentType == EquipmentType.Sellable)
                {
                    GameObject o = Instantiate(equipment.EquipmentDetail.gameObject);
                    FPSHandRotator.Instance.Switch_Hand(Hand_Type.Free);
                    AudioManager.Instance.Play_Item_Grab();
                    o.transform.position = new Vector3(AdvancedGameManager.Instance.incomingOrderBoxPoint.position.x + Random.Range(-2, 2), AdvancedGameManager.Instance.incomingOrderBoxPoint.position.y + 3, AdvancedGameManager.Instance.incomingOrderBoxPoint.position.z + Random.Range(-2, 2));
                    o.layer = LayerMask.NameToLayer("Default");
                    Panel_LocatingInfo.SetActive(false);
                    SavetoSystem(o, equipment);
                    equipment.DecreaseItFromInventory();
                    CurrentEquipmentList.Add(o);
                    LoadMyInventory();
                }
                else
                {
                    FPSHandRotator.Instance.Switch_Hand(Hand_Type.Build);
                    CreatedObject = Instantiate(equipment.EquipmentDetail.gameObject);
                    currentPlacingScript = CreatedObject.GetComponent<ObjectPlacing>();
                    currentEquipment = equipment;
                    Panel_LocatingInfo.SetActive(true);
                    GameCanvas.Instance.Hide_Panel_SellerShop(true);
                    AdvancedGameManager.Instance.CurrentMode = Mode.InInventoryLocating;
                }
            }
        }

        float wheelAcc;
        const float notch = 0.05f;
        const float stepDeg = 45f;

        private ObjectPlacing currentPlacingScript;

        public float HologramOffset = 10;

        private void Update()
        {
            if (AdvancedGameManager.Instance.CurrentMode == Mode.InInventoryLocating)
            {
                Ray ray = Camera.main.ScreenPointToRay(new Vector3(GameCanvas.Instance.Crosshair.transform.position.x, GameCanvas.Instance.Crosshair.transform.position.y - HologramOffset, GameCanvas.Instance.Crosshair.transform.position.z));

                RaycastHit[] hits = Physics.RaycastAll(ray, 6, layermask);
                foreach (RaycastHit hit in hits)
                {
                    if (!hit.collider.CompareTag("Shop") && hit.collider.gameObject != currentPlacingScript.gameObject)
                    {
                        if (currentPlacingScript.GetComponent<PhysicalEquipmentDetails>().equipmentType == EquipmentType.Poster)
                        {
                            CreatedObject.transform.position = hit.point;
                            CreatedObject.SetActive(true);
                            if (AdvancedGameManager.Instance.controllerType == ControllerType.PC)
                            {
                                float delta = Mouse.current.scroll.ReadValue().y;
                                wheelAcc += delta;

                                if (wheelAcc >= notch)
                                {
                                    CreatedObject.transform.Rotate(Vector3.right, stepDeg);
                                    wheelAcc = 0f;
                                }
                                else if (wheelAcc <= -notch)
                                {
                                    CreatedObject.transform.Rotate(Vector3.right, -stepDeg);
                                    wheelAcc = 0f;
                                }
                            }
                        }
                        else
                        {
                            CreatedObject.transform.position = new Vector3(hit.point.x, hit.point.y, hit.point.z);
                            CreatedObject.SetActive(true);
                            if (AdvancedGameManager.Instance.controllerType == ControllerType.PC)
                            {
                                float delta = Mouse.current.scroll.ReadValue().y;
                                wheelAcc += delta;

                                if (wheelAcc >= notch)
                                {
                                    CreatedObject.transform.Rotate(Vector3.up, stepDeg);
                                    wheelAcc = 0f;
                                }
                                else if (wheelAcc <= -notch)
                                {
                                    CreatedObject.transform.Rotate(Vector3.up, -stepDeg);
                                    wheelAcc = 0f;
                                }
                            }
                        }
                    }
                }
                if (hits.Length == 0)
                {
                    CreatedObject.SetActive(false);
                }
                else if (hits.Length == 1 && hits[0].collider.CompareTag("Shop"))
                {
                    CreatedObject.SetActive(false);
                }


                if (AdvancedGameManager.Instance.controllerType == ControllerType.PC && AdvancedGameManager.Instance.mouseAction.WasPressedThisFrame())
                {
                    if (currentPlacingScript.CanLocate)
                    {
                        Click_Button_Locate();
                    }
                }
            }
            else if (AdvancedGameManager.Instance.CurrentMode == Mode.HoldingForOrdering)
            {
                if(AdvancedGameManager.Instance.controllerType == ControllerType.Mobile)
                {
                    if (HeroPlayerScript.Instance.isButtonInteractHeld && Time.time > lastTimeLocating + 0.15f)
                    {
                        lastTimeLocating = Time.time;
                        Click_Button_Locate();
                    }
                    else if (HeroPlayerScript.Instance.isButtonGettingItemBackInteractHeld && Time.time > lastTimeLocating + 0.15f)
                    {
                        lastTimeLocating = Time.time;
                        Click_Button_GetBackToOrderBox();
                    }
                }
                else
                {
                    if (AdvancedGameManager.Instance.mouseAction.IsPressed() && Time.time > lastTimeLocating + 0.15f)
                    {
                        lastTimeLocating = Time.time;
                        Click_Button_Locate();
                    }
                    else if (AdvancedGameManager.Instance.mouseRightAction.IsPressed() && Time.time > lastTimeLocating + 0.15f)
                    {
                        lastTimeLocating = Time.time;
                        Click_Button_GetBackToOrderBox();
                    }
                }
            }
        }

        float lastTimeLocating = 0;

        private IEnumerator JumpCoroutine(Transform target, Vector3 endPosition, float duration)
        {
            target.parent = null;
            target.localScale = Vector3.one;
            target.eulerAngles = Vector3.zero;
            Vector3 startPosition = target.position;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                float progress = elapsedTime / duration;
                target.position = Vector3.Lerp(startPosition, endPosition, progress);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            target.position = endPosition;
        }

        private IEnumerator JumpCoroutineBackToOrderBox(Transform target, PlacablePoint orderBoxItem, float duration)
        {
            target.parent = orderBoxItem.transform.parent;
            target.localScale = Vector3.one;
            target.eulerAngles = Vector3.zero;
            Vector3 startPosition = target.position;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                float progress = elapsedTime / duration;
                target.position = Vector3.Lerp(startPosition, orderBoxItem.transform.position, progress);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            Destroy(target.gameObject);
        }

        public void Click_Button_Rotate()
        {
            if (currentPlacingScript.GetComponent<PhysicalEquipmentDetails>().equipmentType == EquipmentType.Poster)
            {
                CreatedObject.transform.Rotate(0.1f * Vector3.right * 100);
            }
            else
            {
                CreatedObject.transform.Rotate(0.1f * Vector3.up * 100);
            }
        }

        public void Click_Button_GetBackToOrderBox()
        {
            if (AdvancedGameManager.Instance.CurrentMode == Mode.HoldingForOrdering)
            {
                // Let's check do we have any empty area in our basket?
                var availableItem = CurrentOrderBox.GetComponent<ArrayableArea>().PlacablePoints.Where(x => x.isAvailable && x.objectToGrab == null).FirstOrDefault();
                if (availableItem == null) return;

                // Let's check is there any item close to us?
                var availablePlace = TriggerScript.Instance.placablePoints.Where(x => !x.isAvailable && x.objectToGrab != null).FirstOrDefault();
                if (availablePlace == null) return;

                // Find the parent!
                Transform parent = availablePlace.transform;
                // Get all PlacablePoints Of Parent
                PlacablePoint[] allPlaceablePointsOfCurrentFloor = parent.GetComponentsInChildren<PlacablePoint>();
                // Check do we have a different kind of Sellable Object?
                var differentObject = allPlaceablePointsOfCurrentFloor.Where(x => x.objectToGrab != null && x.objectToGrab.GetComponent<SellableObject>().Name != CurrentOrderBox.sellableObjectPrefab.GetComponent<SellableObject>().Name).FirstOrDefault();
                if (differentObject != null)
                {
                    return;
                }

                // If both of them are YES! Relocate it to OrderBox!
                StartCoroutine(JumpCoroutineBackToOrderBox(availablePlace.objectToGrab.gameObject.transform, availableItem, 0.25f));
                AudioManager.Instance.Play_audioClip_Locate();

                // Lets remove it!
                CurrentOrderBox.AddItemBack();
                availablePlace.GetComponentInParent<ArrayableArea>().RemoveItemFromSelftForPuttingOrderBox(availablePlace);
                availablePlace.isAvailable = true;
                availablePlace.objectToGrab = null;

            }
        }


        public void Click_Button_Locate()
        {
            if (AdvancedGameManager.Instance.CurrentMode == Mode.InInventoryLocating)
            {
                if (currentPlacingScript.CanLocate)
                {
                    AdvancedGameManager.Instance.CurrentMode = Mode.Free;
                    CreatedObject.transform.parent = shop.transform;
                    CreatedObject.layer = LayerMask.NameToLayer("Default");
                    Collider[] colliders = currentPlacingScript.GetComponents<Collider>();
                    for (int i = 0; i < colliders.Length; i++)
                    {
                        colliders[i].isTrigger = false;
                    }
                    Destroy(currentPlacingScript.Rigidbody_Placing);
                    Destroy(currentPlacingScript);
                    Panel_LocatingInfo.SetActive(false);
                    FPSHandRotator.Instance.AnimateHand(InteractionType.None);
                    StartCoroutine(FPSHandRotator.Instance.Switch_Hand_InTime(Hand_Type.Free, 1.5f));
                    SavetoSystem(CreatedObject, currentEquipment);
                    currentEquipment.DecreaseItFromInventory();
                    CurrentEquipmentList.Add(CreatedObject);
                    CreatedObject = null;
                }
            }
            else if (AdvancedGameManager.Instance.CurrentMode == Mode.HoldingForOrdering)
            {
                // Let's check do we have any in our basket?
                var availableItem = CurrentOrderBox.GetComponent<ArrayableArea>().PlacablePoints.Where(x => !x.isAvailable && x.objectToGrab != null).FirstOrDefault();
                if (availableItem == null) return;

                // Let's check is there any empty area close to us?
                var availablePlace = TriggerScript.Instance.placablePoints.Where(x => x.isAvailable && x.objectToGrab == null).FirstOrDefault();
                if (availablePlace == null) return;

                // Find the parent!
                Transform parent = availablePlace.transform.parent;
                // Get all PlacablePoints Of Parent
                PlacablePoint[] allPlaceablePointsOfCurrentFloor = parent.GetComponentsInChildren<PlacablePoint>();
                // Check do we have a different kind of Sellable Object?
                var differentObject = allPlaceablePointsOfCurrentFloor.Where(x => x.objectToGrab != null && x.objectToGrab.GetComponent<SellableObject>().Name != availableItem.objectToGrab.GetComponent<SellableObject>().Name).FirstOrDefault();
                if (differentObject != null)
                {
                    return;
                }

                if (!availablePlace.GetComponentInParent<ArrayableArea>().storageType.HasFlag(availableItem.objectToGrab.GetComponent<SellableObject>().storageType))
                {
                    GameCanvas.Instance.Show_Warning_Not("You can't place this product here!", false);
                    return;
                }

                availablePlace.Floor.SetPriceDetails(availableItem.objectToGrab.GetComponent<SellableObject>());

                // If both of them are YES! Locate it!
                StartCoroutine(JumpCoroutine(availableItem.objectToGrab.gameObject.transform, availablePlace.transform.position, 0.25f));
                AudioManager.Instance.Play_audioClip_Locate();
                // Lets save it!

                availablePlace.MainContainer.GetComponent<ArrayableArea>().AddItem(availablePlace.ID, availableItem.objectToGrab.GetComponent<SellableObject>());
                availablePlace.isAvailable = false;
                availablePlace.objectToGrab = availableItem.objectToGrab;

                CurrentOrderBox.GetComponent<ArrayableArea>().RemoveItem(availableItem.ID);
                StartCoroutine(CurrentOrderBox.CheckTheSlots(0));
            }
        }

        public void CancelLocating()
        {
            AdvancedGameManager.Instance.CurrentMode = Mode.Free;
            if (CreatedObject != null)
            {
                Destroy(CreatedObject);
            }
            Panel_LocatingInfo.SetActive(false);
            CreatedObject = null;
        }

        public void SavetoSystem(GameObject newOne, EquipmentScript equipment)
        {
            // Find next available index
            int objectCount = 0;
            var existingObjects = SaveLoadManager.Instance.GetAllPlacedObjects();
            var sameTypeObjects = existingObjects.FindAll(x => x.equipmentName == equipment.EquipmentDetail.Name);
            if (sameTypeObjects.Count > 0)
            {
                objectCount = sameTypeObjects.Max(x => x.objectIndex) + 1;
            }

            SaveLoadManager.Instance.AddPlacedObject(
                equipment.EquipmentDetail.Name,
                objectCount,
                newOne.transform.position,
                newOne.transform.eulerAngles,
                newOne.transform.localScale
            );
            newOne.GetComponent<PhysicalEquipmentDetails>().equipmentIndex = objectCount;
        }

        public void RemovefromSystem(int i, string name)
        {
            EquipmentScript equipment = Inventories.Where(x => x.EquipmentDetail.Name == name).FirstOrDefault();
            if (equipment != null)
            {
                SaveLoadManager.Instance.RemovePlacedObject(equipment.EquipmentDetail.Name, i);
            }
        }

        public void LoadAllObjects()
        {
            inventoryMode = InventoryMode.SellerShopIsOpen;
            Inventories.Clear();
            foreach (Transform child in Container_Decoration.transform)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in Container_Poster.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in Container_Sellables.transform)
            {
                Destroy(child.gameObject);
            }

            // Let's load the Objects to the Inventory UI
            for (int i = 0; i < Equipments.Count; i++)
            {
                if (Equipments[i].equipmentType == EquipmentType.Decoration)
                {
                    GameObject listItem = Instantiate(EquipmentUIPrefab, Container_Decoration.transform);
                    listItem.GetComponent<EquipmentScript>().AssignDetails(Equipments[i]);
                    Inventories.Add(listItem.GetComponent<EquipmentScript>());
                }
                else if (Equipments[i].equipmentType == EquipmentType.Poster)
                {
                    GameObject listItem = Instantiate(EquipmentUIPrefab, Container_Poster.transform);
                    listItem.GetComponent<EquipmentScript>().AssignDetails(Equipments[i]);
                    Inventories.Add(listItem.GetComponent<EquipmentScript>());
                }
                else if (Equipments[i].equipmentType == EquipmentType.Sellable)
                {
                    GameObject listItem = Instantiate(EquipmentUIPrefab, Container_Sellables.transform);
                    listItem.GetComponent<EquipmentScript>().AssignDetails(Equipments[i]);
                    Inventories.Add(listItem.GetComponent<EquipmentScript>());
                }
            }
        }

        public void LoadMyInventory()
        {
            inventoryMode = InventoryMode.PlayerInventoryIsOpen;
            foreach (var item in Inventories)
            {
                item.CurrentInStorage = SaveLoadManager.Instance.GetInventoryItemAmount(item.EquipmentDetail.Name);
                if (item.CurrentInStorage > 0)
                {
                    item.gameObject.SetActive(true);
                }
                else
                {
                    item.gameObject.SetActive(false);
                }

                if (item.CurrentInStorage > 0)
                {
                    item.gameObject.SetActive(true);
                    item.Text_Money.text = "You have " + item.CurrentInStorage.ToString();
                    item.Text_Name.text = item.EquipmentDetail.Name.ToString();
                    item.Image_Money.sprite = item.ImageSpriteInventory;
                }
                else
                {
                    item.gameObject.SetActive(false);
                }
                item.Image.sprite = item.EquipmentDetail.ImageSprite;
            }
        }

        public IEnumerator ArrangeEverything()
        {
            yield return new WaitForSeconds(1);
            // First find all placeablePoints
            PlacablePoint[] placablePoints = GameObject.FindObjectsOfType<PlacablePoint>();
            SellableObject[] sellableObjects = GameObject.FindObjectsOfType<SellableObject>();
            for (int i = 0; i < placablePoints.Length; i++)
            {
                var found = sellableObjects.Where(x => x.transform.position == placablePoints[i].transform.position && x.isAvailable).FirstOrDefault();
                if (found != null)
                {
                    placablePoints[i].isAvailable = false;
                    placablePoints[i].objectToGrab = found.gameObject;
                    found.putPoint = placablePoints[i];
                    placablePoints[i].Floor.SetPriceDetails(found);
                }
            }
        }


        public void LoadSellableObjects()
        {
            var allSellableObjects = SaveLoadManager.Instance.GetAllSellableObjects();
            foreach (var savedObj in allSellableObjects)
            {
                var item = SellableObjects.Find(x => x.Name == savedObj.productName);
                if (item != null)
                {
                    Vector3 position = savedObj.position.ToVector3();
                    Vector3 rotation = savedObj.rotation.ToVector3();

                    GameObject newEquipment = Instantiate(item.gameObject, position, Quaternion.Euler(rotation));
                    newEquipment.layer = LayerMask.NameToLayer("Default");
                    newEquipment.GetComponent<SellableObject>().sellableObjectIndex = savedObj.objectIndex;
                    CurrentEquipmentList.Add(newEquipment);
                }
            }
        }

        public void LoadBuiltObjects()
        {
            var allPlacedObjects = SaveLoadManager.Instance.GetAllPlacedObjects();
            foreach (var savedObj in allPlacedObjects)
            {
                var item = Inventories.Find(x => x.EquipmentDetail.Name == savedObj.equipmentName);
                if (item != null)
                {
                    Vector3 position = savedObj.position.ToVector3();
                    Vector3 rotation = savedObj.rotation.ToVector3();
                    Vector3 scale = savedObj.scale.ToVector3();

                    GameObject newEquipment = Instantiate(item.EquipmentDetail.gameObject, position, Quaternion.Euler(rotation), shop.transform);
                    newEquipment.layer = LayerMask.NameToLayer("Default");
                    if (newEquipment.GetComponent<OrderBox>() == null)
                    {
                        Destroy(newEquipment.GetComponent<ObjectPlacing>().Rigidbody_Placing);
                    }
                    Collider[] colliders = newEquipment.GetComponents<Collider>();
                    for (int z = 0; z < colliders.Length; z++)
                    {
                        colliders[z].isTrigger = false;
                    }
                    Destroy(newEquipment.GetComponent<ObjectPlacing>());
                    newEquipment.transform.localScale = scale;
                    newEquipment.GetComponent<PhysicalEquipmentDetails>().equipmentIndex = savedObj.objectIndex;
                    CurrentEquipmentList.Add(newEquipment);
                }
            }
        }
    }

    public enum InventoryMode
    {
        SellerShopIsOpen,
        PlayerInventoryIsOpen
    }
}