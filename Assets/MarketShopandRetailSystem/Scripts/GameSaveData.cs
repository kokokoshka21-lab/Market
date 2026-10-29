using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    [Serializable]
    public class GameSaveData
    {
        // Player Data
        public int money;
        public int experience;
        public string profileName;
        public int lastXPEarnedMoney;

        // Day/Time Data
        public int currentDay;

        // Inventory Data
        public List<InventoryItemData> inventoryItems = new List<InventoryItemData>();

        // Placed Objects Data
        public List<PlacedObjectData> placedObjects = new List<PlacedObjectData>();

        // Sellable Objects Data
        public List<SellableObjectData> sellableObjects = new List<SellableObjectData>();

        // Shop Data
        public List<ShopData> shops = new List<ShopData>();

        // Product Prices
        public Dictionary<string, int> productSellingPrices = new Dictionary<string, int>();

        // Employee Data
        public bool cashierHired;
        public int cashierHiredAmount;
        public bool cleanerHired;
        public int cleanerHiredAmount;

        // Tutorial/First Time Data
        public bool hasMetSellerBefore;

        // Bills
        public int currentBillAmount;
        public string currentBillType;

        // Destroyed objects tracking
        public List<string> destroyedObjectIDs = new List<string>();

        public GameSaveData()
        {
            inventoryItems = new List<InventoryItemData>();
            placedObjects = new List<PlacedObjectData>();
            sellableObjects = new List<SellableObjectData>();
            shops = new List<ShopData>();
            productSellingPrices = new Dictionary<string, int>();
            destroyedObjectIDs = new List<string>();
        }
    }

    [Serializable]
    public class InventoryItemData
    {
        public string itemName;
        public int amount;
    }

    [Serializable]
    public class PlacedObjectData
    {
        public string equipmentName;
        public int objectIndex;
        public Vector3Data position;
        public Vector3Data rotation;
        public Vector3Data scale;
        public List<ShelfSlotData> shelfSlots = new List<ShelfSlotData>(); // For shelves/displays
        // Optional whole-box attachment; absent in older saves.
        public string boxShelfName;
        public int boxShelfIndex;
        public int boxShelfSlot;
        public bool hasBoxContents;
        public List<BoxSlotData> boxContents = new List<BoxSlotData>();

        public PlacedObjectData()
        {
            position = new Vector3Data();
            rotation = new Vector3Data();
            scale = new Vector3Data();
            shelfSlots = new List<ShelfSlotData>();
        }
    }

    [Serializable]
    public class SellableObjectData
    {
        public string productName;
        public int objectIndex;
        public Vector3Data position;
        public Vector3Data rotation;
        // Optional attachment for moved shelves; older saves keep using world coordinates.
        public string shelfName;
        public int shelfIndex;
        public int slotIndex;
        public Vector3Data slotLocalPosition;
        public Vector3Data slotLocalRotation;

        public SellableObjectData()
        {
            position = new Vector3Data();
            rotation = new Vector3Data();
        }
    }

    [Serializable]
    public class BoxSlotData
    {
        public int slotIndex;
        public string productName;
    }

    [Serializable]
    public class ShelfSlotData
    {
        public int slotIndex;
        public int status; // -1 = empty, 0 = default, 1 = filled
    }

    [Serializable]
    public class ShopData
    {
        public int shopID;
        public bool isRented;
        public string shopName;
        public string signColorHex;
    }

    [Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data()
        {
            x = 0;
            y = 0;
            z = 0;
        }

        public Vector3Data(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3Data(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
}
