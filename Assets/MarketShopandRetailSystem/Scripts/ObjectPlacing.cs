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
        }

        private void Start()
        {
            meshRenderers = GetComponentsInChildren<MeshRenderer>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            Rigidbody_Placing = GetComponent<Rigidbody>();
        }

        private void OnTriggerStay(Collider other)
        {
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
                        materials[i].SetColor("_EmissionColor", Color.red);
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
                        Material[] materials = item.materials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            if (CanLocate)
                            {
                                materials[i].SetColor("_EmissionColor", Color.green);
                            }
                            else
                            {
                                materials[i].SetColor("_EmissionColor", Color.red);
                            }
                        }
                        item.materials = materials;
                    }
                }
            }
        }


        private void OnDestroy()
        {
            if (GetComponent<PhysicalEquipmentDetails>().equipmentType == EquipmentType.Poster)
            {
                if (spriteRenderer == null) return;
                spriteRenderer.color = Color.white;
            }
            else
            {
                if (meshRenderers == null) return;
                foreach (var item in meshRenderers)
                {
                    Material[] materials = item.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i].DisableKeyword("_EMISSION");
                    }
                    item.materials = materials;
                }
            }
            list.Clear();
        }
    }
}