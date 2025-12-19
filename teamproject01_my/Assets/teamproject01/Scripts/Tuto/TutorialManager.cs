using UnityEngine;
using System.Collections;
using TMPro;
public class TutorialManager : MonoBehaviour
{
    [Header("Jump 튜토리얼 설정")]
    [Tooltip("'space' 태그를 가진 오브젝트의 SpriteRenderer")]

    public PlayerHealthAndMovement playerController;
    public SpriteRenderer spaceObjectRenderer;
    public float fadeDuration = 2.0f; // 투명하게 변하는 데 걸리는 시간 (초)
    
    [Header("Dash 튜토리얼 설정")]
    public SpriteRenderer rClickObjectRenderer; // "RClick" 오브젝트의 SpriteRenderer
    public float dashFadeDuration = 1.0f; // RClick이 나타나는 데 걸리는 시간

    private bool isDashTutorialActive = false;

    [Header("Rope 튜토리얼 설정")]
    public SpriteRenderer shiftObjectRenderer; // "Shift" 오브젝트의 SpriteRenderer
    public float ropeFadeDuration = 1.0f;

    [Header("Monster 튜토리얼 설정 (Canvas 버전)")]
    public GameObject targetMonster;      // 씬에 비활성화된 몬스터
    public CanvasGroup monsterTutorialCG; // 텍스트 부모(CanvasGroup 추가된 것)
    public float attackFadeDuration = 1.5f;
    void Start()
    {
        if (monsterTutorialCG != null)
        {
            monsterTutorialCG.alpha = 0f;
            monsterTutorialCG.gameObject.SetActive(false);
        }
        // 1. "space" 태그 오브젝트 초기화
        GameObject spaceObject = GameObject.FindWithTag("space");
        if (spaceObject != null)
        {
            spaceObjectRenderer = spaceObject.GetComponent<SpriteRenderer>();
            if (spaceObjectRenderer != null)
            {
                Color startColor = spaceObjectRenderer.color;
                startColor.a = 0f;
                spaceObjectRenderer.color = startColor;
            }
        }
        else
        {
            Debug.LogWarning("'space' 태그를 가진 오브젝트를 찾을 수 없습니다.");
        }

        // 2. "RClick" 태그 오브젝트 초기화
        GameObject rClickObject = GameObject.FindWithTag("RClick");
        if (rClickObject != null)
        {
            rClickObjectRenderer = rClickObject.GetComponent<SpriteRenderer>();
            if (rClickObjectRenderer != null)
            {
                Color startColor = rClickObjectRenderer.color;
                startColor.a = 0f;
                rClickObjectRenderer.color = startColor;
            }
        }
        else
        {
            Debug.LogWarning("'RClick' 태그를 가진 오브젝트를 찾을 수 없습니다.");
        }
    }
    
    // Jump 튜토리얼용 페이드 인 시작
    public void StartFadeIn()
    {
        if (spaceObjectRenderer == null) return; 

        StopAllCoroutines(); 
        StartCoroutine(FadeInCoroutine(spaceObjectRenderer, fadeDuration));
    }

    public void StartMonsterTutorial()
    {
        // 1. 몬스터 활성화
        if (targetMonster != null)
        {
            targetMonster.SetActive(true);
        }

        // 2. UI 텍스트 페이드 인 시작
        if (monsterTutorialCG != null)
        {
            monsterTutorialCG.gameObject.SetActive(true);
            StartCoroutine(FadeInCanvasGroup(monsterTutorialCG, attackFadeDuration));
        }
    }

