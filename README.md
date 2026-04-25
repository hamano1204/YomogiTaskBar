# YomogiTaskBar

YomogiTaskBarは、Windows用の垂直型タスクバーです。

私はウィンドウを多数開いて必要なウィンドウが探せないことが多い。
それを解決するためにタスクバーの代替として作成しました。
アプリの呼び出しや終了を、手早く操作できる事を目的として作成しています。


YomogiTaskBar is a vertical taskbar for Windows.

I often have many windows open and can't find the window I need.
I created this as an alternative to the taskbar to solve that problem.
I created it with the purpose of being able to quickly call up and exit applications.


## Features / 機能

- 表示方法の選択
　- 現在起動しているアプリ一覧をサイドバーに表示
　- マルチモニタ使用時はアイコンにモニタ番号を表示
　- 通常ウィンドウは上部に、最小化ウィンドウは下部に表示されます。
　- AppBar/エッジトリガーでの表示

- マウス操作
　- 一覧からアプリをアクティブ・終了できる

- キーボード操作
  - Win+Escで本アプリをフォーカス
  - 矢印でアプリを選択
  - Enterでアプリをアクティブ


- Display Options
  - Display list of running applications in sidebar
  - Show monitor numbers on icons when using multiple monitors
  - Normal windows displayed at top, minimized windows at bottom
  - Display via AppBar/edge trigger

- Mouse Operations
  - Activate and exit applications from the list

- Keyboard Operations
  - Focus this app with Win+Esc
  - Select applications with arrow keys
  - Activate application with Enter


## Requirements / 要件

- Windows
- .NET 9.0 SDK


## Build and Run / ビルドと実行

1. Visual Studioでソリューションを開くか、コマンドラインを使用します。
2. リポジトリのルートから次を実行します。

```powershell
cd YomogiTaskBar
dotnet build
dotnet run --project YomogiTaskBar.csproj
```

## Project Structure / プロジェクト構成

- `YomogiTaskBar.csproj` - WPFアプリケーションのプロジェクトファイル
- `App.xaml` / `App.xaml.cs` - アプリ起動
- `MainWindow.xaml` / `MainWindow.xaml.cs` - メインサイドバーのUIとアプリロジック
- `SettingsWindow.xaml` / `SettingsWindow.xaml.cs` - 設定ダイアログとショートカット設定
- `Managers/` - テーマ、設定、ホットキー、起動、ウィンドウ管理のヘルパー
- `Models/` - アプリ設定とショートカット構成のモデル
- `Themes/` - ライトおよびダークテーマリソース


## Notes / 注意事項

- 本アプリは単一インスタンスを維持し、別のコピーがすでに実行中の場合はメッセージを表示します。
- グローバルホットキーの登録は起動時に管理され、設定変更時に自動的に更新されます。
- 本アプリはAIで作成しています。
- プログラム経験が浅いため、コードの品質には注意が必要です。


## License / ライセンス

This project is licensed under CC0-1.0.

このプロジェクトはCC0-1.0の下でライセンスされています。


## スクリーンショット / Screenshots

### メイン画面 / Main Screen
![メイン画面](images/screenshot-main.png)
