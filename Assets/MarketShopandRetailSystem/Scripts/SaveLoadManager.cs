using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class SaveLoadManager : MonoBehaviour
    {
        private static SaveLoadManager _instance;
        public static SaveLoadManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find existing instance
                    _instance = FindObjectOfType<SaveLoadManager>();
                    
                    // If still null, create new one
                    if (_instance == null)
                    {
                        GameObject saveManager = new GameObject("SaveLoadManager");
                        _instance = saveManager.AddComponent<SaveLoadManager>();
                    }
                }
                return _instance;
            }
        }
        
        private GameSaveData currentSaveData;
        private string saveFilePath;
        private bool isInitialized = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Initialize()
        {
            if (isInitialized) return;
            
            if (string.IsNullOrEmpty(saveFilePath))
            {
                saveFilePath = Path.Combine(Application.persistentDataPath, "gamesave.json");
            }
            
            LoadGame();
            isInitialized = true;
        }

        private void EnsureInitialized()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }

        // Initialize new save data
        public void InitializeNewSave()
        {
            currentSaveData = new GameSaveData();
            
            // Use default values if AdvancedGameManager is not ready yet
            if (AdvancedGameManager.Instance != null)
            {
                currentSaveData.money = AdvancedGameManager.Instance.StartingMoneyAmount;
                currentSaveData.experience = AdvancedGameManager.Instance.StartingExperienceAmount;
            }
            else
            {
                currentSaveData.money = 1000; // Default starting money
                currentSaveData.experience = 1; // Default starting experience
            }
            
            currentSaveData.currentDay = 0;
            currentSaveData.profileName = "";
            currentSaveData.lastXPEarnedMoney = 0;
            currentSaveData.cashierHired = false;
            currentSaveData.cashierHiredAmount = 0;
            currentSaveData.cleanerHired = false;
            currentSaveData.cleanerHiredAmount = 0;
            currentSaveData.hasMetSellerBefore = false;
            currentSaveData.currentBillAmount = 0;
        }

        // Save game to JSON file
        public void SaveGame()
        {
            try
            {
                string json = JsonUtility.ToJson(currentSaveData, true);
                File.WriteAllText(saveFilePath, json);
                Debug.Log("Game saved successfully to: " + saveFilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to save game: " + e.Message);
            }
        }

        // Load game from JSON file
        public void LoadGame()
        {
            if (File.Exists(saveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(saveFilePath);
                    currentSaveData = JsonUtility.FromJson<GameSaveData>(json);
                    Debug.Log("Game loaded successfully from: " + saveFilePath);
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to load game: " + e.Message);
                    InitializeNewSave();
                }
            }
            else
            {
                Debug.Log("No save file found. Creating new save data.");
                InitializeNewSave();
            }
        }

        // Check if save file exists
        public bool SaveFileExists()
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(saveFilePath))
            {
                saveFilePath = Path.Combine(Application.persistentDataPath, "gamesave.json");
            }
            return File.Exists(saveFilePath);
        }

        // Delete save file
        public void DeleteSave()
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(saveFilePath))
            {
                saveFilePath = Path.Combine(Application.persistentDataPath, "gamesave.json");
            }
            
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log("Save file deleted.");
            }
            InitializeNewSave();
        }

        // Get current save data
        public GameSaveData GetSaveData()
        {
            EnsureInitialized();
            if (currentSaveData == null)
            {
                LoadGame();
            }
            return currentSaveData;
        }

        // Money operations
        public int GetMoney()
        {
            EnsureInitialized();
            return currentSaveData.money;
        }

        public void SetMoney(int amount)
        {
            EnsureInitialized();
            currentSaveData.money = amount;
            SaveGame();
        }

        public void AddMoney(int amount)
        {
            EnsureInitialized();
            currentSaveData.money += amount;
            SaveGame();
        }

        public void SubtractMoney(int amount)
        {
            EnsureInitialized();
            currentSaveData.money -= amount;
            SaveGame();
        }

        // Experience operations
        public int GetExperience()
        {
            EnsureInitialized();
            return currentSaveData.experience;
        }

        public void SetExperience(int amount)
        {
            EnsureInitialized();
            currentSaveData.experience = amount;
            SaveGame();
        }

        public void AddExperience(int amount)
        {
            EnsureInitialized();
            currentSaveData.experience += amount;
            SaveGame();
        }

        // Profile name
        public string GetProfileName()
        {
            EnsureInitialized();
            return currentSaveData.profileName;
        }

        public void SetProfileName(string name)
        {
            EnsureInitialized();
            currentSaveData.profileName = name;
            SaveGame();
        }

        // Day operations
        public int GetCurrentDay()
        {
            EnsureInitialized();
            return currentSaveData.currentDay;
        }

        public void SetCurrentDay(int day)
        {
            EnsureInitialized();
            currentSaveData.currentDay = day;
            SaveGame();
        }

        // Inventory operations
        public int GetInventoryItemAmount(string itemName)
        {
            EnsureInitialized();
            var item = currentSaveData.inventoryItems.Find(x => x.itemName == itemName);
            return item != null ? item.amount : 0;
        }

        public void SetInventoryItemAmount(string itemName, int amount)
        {
            EnsureInitialized();
            var item = currentSaveData.inventoryItems.Find(x => x.itemName == itemName);
            if (item != null)
            {
                item.amount = amount;
            }
            else
            {
                currentSaveData.inventoryItems.Add(new InventoryItemData { itemName = itemName, amount = amount });
            }
            SaveGame();
        }

        // Placed objects operations
        public void AddPlacedObject(string equipmentName, int objectIndex, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            EnsureInitialized();
            var obj = new PlacedObjectData
            {
                equipmentName = equipmentName,
                objectIndex = objectIndex,
                position = new Vector3Data(position),
                rotation = new Vector3Data(rotation),
                scale = new Vector3Data(scale)
            };
            currentSaveData.placedObjects.Add(obj);
            SaveGame();
        }

        public void RemovePlacedObject(string equipmentName, int objectIndex)
        {
            EnsureInitialized();
            currentSaveData.placedObjects.RemoveAll(x => x.equipmentName == equipmentName && x.objectIndex == objectIndex);
            SaveGame();
        }

        public bool UpdateShelfPlacement(PhysicalEquipmentDetails shelf)
        {
            EnsureInitialized();
            PlacedObjectData placed = GetPlacedObject(shelf.Name, shelf.equipmentIndex);
            if (placed == null) return false;
            PlacablePoint[] points = shelf.GetComponent<ArrayableArea>().PlacablePoints;
            // Validate all records before changing any of them. Never allocate new product IDs here.
            foreach (PlacablePoint point in points)
            {
                if (point.objectToGrab == null) continue;
                SellableObject product = point.objectToGrab.GetComponent<SellableObject>();
                if (product == null || !currentSaveData.sellableObjects.Exists(x =>
                    x.productName == product.Name && x.objectIndex == product.sellableObjectIndex)) return false;
            }

            placed.position = new Vector3Data(shelf.transform.position);
            placed.rotation = new Vector3Data(shelf.transform.eulerAngles);
            placed.scale = new Vector3Data(shelf.transform.localScale);
            foreach (PlacablePoint point in points)
            {
                if (point.objectToGrab == null) continue;
                SellableObject product = point.objectToGrab.GetComponent<SellableObject>();
                SellableObjectData record = currentSaveData.sellableObjects.Find(x =>
                    x.productName == product.Name && x.objectIndex == product.sellableObjectIndex);
                record.position = new Vector3Data(product.transform.position);
                record.rotation = new Vector3Data(product.transform.eulerAngles);
                record.shelfName = shelf.Name;
                record.shelfIndex = shelf.equipmentIndex;
                record.slotIndex = point.ID;
                record.slotLocalPosition = new Vector3Data(point.transform.InverseTransformPoint(product.transform.position));
                record.slotLocalRotation = new Vector3Data((Quaternion.Inverse(point.transform.rotation)
                    * product.transform.rotation).eulerAngles);
            }
            SaveGame();
            return true;
        }

        public PlacedObjectData GetPlacedObject(string equipmentName, int objectIndex)
        {
            EnsureInitialized();
            return currentSaveData.placedObjects.Find(x => x.equipmentName == equipmentName && x.objectIndex == objectIndex);
        }

        public List<PlacedObjectData> GetAllPlacedObjects()
        {
            EnsureInitialized();
            return currentSaveData.placedObjects;
        }

        // Sellable objects operations
        public void AddSellableObject(string productName, int objectIndex, Vector3 position, Vector3 rotation)
        {
            EnsureInitialized();
            var obj = new SellableObjectData
            {
                productName = productName,
                objectIndex = objectIndex,
                position = new Vector3Data(position),
                rotation = new Vector3Data(rotation)
            };
            currentSaveData.sellableObjects.Add(obj);
            SaveGame();
        }

        public void RemoveSellableObject(string productName, int objectIndex)
        {
            EnsureInitialized();
            currentSaveData.sellableObjects.RemoveAll(x => x.productName == productName && x.objectIndex == objectIndex);
            SaveGame();
        }

        public List<SellableObjectData> GetAllSellableObjects()
        {
            EnsureInitialized();
            return currentSaveData.sellableObjects;
        }

        // Shop operations
        public void SetShopData(int shopID, bool isRented, string shopName, string signColorHex)
        {
            EnsureInitialized();
            var shop = currentSaveData.shops.Find(x => x.shopID == shopID);
            if (shop != null)
            {
                shop.isRented = isRented;
                shop.shopName = shopName;
                shop.signColorHex = signColorHex;
            }
            else
            {
                currentSaveData.shops.Add(new ShopData
                {
                    shopID = shopID,
                    isRented = isRented,
                    shopName = shopName,
                    signColorHex = signColorHex
                });
            }
            SaveGame();
        }

        public ShopData GetShopData(int shopID)
        {
            EnsureInitialized();
            return currentSaveData.shops.Find(x => x.shopID == shopID);
        }

        // Product prices
        public void SetProductSellingPrice(string productName, int price)
        {
            EnsureInitialized();
            if (currentSaveData.productSellingPrices.ContainsKey(productName))
            {
                currentSaveData.productSellingPrices[productName] = price;
            }
            else
            {
                currentSaveData.productSellingPrices.Add(productName, price);
            }
            SaveGame();
        }

        public int GetProductSellingPrice(string productName, int defaultPrice)
        {
            EnsureInitialized();
            if (currentSaveData.productSellingPrices.ContainsKey(productName))
            {
                return currentSaveData.productSellingPrices[productName];
            }
            return defaultPrice;
        }

        // Employee operations
        public bool GetCashierHired()
        {
            EnsureInitialized();
            return currentSaveData.cashierHired;
        }

        public void SetCashierHired(bool hired)
        {
            EnsureInitialized();
            currentSaveData.cashierHired = hired;
            SaveGame();
        }

        public int GetCashierHiredAmount()
        {
            EnsureInitialized();
            return currentSaveData.cashierHiredAmount;
        }

        public void SetCashierHiredAmount(int amount)
        {
            EnsureInitialized();
            currentSaveData.cashierHiredAmount = amount;
            SaveGame();
        }

        public bool GetCleanerHired()
        {
            EnsureInitialized();
            return currentSaveData.cleanerHired;
        }

        public void SetCleanerHired(bool hired)
        {
            EnsureInitialized();
            currentSaveData.cleanerHired = hired;
            SaveGame();
        }

        public int GetCleanerHiredAmount()
        {
            EnsureInitialized();
            return currentSaveData.cleanerHiredAmount;
        }

        public void SetCleanerHiredAmount(int amount)
        {
            EnsureInitialized();
            currentSaveData.cleanerHiredAmount = amount;
            SaveGame();
        }

        // Tutorial/First time
        public bool GetHasMetSellerBefore()
        {
            EnsureInitialized();
            return currentSaveData.hasMetSellerBefore;
        }

        public void SetHasMetSellerBefore(bool met)
        {
            EnsureInitialized();
            currentSaveData.hasMetSellerBefore = met;
            SaveGame();
        }

        // Bills
        public int GetCurrentBillAmount()
        {
            EnsureInitialized();
            return currentSaveData.currentBillAmount;
        }

        public void SetCurrentBillAmount(int amount)
        {
            EnsureInitialized();
            currentSaveData.currentBillAmount = amount;
            SaveGame();
        }

        public string GetCurrentBillType()
        {
            EnsureInitialized();
            return currentSaveData.currentBillType;
        }

        public void SetCurrentBillType(string billType)
        {
            EnsureInitialized();
            currentSaveData.currentBillType = billType;
            SaveGame();
        }

        public void ClearCurrentBill()
        {
            EnsureInitialized();
            currentSaveData.currentBillAmount = 0;
            currentSaveData.currentBillType = "";
            SaveGame();
        }

        public bool HasActiveBill()
        {
            EnsureInitialized();
            return !string.IsNullOrEmpty(currentSaveData.currentBillType) && currentSaveData.currentBillAmount > 0;
        }

        // Last XP earned money
        public int GetLastXPEarnedMoney()
        {
            EnsureInitialized();
            return currentSaveData.lastXPEarnedMoney;
        }

        public void SetLastXPEarnedMoney(int amount)
        {
            EnsureInitialized();
            currentSaveData.lastXPEarnedMoney = amount;
            SaveGame();
        }

        // Shelf slot operations
        public void SetShelfSlotStatus(string equipmentName, int objectIndex, int slotIndex, int status)
        {
            EnsureInitialized();
            var obj = GetPlacedObject(equipmentName, objectIndex);
            if (obj != null)
            {
                var slot = obj.shelfSlots.Find(x => x.slotIndex == slotIndex);
                if (slot != null)
                {
                    slot.status = status;
                }
                else
                {
                    obj.shelfSlots.Add(new ShelfSlotData { slotIndex = slotIndex, status = status });
                }
                SaveGame();
            }
        }

        public int GetShelfSlotStatus(string equipmentName, int objectIndex, int slotIndex, int defaultValue = 0)
        {
            EnsureInitialized();
            var obj = GetPlacedObject(equipmentName, objectIndex);
            if (obj != null)
            {
                var slot = obj.shelfSlots.Find(x => x.slotIndex == slotIndex);
                if (slot != null)
                {
                    return slot.status;
                }
            }
            return defaultValue;
        }

        // Destroyed objects tracking
        public bool IsObjectDestroyed(string objectID)
        {
            EnsureInitialized();
            return currentSaveData.destroyedObjectIDs.Contains(objectID);
        }

        public void MarkObjectAsDestroyed(string objectID)
        {
            EnsureInitialized();
            if (!currentSaveData.destroyedObjectIDs.Contains(objectID))
            {
                currentSaveData.destroyedObjectIDs.Add(objectID);
                SaveGame();
            }
        }
    }
}
