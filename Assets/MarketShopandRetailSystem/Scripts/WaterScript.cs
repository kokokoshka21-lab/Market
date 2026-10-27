using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class WaterScript : MonoBehaviour
    {
        public float speed = 0.5f;
        private Material waterMaterial;
        private Vector2 offset = Vector2.zero;

        private void Start()
        {
            waterMaterial = GetComponent<Renderer>().material;
        }

        void Update()
        {
            offset.x += speed * Time.deltaTime;
            waterMaterial.SetTextureOffset("_MainTex", offset);
        }
    }
}
