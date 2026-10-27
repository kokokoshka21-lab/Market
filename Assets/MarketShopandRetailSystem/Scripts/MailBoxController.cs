using System.Collections.Generic;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class MailBoxController : MonoBehaviour
    {
        public int Period_ForNewBill;
        public GameObject Sprite_Bill;
        private float LastTime_NewBill = 0;
        public int MinimumAmountOnBill = 5;
        public int MaximumAmountOnBill = 100;
        [HideInInspector]
        public bool thereIsBill = false;
        public List<string> BillTypes = new List<string>();
        public ShopScript shop;
        public static MailBoxController Instance;

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if(SaveLoadManager.Instance.HasActiveBill())
            {
                thereIsBill = true;
                Sprite_Bill.SetActive(true);
            }
        }

        public void RemoveTheBill()
        {
            SaveLoadManager.Instance.ClearCurrentBill();
            SaveLoadManager.Instance.SaveGame();
            thereIsBill = false;
            LastTime_NewBill = Time.time;
            Sprite_Bill.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            if(Time.time > LastTime_NewBill + Period_ForNewBill && !thereIsBill && shop.isRented)
            {
                LastTime_NewBill = Time.time;
                if(Random.Range(0,3) == 0)
                {
                    Sprite_Bill.SetActive(true);
                    string newBillType = BillTypes[Random.Range(0, BillTypes.Count)];
                    int billPrice = Random.Range(MinimumAmountOnBill, MaximumAmountOnBill);
                    SaveLoadManager.Instance.SetCurrentBillType(newBillType);
                    SaveLoadManager.Instance.SetCurrentBillAmount(billPrice);
                    SaveLoadManager.Instance.SaveGame();
                    thereIsBill = true;
                    GameCanvas.Instance.Show_Warning_Not("There is a new Bill in the MailBox!", false);
                }
            }
        }

        public void Interact()
        {
            string newBillType = SaveLoadManager.Instance.GetCurrentBillType();
            int BillAmount = SaveLoadManager.Instance.GetCurrentBillAmount();
            if(Sprite_Bill.activeSelf)
            {
                GameCanvas.Instance.ShowBills(newBillType, BillAmount);
            }
            else
            {
                GameCanvas.Instance.Show_Warning_Not("There is no Bill in the MailBox!", true);
            }
        }
    }
}
