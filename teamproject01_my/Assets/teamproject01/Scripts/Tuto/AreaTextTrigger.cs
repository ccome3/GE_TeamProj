using UnityEngine;

public class AreaTextTrigger : MonoBehaviour
{
    public TutorialManager manager;
    [TextArea(3, 5)]
    public string messageToShow = "이곳은 위험한 구역입니다.";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (manager != null)
            {
                manager.StartCenterTextTutorial(messageToShow);
                Destroy(gameObject); // 한 번만 나오고 파괴
            }
        }
    }
}