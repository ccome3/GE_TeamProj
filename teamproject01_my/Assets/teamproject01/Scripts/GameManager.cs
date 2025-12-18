using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI 연결 - 리스폰")]
    public CanvasGroup blackPanelGroup; 
    public float fadeDuration = 1.0f; 
    public float waitDuration = 2.0f; 

    [Header("UI 연결 - 일시정지")]
    public GameObject pausePanel;         // 일시정지 메인 패널
    public GameObject confirmQuitPanel;   // 종료 확인 팝업

    private bool isPaused = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 씬이 바뀌어도 파괴되지 않게 하려면 아래 주석을 해제하세요.
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // UI 초기 상태 설정
        if (pausePanel != null) pausePanel.SetActive(false);
        if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
        if (blackPanelGroup != null) blackPanelGroup.gameObject.SetActive(false);
    }

    private void Update()
    {
        // ESC 키로 일시정지 토글 (단, 플레이어가 사망한 상태가 아닐 때만)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeInput();
        }
    }

    private void HandleEscapeInput()
    {
        // 만약 종료 확인 창이 열려있다면 그것부터 닫기
        if (confirmQuitPanel != null && confirmQuitPanel.activeSelf)
        {
            confirmQuitPanel.SetActive(false);
        }
        else
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        
        if (pausePanel != null)
            pausePanel.SetActive(isPaused);

        // 시간 정지/재생
        Time.timeScale = isPaused ? 0f : 1f;
    }

    // --- 버튼 연결용 함수들 ---

    public void SaveGame()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("SavedScene", currentScene);
        PlayerPrefs.Save();
        Debug.Log("저장 완료: " + currentScene);
    }

    public void OpenQuitConfirmation()
    {
        if (confirmQuitPanel != null){
            confirmQuitPanel.SetActive(true);
            if (pausePanel != null) pausePanel.SetActive(false);
        }
    }
    

    public void QuitWithSave()
    {
        SaveGame();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main"); // 메인 메뉴 씬 이름
    }

    public void QuitWithoutSave()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main");
    }

    // --- 리스폰 로직 (기존 유지) ---

    public void OnPlayerDied()
    {
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        if (blackPanelGroup != null)
        {
            blackPanelGroup.gameObject.SetActive(true);
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                blackPanelGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            blackPanelGroup.alpha = 1f;
        }

        yield return new WaitForSeconds(waitDuration);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}