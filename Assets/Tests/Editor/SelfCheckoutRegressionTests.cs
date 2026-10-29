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
