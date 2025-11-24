using UnityEngine;
using System.Collections.Generic;

public class NPCInteraction : MonoBehaviour
{
    [Header("NPC 설정")]
    public string npcName = "낯선 NPC";
    public List<DialogueLine> dialogue; // 🌟 대화 내용 목록
    
    [Header("UI 연결 (3단계에서 사용)")]
    public DialogueManager dialogueManager; // 🌟 대화 관리자 스크립트 참조
    
    private bool isPlayerInRange = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor; 

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        // 씬에서 DialogueManager를 찾아 연결합니다. (3단계에서 주석 해제)
        dialogueManager = FindObjectOfType<DialogueManager>();
    }

    private void Update()
    {
        // 🌟 F키 입력 감지 및 대화 시작
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.F))
        {
            if (dialogueManager != null)
            {
                dialogueManager.StartDialogue(dialogue);
            }
            Debug.Log("F키 입력: 대화 시작 요청!");
        }
    }

    // 🌟 플레이어 감지: 범위 안에 들어올 시 테두리 빛남 (아이템 로직과 동일)
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