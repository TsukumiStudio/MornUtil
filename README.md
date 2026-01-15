# MornUtil

## 概要

Unity開発向けのユーティリティ機能を提供する基盤ライブラリ。拡張メソッド、数学計算、オブジェクトプール、非同期タスク処理、暗号化、UIヘルパーなどを統合。

## 依存関係

| 種別 | 名前 |
|------|------|
| 外部パッケージ | UniRx, UniTask, VContainer, TextMesh Pro |
| Mornライブラリ | なし |

## 使い方

### アプリケーション管理

```csharp
// アプリケーション終了時のCancellationToken取得
CancellationToken quitToken = MornApp.QuitToken;

// アプリ終了
MornApp.Quit();
```

### 非同期トランジション

```csharp
await MornTask.TransitionAsync(
    duration: TimeSpan.FromSeconds(0.5f),
    startValue: 0f,
    endValue: 1f,
    action: value => transform.localScale = Vector3.one * value,
    cancellationToken: ct
);
```

### オブジェクトプール

```csharp
var pool = new MornObjectPool<Enemy>(
    onGenerate: () => Instantiate(enemyPrefab),
    onRent: enemy => enemy.gameObject.SetActive(true),
    onReturn: enemy => enemy.gameObject.SetActive(false),
    startCount: 10
);

Enemy enemy = pool.Rent();
pool.Return(enemy);
```

### 拡張メソッド

```csharp
// リスト: ランダム選択
T randomItem = list.RandomValue();

// 文字列: マッチカウント
int count = "hello".MatchCount('l');

// Vector: 各種操作
vector.SetX(5f);
```

### 数学・暗号化ユーティリティ

```csharp
// 数学関数
float normalized = MornMath.NormalizeDegree(370, centerValue: 0);

// AES暗号化/復号化
string encrypted = MornCrypt.Encrypt(plainText, iv, key);
string decrypted = MornCrypt.Decrypt(encrypted, iv, key);
```
