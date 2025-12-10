using UnityEngine;
using System.Collections.Generic;
using System.Linq; // LINQ를 사용하여 검색 기능 활용

public class NPCInteraction : MonoBehaviour
{
    [Header("NPC 설정")]
    public string npcName = "낯선 NPC";
    public List<DialogueLine> dialogue; // 🌟 기본 대화 내용 목록 (조건 불충족 시)
    
    [Header("UI 연결 (3단계에서 사용)")]
    public DialogueManager dialogueManager; // 🌟 대화 관리자 스크립트 참조
    
    // 🌟🌟🌟 조건부 대화 및 액션 변수 🌟🌟🌟
    [Header("조건부 대화 및 액션")]
    public string requiredItemID = "Letter"; // 🌟 필요한 아이템 ID
    public List<DialogueLine> conditionalDialogue; // 🌟 아이템 소지 시의 대화 내용
    public List<DialogueLine> currentDialogueSet; // 🌟 현재 사용될 대화 목록 (시작 시점에 결정)
    
    [Header("퀘스트 액션 설정")]
    public List<GameObject> wreckagesToActivate; // 🌟 활성화할 오브젝트의 태그
    private InventoryManager inventoryManager; // 인벤토리 참조를 위해 추가
    // 🌟🌟🌟 ---------------------- 🌟🌟🌟
    
    private bool isPlayerInRange = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor; 

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        dialogueManager = FindObjectOfType<DialogueManager>();
        
        // 🌟 [추가] 인벤토리 관리자 참조
        inventoryManager = FindObjectOfType<InventoryManager>(); 
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager를 씬에서 찾을 수 없습니다!");
        }
        
        // 🌟 시작 시 기본 대화를 현재 대화 세트로 설정
        currentDialogueSet = dialogue; 
    }

    private void Update()
    {
        // 🌟 F키 입력 감지 및 대화 시작
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.F))
        {
            if (dialogueManager != null)
            {
                // 🌟 [핵심] 대화 시작 전에 조건 검사 및 대화 목록 설정
                CheckAndSetDialogue(); 
                
                // 🌟 [핵심] DialogueManager에게 이 NPC 스크립트(this)를 전달
                dialogueManager.StartDialogue(currentDialogueSet, this); 
            }
            Debug.Log("F키 입력: 대화 시작 요청!");
        }
    }

    /// <summary>
    /// 🌟 [추가] 아이템 소지 여부에 따라 사용할 대화 목록을 설정합니다.
    /// </summary>
    private void CheckAndSetDialogue()
    {
        // 인벤토리에 요구 아이템이 있고 수량이 1개 이상일 경우
        if (inventoryManager != null && inventoryManager.HasItem(requiredItemID))
        {
            currentDialogueSet = conditionalDialogue; // 조건부 대화 사용
            Debug.Log($"아이템 '{requiredItemID}' 소지 확인. 조건부 대화로 설정.");
        }
        else
        {
            currentDialogueSet = dialogue; // 기본 대화 사용
        }
    }

    /// <summary>
    /// 🌟 [추가] 대화 종료 후 실행할 조건부 액션 (DialogueManager가 호출)
    /// </summary>
    public void PostDialogueAction()
    {
        // 조건부 대화(Letter 소지)가 사용되었을 때만 후속 액션 실행
        if (currentDialogueSet == conditionalDialogue) 
        {
            Debug.Log("조건부 대화 종료됨. 후속 액션 실행 시작.");
            
            // 1. 아이템 소모 (RemoveItem은 성공 시 true 반환)
            bool consumed = inventoryManager.RemoveItem(requiredItemID, 1);
            
            if (consumed)
            {
                Debug.Log($"조건 충족! 아이템 '{requiredItemID}' 소모 완료.");
                
                if (wreckagesToActivate.Count > 0)
                {
                    foreach (GameObject obj in wreckagesToActivate)
                    {
                        // null 체크는 항상 안전합니다.
                        if (obj != null) 
                        {
                            obj.SetActive(true); // 비활성화 -> 활성화
                            Debug.Log($"오브젝트 '{obj.name}' 활성화됨.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("NPCInteraction의 'Wreckages To Activate' 목록이 비어 있거나 할당되지 않았습니다.");
                }
            }
            else
            {
                Debug.LogWarning("아이템 소모에 실패했습니다 (아이템이 이미 사라졌을 수 있음). 오브젝트 활성화는 건너뜁니다.");
            }
        }
    }
    
    // 🌟 플레이어 감지: 범위 안에 들어올 시 테두리 빛남 (기존과 동일)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            spriteRenderer.color = originalColor * 1.5f; // 강조 표시
            Debug.Log($"NPC '{npcName}'와 상호작용 가능. F키를 누르세요.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            spriteRenderer.color = originalColor; // 원래 색상으로 복구
        }
    }
}

[System.Serializable]
public class DialogueLine
{
    // NPC 프로필에 표시될 이름 (필요 없으면 비워도 됨)
    public string CharacterName;
    
    // NPC의 대사 내용
    [TextArea(3, 10)] // Unity 에디터에서 여러 줄 입력 가능하게 함
    public string LineText;
    
    // NPC의 임시 프로필 사진용 스프라이트
    public Sprite ProfileImage; 
}