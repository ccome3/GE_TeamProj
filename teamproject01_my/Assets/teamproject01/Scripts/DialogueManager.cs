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
    
    // 🌟🌟🌟 [추가] 플레이어 움직임 스크립트 참조 🌟🌟🌟
    private MonoBehaviour playerMovementScript; // PlayerHealthAndMovement 스크립트 참조용

    private List<DialogueLine> currentDialogue;
    private int currentLineIndex = 0;
    private bool isDialogueActive = false;

    private void Start()
    {
        dialoguePanel.SetActive(false);
        
        // 🌟🌟🌟 [수정] PlayerMovementScript 찾기 🌟🌟🌟
        // 플레이어 오브젝트에 붙은 움직임 스크립트를 찾아 참조합니다.
        // (사용자님의 스크립트 이름이 PlayerHealthAndMovement라고 가정합니다.)
        playerMovementScript = FindObjectOfType<PlayerHealthAndMovement>();
        if (playerMovementScript == null)
        {
            Debug.LogError("PlayerHealthAndMovement 스크립트를 씬에서 찾을 수 없습니다! 플레이어 제어 불가.");
        }
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            DisplayNextLine();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            EndDialogue();
        }
    }

    public void StartDialogue(List<DialogueLine> dialogueLines)
    {
        if (isDialogueActive) return;

        currentDialogue = dialogueLines;
        currentLineIndex = 0;
        isDialogueActive = true;
        
        dialoguePanel.SetActive(true);
        
        // 🌟🌟🌟 [수정] 시간 정지 제거 및 움직임 제어 🌟🌟🌟
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false; // 플레이어 움직임 비활성화
        }
        
        DisplayCurrentLine();
    }

    // (DisplayCurrentLine() 함수와 DisplayNextLine() 함수는 동일합니다.)
    private void DisplayCurrentLine()
    {
        if (currentDialogue == null || currentLineIndex >= currentDialogue.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = currentDialogue[currentLineIndex];

        dialogueText.text = line.LineText;
        if (line.ProfileImage != null)
        {
            profileImage.sprite = line.ProfileImage;
            profileImage.color = Color.white;
        }
        else
        {
            profileImage.color = new Color(1, 1, 1, 0); 
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


    private void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        
        // 🌟🌟🌟 [수정] 시간 재개 제거 및 움직임 복구 🌟🌟🌟
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true; // 플레이어 움직임 다시 활성화
        }
        
        currentDialogue = null;
        Debug.Log("대화 종료.");
    }
}