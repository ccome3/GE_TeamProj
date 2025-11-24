using UnityEngine;

/// <summary>
/// 시간 지연의 영향을 받는 모든 오브젝트에 부착되어야 합니다.
/// 이 오브젝트는 Time.deltaTime 대신 DeltaTime 프로퍼티를 사용하여 움직여야 합니다.
/// </summary>
public class TimeAffectedObject : MonoBehaviour
{
    // 이 오브젝트의 로컬 시간 계수를 반환합니다.
    public float TimeFactor
    {
        get { return TimeDilationManager.GetTimeFactor(this); }
    }

    /// <summary>
    /// 이 오브젝트가 사용해야 할 조정된 델타 시간(Delta Time)을 반환합니다.
    /// (Time.deltaTime * TimeFactor)
    /// </summary>
    public float DeltaTime()
    {
        return Time.deltaTime * TimeFactor;
    }
    
    // 이 컴포넌트가 파괴될 때 오버라이드 목록에서 자신을 제거합니다.
    private void OnDestroy()
    {
        TimeDilationManager.ClearOverride(this);
    }
    
    // 예시: 만약 레이저 스크립트가 LaserMovement.cs라면,
    // LaserMovement.cs의 Update() 함수 내에서 다음과 같이 사용해야 합니다.
    /*
    private TimeAffectedObject timeAffect;
    void Start() { timeAffect = GetComponent<TimeAffectedObject>(); }
    void Update() 
    {
        // Vector3.MoveTowards(transform.position, target, speed * timeAffect.DeltaTime());
    }
    */
}