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

    [TestCase(false)]
    [TestCase(true)]
    public void AimTintPreservesSharedMaterialAndRestoresExistingOverrides(bool perMaterial)
    {
        Component box = Create("OrderBox");
        MeshRenderer renderer = box.gameObject.AddComponent<MeshRenderer>();
        Shader shader = Shader.Find("Sprites/Default");
        Assert.That(shader, Is.Not.Null);
        var material = new Material(shader);
        try
        {
            renderer.sharedMaterial = material;
            Color sharedColor = material.GetColor("_Color");
            var original = new MaterialPropertyBlock();
            original.SetColor("_Color", Color.blue);
            if (perMaterial) renderer.SetPropertyBlock(original, 0);
            else renderer.SetPropertyBlock(original);
            MethodInfo highlight = box.GetType().GetMethod("SetAimHighlight");
            highlight.Invoke(box, new object[] { true });
            var tinted = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(tinted, 0);
            Assert.That(tinted.GetColor("_Color"), Is.EqualTo(Color.Lerp(Color.blue, Color.yellow, 0.25f)));
            Assert.That(renderer.sharedMaterial, Is.SameAs(material));
            Assert.That(material.GetColor("_Color"), Is.EqualTo(sharedColor));
            highlight.Invoke(box, new object[] { false });
            var restored = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(restored, 0);
            if (perMaterial) Assert.That(restored.GetColor("_Color"), Is.EqualTo(Color.blue));
            else
            {
                Assert.That(restored.isEmpty, Is.True);
                renderer.GetPropertyBlock(restored);
                Assert.That(restored.GetColor("_Color"), Is.EqualTo(Color.blue));
            }
        }
        finally
        {
            box.GetType().GetMethod("SetAimHighlight").Invoke(box, new object[] { false });
            renderer.sharedMaterial = null;
            UnityEngine.Object.DestroyImmediate(material);
        }
    }

    [Test]
    public void AimSelectsTheSpecificBoxInsideShelfBoundsAndDoesNotLookThroughWalls()
    {
        Component hero = Create("HeroPlayerScript");
        Set(hero, "LayersToCheck", (LayerMask)(1 << 31));
        Component shelf = Create("ArrayableArea");
        Set(shelf, "PlacablePoints", Array.CreateInstance(Script("PlacablePoint"), 0));
        Set(shelf, "boxesOnly", true);
        shelf.gameObject.layer = 31;
        shelf.transform.position = new Vector3(0f, 1000f, 1.5f);
        BoxCollider bounds = shelf.gameObject.AddComponent<BoxCollider>();
        bounds.size = new Vector3(2f, 2f, 2f);
        shelf.gameObject.SetActive(true);
        Component[] boxes = { Create("OrderBox"), Create("OrderBox") };
        for (int index = 0; index < boxes.Length; index++)
        {
            Component point = Create("PlacablePoint");
            point.transform.SetParent(shelf.transform);
            Component box = boxes[index];
            box.gameObject.layer = 31;
            box.transform.position = new Vector3(index == 0 ? -0.4f : 0.4f, 1000f, 1.5f);
            box.gameObject.AddComponent<BoxCollider>().size = Vector3.one * 0.3f;
            point.gameObject.layer = 31;
            point.transform.position = box.transform.position - Vector3.forward * 0.4f;
            SphereCollider marker = point.gameObject.AddComponent<SphereCollider>();
            marker.radius = 0.1f;
            marker.isTrigger = true;
            point.gameObject.SetActive(true);
            box.GetType().GetField("<ShelfPoint>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(box, point);
            box.gameObject.SetActive(true);
        }
        MethodInfo raycast = hero.GetType().GetMethod("RaycastInteraction", BindingFlags.Instance | BindingFlags.NonPublic);
        Physics.SyncTransforms();
        foreach (Component box in boxes)
        {
            Ray ray = new Ray(new Vector3(box.transform.position.x, 1000f, 0f), Vector3.forward);
            object[] args = { ray, default(RaycastHit) };
            Assert.That(raycast.Invoke(hero, args), Is.True);
            Assert.That(((RaycastHit)args[1]).collider.gameObject, Is.EqualTo(box.gameObject));
        }
        var wall = new GameObject("Obstruction");
        objects.Add(wall);
        wall.layer = 31;
        wall.transform.position = new Vector3(-0.4f, 1000f, 0.9f);
        wall.AddComponent<BoxCollider>().size = new Vector3(0.4f, 1f, 0.1f);
        Physics.SyncTransforms();
        object[] blocked = { new Ray(new Vector3(-0.4f, 1000f, 0f), Vector3.forward), default(RaycastHit) };
        Assert.That(raycast.Invoke(hero, blocked), Is.True);
        Assert.That(((RaycastHit)blocked[1]).collider.GetComponentInParent(Script("OrderBox")), Is.Null);
    }

    [Test]
    public void PlacementPreviewIsWhiteVisualOnlyAndCleansUpWithoutChangingTheBox()
    {
        Component box = Create("OrderBox");
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Box visual";
        visual.transform.SetParent(box.transform, false);
        visual.transform.localPosition = new Vector3(0.1f, 0.2f, 0.3f);
        Component contents = Create("SellableObject");
        contents.name = "Stored Product";
        contents.transform.SetParent(box.transform, false);
        Shader shader = Shader.Find("Sprites/Default");
        Assert.That(shader, Is.Not.Null);
        Material sourceMaterial = new Material(shader);
        sourceMaterial.SetColor("_Color", Color.red);
        visual.GetComponent<MeshRenderer>().sharedMaterial = sourceMaterial;
        MethodInfo create = box.GetType().GetMethod("CreatePlacementPreview", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo cleanup = box.GetType().GetMethod("DestroyPlacementPreview", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo previewField = box.GetType().GetField("placementPreview", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            create.Invoke(box, null);
            GameObject preview = (GameObject)previewField.GetValue(box);
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
            Assert.That(preview.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(preview.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
            Assert.That(preview.transform.Find("Stored Product"), Is.Null);
            MeshRenderer ghost = preview.GetComponentInChildren<MeshRenderer>(true);
            Material ghostMaterial = ghost.sharedMaterial;
            Assert.That(ghostMaterial, Is.Not.SameAs(sourceMaterial));
            Assert.That(ghostMaterial.GetColor("_Color"), Is.EqualTo(Color.white));
            Assert.That(ghost.transform.localPosition, Is.EqualTo(visual.transform.localPosition));
            Assert.That(sourceMaterial.GetColor("_Color"), Is.EqualTo(Color.red));
            Assert.That(visual.GetComponent<BoxCollider>().enabled, Is.True);
            cleanup.Invoke(box, null);
            Assert.That(preview == null, Is.True);
            Assert.That(ghostMaterial == null, Is.True);
            Assert.That(previewField.GetValue(box), Is.Null);
            cleanup.Invoke(box, null); // Repeated cleanup on disable/destroy is safe.
        }
        finally
        {
            cleanup.Invoke(box, null);
            visual.GetComponent<MeshRenderer>().sharedMaterial = null;
            UnityEngine.Object.DestroyImmediate(sourceMaterial);
        }
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
