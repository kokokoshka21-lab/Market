using UnityEngine;
using UnityEngine.UI;

namespace MarketShopandRetailSystem
{
    public class PanelInventoryTabsSelector : MonoBehaviour
    {
        public GameObject[] Tabs;
        public GameObject[] TabContents;
        public static PanelInventoryTabsSelector Instance;


        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            for (int i = 0; i < TabContents.Length; i++)
            {
                TabContents[i].GetComponent<ScrollRect>().verticalNormalizedPosition = 1.0f;
            }
        }

        public void Select(int index)
        {
            for (int i = 0; i < Tabs.Length; i++)
            {
                Tabs[i].GetComponent<UnityEngine.UI.Outline>().enabled = false;
            }
            for (int i = 0; i < TabContents.Length; i++)
            {
                TabContents[i].SetActive(false);
            }
            Tabs[index].GetComponent<UnityEngine.UI.Outline>().enabled = true;
            TabContents[index].SetActive(true);
        }
    }
}