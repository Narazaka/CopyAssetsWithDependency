# CopyAssetsWithDependency

アセットを依存関係を保ったまま一括コピーするやつ

## 概要

元の依存関係を保ったまま新しいGUIDでアセットが作られます。

依存関係を保ってコピーしたいアセット群全てを一度に選択して、右クリックからCopyAssetsWithDependencyを選択するとコピーされます（確認ダイアログが出ます）。

### ユースケース

- 配布モデルのバリエーションを作りたい場合にマテリアルを直接いじった方が楽だと言う場合、これを使ってモデル依存関係丸ごとコピーして編集すると、元のバリエーションを上書きしないで作れます。
- 元のバリエーションを上書きして作られたモデルをインポートする場合、これを使ってあらかじめ別GUIDにしておくと上書きされずに済みます。

### 注意

- ファイルが増えると実行時間が長くなります（150ファイルで数分くらいはかかった）。気長に放置してお待ち下さい。
- 本ツールはとりあえずVRChatでの利用に問題ないという範囲で動作確認しています。アセットバンドル名などは変更しないためゲーム用などに使う場合は改修が必要かも知れません。

### ソース

https://gist.github.com/Narazaka/1ae51c8515e55ca3dbeec5a3eba313ed をUPM/VPM化したものです。

また本ツールはk7aさんの [[Unity]対象のアセットと依存関係のあるアセットたちを、依存関係含めてまるごとコピーする]( https://qiita.com/k7a/items/eb5a3ee4ed6448343543 )を一部改変して作ったものです。（「あるアセットの依存関係全部」ではなく「選択したアセット全部」という範囲にした）

## Install

### OpenUPM

See [OpenUPM page](https://openupm.com/packages/net.narazaka.unity.copy-assets-with-dependency/)

### VCC用インストーラーunitypackageによる方法（VRChatプロジェクトおすすめ）

https://github.com/Narazaka/CopyAssetsWithDependency/releases/latest から `net.narazaka.unity.copy-assets-with-dependency-installer.zip` をダウンロードして解凍し、対象のプロジェクトにインポートする。

### VCCによる方法

1. https://vpm.narazaka.net/ から「Add to VCC」ボタンを押してリポジトリをVCCにインストールします。
2. VCCでSettings→Packages→Installed Repositoriesの一覧中で「Narazaka VPM Listing」にチェックが付いていることを確認します。
3. アバタープロジェクトの「Manage Project」から「CopyAssetsWithDependency」をインストールします。

## License

[Zlib License](LICENSE.txt)
