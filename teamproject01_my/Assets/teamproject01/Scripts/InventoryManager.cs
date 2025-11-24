using UnityEngine;
using System.Collections.Generic;
using System.Linq; // LINQ를 사용하여 정렬 및 검색 기능 활용

public class InventoryManager : MonoBehaviour
{
    // 🌟🌟🌟 인벤토리 핵심 변수 🌟🌟🌟
    public List<Item> inventoryItems = new List<Item>();
    public GameObject inventoryUI; // 인벤토리 UI 패널 (캔버스 아래에 있어야 함)
    
    private bool isInventoryOpen = false;
    
    // 🌟🌟🌟 UI 담당 스크립트 (3단계에서 사용) 🌟🌟🌟
    // public InventoryUIController uiController; 

    private void Start()
    {
        // 인벤토리 UI는 시작 시 숨겨져 있어야 합니다.
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
        }
        
        // 디버그용 임시 아이템 추가 (예시)
        AddItem("CLUE_NOTE1", "낡은 노트", "단서", 1); 
        AddItem("AMMO_BULLET", "탄약", "소모품", 10);
        AddItem("AMMO_BULLET", "탄약", "소모품", 5); // 중첩 테스트
        AddItem("WEAPON_PIPE", "쇠 파이프", "무기", 1);
        
        // 획득 순으로 정렬 (List의 기본 속성이 획득 순서입니다.)
        // 여기서 별도의 정렬 로직은 필요 없습니다.
    }

    private void Update()
    {
        // 'B' 또는 'ESC' 입력 감지
        if (Input.GetKeyDown(KeyCode.B) || (isInventoryOpen && Input.GetKeyDown(KeyCode.Escape)))
        {
            ToggleInventory();
        }
    }

    /// <summary>
    /// 인벤토리 창을 열고 닫고, 게임 시간을 제어합니다.
    /// </summary>
    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        if (inventoryUI != null)
        {
            inventoryUI.SetActive(isInventoryOpen);
        }
        
        // 🌟🌟🌟 게임 일시 정지/재개 로직 🌟🌟🌟
        if (isInventoryOpen)
        {
            Time.timeScale = 0f; // 시간 정지
            // UI 컨트롤러가 있다면 여기서 갱신 로직 호출 (3단계)
            // uiController.RefreshInventoryUI(inventoryItems);
            
            Debug.Log("인벤토리 열림. 게임 일시 정지.");
        }
        else
        {
            Time.timeScale = 1f; // 시간 재개
            Debug.Log("인벤토리 닫힘. 게임 재개.");
        }
    }

    /// <summary>
    /// 아이템을 인벤토리에 추가하고, 중첩 가능한 아이템은 수량을 증가시킵니다.
    /// </summary>
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

        // 획득 시 UI 갱신 (3단계에서 구현)
    }
}