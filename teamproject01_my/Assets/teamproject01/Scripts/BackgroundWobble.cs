using UnityEngine;

public class BackgroundWobble : MonoBehaviour
{
    [Header("흔들림 설정")]
    public float moveSpeed = 0.5f;   // 움직이는 속도
    public float moveRange = 0.2f;   // 움직이는 범위 (크면 많이 움직임)

    private Vector3 startPosition;
    private float seedX;
    private float seedY;

    void Start()
    {
        startPosition = transform.position;
        // 각 오브젝트마다 다른 움직임을 주도록 무작위 시드 설정
        seedX = Random.Range(0f, 100f);
        seedY = Random.Range(0f, 100f);
    }

    void Update()
    {
        // 펄린 노이즈를 이용해 부드러운 랜덤 좌표 계산
        float x = Mathf.PerlinNoise(seedX + Time.time * moveSpeed, 0f) * 2f - 1f;
        float y = Mathf.PerlinNoise(0f, seedY + Time.time * moveSpeed) * 2f - 1f;

        Vector3 offset = new Vector3(x, y, 0) * moveRange;
        transform.position = startPosition + offset;
    }
}