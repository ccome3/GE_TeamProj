using UnityEngine;
using TMPro; // TextMeshPro 사용 시 필수
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // 씬 전환용

public class IntroSequence : MonoBehaviour
{
    [Header("UI 설정")]
    public TextMeshProUGUI introText; // 화면 정중앙 텍스트 UI
    public CanvasGroup textCanvasGroup; // 페이드 효과용 (선택사항)

    [Header("출력할 대사")]
    public List<string> lines = new List<string> { "첫 번째 대사입니다.", "두 번째 대사입니다.", "마지막 대사입니다." };

    [Header("시간 설정")]
    public float delayBetweenLines = 2.0f; // 대사 간 간격
    public float fadeDuration = 0.5f;      // 페이드 인/아웃 속도
    public string nextSceneName;           // 다음으로 넘어갈 씬 이름

    private void Start()
    {
        // 시작하자마자 코루틴 실행
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        // 텍스트 초기화
        introText.text = "";
        
        foreach (string line in lines)
        {
            // 1. 텍스트 설정 및 페이드 인
            introText.text = line;
            yield return StartCoroutine(FadeText(0, 1));

            // 2. 대사 유지 시간
            yield return new WaitForSeconds(delayBetweenLines);

            // 3. 페이드 아웃
            yield return StartCoroutine(FadeText(1, 0));
        }

        // 모든 대사가 끝나면 다음 씬으로 이동
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("다음 씬 이름이 설정되지 않았습니다.");
        }
    }

    private IEnumerator FadeText(float startAlpha, float endAlpha)
    {
        if (textCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            textCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            yield return null;
        }
        textCanvasGroup.alpha = endAlpha;
    }
}