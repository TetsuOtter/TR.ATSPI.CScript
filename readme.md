# TR.ATSPI.CScript
BVE6のATSプラグインを, C#スクリプトで書き, 実行するためのプラグインです.

このプラグインを使用することで, プラグインの開発に必須といっても過言ではない「コンパイル/ビルド」の処理から解放されます.  つまり, 重いIDEを用いた開発を行う必要がなくなります.  (とはいえ, 逆に言うとIDEにある便利機能は使えなくなるわけですが)

軽い, ちょっとしたプラグインを作成する必要が出た際は, 本プラグインの使用がおすすめです.

## 使い方
### インストール
1. ファイル ( `TR.ATSPI.CScript.zip` ) をダウンロードします
2. ダウンロードしたファイルを解凍します
3. 解凍してできたフォルダを, その中身とともに車両データの好きな場所にコピペします.  
    なお, 推奨はしませんが, フォルダからファイル群を取り出して使用することもできます.
4. 車両データのATSプラグインとして `TR.ATSPI.CScript.dll` を指定します.  
    本プラグインを単体で使用する場合は `Vehicle.txt` の `Ats64` セクションに `TR.ATSPI.CScript.dll` へのパスを指定するだけですが, 他のプラグインと併用する場合, DetailManager.dll等を使用する必要があります.

### 処理を書く
[専用の説明を用意したので, そちらをご覧ください](./ScriptingManual.md)

また, スクリプトファイルをプラグインとして使用するためには, 設定ファイルにパスを追加することが必要です.  
[設定ファイルの書き方は, こちらの説明をご覧ください](./XMLSettingManual.md)

### プラグインを使用する
1. スクリプトファイルへのパスを `TR.ATSPI.CScript.xml` に書きます
2. 通常通りシナリオをロードします
3. エラーがあればその都度表示されます.  通常通りシナリオが表示されれば, エラーなく実行できています.  
    なお, 一部エラーについては実行中に出る場合がありますので, ご注意ください

### アンインストール
1. インストールでコピペしたフォルダ/ファイルをすべて削除します
2. detailmanagerを使用していた場合, detailmodules.txtから `TR.ATSPI.CScript.dll` を指定している行を削除します
3. 本プラグインを単体で使用していた場合, Vehicleファイルを開き, `Ats64` セクションに記述されたDLLへのパスを削除します

## ライセンス
MITライセンスの下で, 再配布や改変等可能です.

車両データに同梱して配布する際は, [インストール](#インストール)でコピペしたフォルダの中身 (ファイル群) を一切削除せずに同梱することをおすすめします.

## 注意事項
- BVE5では使用できません.  BVE6専用です
- ロード時にコンパイルを行うため, シナリオのロード時間が延びます
- ほとんど制限なくスクリプトファイルやスクリプトファイルリストファイルをロードすることができますが, 数が多くなれば多くなるほどロードに時間がかかりますので, ご注意ください
- ロード/リロードが行われた際にスクリプトおよびスクリプトファイルリストが読み込まれます.  それ以外のタイミングでは読み込まれませんので, ご注意ください.  (例: 駅ジャンプではリロードされません)

## ダウンロード
[Releasesページからどうぞ](https://github.com/TetsuOtter/TR.ATSPI.CScript/releases)
