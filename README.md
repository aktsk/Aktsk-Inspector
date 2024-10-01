# Aktsk Inspector 

MITライセンスで公開されている [Tri Inspector](https://github.com/codewriter-packages/Tri-Inspector) をアカツキ側でカスタマイズするためにフォークしたリポジトリです。

## インストール方法

### Package Manager 経由でのインストール

左上の＋ボタンから「Add package from git URL...」を選択し、「https://github.com/aktsk/Aktsk-Inspector.git」を入力して「Add」

### manifest.json 直接編集でのインストール

Packages/manifest.json に `"com.codewriter.triinspector": "https://github.com/aktsk/Aktsk-Inspector.git"` の記述を追加

```manifest.json
{
  "dependencies": {
    "com.codewriter.triinspector": "https://github.com/aktsk/Aktsk-Inspector.git",
    ...
}
```

## Tri Inspector からの変更点
