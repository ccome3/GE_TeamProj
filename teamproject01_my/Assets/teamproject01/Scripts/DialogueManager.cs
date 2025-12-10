using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; 
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject dialoguePanel;
    public Image profileImage;
    public TextMeshProUGUI dialogueText;  
    public TextMeshProUGUI nameText; // 🌟 [추가] NPC 이름을 표시할 Text 컴포넌트
    
    private MonoBehaviour playerMovementScript; 
    
    // 🌟 현재 대화를 시작한 NPC 스크립트 참조
    private NPCInteraction currentNPC; 

    private List<DialogueLine> currentDialogue;
    private int currentLineIndex = 0;
    private bool isDialogueActive = false;

    private void Start()
    {
        dialoguePanel.SetActive(false);
        
        // PlayerMovementScript 찾기 (PlayerHealthAndMovement라고 가정)
        playerMovementScript = FindObjectOfType<PlayerHealthAndMovement>();
        if (playerMovementScript == null)
        {
            Debug.LogError("PlayerHealthAndMovement 스크립트를 씬에서 찾을 수 없습니다! 플레이어 제어 불가.");
        }
        
        // 🌟 [추가] 이름 텍스트 초기화
        if (nameText != null)
        {
            nameText.text = "";
        }
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) 
    {
        DisplayNextLine();
    }
    else if (Input.GetKeyDown(KeyCode.Escape))
    {
        EndDialogue();
    }
    }

    /// <summary>
    /// 🌟 대화 시작 시 NPCInteraction 참조를 매개변수로 받음
    /// </summary>
    public void StartDialogue(List<DialogueLine> dialogueLines, NPCInteraction npc)
    {
        if (isDialogueActive) return;

        currentNPC = npc; // 🌟 현재 NPC 저장
        currentDialogue = dialogueLines;
        currentLineIndex = 0;
        isDialogueActive = true;
        
        dialoguePanel.SetActive(true);
        
        // 플레이어 움직임 비활성화
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false; 
        }
        
        DisplayCurrentLine();
    }

    private void DisplayCurrentLine()
    {
        if (currentDialogue == null || currentLineIndex >= currentDialogue.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = currentDialogue[currentLineIndex];

        dialogueText.text = line.LineText;
        
        // 🌟 [핵심 수정] NPC 이름 표시
        if (nameText != null)
        {
            // DialogueLine에 CharacterName이 비어있다면, 이름창을 비웁니다.
            nameText.text = string.IsNullOrEmpty(line.CharacterName) ? "" : line.CharacterName; 
        }
        
        // 프로필 이미지 표시
        if (line.ProfileImage != null)
        {
            profileImage.sprite = line.ProfileImage;
            profileImage.color = Color.white;
        }
        else
        {
            profileImage.color = new Color(1, 1, 1, 0); // 투명하게 만듦
        }
    }

    private void DisplayNextLine()
    {
        currentLineIndex++;
        if (currentLineIndex < currentDialogue.Count)
        {
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// 🌟 대화 종료 시 NPC에게 PostDialogueAction 실행을 요청
    /// </summary>
    private void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        
        // 🌟 [추가] 이름 텍스트 숨기기
        if (nameText != null)
        {
            nameText.text = ""; 
        }
        
        // 플레이어 움직임 복구
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true; 
        }
        
        // 🌟 대화 종료 시 NPC의 후속 액션 실행
        if (currentNPC != null)
        {
            currentNPC.PostDialogueAction(); 
            currentNPC = null; 
        }

        currentDialogue = null;
        Debug.Log("대화 종료.");
    }
}