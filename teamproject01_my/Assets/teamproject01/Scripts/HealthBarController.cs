// HealthBarController.cs (수정됨)

using UnityEngine;
using UnityEngine.UI; 
using System.Collections; // 코루틴 사용을 위해 추가

/// <summary>
/// UI Slider 컴포넌트와 연동하여 체력 값을 시각적으로 표시하는 컨트롤러.
/// 체력 변화를 부드럽게(Lerp) 애니메이션합니다.
/// </summary>
public class HealthBarController : MonoBehaviour
{
    // 슬라이더 컴포넌트를 연결할 필드 (Inspector에서 드래그하여 연결)
    public Slider healthSlider; 

    [Header("애니메이션 설정")]
    // 체력바가 목표 값까지 줄어드는 데 걸리는 시간 (작을수록 빠름)
    public float decreaseSpeed = 5f; 
    
    private Coroutine decreaseCoroutine; // 현재 진행 중인 감소 코루틴 저장

    // 외부에서 현재 체력 값을 받아와 슬라이더를 업데이트하는 함수
    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthSlider == null)
        {
            Debug.LogError("HealthBarController: healthSlider가 연결되지 않았습니다.");
            return;
        }

        // 최대 체력 설정 (한 번만 설정해도 되지만, 안전을 위해 매번 업데이트)
        healthSlider.maxValue = maxHealth;
        
        // 새로운 체력(currentHealth)을 목표 값으로 설정
        float targetValue = (float)currentHealth;

        // 기존에 진행 중인 코루틴이 있다면 중지
        if (decreaseCoroutine != null)
        {
            StopCoroutine(decreaseCoroutine);
        }

        // 부드럽게 감소하는 코루틴 시작
        decreaseCoroutine = StartCoroutine(DecreaseHealthBarSmoothly(targetValue));
        healthSlider.gameObject.SetActive(currentHealth > 0);
    }

    private IEnumerator DecreaseHealthBarSmoothly(float targetValue)
    {
        // 현재 슬라이더의 값
        float currentValue = healthSlider.value;
        
        // 목표 값과의 차이가 아주 작아질 때까지 반복
        while (currentValue > targetValue)
        {
            // Lerp (선형 보간)를 사용하여 현재 값에서 목표 값으로 부드럽게 이동
            // Time.deltaTime * decreaseSpeed를 사용하여 프레임 속도에 관계없이 일정하게 감소
            currentValue = Mathf.Lerp(currentValue, targetValue, Time.deltaTime * decreaseSpeed);

            healthSlider.value = currentValue;
            
            // 다음 프레임을 기다립니다.
            yield return null;
        }

        // 루프를 빠져나오면 목표 값에 근접했으므로, 목표 값으로 정확하게 설정합니다.
        healthSlider.value = targetValue;
    }
}