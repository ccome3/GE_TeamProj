using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class VideoSceneManager : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private bool isTransitioning = false;

    [Header("설정")]
    public string nextSceneName = "Stage1";
    public float waitTime = 1.0f; 
    public float fadeDuration = 1.0f;

    [Header("UI 연결")]
    public CanvasGroup fadePanel; 

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        // 혹시 모르니 여기서 다시 한번 초기화
        videoPlayer.playOnAwake = false; 
        
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            fadePanel.alpha = 1f; // 처음엔 무조건 검은 화면
        }
    }

    void Start()
    {
        videoPlayer.loopPointReached += OnVideoFinished;
        StartCoroutine(VideoSequenceRoutine());
    }

    IEnumerator VideoSequenceRoutine()
    {
        // 1. 영상을 미리 준비시킴
        videoPlayer.Prepare();

        // 2. 준비될 때까지 기다림 (안전 장치)
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        // 3. 영상 준비 완료 후 1초 더 대기 (요청하신 정적 시간)
        yield return new WaitForSeconds(waitTime);

        // 4. 영상 재생 시작
        videoPlayer.Play();

        // 5. 페이드 인 (검은 화면 -> 영상)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (fadePanel != null) fadePanel.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.gameObject.SetActive(false);
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (!isTransitioning) StartCoroutine(ExitSequenceRoutine());
    }

    IEnumerator ExitSequenceRoutine()
    {
        isTransitioning = true;
        if (fadePanel != null) fadePanel.gameObject.SetActive(true);

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (fadePanel != null) fadePanel.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }

        yield return new WaitForSeconds(waitTime);
        SceneManager.LoadScene(nextSceneName);
    }

    void Update()
    {
        if (!isTransitioning && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            StopAllCoroutines();
            StartCoroutine(ExitSequenceRoutine());
        }
    }
}