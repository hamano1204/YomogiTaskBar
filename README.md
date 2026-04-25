# YomogiTaskBar

YomogiTaskBar is a lightweight Windows taskbar-sidebar application built with WPF and .NET 10.0. It provides a compact window/task switcher that integrates with virtual desktops and supports customizable keyboard shortcuts.

YomogiTaskBarは、WPFと.NET 10.0で構築された軽量なWindows用タスクバーサイドバーアプリです。仮想デスクトップと連携し、カスタマイズ可能なキーボードショートカットを備えたコンパクトなウィンドウ/タスク切り替え機能を提供します。

## Features / 機能

- Custom taskbar-style sidebar for active windows
- Virtual desktop switcher with current desktop indicator
- System tray icon with exit menu
- Configurable hotkeys for:
  - Activate app
  - Minimize
  - Toggle maximize
  - Close window
  - Move window to next monitor
  - Move window to previous monitor
- Appearance settings with light / dark / system theme support
- Startup launch toggle
- Localization support for English and Japanese

- アクティブウィンドウ表示のタスクバー風サイドバー
- 現在の仮想デスクトップを示す仮想デスクトップ切り替え機能
- 終了メニュー付きのシステムトレイアイコン
- 以下のホットキーをカスタマイズ可能
  - アプリ起動
  - 最小化
  - 最大化/復元
  - ウィンドウを閉じる
  - 次のモニターへ移動
  - 前のモニターへ移動
- ライト / ダーク / システムテーマ対応の外観設定
- 起動時自動起動のオン/オフ切り替え
- 英語と日本語のローカライズ対応

## Requirements / 要件

- Windows
- .NET 10.0 SDK

- Windows
- .NET 10.0 SDK

## Build and Run / ビルドと実行

1. Open the solution in Visual Studio or use the command line.
2. From the repository root:

```powershell
cd c:\Users\haman_9\OneDrive\Desktop\dev\NewTaskBar3
dotnet build
dotnet run --project YomogiTaskBar.csproj
```

1. Visual Studioでソリューションを開くか、コマンドラインを使用します。
2. リポジトリのルートから次を実行します。

```powershell
cd c:\Users\haman_9\OneDrive\Desktop\dev\NewTaskBar3
dotnet build
dotnet run --project YomogiTaskBar.csproj
```

## Project Structure / プロジェクト構成

- `YomogiTaskBar.csproj` - WPF application project file
- `App.xaml` / `App.xaml.cs` - application startup and localization setup
- `MainWindow.xaml` / `MainWindow.xaml.cs` - main sidebar UI and application logic
- `SettingsWindow.xaml` / `SettingsWindow.xaml.cs` - settings dialog and shortcut configuration
- `Managers/` - theme, localization, settings, hotkey, startup, and window management helpers
- `Models/` - application settings and shortcut configuration models
- `Themes/` - light and dark theme resources
- `Localization/` - English and Japanese resource dictionaries

- `YomogiTaskBar.csproj` - WPFアプリケーションのプロジェクトファイル
- `App.xaml` / `App.xaml.cs` - アプリ起動とローカライズ設定
- `MainWindow.xaml` / `MainWindow.xaml.cs` - メインサイドバーのUIとアプリロジック
- `SettingsWindow.xaml` / `SettingsWindow.xaml.cs` - 設定ダイアログとショートカット設定
- `Managers/` - テーマ、ローカライズ、設定、ホットキー、起動、ウィンドウ管理のヘルパー
- `Models/` - アプリ設定とショートカット構成のモデル
- `Themes/` - ライトおよびダークテーマリソース
- `Localization/` - 英語と日本語のリソース辞書

## Notes / 注意事項

- The app keeps a single instance running and shows a message if another copy is already open.
- Global hotkey registration is managed on startup and automatically updated when settings change.

- 本アプリは単一インスタンスを維持し、別のコピーがすでに実行中の場合はメッセージを表示します。
- グローバルホットキーの登録は起動時に管理され、設定変更時に自動的に更新されます。

## License / ライセンス

This project is licensed under CC0-1.0.

このプロジェクトはCC0-1.0の下でライセンスされています。
