using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NPCInteraction : MonoBehaviour
{
    [Header("NPC 설정")]
    public string npcName = "낯선 NPC";
    public List<DialogueLine> dialogue; 
    
    [Header("UI 연결")]
    public DialogueManager dialogueManager; 
    
    [Header("조건부 대화 및 액션")]
    public string requiredItemID = "Letter"; 
    public List<DialogueLine> conditionalDialogue; 
    public List<DialogueLine> currentDialogueSet; 
    
    [Header("퀘스트 액션 설정")]
    public List<GameObject> wreckagesToActivate; 
    private InventoryManager inventoryManager; 
    
    private bool isPlayerInRange = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor; 

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        dialogueManager = FindObjectOfType<DialogueManager>();
        inventoryManager = FindObjectOfType<InventoryManager>(); 
        
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager를 씬에서 찾을 수 없습니다!");
        }
        
        currentDialogueSet = dialogue; 
    }

    private void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.F))
        {
            if (dialogueManager != null)
            {
                CheckAndSetDialogue(); 
                dialogueManager.StartDialogue(currentDialogueSet, this); 
            }
        }
    }

    private void CheckAndSetDialogue()
    {
        // 🌟 아이템 소지 여부만 확인 (소모하지 않음)
        if (inventoryManager != null && inventoryManager.HasItem(requiredItemID))
        {
            currentDialogueSet = conditionalDialogue;
            Debug.Log($"아이템 '{requiredItemID}' 소지 중. 조건부 대화 시작.");
        }
        else
        {
            currentDialogueSet = dialogue;
        }
    }

    /// <summary>
    /// 🌟 대화 종료 후 실행할 액션 (아이템 소모 로직 제거됨)
    /// </summary>
    public void PostDialogueAction()
    {
        // 조건부 대화가 정상적으로 진행된 경우에만 오브젝트 활성화 등 후속 액션 실행
        if (currentDialogueSet == conditionalDialogue) 
        {
            Debug.Log("조건부 대화 종료됨. 후속 오브젝트 활성화 액션 실행.");
            
            // 🌟 아이템 삭제 로직을 삭제했습니다.
            // 아이템은 인벤토리에 그대로 남습니다.

            if (wreckagesToActivate.Count > 0)
            {
                foreach (GameObject obj in wreckagesToActivate)
                {
                    if (obj != null) 
                    {
                        obj.SetActive(true); 
                        Debug.Log($"오브젝트 '{obj.name}' 활성화됨.");
                    }
                }
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            spriteRenderer.color = originalColor * 1.5f;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            spriteRenderer.color = originalColor;
        }
    }
}

[System.Serializable]
public class DialogueLine
{
    public string CharacterName;
    [TextArea(3, 10)]
    public string LineText;
    public Sprite ProfileImage; 
    public Sprite Illustration;
}