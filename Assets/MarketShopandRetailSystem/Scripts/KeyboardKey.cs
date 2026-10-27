using UnityEngine;
using UnityEngine.UI;

namespace MarketShopandRetailSystem
{
    public class KeyboardKey : MonoBehaviour
    {
        public void Click()
        {
            GetComponent<Button>().onClick.Invoke();
        }
    }
}
