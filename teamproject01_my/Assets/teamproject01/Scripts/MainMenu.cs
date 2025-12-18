using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("설정")]
    public string videoSceneName = "VideoScene";
    public float waitTime = 3.0f;      // 버튼 누른 후 대기 시간
    public float fadeDuration = 2.0f;  // 페이드가 일어나는 시간

    [Header("UI 및 오디오 연결")]
    public CanvasGroup fadePanel;      // 검은색 패널 (Canvas Group)
    public AudioSource bgmSource;      // 로비 배경음악

    private bool isStarting = false;

    private void Start()
    {
        // 시작 시 화면은 밝게 초기화
        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.gameObject.SetActive(false);
        }
    }

    public void StartNewGame()
    {
        if (isStarting) return;
        StartCoroutine(StartGameSequence());
    }

    private IEnumerator StartGameSequence()
    {
        isStarting = true;

        // 1. 페이드 패널 활성화
        if (fadePanel != null) fadePanel.gameObject.SetActive(true);

        float timer = 0f;
        float startVolume = (bgmSource != null) ? bgmSource.volume : 0f;

        // 2. 3초 대기하는 동안 페이드 진행 (화면 어둡게 + 소리 작게)
        while (timer < waitTime)
        {
            timer += Time.deltaTime;
            
            // 비율 계산 (0에서 1까지)
            float progress = timer / fadeDuration; // 실제 페이드 시간 기준

            // 화면 페이드 아웃
            if (fadePanel != null)
                fadePanel.alpha = Mathf.Clamp01(progress);

            // BGM 페이드 아웃
            if (bgmSource != null)
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, progress);

            yield return null;
        }

        // 3. 확실히 마무리 후 씬 이동
        if (bgmSource != null) bgmSource.volume = 0f;
        SceneManager.LoadScene(videoSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("게임 종료");
    }
}