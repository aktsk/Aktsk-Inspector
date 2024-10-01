# Aktsk Inspector 

MITライセンスで公開されている [Tri Inspector](https://github.com/codewriter-packages/Tri-Inspector) をアカツキ側でカスタマイズするためにフォークしたリポジトリです。

## インストール方法

### Package Manager 経由でのインストール

1. Package Manager を開く
2. 左上の＋ボタンをクリック
3. 「Add package from git URL...」をクリック
4. テキストフィールドに `https://github.com/aktsk/Aktsk-Inspector.git` と入力
5. 「Add」ボタンをクリック

### manifest.json 直接編集でのインストール

Packages/manifest.json に `"com.codewriter.triinspector": "https://github.com/aktsk/Aktsk-Inspector.git"` の記述を追加

```manifest.json
{
  ...
  "dependencies": {
    "com.codewriter.triinspector": "https://github.com/aktsk/Aktsk-Inspector.git",
    ...
  },
  ...
}
```

## Tri Inspector からの変更点
