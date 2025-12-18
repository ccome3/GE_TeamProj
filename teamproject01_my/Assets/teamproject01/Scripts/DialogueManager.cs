using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI; 
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject dialoguePanel;
    public Image profileImage;
    public TextMeshProUGUI dialogueText;  
    public TextMeshProUGUI nameText; 

    [Header("중앙 일러스트 설정")]
    public Image centerIllustration; 
    public float fadeDuration = 0.5f; // 페이드인 속도 조절
    
    private MonoBehaviour playerMovementScript; 
    private NPCInteraction currentNPC; 
    private List<DialogueLine> currentDialogue;
    private int currentLineIndex = 0;
    private bool isDialogueActive = false;

    private Coroutine fadeCoroutine; // 현재 진행 중인 페이드 관리

    private void Start()
    {
        dialoguePanel.SetActive(false);
        
        // 중앙 이미지 초기화 (투명도 0으로 시작)
        if (centerIllustration != null) 
        {
            centerIllustration.gameObject.SetActive(false);
            SetAlpha(0f);
        }

        playerMovementScript = FindObjectOfType<PlayerHealthAndMovement>();
        if (nameText != null) nameText.text = "";
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

    public void StartDialogue(List<DialogueLine> dialogueLines, NPCInteraction npc)
    {
        if (isDialogueActive) return;

        currentNPC = npc; 
        currentDialogue = dialogueLines;
        currentLineIndex = 0;
        isDialogueActive = true;
        
        dialoguePanel.SetActive(true);
        
        if (playerMovementScript != null) playerMovementScript.enabled = false; 
        
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
        
        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(line.CharacterName) ? "" : line.CharacterName; 
        }
        
        // 1. 프로필 이미지 로직
        if (line.ProfileImage != null)
        {
            profileImage.sprite = line.ProfileImage;
            profileImage.color = Color.white;
        }
        else
        {
            profileImage.color = new Color(1, 1, 1, 0); 
        }

        // 🌟 2. 중앙 일러스트 페이드인 로직 적용
        HandleIllustration(line.Illustration);
    }

    // 🌟 이미지를 교체하고 페이드인을 시작하는 함수
    private void HandleIllustration(Sprite newIllustration)
    {
        if (centerIllustration == null) return;

        // 이미 진행 중인 페이드가 있다면 중지
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        if (newIllustration != null)
        {
            // 만약 이전 이미지와 다르다면 새로 페이드인
            if (centerIllustration.sprite != newIllustration || !centerIllustration.gameObject.activeSelf)
            {
                centerIllustration.sprite = newIllustration;
                centerIllustration.SetNativeSize();
                fadeCoroutine = StartCoroutine(FadeInRoutine());
            }
        }
        else
        {
            // 이미지가 없으면 비활성화
            centerIllustration.gameObject.SetActive(false);
            SetAlpha(0f);
        }
    }

    // 🌟 실제 페이드 연출 코루틴
    private IEnumerator FadeInRoutine()
    {
        centerIllustration.gameObject.SetActive(true);
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }
        
        SetAlpha(1f);
    }

    private void SetAlpha(float alpha)
    {
        if (centerIllustration != null)
        {
            Color c = centerIllustration.color;
            c.a = alpha;
            centerIllustration.color = c;
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
        
        // 대화 종료 시 중앙 이미지 숨김 및 초기화
        if (centerIllustration != null) 
        {
            centerIllustration.gameObject.SetActive(false);
            SetAlpha(0f);
        }

        if (nameText != null) nameText.text = ""; 
        
        if (playerMovementScript != null) playerMovementScript.enabled = true; 
        
        if (currentNPC != null)
        {
            currentNPC.PostDialogueAction(); 
            currentNPC = null; 
        }

        currentDialogue = null;
    }
}