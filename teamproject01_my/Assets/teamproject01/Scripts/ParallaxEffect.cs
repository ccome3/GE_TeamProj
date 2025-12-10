using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Header("패럴랙스 강도")]
    // 이 값은 X축과 Y축 움직임에 모두 적용됩니다.
    public float parallaxStrength = 0.2f; 

    [Header("카메라 참조")]
    public Transform cameraTransform;

    private float tileWidth;
    private Vector3 lastCameraPosition;

    void Start()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
            {
                Debug.LogError("Main Camera를 찾을 수 없습니다.");
                enabled = false;
                return;
            }
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            tileWidth = spriteRenderer.bounds.size.x;
        }

        lastCameraPosition = cameraTransform.position;
    }

    void LateUpdate()
    {
        // 1. 패럴랙스 이동 계산 및 적용
        
        // 현재 카메라 이동량 계산
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        
        // X축 패럴랙스 이동량
        float parallaxX = deltaMovement.x * parallaxStrength;
        
        // 🌟 수정: Y축 패럴랙스 이동량 계산 
        float parallaxY = deltaMovement.y * parallaxStrength; 
        
        // 위치 업데이트: X와 Y 모두 적용 (Z는 0으로 고정)
        transform.position += new Vector3(parallaxX, parallaxY, 0);

        // 다음 프레임을 위해 현재 카메라 위치 저장
        lastCameraPosition = cameraTransform.position;

        // 2. X축 무한 스크롤 재배치 로직
        CheckInfiniteScroll();
    }

    private void CheckInfiniteScroll()
    {
        // Y축은 무한 스크롤 하지 않으므로 X축만 확인합니다.
        
        float distanceX = cameraTransform.position.x - transform.position.x;

        if (Mathf.Abs(distanceX) >= tileWidth)
        {
            // 이동 방향(부호) 구하기: 카메라가 오른쪽에 있으면 1, 왼쪽에 있으면 -1
            float direction = distanceX > 0 ? 1 : -1;

            // X축 위치만 타일 너비의 2배만큼 이동하여 무한 스크롤 구현
            transform.position += new Vector3(tileWidth * 2f * direction, 0, 0);
        }
    }
}