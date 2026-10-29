using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Reflection keeps the test assembly independent of the project's predefined Assembly-CSharp.
public class BoxShelfRegressionTests
{
    private readonly List<GameObject> objects = new List<GameObject>();

    private static Type Script(string name)
    {
        Type type = Type.GetType("MarketShopandRetailSystem." + name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, name + " must be compiled");
        return type;
    }

    private Component Create(string script)
    {
        var go = new GameObject(script);
        go.SetActive(false);
        objects.Add(go);
        return go.AddComponent(Script(script));
    }

    private static void Set(object target, string name, object value)
    {
        target.GetType().GetField(name).SetValue(target, value);
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (GameObject go in objects) UnityEngine.Object.DestroyImmediate(go);
        objects.Clear();
    }

    [TestCase("boxShelfName")]
    [TestCase("boxShelfSceneId")]
    public void SavedAttachmentFreezesBoxBeforeSlotRestoration(string attachmentField)
    {
        Component box = Create("OrderBox");
        Rigidbody body = box.gameObject.AddComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = true;
        object saved = Activator.CreateInstance(Script("PlacedObjectData"));
        Set(saved, attachmentField, "saved shelf");

        box.GetType().GetMethod("PrepareShelfRestore").Invoke(box, new[] { saved });

        Assert.That(body.isKinematic, Is.True);
        Assert.That(body.useGravity, Is.False);
    }

    [Test]
    public void LooseBoxKeepsPhysicsOnLoad()
    {
        Component box = Create("OrderBox");
        Rigidbody body = box.gameObject.AddComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = true;
        box.GetType().GetMethod("PrepareShelfRestore").Invoke(box,
            new[] { Activator.CreateInstance(Script("PlacedObjectData")) });
        Assert.That(body.isKinematic, Is.False);
        Assert.That(body.useGravity, Is.True);
    }

    [Test]
    public void BoxSlotRequiresEnabledModeAndAnEmptyRegisteredPoint()
    {
        Component area = Create("ArrayableArea");
        Component point = Create("PlacablePoint");
        Array points = Array.CreateInstance(Script("PlacablePoint"), 1);
        points.SetValue(point, 0);
        Set(area, "PlacablePoints", points);
        MethodInfo canPlace = area.GetType().GetMethod("CanPlaceBox");
        Assert.That(canPlace.Invoke(area, new object[] { point }), Is.False);
        Set(area, "boxesOnly", true);
        Assert.That(canPlace.Invoke(area, new object[] { point }), Is.True);
        Set(point, "objectToGrab", objects[0]);
        Assert.That(canPlace.Invoke(area, new object[] { point }), Is.False);
        Set(point, "objectToGrab", null);
        Set(point, "isAvailable", false);
        Assert.That(canPlace.Invoke(area, new object[] { point }), Is.False);
        Assert.That(canPlace.Invoke(area, new object[] { Create("PlacablePoint") }), Is.False);
    }

    [Test]
    public void FlightStartsAtHeldPositionAndReleasesItsInteractionGuardOnCancellation()
    {
        Component box = Create("OrderBox");
        BoxCollider collider = box.gameObject.AddComponent<BoxCollider>();
        Component point = Create("PlacablePoint");
        point.transform.position = new Vector3(3f, 2f, 1f);
        Vector3 heldPosition = new Vector3(-1f, 1f, 0f);
        var flight = (System.Collections.IEnumerator)box.GetType()
            .GetMethod("FlyToShelf", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(box, new object[] { point, heldPosition });
        Assert.That(flight.MoveNext(), Is.True);
        Assert.That(box.transform.position, Is.EqualTo(heldPosition));
        Assert.That(collider.enabled, Is.False);
        Assert.That(box.GetType().GetProperty("IsMovingToShelf").GetValue(box), Is.True);
        ((IDisposable)flight).Dispose();
        Assert.That(collider.enabled, Is.True);
        Assert.That(box.GetType().GetProperty("IsMovingToShelf").GetValue(box), Is.False);
    }

    [Test]
    public void SceneShelfIdentityDistinguishesDuplicateNamesAndIsRepeatable()
    {
        Component first = Create("ArrayableArea");
        Component second = Create("ArrayableArea");
        second.gameObject.name = first.gameObject.name;
        foreach (Component area in new[] { first, second })
        {
            Set(area, "PlacablePoints", Array.CreateInstance(Script("PlacablePoint"), 0));
            area.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(area, null);
        }
        object firstId = first.GetType().GetProperty("SceneShelfId").GetValue(first);
        Assert.That(firstId, Is.Not.EqualTo(second.GetType().GetProperty("SceneShelfId").GetValue(second)));
        first.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(first, null);
        Assert.That(first.GetType().GetProperty("SceneShelfId").GetValue(first), Is.EqualTo(firstId));
    }

    [Test]
    public void OrdinarySceneObjectDoesNotAcquirePreviewMaterials()
    {
        Component placing = Create("ObjectPlacing");
        placing.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(placing, null);
        // HashSet<T> exposes Count through its concrete type.
        object set = placing.GetType().GetField("previewMaterials", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(placing);
        Assert.That((int)set.GetType().GetProperty("Count").GetValue(set), Is.Zero);
        Assert.That((bool)placing.GetType().GetField("isPlacementPreview", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(placing), Is.False);
    }
}
