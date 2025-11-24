using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 호버 감지를 위해 필요

// IPointerEnterHandler와 IPointerExitHandler 인터페이스를 상속받습니다.
public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // 🌟 슬롯 내 UI 컴포넌트
    public Image itemIcon;
    public Text quantityText; // 호버 시 수량을 표시할 임시 텍스트
    
    private Item currentItem;

    /// <summary>
    /// 슬롯 UI를 초기화하고 아이템 정보를 설정합니다.
    /// </summary>
    public void SetupSlot(Item item)
    {
        currentItem = item;
        
        // 아이콘 표시
        if (item.Icon != null)
        {
            itemIcon.sprite = item.Icon;
            itemIcon.color = Color.white; // 아이템이 있으면 보이게
        }
        else
        {
            // 임시 도형 아이콘이 없다면 투명하게 처리
            itemIcon.color = new Color(1, 1, 1, 0.1f);
        }
        
        // 시작 시 수량 텍스트 숨기기
        quantityText.gameObject.SetActive(false);
    }
    
    // 🌟 호버 기능 구현 (마우스 커서가 올라갔을 때)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            quantityText.text = $"수량: {currentItem.Quantity}";
            quantityText.gameObject.SetActive(true); // 수량 텍스트 표시
        }
    }

    // 🌟 호버 기능 구현 (마우스 커서가 나갔을 때)
    public void OnPointerExit(PointerEventData eventData)
    {
        quantityText.gameObject.SetActive(false); // 수량 텍스트 숨기기
    }
}