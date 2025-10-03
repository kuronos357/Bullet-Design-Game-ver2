using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("カメラ位置の設定")]
    [Tooltip("親オブジェクトからの基本オフセット座標")]
    [SerializeField] private Vector3 カメラオフセット = new Vector3(0, 1.5f, -5f);

    [Tooltip("ローカル座標移動のスムージング係数")]
    [SerializeField] private float スムーズ速度 = 0.1f;

    [Header("視点設定")]
    [Tooltip("どれだけ前方を見るか（大きいほど前方を見る）")]
    [SerializeField] private float 前方注視係数 = 2.0f;

    [Header("加速度によるオフセット")]
    [Tooltip("加速度に基づく画面中央からのずれ係数")]
    [SerializeField] private float 加速度オフセット係数 = 0.1f;

    [Tooltip("加速度オフセットのスムージング係数")]
    [SerializeField] private float オフセットスムーズ速度 = 0.1f;

    // 内部変数
    private Transform 追従対象; // 親オブジェクト（プレイヤー）
    private Vector3 localPosSmoothVel;
    private Vector3 prevTargetPos;
    private Vector3 prevTargetVel;
    private Vector3 accelOffset;
    private Vector3 accelOffsetSmoothVel;

    private void Start()
    {
        追従対象 = transform.parent;
        if (追従対象 == null)
        {
            Debug.LogError("カメラがどのオブジェクトの子にもなっていません！プレイヤーの子オブジェクトに設定してください。");
            enabled = false; // スクリプトを無効化
            return;
        }

        // 初期化
        prevTargetPos = 追従対象.position;
        prevTargetVel = Vector3.zero;
        accelOffset = Vector3.zero;
        accelOffsetSmoothVel = Vector3.zero;
        localPosSmoothVel = Vector3.zero;

        // 自身の初期ローカル位置をオフセット値に設定
        transform.localPosition = カメラオフセット;
    }

    private void LateUpdate()
    {
        if (追従対象 == null) return;

        // 1. 親（追従対象）の速度と加速度を計算
        Vector3 targetVel = (追従対象.position - prevTargetPos) / Time.deltaTime;
        prevTargetPos = 追従対象.position;
        Vector3 targetAccel = (targetVel - prevTargetVel) / Time.deltaTime;
        prevTargetVel = targetVel;

        // 2. 加速度に基づくオフセットを計算（Y軸は無視）
        Vector3 horizontalAccel = new Vector3(targetAccel.x, 0, targetAccel.z);
        Vector3 targetAccelOffset = Vector3.zero;
        if (horizontalAccel.magnitude > 0.1f)
        {
            // 加速方向と逆向きにオフセット
            targetAccelOffset = -horizontalAccel.normalized * horizontalAccel.magnitude * 加速度オフセット係数;
        }

        // 3. 加速度オフセットをスムーズに適用
        accelOffset = Vector3.SmoothDamp(accelOffset, targetAccelOffset, ref accelOffsetSmoothVel, オフセットスムーズ速度);

        // 4. 基本オフセットと加速度オフセットを合成して、目標ローカル座標を決定
        Vector3 targetLocalPos = カメラオフセット + accelOffset;

        // 5. 現在のローカル座標を目標値にスムーズに移動
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetLocalPos, ref localPosSmoothVel, スムーズ速度);

        // 6. カメラの向きを計算
        Vector3 lookAtPos = 追従対象.position + 追従対象.forward * 前方注視係数;
        Quaternion targetRotation = Quaternion.LookRotation(lookAtPos - transform.position);

        // 7. 親のZ軸回転（ロール）を合成
        Quaternion roll = Quaternion.Euler(0, 0, 追従対象.eulerAngles.z);
        
        // 8. 最終的な向きを適用
        transform.rotation = targetRotation * roll;
    }

    private void OnDrawGizmos()
    {
        if (追従対象 == null) 追従対象 = transform.parent;
        if (追従対象 == null) return;

        // 注視点を描画
        Gizmos.color = Color.red;
        Vector3 lookAtPoint = 追従対象.position + 追従対象.forward * 前方注視係数;
        Gizmos.DrawSphere(lookAtPoint, 0.2f);
        Gizmos.DrawLine(transform.position, lookAtPoint);
    }
}