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
    
    // 플레이어 위치 파악을 위한 참조 변수
    private Transform playerTransform;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        dialogueManager = FindObjectOfType<DialogueManager>();
        inventoryManager = FindObjectOfType<InventoryManager>(); 
        
        // 🌟 처음 시작할 때 플레이어를 찾아둡니다.
        FindPlayer();

        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager를 씬에서 찾을 수 없습니다!");
        }
        
        currentDialogueSet = dialogue; 
    }

    private void Update()
    {
        // 🌟 플레이어가 범위 안에 있을 때만 로직 실행
        if (isPlayerInRange)
        {
            // 플레이어 참조가 날아갔을 경우를 대비한 안전장치
            if (playerTransform == null) FindPlayer();

            // 플레이어 방향을 바라보도록 Flip 실행
            if (playerTransform != null)
            {
                FlipTowardsPlayer();
            }

            // 대화 시작 키 입력
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (dialogueManager != null)
                {
                    CheckAndSetDialogue(); 
                    dialogueManager.StartDialogue(currentDialogueSet, this); 
                }
            }
        }
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void FlipTowardsPlayer()
    {
        // 플레이어와 NPC의 X 좌표 차이 계산
        float direction = playerTransform.position.x - transform.position.x;

        // 플레이어가 오른쪽에 있으면 (차이가 양수)
        if (direction > 0.01f)
        {
            // NPC 원본이 왼쪽을 보고 있다면 true, 오른쪽을 보고 있다면 false로 수정하세요.
            spriteRenderer.flipX = false; 
        }
        // 플레이어가 왼쪽에 있으면 (차이가 음수)
        else if (direction < -0.01f)
        {
            spriteRenderer.flipX = true;
        }
    }

    private void CheckAndSetDialogue()
    {
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

    public void PostDialogueAction()
    {
        if (currentDialogueSet == conditionalDialogue) 
        {
            Debug.Log("조건부 대화 종료됨. 후속 오브젝트 활성화 액션 실행.");
            
            if (wreckagesToActivate != null && wreckagesToActivate.Count > 0)
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