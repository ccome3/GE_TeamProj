using UnityEngine;

public class WorldItem : MonoBehaviour
{
    // 🌟 맵 상의 아이템이 가진 인벤토리 데이터 (Item.cs 클래스 사용)
    public Item itemData;
    
    // 🌟 상호작용 준비 상태 변수
    private bool isPlayerInRange = false;
    
    // 🌟 참조할 컴포넌트
    private SpriteRenderer spriteRenderer;
    private InventoryManager inventoryManager;

    [Header("상호작용 설정")]
    public float highlightIntensity = 0.5f; // 테두리를 빛나게 할 강도
    private Color originalColor; 

    private void Start()
    {
        // 스프라이트 렌더러와 인벤토리 관리자 초기화
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        // InventoryManager를 씬에서 찾습니다. (플레이어 오브젝트에 있다고 가정)
        inventoryManager = FindObjectOfType<InventoryManager>(); 
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager를 씬에서 찾을 수 없습니다! 아이템 획득 불가.");
            enabled = false;
        }

        // 아이템의 SpriteRenderer에 ItemData의 Icon을 설정합니다. (선택 사항)
        if (itemData != null && itemData.Icon != null)
        {
            spriteRenderer.sprite = itemData.Icon;
        }
    }

    private void Update()
    {
        // 3. F를 누를 시 맵상에서 사라지고 인벤토리에 들어옴
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.F))
        {
            PickupItem();
        }
    }

    /// <summary>
    /// 아이템을 인벤토리에 추가하고 월드에서 제거하는 로직
    /// </summary>
    public void PickupItem()
    {
        if (inventoryManager != null && itemData != null)
        {
            // 인벤토리 관리자에게 아이템 추가 요청
            inventoryManager.AddItem(
                itemData.ItemID, 
                itemData.DisplayName, 
                itemData.ItemType, 
                itemData.Quantity
            );
            
            // 맵상에서 오브젝트 제거
            Destroy(gameObject); 
        }
    }

    // 2. 범위 안에 들어올 시 테두리 빛나며 상호작용할 준비
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어 태그 확인
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            // 테두리 빛나게 처리 (Shader/Material에 따라 다름. 여기서는 색상 변경으로 임시 구현)
            spriteRenderer.color = originalColor * (1f + highlightIntensity); 
            Debug.Log("F를 눌러 " + itemData.DisplayName + " 획득 가능");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            // 원래 색상으로 복구
            spriteRenderer.color = originalColor;
        }
    }
}