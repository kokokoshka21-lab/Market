using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class CashRegister : MonoBehaviour
    {
        public bool isBusy = false;
        public List<Transform> pointsToPutProducts;
        public List<SellableObject> productsofCustomer;
        public Transform pointAfterScanning;
        public Transform pointPlayerStand;
        public Transform pointCustomersCome;
        private int TotalPrice = 0;
        private int CustomerPaid = 0;
        public TMPro.TMP_Text Text_Total;
        public TMPro.TMP_Text Text_Change;
        public TMPro.TMP_Text Text_CustomerPaid;
        public GameObject Bag;
        public GameObject Keyboard;
        public GameObject Numbers;
        public GameObject BankNotes;
        public GameObject Sprite_Wrong;
        public GameObject Sprite_ShoppingDone;
        private CivilianController CurrentCivilian;
        public CashierController CurrentCashier;
        int ProductIndex = 0;

        public void PutProductsYouGrabbed(CivilianController civilian, List<GameObject> products)
        {
            if (products.Count > 0)
            {
                for (int i = 0; i < products.Count; i++)
                {
                    productsofCustomer.Add(products[i].GetComponent<SellableObject>());
                    products[i].transform.parent = this.transform;
                    products[i].SetActive(true);
                    products[i].transform.localPosition = pointsToPutProducts[i].transform.localPosition;
                }
                CurrentCivilian = civilian;
                TotalPrice = 0;
                Text_Total.text = AdvancedGameManager.Instance.CurrencySymbol + "0";
                ProductIndex = 0;
                Text_Change.text = AdvancedGameManager.Instance.CurrencySymbol + "0";
                isBusy = true;
                Bag.SetActive(true);
            }
            else
            {
                CurrentCivilian.GoAfterShopping();
            }
        }

        float lastTimeClick = 0;

        private void Update()
        {
            if(CurrentCashier != null && !CurrentCashier.gameObject.activeSelf)
            {
                CurrentCashier = null;
            }
        }

        public void Click_Button_Keyboard(int i)
        {
            if (Time.time > lastTimeClick + 0.25f)
            {
                lastTimeClick = Time.time;
                if (Text_Change.text.Length <= 5)
                {
                    Text_Change.text = AdvancedGameManager.Instance.CurrencySymbol + Text_Change.text.Replace(AdvancedGameManager.Instance.CurrencySymbol + "0", "").Replace(AdvancedGameManager.Instance.CurrencySymbol, "") + i.ToString();
                    AudioManager.Instance.Play_audioClip_KeyboardPress();
                }
            }
        }

        public void AddBanknote(int Amount)
        {
            if (Time.time > lastTimeClick + 0.25f)
            {
                lastTimeClick = Time.time;
                string currentText = Text_Change.text.Replace(AdvancedGameManager.Instance.CurrencySymbol + "0", "").Replace(AdvancedGameManager.Instance.CurrencySymbol, "");
                if(currentText.Length > 0)
                {
                    int currentamount = Convert.ToInt32(currentText);
                    currentamount += Amount;
                    Text_Change.text = AdvancedGameManager.Instance.CurrencySymbol + currentamount.ToString();
                }
                else
                {
                    int currentamount = 0;
                    currentamount += Amount;
                    Text_Change.text = AdvancedGameManager.Instance.CurrencySymbol + currentamount.ToString();
                }
            }
        }




        public void Click_Button_Process()
        {
            if (Time.time > lastTimeClick + 0.25f)
            {
                lastTimeClick = Time.time;
                if (Text_Change.text.Replace(AdvancedGameManager.Instance.CurrencySymbol + "0", "").Replace(AdvancedGameManager.Instance.CurrencySymbol, "").Length > 0)
                {
                    int playersinput = Convert.ToInt32(Text_Change.text.Replace(AdvancedGameManager.Instance.CurrencySymbol + "0", "").Replace(AdvancedGameManager.Instance.CurrencySymbol, ""));
                    int correctChange = CustomerPaid - TotalPrice;
                    if (playersinput == correctChange)
                    {
                        AdvancedGameManager.Instance.Get(CollactableType.Money, TotalPrice);
                        AudioManager.Instance.Play_audioClip_CashRegisterResult(true);
                        Bag.SetActive(false);
                        CurrentCivilian.GoAfterShopping();
                        for (int i = 0; i < productsofCustomer.Count; i++)
                        {
                            SaveLoadManager.Instance.RemoveSellableObject(productsofCustomer[i].Name, productsofCustomer[i].sellableObjectIndex);
                            InventoryManager.Instance.CurrentEquipmentList.Remove(productsofCustomer[i].gameObject);
                            Destroy(productsofCustomer[i].gameObject);
                        }
                        productsofCustomer.Clear();
                        StartCoroutine(ShowShoppingDone());
                        Text_Change.text = AdvancedGameManager.Instance.CurrencySymbol + "0";
                        Text_Total.text = AdvancedGameManager.Instance.CurrencySymbol + "0";
                        Text_CustomerPaid.text = AdvancedGameManager.Instance.CurrencySymbol + "0";
                    }
                    else
                    {
                        StartCoroutine(ShowError());
                    }
                }
            }
        }

        public void Click_Button_Process_ByCashier()
        {
            int correctChange = CustomerPaid - TotalPrice;
            AdvancedGameManager.Instance.Get(CollactableType.Money, TotalPrice);
            AudioManager.Instance.Play_audioClip_CashRegisterResult(true);
            Bag.SetActive(false);
            CurrentCivilian.GoAfterShopping();
            for (int i = 0; i < productsofCustomer.Count; i++)
            {
                SaveLoadManager.Instance.RemoveSellableObject(productsofCustomer[i].Name, productsofCustomer[i].sellableObjectIndex);
                InventoryManager.Instance.CurrentEquipmentList.Remove(productsofCustomer[i].gameObject);
                Destroy(productsofCustomer[i].gameObject);
            }
            productsofCustomer.Clear();
            StartCoroutine(ShowShoppingDone());
            Text_Change.text = AdvancedGameManager.Instance.CurrencySymbol + "0";
            Text_Total.text = AdvancedGameManager.Instance.CurrencySymbol + "0";
            Text_CustomerPaid.text = AdvancedGameManager.Instance.CurrencySymbol + "0";
        }


        IEnumerator ShowShoppingDone()
        {
            Keyboard.SetActive(false);
            Sprite_ShoppingDone.SetActive(true);
            yield return new WaitForSeconds(1);
            Sprite_ShoppingDone.SetActive(false);
            isBusy = false;
        }

        IEnumerator ShowError()
        {
            Keyboard.SetActive(false);
            Sprite_Wrong.SetActive(true);
            AudioManager.Instance.Play_audioClip_CashRegisterResult(false);
            yield return new WaitForSeconds(1);
            Keyboard.SetActive(true);
            if(AdvancedGameManager.Instance.givingChangeOption == GivingChangeOption.Both)
            {
                BankNotes.SetActive(true);
                Numbers.SetActive(true);
            }
            else if (AdvancedGameManager.Instance.givingChangeOption == GivingChangeOption.Banknotes)
            {
                BankNotes.SetActive(true);
                Numbers.SetActive(false);
            }
            else if (AdvancedGameManager.Instance.givingChangeOption == GivingChangeOption.Keyboard)
            {
                BankNotes.SetActive(false);
                Numbers.SetActive(true);
            }
            Sprite_Wrong.SetActive(false);
        }

        public void Click_Button_Remove()
        {
            if (Time.time > lastTimeClick + 0.25f)
            {
                lastTimeClick = Time.time;
                if (Text_Change.text.Length > 0)
                {
                    Text_Change.text = Text_Change.text.Substring(0, Text_Change.text.Length - 1);
                    AudioManager.Instance.Play_audioClip_KeyboardPress();
                }
            }
        }

        public void Scan(SellableObject product)
        {
            product.enabled = false;
            TotalPrice = TotalPrice + product.GetSellingPrice();
            Text_Total.text = AdvancedGameManager.Instance.CurrencySymbol + TotalPrice.ToString();
            AudioManager.Instance.Play_audioClip_Beep();
            StartMove(product.transform, pointAfterScanning.position, 0.5f);
            ProductIndex++;
            if (productsofCustomer.Count == ProductIndex)
            {
                if (CurrentCivilian != null)
                {
                    CustomerPaid = CurrentCivilian.MakePayment(TotalPrice);
                    Text_CustomerPaid.text = AdvancedGameManager.Instance.CurrencySymbol + CustomerPaid.ToString();
                    Keyboard.SetActive(true);
                    if (AdvancedGameManager.Instance.givingChangeOption == GivingChangeOption.Both)
                    {
                        BankNotes.SetActive(true);
                        Numbers.SetActive(true);
                    }
                    else if (AdvancedGameManager.Instance.givingChangeOption == GivingChangeOption.Banknotes)
                    {
                        BankNotes.SetActive(true);
                        Numbers.SetActive(false);
                    }
                    else if (AdvancedGameManager.Instance.givingChangeOption == GivingChangeOption.Keyboard)
                    {
                        BankNotes.SetActive(false);
                        Numbers.SetActive(true);
                    }
                }
            }
        }

        public void StartMove(Transform movingObject, Vector3 targetPosition, float duration)
        {
            StartCoroutine(MoveToPosition(movingObject, targetPosition, duration));
        }

        private IEnumerator MoveToPosition(Transform movingObject, Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = movingObject.position;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                movingObject.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }
            if(movingObject != null)
            {
                movingObject.position = targetPosition;
                movingObject.gameObject.SetActive(false);
            }
        }
    }
}
