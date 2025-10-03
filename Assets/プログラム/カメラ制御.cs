using UnityEngine;

public class カメラ制御 : MonoBehaviour


/// <summary>
/// 機体の動きに合わせてカメラが追従し、画面中央から機体がずれる効果を表現します
/// </summary>
{
    [Header("ターゲット設定")]
    [Tooltip("追従する対象（プレイヤー機体）")]
    [SerializeField] private Transform 追従対象;

    [Header("カメラ位置の設定")]
    [Tooltip("対象から見たカメラの座標")]
    [SerializeField] private Vector3 カメラオフセット = new Vector3(0, 0, -5);
    
    [Tooltip("カメラ移動のスムージング係数（大きいほど滑らか/遅い）")]
    [SerializeField] private float スムーズ速度 = 0.125f;

    [Header("視点設定")]
    [Tooltip("どれだけ前方を見るか（大きいほど前方を見る）")]
    [SerializeField] private float 前方注視係数 = 2.0f;
    
    [Tooltip("画面中央からのずれ係数（大きいほどずれる）")]
    [SerializeField] private float 中心からのオフセット = 0.3f;

    // 内部変数
    private Vector3 速度ベクトル = Vector3.zero; // SmoothDamp用の参照速度
    private Vector3 前回の対象位置;            // 速度計算用
    private Vector3 対象速度;                  // 計算された対象の移動速度

    /// <summary>
    /// 初期化処理
    /// </summary>
    private void Start()
    {
        // 追従対象が設定されているか確認
        if (追従対象 == null)
        {
            Debug.LogWarning("カメラの追従対象が設定されていません！");
            return;
        }

        // 初期位置を記録
        前回の対象位置 = 追従対象.position;
        Debug.Log("初期化が完了しました");
    }

    /// <summary>
    /// すべての更新処理後に実行（カメラ更新に最適）
    /// </summary>
    private void LateUpdate()
    {
        // 追従対象が無効化されている場合は何もしない
        if (追従対象 == null) return;

        // 対象の移動速度を計算（位置の変化量÷経過時間）
        対象速度 = (追従対象.position - 前回の対象位置) / Time.deltaTime;
        前回の対象位置 = 追従対象.position;

        // 水平方向の速度成分のみを取得（上下動は無視）
        Vector3 水平速度 = new Vector3(対象速度.x, 0, 対象速度.z);
        float 速度の大きさ = 水平速度.magnitude;
        
        // 速度方向に基づくオフセット位置の計算
        Vector3 正規化速度 = 水平速度.normalized; // 方向のみの単位ベクトル
        Vector3 オフセット位置 = 追従対象.position;
        
        // 一定以上の速度がある場合のみオフセットを適用
        if (速度の大きさ > 0.1f)
        {
            // AC風の効果：進行方向の反対側にオフセット
            // 速度が速いほどオフセットが大きくなる
            オフセット位置 -= 正規化速度 * 中心からのオフセット * 速度の大きさ;
        }

        // 注視点の計算（対象の前方）
        Vector3 注視位置 = 追従対象.position + 追従対象.forward * 前方注視係数;
        
        // カメラの目標位置を計算
        // 1. オフセット位置から対象の後方に移動
        // 2. 高さを追加
        Vector3 目標位置 = オフセット位置 - 追従対象.forward * カメラオフセット.z + Vector3.up * カメラオフセット.y;
        
        // スムーズにカメラを移動
        transform.position = Vector3.SmoothDamp(
            transform.position, // 現在のカメラ位置
            目標位置,           // 目標位置
            ref 速度ベクトル,   // 速度参照（内部で更新される）
            スムーズ速度        // スムーズ化の度合い
        );
        
        // カメラの向きを注視点に向ける
        transform.LookAt(注視位置);
        // カメラにロール角を適用（Z軸回転を継承）
        Vector3 機体の回転 = 追従対象.rotation.eulerAngles;
        Vector3 カメラの回転 = transform.rotation.eulerAngles;

        // Z軸だけ追従対象に合わせる（X,YはLookAtに任せる）
        transform.rotation = Quaternion.Euler(カメラの回転.x, カメラの回転.y, 機体の回転.z);


    }

    /// <summary>
    /// ギズモ描画（デバッグ用）
    /// エディタ上でカメラの追従対象と注視点を視覚化
    /// </summary>
    private void OnDrawGizmos()
    {
        if (追従対象 == null) return;
        
        // 追従対象との線を描画
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, 追従対象.position);
        
        // 注視点を描画
        Vector3 注視位置 = 追従対象.position + 追従対象.forward * 前方注視係数;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(注視位置, 0.2f);
    }
}