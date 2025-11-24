using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUIController : MonoBehaviour
{
    // 🌟 외부 연결 변수
    public GameObject slotPrefab; // 인벤토리 슬롯 프리팹 (3단계에서 생성)
    public Transform contentParent; // 슬롯이 배치될 부모 오브젝트 (Grid Layout Group)
    
    private InventoryManager inventoryManager;

    // 현재 생성된 슬롯 오브젝트 리스트 (갱신 및 삭제용)
    private List<GameObject> activeSlots = new List<GameObject>(); 

    private void Start()
    {
        // 씬에서 InventoryManager를 찾아 연결합니다.
        inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager를 씬에서 찾을 수 없습니다.");
            enabled = false;
        }
    }

    /// <summary>
    /// InventoryManager에서 이 함수를 호출하여 UI를 갱신합니다.
    /// </summary>
    public void RefreshInventoryUI()
    {
        // 1. 기존 슬롯 제거
        foreach (GameObject slot in activeSlots)
        {
            Destroy(slot);
        }
        activeSlots.Clear();

        // 2. 인벤토리 아이템을 순회하며 슬롯 생성
        List<Item> items = inventoryManager.inventoryItems;
        
        foreach (Item item in items)
        {
            if (item.Quantity > 0)
            {
                // 슬롯 생성 및 부모 설정
                GameObject slotObject = Instantiate(slotPrefab, contentParent);
                activeSlots.Add(slotObject);

                // 3. 슬롯의 UI 컴포넌트 설정
                // 이 부분은 2단계에서 정의할 InventorySlotUI 스크립트에 접근하여 처리합니다.
                
                // 예시: 슬롯 스크립트가 Item 객체를 받아 UI를 설정한다고 가정
                InventorySlotUI slotUI = slotObject.GetComponent<InventorySlotUI>();
                if (slotUI != null)
                {
                    slotUI.SetupSlot(item);
                }
            }
        }
    }
}