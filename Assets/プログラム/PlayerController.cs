using UnityEngine;
using System.Collections.Generic;

public class プレイヤー制御 : MonoBehaviour



/// <summary>
/// 機体操作を制御するコンポーネント
/// シューティングゲームの主人公機体の動きを管理します
/// </summary>

{
    [Header("移動関連の設定")]
    [Tooltip("左右移動の速度係数")]
    [SerializeField] private float 水平速度 = 10f;
    
    [Tooltip("上下移動の速度係数")]
    [SerializeField] private float 垂直速度 = 8f;
    
    [Tooltip("前後移動の速度係数")]
    [SerializeField] private float 前後速度 = 15f;
    
    [Tooltip("視点回転の速度係数")]
    [SerializeField] private float 回転速度 = 120f;
    
    [Tooltip("移動のスムージング係数（大きいほど滑らか/遅い）")]
    [SerializeField] private float スムーズ係数 = 0.1f;
    
    [Tooltip("エンコーダー（矢印キー）の感度係数")]
    [SerializeField] private float エンコーダー感度 = 5f;

    [Header("キーマップ設定")]
    [SerializeField] private KeyCode 左キー = KeyCode.I;
    [SerializeField] private KeyCode 右キー = KeyCode.O;
    [SerializeField] private KeyCode 上キー = KeyCode.E;
    [SerializeField] private KeyCode 下キー = KeyCode.A;
    [SerializeField] private KeyCode 右回転 = KeyCode.Y;
    [SerializeField] private KeyCode 左回転 = KeyCode.W;

    [Header("カメラ関連の設定")]
    [Tooltip("カメラと機体の距離")]
    [SerializeField] private float カメラ距離 = 2.0f;
    
    [Tooltip("カメラの追従速度（大きいほど速い）")]
    [SerializeField] private float カメラ追従速度 = 5.0f;
    
    [Tooltip("カメラのターゲット（通常はメインカメラ）")]
    [SerializeField] private Transform カメラ対象;

    // 内部変数
    private Vector3 スムーズ速度ベクトル; // SmoothDamp用の参照速度
    private float 深度移動値 = 0f;       // 前後方向の累積移動量
    private Vector3 目標位置;            // 機体の移動目標位置

    /// <summary>
    /// 初期化処理
    /// </summary>
    private void Start()
    {
        // カメラ対象が設定されていなければ、メインカメラを使用
        if (カメラ対象 == null)
        {
            カメラ対象 = Camera.main.transform;
            Debug.Log("カメラ対象が設定されていないため、メインカメラを使用します");
        }

        // 初期位置を記録
        目標位置 = transform.position;
        
        // FPS操作のためにマウスカーソルをロック
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("マウスカーソルをロックしました");
    }

    /// <summary>
    /// 毎フレーム実行される更新処理
    /// </summary>
    private void Update()
    {
        // 入力処理と位置更新
        入力処理();
        
        // カメラ位置の更新
        //カメラ位置更新();
    }

    /// <summary>
    /// プレイヤーの入力を処理し、機体の移動と回転を行う
    /// </summary>
    private void 入力処理()
    {
        // WASD入力処理（上下左右）このキーボードの場合EIAO入力
        // Input.GetAxisは-1.0～1.0の値を返す
        float 水平入力 = 0f;
        float 垂直入力 = 0f;

        // カスタムキー入力
        if (Input.GetKey(左キー)) 水平入力 -= 水平速度;
        if (Input.GetKey(右キー)) 水平入力 += 水平速度;
        if (Input.GetKey(上キー)) 垂直入力 += 垂直速度;
        if (Input.GetKey(下キー)) 垂直入力 -= 垂直速度;
        if (Input.GetKey(右回転)) transform.Rotate(Vector3.forward, -回転速度 * Time.deltaTime, Space.Self);
        if (Input.GetKey(左回転)) transform.Rotate(Vector3.forward, 回転速度 * Time.deltaTime, Space.Self);
        // デバッグログで確認
        Debug.Log($"水平入力: {水平入力}, 垂直入力: {垂直入力}");

        // ロータリーエンコーダー入力（矢印キー左右）をシミュレート
        float エンコーダー入力 = 0f;
        if (Input.GetKey(KeyCode.RightArrow)) 
        {
            // 右矢印キーで前進
            エンコーダー入力 += 1f;
        }
        if (Input.GetKey(KeyCode.LeftArrow)) 
        {
            // 左矢印キーで後退
            エンコーダー入力 -= 1f;
        }
        
        // エンコーダー入力を累積して前後移動に変換
        // フレームレートに依存しないよう Time.deltaTime をかける
        深度移動値 += エンコーダー入力 * エンコーダー感度 * Time.deltaTime;
        
        // マウス入力で視点回転
        float マウスX = Input.GetAxis("Mouse X"); // 左右移動
        float マウスY = Input.GetAxis("Mouse Y"); // 上下移動

        // 移動ベクトルの計算
        // WASD: 上下左右の移動、矢印キー: 前後移動
        Vector3 横方向移動 = new Vector3(水平入力, 垂直入力, 0) ;
        Vector3 前方向移動 = transform.forward * 深度移動値 * 前後速度;
        
        // 自機の向き（ローカル座標系）に合わせて横方向移動を変換（ワールド座標系へ）
        横方向移動 = transform.TransformDirection(横方向移動);
        
        // 目標位置を更新（現在の位置から移動量を加算）
        目標位置 += 横方向移動 * Time.deltaTime + 前方向移動 * Time.deltaTime;
        
        // スムーズな移動を実現（直接移動せず、徐々に目標位置に近づける）
        transform.position = Vector3.SmoothDamp(
            transform.position,  // 現在位置
            目標位置,            // 目標位置
            ref スムーズ速度ベクトル, // 速度参照（内部で更新される）
            スムーズ係数         // スムーズ化の度合い
        );

        // 回転処理
        // マウスX: 水平方向の回転（Y軸周り）
        transform.Rotate(Vector3.up, マウスX * 回転速度 * Time.deltaTime, Space.World);
        // マウスY: 垂直方向の回転（X軸周り、符号反転で直感的な操作に）
        transform.Rotate(Vector3.right, -マウスY * 回転速度 * Time.deltaTime, Space.Self);
        
        // 過度の傾きを防止（頭を180度以上回さないようにする）
        Vector3 現在の回転 = transform.rotation.eulerAngles;
        
        // 180度を超える角度を-180～180度の範囲に変換
        if (現在の回転.x > 180f) 現在の回転.x -= 360f;
        
        // X軸回転（上下の傾き）を-80～80度に制限
        現在の回転.x = Mathf.Clamp(現在の回転.x, -80f, 80f);
        
        // 修正した回転を適用
        transform.rotation = Quaternion.Euler(現在の回転);

        //速度を表示
        Debug.Log($"移動速度: {transform.position}");
        Debug.Log($"回転角度: {transform.rotation.eulerAngles}");
    }

    /// <summary>
    /// カメラワークを実現するメソッド
    /// 機体の動きに合わせてカメラが追従する
    /// </summary>
    private void カメラ位置更新()
    {
        // カメラ対象が設定されていなければ何もしない
        if (カメラ対象 == null) return;

        // 機体の後方にカメラを配置（ACスタイル）
        // 機体の位置から機体の後ろ方向に一定距離離れた位置
        Vector3 目標カメラ位置 = transform.position - transform.forward * カメラ距離;
        
        // カメラをスムーズに目標位置へ移動
        カメラ対象.position = Vector3.Lerp(
            カメラ対象.position,   // 現在のカメラ位置
            目標カメラ位置,        // 目標位置
            カメラ追従速度 * Time.deltaTime // スムーズ化係数
        );
        
        // カメラの向きを機体の少し前方に向ける
        // これにより、機体が画面の中央よりやや下に表示される
        カメラ対象.LookAt(transform.position + transform.forward * 10f);
    }
}