using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class StageEndPoint : MonoBehaviour
{
    [Header("다음 스테이지 이름")]
    public string nextSceneName;

    [Header("페이드용 UI 패널 (CanvasGroup 추가된 것)")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 1.0f;

    private bool isTransitioning = false;

    private void Awake()
    {
        // 시작할 때 화면을 검은색으로 고정하고 페이드 인 시작
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            fadePanel.alpha = 1f;
        }
    }

    private void Start()
    {
        // 씬이 시작되자마자 페이드 인 코루틴 실행
        StartCoroutine(FadeInRoutine());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어에 닿으면 페이드 아웃 후 다음 스테이지로
        if (!isTransitioning && other.CompareTag("Player"))
        {
            StartCoroutine(LoadNextStageRoutine());
        }
    }

    // 화면이 서서히 밝아지는 로직
    private IEnumerator FadeInRoutine()
    {
        if (fadePanel != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
            fadePanel.alpha = 0f;
            fadePanel.gameObject.SetActive(false); // 페이드가 끝나면 클릭 방해 안 되게 비활성화
        }
    }

    // 화면이 서서히 어두워지며 다음 스테이지로 넘어가는 로직
    private IEnumerator LoadNextStageRoutine()
    {
        isTransitioning = true;

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (fadePanel != null)
            {
                fadePanel.gameObject.SetActive(true);
                float timer = 0f;
                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    fadePanel.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                    yield return null;
                }
                fadePanel.alpha = 1f;
            }

            yield return new WaitForSeconds(0.3f);
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("다음 스테이지 이름이 설정되지 않았습니다!");
            isTransitioning = false;
        }
    }
}