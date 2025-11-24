using UnityEngine;

// [System.Serializable]은 Unity 인스펙터에 이 클래스를 표시할 수 있게 해줍니다.
[System.Serializable]
public class Item
{
    // 아이템을 식별하기 위한 고유 ID (예: "GUN_PISTOL", "CLUE_NOTE1")
    public string ItemID;
    
    // 인벤토리 UI에 표시될 이름
    public string DisplayName; 
    
    // 아이템이 속하는 카테고리 (무기, 소모품, 단서 등)
    public string ItemType; 
    
    // 아이템이 인벤토리에서 차지하는 수량
    public int Quantity; 
    
    // 아이템의 시각적 표현을 위한 스프라이트 (임시 도형 스프라이트)
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