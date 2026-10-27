using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class BanknoteScript : MonoBehaviour
    {
        public TMPro.TMP_Text Text_Amount1;
        public TMPro.TMP_Text Text_Amount2;
        public TMPro.TMP_Text Text_Currency;
        public int Amount;
        public CashRegister ParentCashRegister;

        void Start()
        {
            Text_Currency.text = AdvancedGameManager.Instance.CurrencySymbol;
            Text_Amount1.text = Amount.ToString();
            Text_Amount2.text = Amount.ToString();
        }

        public void Select_Banknote()
        {
            ParentCashRegister.AddBanknote(Amount);
            AudioManager.Instance.Play_Banknotes();
        }
    }
}
