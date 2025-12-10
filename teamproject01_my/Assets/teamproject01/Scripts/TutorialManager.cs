using UnityEngine;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [Header("Jump 튜토리얼 설정")]
    [Tooltip("'space' 태그를 가진 오브젝트의 SpriteRenderer")]
    public SpriteRenderer spaceObjectRenderer;
    public float fadeDuration = 2.0f; // 투명하게 변하는 데 걸리는 시간 (초)
    
    [Header("Dash 튜토리얼 설정")]
    public SpriteRenderer rClickObjectRenderer; // "RClick" 오브젝트의 SpriteRenderer
    public float dashFadeDuration = 1.0f; // RClick이 나타나는 데 걸리는 시간

    private bool isDashTutorialActive = false;

    void Start()
    {
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

    // Dash 튜토리얼 시작 함수
    public void StartDashTutorial()
    {
        if (isDashTutorialActive) return;

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

        isDashTutorialActive = false;
        
        // 1. 시간 재개
        Time.timeScale = 1f;

        // 2. [수정됨] 오브젝트 숨기는 코드를 제거하여 계속 남아있게 함
        /* if (rClickObjectRenderer != null)
        {
            Color endColor = rClickObjectRenderer.color;
            endColor.a = 0f;
            rClickObjectRenderer.color = endColor;
        }
        */
        
        // 혹시 페이드 인 도중에 대쉬를 해서 100% 안 보일 수도 있으니 확실하게 보이게 설정 (선택 사항)
        if (rClickObjectRenderer != null)
        {
            Color c = rClickObjectRenderer.color;
            c.a = 1f; // 완전히 보이게 고정
            rClickObjectRenderer.color = c;
        }
        
        Debug.Log("Dash 튜토리얼 종료. 시간 재개. RClick 오브젝트 유지.");
    }

    // 범용 FadeIn 코루틴
    private IEnumerator FadeInCoroutine(SpriteRenderer renderer, float duration)
    {
        float timer = 0f;
        Color startColor = renderer.color;
        Color targetColor = startColor;
        targetColor.a = 1f; 
        
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; 
            float normalizedTime = timer / duration; 
            
            Color currentColor = Color.Lerp(startColor, targetColor, normalizedTime);
            renderer.color = currentColor;
            
            yield return null; 
        }

        renderer.color = targetColor;
    }
}