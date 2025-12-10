using UnityEngine;
using System.Collections.Generic;
using System.Linq; // LINQ를 사용하여 정렬 및 검색 기능 활용

public class InventoryManager : MonoBehaviour
{
    // 🌟🌟🌟 인벤토리 핵심 변수 🌟🌟🌟
    public List<Item> inventoryItems = new List<Item>();
    
    private void Start()
    {
        // 디버그용 임시 아이템 추가 (예시)
        AddItem("CLUE_NOTE1", "낡은 노트", "단서", 1); 
        AddItem("AMMO_BULLET", "탄약", "소모품", 10);
        AddItem("AMMO_BULLET", "탄약", "소모품", 5); // 중첩 테스트
        AddItem("WEAPON_PIPE", "쇠 파이프", "무기", 1);
        
        // 🌟 테스트용 Letter 아이템 추가 🌟
        // AddItem("Letter", "NPC의 편지", "퀘스트", 1); 
    }

    public void AddItem(string id, string name, string type, int quantity)
    {
        // 1. 중첩 가능한 아이템인지 확인 (ItemID가 같으면 중첩)
        Item existingItem = inventoryItems.FirstOrDefault(item => item.ItemID == id);

        if (existingItem != null)
        {
            // 중첩 가능한 아이템이 이미 있다면 수량만 증가
            existingItem.Quantity += quantity;
            Debug.Log($"{name} 수량 증가: {existingItem.Quantity}");
        }
        else
        {
            // 새로운 아이템 생성 및 추가 (아이콘은 일단 null)
            Item newItem = new Item(id, name, type, quantity, null);
            inventoryItems.Add(newItem);
            Debug.Log($"{name} 획득. 새 아이템 추가됨.");
        }

        // 획득 시 UI 갱신 (추후 구현)
    }

    /// <summary>
    /// 🌟 [추가] 특정 아이템을 1개 이상 가지고 있는지 확인
    /// </summary>
    public bool HasItem(string id)
    {
        return inventoryItems.Any(item => item.ItemID == id && item.Quantity > 0);
    }

    /// <summary>
    /// 🌟 [추가] 특정 아이템 수량을 감소시키고, 수량이 0이 되면 제거
    /// </summary>
    public bool RemoveItem(string id, int quantityToRemove = 1)
    {
        Item itemToRemove = inventoryItems.FirstOrDefault(item => item.ItemID == id);

        if (itemToRemove == null || itemToRemove.Quantity < quantityToRemove)
        {
            // 아이템이 없거나 수량이 부족함
            Debug.LogWarning($"아이템 소모 실패: {id}. 수량 부족.");
            return false;
        }

        itemToRemove.Quantity -= quantityToRemove;
        Debug.Log($"아이템 소모: {itemToRemove.DisplayName}. 남은 수량: {itemToRemove.Quantity}");

        if (itemToRemove.Quantity <= 0)
        {
            // 수량이 0이 되면 인벤토리에서 완전히 제거
            inventoryItems.Remove(itemToRemove);
            Debug.Log($"{itemToRemove.DisplayName} 인벤토리에서 완전히 제거됨.");
        }
        
        // UI 갱신 (추후 구현)

        return true;
    }
}