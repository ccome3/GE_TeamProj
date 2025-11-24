using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 모든 TimeAffectedObject의 시간 흐름을 관리하는 중앙 관리자입니다.
/// </summary>
public static class TimeDilationManager
{
    // === 설정 변수 ===
    // Player가 Zone 외부에 있을 때: Zone 내부 기믹의 속도 (역배속)
    public const float ZONE_IN_FACTOR_SLOW = 0.25f; 
    // Player가 Zone 내부에 있을 때: Zone 외부 기믹의 속도 (배속)
    public const float GLOBAL_FACTOR_FAST = 4.0f;
    // 정상 속도
    public const float NORMAL_FACTOR = 1.0f;

    // 현재 기믹들이 따라야 하는 전역 시간 계수 (플레이어가 Zone 안에 있을 때 Zone 외부 기믹에 적용됨)
    private static float globalTimeFactor = NORMAL_FACTOR;

    // Zone 내부에 있는 기믹들의 오버라이드 계수 (개별 설정)
    private static Dictionary<TimeAffectedObject, float> factorOverrides = new Dictionary<TimeAffectedObject, float>();

    /// <summary>
    /// 현재 모든 기믹이 따라야 하는 전역 시간 계수를 설정합니다.
    /// </summary>
    public static void SetGlobalFactor(float factor)
    {
        globalTimeFactor = factor;
        // Debug.Log($"Global Time Factor set to: {globalTimeFactor}");
    }

    /// <summary>
    /// 특정 기믹에 대한 시간 계수를 오버라이드(개별 설정)합니다.
    /// </summary>
    public static void OverrideFactor(TimeAffectedObject obj, float factor)
    {
        if (factorOverrides.ContainsKey(obj))
        {
            factorOverrides[obj] = factor;
        }
        else
        {
            factorOverrides.Add(obj, factor);
        }
        // Debug.Log($"Override set for {obj.gameObject.name} to: {factor}");
    }

    /// <summary>
    /// 특정 기믹에 대한 오버라이드 설정을 제거합니다.
    /// </summary>
    public static void ClearOverride(TimeAffectedObject obj)
    {
        if (factorOverrides.ContainsKey(obj))
        {
            factorOverrides.Remove(obj);
            // Debug.Log($"Override cleared for {obj.gameObject.name}");
        }
    }

    /// <summary>
    /// 해당 오브젝트가 사용해야 할 최종 시간 계수를 반환합니다.
    /// </summary>
    public static float GetTimeFactor(TimeAffectedObject obj)
    {
        // 오버라이드가 있으면 오버라이드 값을 반환 (Zone 내부 기믹)
        if (factorOverrides.ContainsKey(obj))
        {
            return factorOverrides[obj];
        }

        // 오버라이드가 없으면 전역 값을 반환 (Zone 외부 기믹)
        return globalTimeFactor;
    }
}