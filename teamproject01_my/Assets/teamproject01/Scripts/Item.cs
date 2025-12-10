using UnityEngine;

// [System.Serializable]은 Unity 인스펙터에 이 클래스를 표시할 수 있게 해줍니다.
[System.Serializable]
public class Item
{
    // 아이템을 식별하기 위한 고유 ID (예: "GUN_PISTOL", "CLUE_NOTE1")
    public string ItemID;
    public string DisplayName; 
    public string ItemType; 
    public int Quantity; 
    public Sprite Icon; 

    public Item(string id, string name, string type, int quantity, Sprite icon)
    {
        ItemID = id;
        DisplayName = name;
        ItemType = type;
        Quantity = quantity;
        Icon = icon;
    }
}