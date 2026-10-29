using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SelfCheckoutRegressionTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private static Type RegisterType => Type.GetType("MarketShopandRetailSystem.CashRegister, Assembly-CSharp");

    private Component Register(bool automatic, float distance)
    {
        var go = new GameObject("Checkout");
        go.SetActive(false);
        objects.Add(go);
        Component register = go.AddComponent(RegisterType);
        FieldInfo products = RegisterType.GetField("productsofCustomer");
        if (products.GetValue(register) == null)
            products.SetValue(register, Activator.CreateInstance(products.FieldType));
        RegisterType.GetField("selfService").SetValue(register, automatic);
        var point = new GameObject("Customer point");
        point.transform.SetParent(go.transform);
        point.transform.position = Vector3.right * distance;
        RegisterType.GetField("pointCustomersCome").SetValue(register, point.transform);
        go.SetActive(true);
        return register;
    }

    private static object Choose(float chance, float roll, params Component[] registers)
    {
        Array candidates = Array.CreateInstance(RegisterType, registers.Length);
        for (int i = 0; i < registers.Length; i++) candidates.SetValue(registers[i], i);
        return RegisterType.GetMethod("SelectForCustomer").Invoke(null,
            new object[] { candidates, Vector3.zero, chance, roll });
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (GameObject go in objects) UnityEngine.Object.DestroyImmediate(go);
        objects.Clear();
    }

    [TestCase(0.5f, 0.25f, true)]
    [TestCase(0.5f, 0.75f, false)]
    [TestCase(0f, 0f, false)]
    [TestCase(1f, 1f, true)]
    public void CustomerChoosesTypeByChanceBeforeChoosingNearest(float chance, float roll, bool automatic)
    {
        Component ordinary = Register(false, 1f);
        Component selfService = Register(true, 3f);
        Assert.That(Choose(chance, roll, ordinary, selfService), Is.SameAs(automatic ? selfService : ordinary));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void MissingTypeFallsBackToTheOtherType(bool automatic)
    {
        Component only = Register(automatic, 1f);
        Assert.That(Choose(automatic ? 0f : 1f, 0.5f, only), Is.SameAs(only));
    }

    [Test]
    public void ChoosesNearestOfPreferredTypeAndAllowsWaitingAtBusyRegister()
    {
        Component far = Register(true, 5f);
        Component near = Register(true, 2f);
        RegisterType.GetField("isBusy").SetValue(near, true);
        Assert.That(Choose(1f, 0.5f, far, near), Is.SameAs(near));
    }

    [Test]
    public void IgnoresDisabledRegistersMissingCustomerPointsAndConstructionPreviews()
    {
        Component disabled = Register(true, 1f);
        disabled.gameObject.SetActive(false);
        Component noPoint = Register(true, 2f);
        RegisterType.GetField("pointCustomersCome").SetValue(noPoint, null);
        Component preview = Register(true, 3f);
        preview.gameObject.AddComponent(Type.GetType("MarketShopandRetailSystem.ObjectPlacing, Assembly-CSharp"));
        Component ordinary = Register(false, 5f);
        Assert.That(Choose(1f, 0.5f, disabled, noPoint, preview, ordinary), Is.SameAs(ordinary));
        Assert.That(Choose(1f, 0.5f, disabled, noPoint, preview), Is.Null);
    }

    [Test]
    public void PaymentRequiresEveryProductScannedAndCannotCompleteTwice()
    {
        Component register = Register(true, 1f);
        var customerObject = new GameObject("Customer");
        customerObject.SetActive(false);
        objects.Add(customerObject);
        Component customer = customerObject.AddComponent(Type.GetType("MarketShopandRetailSystem.CivilianController, Assembly-CSharp"));
        var productObject = new GameObject("Product");
        productObject.SetActive(false);
        objects.Add(productObject);
        Component product = productObject.AddComponent(Type.GetType("MarketShopandRetailSystem.SellableObject, Assembly-CSharp"));
        ((System.Collections.IList)RegisterType.GetField("productsofCustomer").GetValue(register)).Add(product);
        RegisterType.GetField("isBusy").SetValue(register, true);
        RegisterType.GetField("CurrentCivilian", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(register, customer);
        MethodInfo canComplete = RegisterType.GetMethod("CanCompletePurchase", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(canComplete.Invoke(register, null), Is.False);
        RegisterType.GetField("ProductIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(register, 1);
        Assert.That(canComplete.Invoke(register, null), Is.True);
        RegisterType.GetField("paymentCompleted", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(register, true);
        Assert.That(canComplete.Invoke(register, null), Is.False);
    }

    [TestCase(29f, false, false, false)]
    [TestCase(30f, false, false, true)]
    [TestCase(30f, true, false, true)]
    [TestCase(120f, false, true, true)]
    [TestCase(500f, true, true, false)]
    public void PatienceTimesOutUnattendedCheckoutOrQueueButNotActiveAttendedService(
        float now, bool serving, bool attended, bool expected)
    {
        Type civilian = Type.GetType("MarketShopandRetailSystem.CivilianController, Assembly-CSharp");
        object expired = civilian.GetMethod("QueuePatienceExpired").Invoke(null,
            new object[] { now, 0f, attended ? -1f : 0f, serving, attended, 30f, 120f });
        Assert.That(expired, Is.EqualTo(expected));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CancelReturnsPartiallyScannedBasketOnlyForItsOwner(bool correctOwner)
    {
        Component register = Register(false, 1f);
        Type civilianType = Type.GetType("MarketShopandRetailSystem.CivilianController, Assembly-CSharp");
        var ownerObject = new GameObject("Owner"); ownerObject.SetActive(false); objects.Add(ownerObject);
        Component owner = ownerObject.AddComponent(civilianType);
        var otherObject = new GameObject("Other customer"); otherObject.SetActive(false); objects.Add(otherObject);
        Component other = otherObject.AddComponent(civilianType);
        var item = new GameObject("Scanned product"); item.SetActive(false); objects.Add(item);
        Component product = item.AddComponent(Type.GetType("MarketShopandRetailSystem.SellableObject, Assembly-CSharp"));
        ((Behaviour)product).enabled = false;
        item.transform.SetParent(register.transform);
        var products = (System.Collections.IList)RegisterType.GetField("productsofCustomer").GetValue(register);
        products.Add(product);
        RegisterType.GetField("isBusy").SetValue(register, true);
        RegisterType.GetField("CurrentCivilian", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(register, owner);
        RegisterType.GetField("ProductIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(register, 1);
        object result = RegisterType.GetMethod("CancelCustomer").Invoke(register,
            new object[] { correctOwner ? owner : other });
        Assert.That(result, Is.EqualTo(correctOwner));
        Assert.That(products.Count, Is.EqualTo(correctOwner ? 0 : 1));
        Assert.That(RegisterType.GetField("isBusy").GetValue(register), Is.EqualTo(!correctOwner));
        if (correctOwner)
        {
            Assert.That(item.transform.parent, Is.EqualTo(owner.transform));
            Assert.That(item.activeSelf, Is.False);
            Assert.That(((Behaviour)product).enabled, Is.True);
            Assert.That(RegisterType.GetMethod("CanCompletePurchase", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(register, null), Is.False);
        }
    }

    [Test]
    public void PendingReturnRoundTripsAndReusesProductIdentityWhenRestocked()
    {
        Type saveType = Type.GetType("MarketShopandRetailSystem.SaveLoadManager, Assembly-CSharp");
        Type dataType = Type.GetType("MarketShopandRetailSystem.GameSaveData, Assembly-CSharp");
        Type recordType = Type.GetType("MarketShopandRetailSystem.SellableObjectData, Assembly-CSharp");
        var go = new GameObject("Test save"); go.SetActive(false); objects.Add(go);
        Component save = go.AddComponent(saveType);
        object data = Activator.CreateInstance(dataType);
        dataType.GetField("money").SetValue(data, 100);
        saveType.GetField("currentSaveData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(save, data);
        saveType.GetField("isInitialized", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(save, true);
        string file = System.IO.Path.GetTempFileName();
        saveType.GetField("saveFilePath", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(save, file);
        var item = new GameObject("Returned product"); item.SetActive(false); objects.Add(item);
        Type productType = Type.GetType("MarketShopandRetailSystem.SellableObject, Assembly-CSharp");
        Component product = item.AddComponent(productType);
        productType.GetField("Name").SetValue(product, "Milk");
        productType.GetField("sellableObjectIndex").SetValue(product, 7);
        object record = Activator.CreateInstance(recordType);
        recordType.GetField("productName").SetValue(record, "Milk");
        recordType.GetField("objectIndex").SetValue(record, 7);
        var records = (System.Collections.IList)dataType.GetField("sellableObjects").GetValue(data);
        records.Add(record);
        try
        {
            saveType.GetMethod("SaveReturnedProduct").Invoke(save, new object[] { product, null });
            object loaded = JsonUtility.FromJson(System.IO.File.ReadAllText(file), dataType);
            object pending = ((System.Collections.IList)dataType.GetField("sellableObjects").GetValue(loaded))[0];
            Assert.That(recordType.GetField("awaitingShelfReturn").GetValue(pending), Is.True);
            Assert.That(recordType.GetField("objectIndex").GetValue(pending), Is.EqualTo(7));
            var shelf = new GameObject("Shelf"); objects.Add(shelf);
            Type detailType = Type.GetType("MarketShopandRetailSystem.PhysicalEquipmentDetails, Assembly-CSharp");
            Component detail = shelf.AddComponent(detailType);
            detailType.GetField("Name").SetValue(detail, "Shelf");
            detailType.GetField("equipmentIndex").SetValue(detail, 2);
            Type placedType = Type.GetType("MarketShopandRetailSystem.PlacedObjectData, Assembly-CSharp");
            object placed = Activator.CreateInstance(placedType);
            placedType.GetField("equipmentName").SetValue(placed, "Shelf");
            placedType.GetField("objectIndex").SetValue(placed, 2);
            ((System.Collections.IList)dataType.GetField("placedObjects").GetValue(data)).Add(placed);
            var pointObject = new GameObject("Slot"); pointObject.transform.SetParent(shelf.transform);
            Component point = pointObject.AddComponent(Type.GetType("MarketShopandRetailSystem.PlacablePoint, Assembly-CSharp"));
            saveType.GetMethod("SaveReturnedProduct").Invoke(save, new object[] { product, point });
            Assert.That(records.Count, Is.EqualTo(1));
            Assert.That(recordType.GetField("awaitingShelfReturn").GetValue(record), Is.False);
            Assert.That(recordType.GetField("shelfName").GetValue(record), Is.EqualTo("Shelf"));
            Assert.That(recordType.GetField("objectIndex").GetValue(record), Is.EqualTo(7));
            Assert.That(dataType.GetField("money").GetValue(data), Is.EqualTo(100));
            object slot = ((System.Collections.IList)placedType.GetField("shelfSlots").GetValue(placed))[0];
            Assert.That(slot.GetType().GetField("status").GetValue(slot), Is.EqualTo(1));
        }
        finally { System.IO.File.Delete(file); }
    }

    [Test]
    public void BusyRegisterRejectsAnotherBasketWithoutReplacingCurrentProducts()
    {
        Component register = Register(true, 1f);
        RegisterType.GetField("isBusy").SetValue(register, true);
        object products = RegisterType.GetField("productsofCustomer").GetValue(register);
        var customerObject = new GameObject("Waiting customer");
        customerObject.SetActive(false);
        objects.Add(customerObject);
        Component customer = customerObject.AddComponent(Type.GetType("MarketShopandRetailSystem.CivilianController, Assembly-CSharp"));
        RegisterType.GetMethod("PutProductsYouGrabbed").Invoke(register,
            new object[] { customer, new List<GameObject>() });
        Assert.That(RegisterType.GetField("productsofCustomer").GetValue(register), Is.SameAs(products));
        Assert.That(RegisterType.GetField("isBusy").GetValue(register), Is.True);
    }
}
