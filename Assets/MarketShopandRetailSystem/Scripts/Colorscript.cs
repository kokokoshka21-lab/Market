using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class Colorscript : MonoBehaviour
    {
        public Color colorCode;

        public void Click_Code()
        {
            GameCanvas.Instance.SelectColor(colorCode);
        }
    }
}
