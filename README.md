# MornUtil

<p align="center">
  <img src="src/Editor/MornUtil.png" alt="MornUtil" width="640" />
</p>

<p align="center">
  <img src="https://img.shields.io/github/license/TsukumiStudio/MornUtil" alt="License" />
</p>

## 概要

Unity 開発向けのユーティリティ機能を提供する基盤ライブラリ。拡張メソッド、数学計算、オブジェクトプール、非同期タスク処理、暗号化、UI ヘルパーなどを統合。

## 導入方法

Unity Package Manager で以下の Git URL を追加:

```
https://github.com/TsukumiStudio/MornUtil.git?path=src#1.0.0
```

`Window > Package Manager > + > Add package from git URL...` に貼り付けてください。

### 依存パッケージ

- [UniRx](https://github.com/neuecc/UniRx) (`com.neuecc.unirx`)
- [UniTask](https://github.com/Cysharp/UniTask) (`com.cysharp.unitask`)
- [VContainer](https://github.com/hadashiA/VContainer) (`jp.hadashikick.vcontainer`)
- [TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest) (`com.unity.textmeshpro`)

## 機能

| カテゴリ | 主なクラス | 用途 |
|----------|-----------|------|
| アプリ管理 | `MornApp` | 終了 / `QuitToken` 取得 |
| 非同期処理 | `MornTask` `MornTaskCanceller` | トランジション・キャンセル管理 |
| オブジェクトプール | `MornObjectPool<T>` | 汎用プーリング |
| 数学・乱数 | `MornMath` `MornRandom` `MornFloatRange` `MornColorRange` | 角度正規化・範囲乱数 |
| 暗号化 | `MornCrypt` | AES 暗号化/復号化 |
| バイト列 | `MornByte` | バイナリ操作 |
| 拡張メソッド | `Extensions/*` | List / String / Vector / Color など |
| Bind | `BindAnimatorClip` `BindAnimatorState` | Animator パラメータ参照 |
| 簡易アニメ | `MornSimpleImageAnimation` `MornSimpleSpriteAnimation` | スプライトコマ送り |
| Editor 拡張 | `Editor/*` | 各種 Inspector ツール |

## 使い方

### アプリケーション管理

```csharp
// アプリケーション終了時の CancellationToken
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
    cancellationToken: ct);
```

### オブジェクトプール

```csharp
var pool = new MornObjectPool<Enemy>(
    onGenerate: () => Instantiate(enemyPrefab),
    onRent: enemy => enemy.gameObject.SetActive(true),
    onReturn: enemy => enemy.gameObject.SetActive(false),
    startCount: 10);

Enemy enemy = pool.Rent();
pool.Return(enemy);
```

### 拡張メソッド

```csharp
T randomItem = list.RandomValue();
int count = "hello".MatchCount('l');
vector.SetX(5f);
```

### 数学・暗号化

```csharp
float normalized = MornMath.NormalizeDegree(370, centerValue: 0);
string encrypted = MornCrypt.Encrypt(plainText, iv, key);
string decrypted = MornCrypt.Decrypt(encrypted, iv, key);
```

## ライセンス

[The Unlicense](LICENSE)
