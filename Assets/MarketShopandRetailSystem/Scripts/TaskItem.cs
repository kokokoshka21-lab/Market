using UnityEngine;

namespace MarketShopandRetailSystem
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/TaskItem", order = 1)]
    public class TaskItem : ScriptableObject
    {
        public string Starting_GameObject;
        public string Starting_Event_Name;
        public int ID;
        public string Title;
        public string Description;
        public Sprite Icon;
        public bool isDone;
        public bool isAssigned;
        public int CoinAmount;
        public bool ShowMarker = false;
        public string MarkerName;
        public int Amount = 1;
        public int TotalAmount = 1;
        public string ObjectiveName;
        public string End_GameObject;
        public string End_Event_Name;
        public int NextTaskIDToAssign;
    }
}