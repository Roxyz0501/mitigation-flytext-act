# Mitigation Flytext for ACT

FFXIVで自分が受けたダメージを、フライテキスト風のオーバーレイへ表示するACTプラグインです。

表示形式:

`攻撃名 受けたダメージ [軽減前の推定ダメージ] (-合計軽減%) [軽減アイコン…]`

自分が付与した軽減は金色の枠とグローで強調します。対象本人につく軽減バフと、攻撃者につくReprisal等の軽減デバフを同時に追跡し、その攻撃に有効だったものをすべて表示します。

## 重要: 軽減前ダメージについて

ACTのNetworkAbility（21/22）には軽減前の確定値が含まれません。本プラグインの角括弧内は、NetworkBuff/NetworkBuffRemove（26/30）で追跡できた割合軽減を乗算し、受けたダメージから逆算した推定値です。

バリア、ブロック／受け流し、物理・魔法限定軽減、上限付き効果、未登録またはログに出ないステータスでは差が出る場合があります。確定値であるかのようには表示しません。

## 対応機能

- Rampart等の個人軽減、Reprisal/Feint/Addle等の敵デバフ、レンジ・ヒーラーの代表的な全体軽減を追跡
- 自分が付与した軽減を金色で強調
- 表示時間、最大行数、透明度、文字サイズ、位置、固定、プレビューを設定
- English / 日本語 / 简体中文 / 한국어（英語フォールバック）
- 通常設定と分離した支援タブ。支援は任意で機能差なし
- 起動時／手動のGitHub Releases更新確認と、明示操作時だけ行う安全な更新
- 設定: `%APPDATA%\Advanced Combat Tracker\Config\MitigationFlytext.xml`

## インストール

1. Release ZIPの `MitigationFlytext.dll` を専用フォルダへ展開します。
2. ACTのPluginsタブでDLLを追加し、FFXIV Parsing Pluginより後で有効にします。
3. 「被ダメージフライテキスト」タブで固定をOFF、プレビューをONにして配置します。
4. 配置後にプレビューをOFF、固定をONにします。

ボーダーレスウィンドウでの利用を推奨します。

## ビルドとテスト

```powershell
.\build.ps1
.\tests\MitigationFlytext.Tests\bin\Release\net48\MitigationFlytext.Tests.exe
```

Release assetは `MitigationFlytext-vX.Y.Z.zip` と `MitigationFlytext-vX.Y.Z.sha256` です。更新元は公開専用リポジトリ [Roxyz0501/mitigation-flytext-act](https://github.com/Roxyz0501/mitigation-flytext-act) に固定します。draft/prereleaseを除外し、SemVerで比較します。更新は利用者が「更新する」を押した場合だけ行います。

ZIPとSHA-256マニフェスト、asset名、アセンブリ名、DLLバージョン、Releaseタグ、展開先を検証します。HTTPSのGitHub API/Release asset以外は拒否し、Zip Slipと想定外DLLを防止します。ロード中DLLを直接上書きせず、補助アップデータがACT終了を待ち、現行DLLをバックアップして置換します。失敗時は復元します。GitHubトークンをプラグインやConfigへ保存しません。

## 開発支援

[Ko-fiでRoxyz0501を支援する](https://ko-fi.com/roxyz0501)

支援は完全に任意です。支援しなくても全機能を利用でき、機能差はありません。起動時ポップアップ、自動遷移、繰り返し通知、機能制限は行いません。

ACTを含む外部ツールの利用はFINAL FANTASY XIVの利用規約上の扱いを理解したうえで、自己責任で行ってください。本プラグインはゲームへの入力を送信しません。