    // CanvasGroup 전용 페이드 인 코루틴
    private IEnumerator FadeInCanvasGroup(CanvasGroup cg, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // 시간 정지 상태에서도 작동하게 unscaled 사용
            cg.alpha = Mathf.Lerp(0f, 1f, timer / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    // Dash 튜토리얼 시작 함수
    public void StartDashTutorial()
    {
        if (isDashTutorialActive) return;

        if (playerController != null)
        {
            playerController.StopAllActionsForTutorial(); // 이 함수를 아래에서 새로 정의합니다.
        }

        isDashTutorialActive = true;
        
        // 1. 시간 정지
        Time.timeScale = 0f;
        Debug.Log("Dash 튜토리얼 시작: 시간 정지");

        // 2. RClick 오브젝트 페이드 인 시작
        if (rClickObjectRenderer != null)
        {
            StartCoroutine(FadeInCoroutine(rClickObjectRenderer, dashFadeDuration));
        }
    }

    // 🌟 Dash 튜토리얼 종료 함수 (수정됨) 🌟
    public void EndDashTutorial()
    {
        if (!isDashTutorialActive) return;

        if (playerController != null)
        {
            playerController.StopAllActionsForTutorial(); // 이 함수를 아래에서 새로 정의합니다.
        }

        isDashTutorialActive = false;
        
        Time.timeScale = 1f;

        if (rClickObjectRenderer != null)
        {
            Color c = rClickObjectRenderer.color;
            c.a = 1f;
            rClickObjectRenderer.color = c;
        }
        
        Debug.Log("Dash 튜토리얼 종료. 시간 재개. RClick 오브젝트 유지.");
    }

    public void StartRopeTutorial()
    {
        // isRopeTutorialActive 변수가 TutorialManager에 필요하다면 추가하고 사용하세요.
        // isRopeTutorialActive = true; 
        
        // 1. 시간 정지
        Time.timeScale = 0f;
        Debug.Log("Rope 튜토리얼 시작: 시간 정지");

        // 2. Shift 오브젝트 페이드 인 시작
        if (shiftObjectRenderer != null)
        {
            StartCoroutine(FadeInCoroutine(shiftObjectRenderer, ropeFadeDuration));
        }
    }

    // Rope 튜토리얼 종료 함수
    public void EndRopeTutorial()
    {
        // isRopeTutorialActive = false; // TutorialManager 변수가 있다면 해제

        // PlayerHealthAndMovement에서 이미 Time.timeScale = 1f 을 처리했습니다.
        
        // Shift 오브젝트의 애니메이션을 끄거나 투명도를 고정하는 로직
        if (shiftObjectRenderer != null)
        {
            // 오브젝트를 남겨두기 위해 투명도를 1로 고정
            Color c = shiftObjectRenderer.color;
            c.a = 1f; 
            shiftObjectRenderer.color = c;
            
            // 애니메이션이 있다면 여기서 멈춰야 합니다.
            Animator shiftAnimator = shiftObjectRenderer.GetComponent<Animator>();
            if (shiftAnimator != null)
            {
                shiftAnimator.enabled = false; // 애니메이터 비활성화
            }
        }
        
        Debug.Log("Rope 튜토리얼 종료. RClick 오브젝트 유지.");
    }

    // 범용 FadeIn 코루틴
    private IEnumerator FadeInCoroutine(SpriteRenderer renderer, float duration)
    {
        float timer = 0f;
        Color startColor = renderer.color;
        Color targetColor = startColor;
        targetColor.a = 1f; 
        
        TextMeshProUGUI[] childTexts = renderer.GetComponentsInChildren<TextMeshProUGUI>();

        // TextMeshProUGUI의 시작 색상(투명) 설정
        Color[] textStartColors = new Color[childTexts.Length];
        Color[] textTargetColors = new Color[childTexts.Length];

        for (int i = 0; i < childTexts.Length; i++)
        {
            textStartColors[i] = childTexts[i].color;
            textTargetColors[i] = childTexts[i].color;
            textTargetColors[i].a = 1f; // 최종 알파값은 1
        }

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; 
            float normalizedTime = timer / duration; 
            
            // 1. SpriteRenderer 색상 업데이트
            Color currentSpriteColor = Color.Lerp(startColor, targetColor, normalizedTime);
            renderer.color = currentSpriteColor;
            
            // 2. 🌟 [추가] TextMeshProUGUI 색상 업데이트
            for (int i = 0; i < childTexts.Length; i++)
            {
                Color currentTextColor = Color.Lerp(textStartColors[i], textTargetColors[i], normalizedTime);
                childTexts[i].color = currentTextColor;
            }
            
            yield return null; 
        }

        // 최종 색상 설정 (오류 방지)
        renderer.color = targetColor;
        for (int i = 0; i < childTexts.Length; i++)
        {
            childTexts[i].color = textTargetColors[i];
        }
    }
}