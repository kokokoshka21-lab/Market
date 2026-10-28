using System.Collections.Generic;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class ObjectPlacing : MonoBehaviour
    {
        public bool CanLocate = false;
        [HideInInspector]
        public List<GameObject> list = new List<GameObject>();
        private bool isTriggeringCorrect = false;
        private MeshRenderer[] meshRenderers;
        private SpriteRenderer spriteRenderer;
        [HideInInspector]
        public Rigidbody Rigidbody_Placing;
        private readonly HashSet<Material> previewMaterials = new HashSet<Material>();

        public void ConfigureRelocationPreview()
        {
            CacheComponents();
            foreach (MeshRenderer renderer in meshRenderers)
            {
                Material[] copies = renderer.sharedMaterials;
                for (int i = 0; i < copies.Length; i++)
                {
                    copies[i] = new Material(copies[i]);
                    copies[i].EnableKeyword("_EMISSION");
                    previewMaterials.Add(copies[i]);
                }
                renderer.sharedMaterials = copies;
            }
        }

        private void CacheComponents()
        {
            meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            Rigidbody_Placing = GetComponent<Rigidbody>();
        }


        public void OnTriggerEnter(Collider other)
        {
            if (GetComponent<PhysicalEquipmentDetails>().equipmentType == EquipmentType.Decoration)
            {
                if (other.CompareTag("Shop"))
                {
                    isTriggeringCorrect = true;
                }
                else if (!other.CompareTag("Player") && other.name != "Trigger" && !list.Contains(other.gameObject))
                {
                    list.Add(other.gameObject);
                }
            }
            else if(GetComponent<PhysicalEquipmentDetails>().equipmentType == EquipmentType.Sellable)
            {
                isTriggeringCorrect = true;
                if (!other.CompareTag("Player") && !other.CompareTag("Shop") && !list.Contains(other.gameObject))
                {
                    list.Add(other.gameObject);
                }
            }
        }

        private void OnDisable()
        {
            list.Clear();
            isTriggeringCorrect = false;
            CanLocate = false;
        }

        public bool CanConfirmPlacement()
        {
            if (!isActiveAndEnabled || !CanLocate) return false;
            if (GetComponent<PhysicalEquipmentDetails>().equipmentType == EquipmentType.Poster) return true;
            Physics.SyncTransforms();
            bool insideShop = false;
            foreach (BoxCollider box in GetComponents<BoxCollider>())
            {
                if (!box.enabled) continue;
                Vector3 scale = box.transform.lossyScale;
                scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                Vector3 half = Vector3.Scale(box.size, scale) * 0.5f;
                half = new Vector3(Mathf.Max(0.001f, half.x - 0.015f), Mathf.Max(0.001f, half.y - 0.015f),
                    Mathf.Max(0.001f, half.z - 0.015f));
                foreach (Collider other in Physics.OverlapBox(box.transform.TransformPoint(box.center), half,
                    box.transform.rotation, ~0, QueryTriggerInteraction.Collide))
                {
                    if (other.transform.IsChildOf(transform)) continue;
                    if (other.CompareTag("Shop")) insideShop = true;
                    else if (!other.isTrigger && !other.CompareTag("Player") && other.name != "Trigger") return false;
                }
            }
            return insideShop;
        }

        private void Start()
        {
            CacheComponents();
        }

        private void OnTriggerStay(Collider other)
        {
            if (GetComponent<PhysicalEquipmentDetails>().equipmentType != EquipmentType.Poster)
            {
                OnTriggerEnter(other);
                return;
            }
            if (GetComponent<PhysicalEquipmentDetails>().equipmentType == EquipmentType.Poster)
            {
                if (other.CompareTag("Wall"))
                {
                    isTriggeringCorrect = true;
                    if (!list.Contains(other.gameObject)) list.Add(other.gameObject);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (GetComponent<PhysicalEquipmentDetails>().equipmentType == EquipmentType.Decoration)
            {
                if (other.CompareTag("Shop"))
                {
                    isTriggeringCorrect = false;
                }
                else
                {
                    list.Remove(other.gameObject);
                }
            }
            else if (GetComponent<PhysicalEquipmentDetails>().equipmentType == EquipmentType.Poster)
            {
                if (other.CompareTag("Wall"))
                {
                    isTriggeringCorrect = false;
                    if (list.Contains(other.gameObject)) list.Remove(other.gameObject);
                }
            }
            else if (GetComponent<PhysicalEquipmentDetails>().equipmentType == EquipmentType.Sellable && list.Contains(other.gameObject))
            {
                list.Remove(other.gameObject);
            }
        }

        float lastTimeRed = 0;

        public void MakeRed()
        {
            CanLocate = false;
            if(Time.time > lastTimeRed + 0.25f)
            {
                lastTimeRed = Time.time;
                foreach (var item in meshRenderers)
                {
                    Material[] materials = item.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i].HasProperty("_EmissionColor")) materials[i].SetColor("_EmissionColor", Color.red);
                    }
                    item.materials = materials;
                }
            }
        }

        private float LastTime = 0;
        private void Update()
        {
            if (Time.time > LastTime + 0.2f)
            {
                LastTime = Time.time;
                if (GetComponent<PhysicalEquipmentDetails>().equipmentType == EquipmentType.Poster)
                {
                    CanLocate = isTriggeringCorrect;
                    if (list.Count > 0)
                    {
                        transform.eulerAngles = new Vector3(transform.eulerAngles.x, list[0].transform.eulerAngles.y, list[0].transform.eulerAngles.z);
                    }

                    if (CanLocate)
                    {
                        spriteRenderer.color = Color.green;
                    }
                    else
                    {
                        spriteRenderer.color = Color.red;
                    }
                }
                else
                {
                    CanLocate = isTriggeringCorrect;
                    if (list.Count > 0)
                    {
                        CanLocate = false;
                    }
                    foreach (var item in meshRenderers)
                    {
                        if (item == null) continue;
                        Material[] materials = item.materials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            if (CanLocate)
                            {
                                if (materials[i].HasProperty("_EmissionColor")) materials[i].SetColor("_EmissionColor", Color.green);
                            }
                            else
                            {
                                if (materials[i].HasProperty("_EmissionColor")) materials[i].SetColor("_EmissionColor", Color.red);
                            }
                        }
                        item.materials = materials;
                    }
                }
            }
        }


        private void OnDestroy()
        {
            PhysicalEquipmentDetails detail = GetComponent<PhysicalEquipmentDetails>();
            if (detail != null && detail.equipmentType == EquipmentType.Poster)
            {
                if (spriteRenderer == null) return;
                spriteRenderer.color = Color.white;
            }
            else
            {
                if (meshRenderers == null) return;
                foreach (var item in meshRenderers)
                {
                    if (item == null) continue;
                    Material[] materials = item.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i].DisableKeyword("_EMISSION");
                    }
                    item.materials = materials;
                }
            }
            list.Clear();
            foreach (Material material in previewMaterials)
                if (material != null) Destroy(material);
            previewMaterials.Clear();
        }
    }
}
