# YomogiTaskBar 設計書

## 1. システム概要

### 1.1 目的
YomogiTaskBarは、Windows用の垂直型タスクバーアプリケーションです。起動中のアプリケーションを一覧表示し、マウス操作またはキーボードショートカットによるアプリ切り替えを可能にします。

### 1.2 主な機能
- 起動中アプリの一覧表示と切り替え
- マウス操作によるアプリ切り替え
- キーボードショートカットによる操作（Win+Escでフォーカス、上下キーで選択、Enterでアクティブ）
- 仮想デスクトップの切り替え・作成・削除
- ウィンドウの最小化/最大化/閉じる操作
- マルチモニター対応（ウィンドウのモニター間移動）
- ピン留めモードとエッジトリガーモード
- テーマ切り替え（Light/Dark/System）
- スタートアップ起動設定

### 1.3 技術スタック
- **フレームワーク**: .NET 9.0
- **UI**: WPF (Windows Presentation Foundation)
- **言語**: C#
- **ターゲット**: Windows 10 (Build 19041.0) 以降
- **依存関係**: Windows Forms (NotifyIcon用), Windows.Management.Deployment (UWPアイコン取得用)

---

## 2. アーキテクチャ

### 2.1 全体構成
```
┌─────────────────────────────────────────────────────────┐
│                      App.xaml                           │
│              (アプリケーションエントリーポイント)         │
│              App.xaml.cs (多重起動防止)                   │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│                  MainWindow.xaml                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │            MainWindow.xaml.cs                     │  │
│  │  ┌────────────────────────────────────────────┐  │  │
│  │  │  Controllers                              │  │  │
│  │  │  - AppBarController                        │  │  │
│  │  │  - WindowStateManager                      │  │  │
│  │  └────────────────────────────────────────────┘  │  │
│  │  ┌────────────────────────────────────────────┐  │  │
│  │  │  Managers                                  │  │  │
│  │  │  - WindowManager                           │  │  │
│  │  │  - AppBarManager                           │  │  │
│  │  │  - SettingsManager                         │  │  │
│  │  │  - HotkeyListener                          │  │  │
│  │  │  - StartupManager                          │  │  │
│  │  └────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                     │
         ┌───────────┼───────────┐
         ▼           ▼           ▼
┌─────────────┐ ┌─────────┐ ┌──────────────┐
│  ViewModels │ │ Models  │ │  Utilities   │
│             │ │         │ │              │
│ WindowItem  │ │Settings │ │ NativeMethods│
│ ViewModel   │ │(統合)   │ │ Logger       │
│             │ │         │ │              │
└─────────────┘ └─────────┘ └──────────────┘
```

### 2.2 レイヤー構成
- **Presentation Layer**: XAML (MainWindow.xaml, SettingsWindow.xaml)
- **View Layer**: MainWindow.xaml.cs, SettingsWindow.xaml.cs
- **Controller Layer**: AppBarController, WindowStateManager
- **Manager Layer**: 各機能マネージャー
- **ViewModel Layer**: WindowItemViewModel
- **Model Layer**: Settings (AppSettings, WindowSettings, ShortcutConfig, LayoutMode, MonitorIndicatorDisplay)
- **Utility Layer**: NativeMethods, Logger

---

## 3. コンポーネント詳細

### 3.1 App.xaml / App.xaml.cs
**役割**: アプリケーションのエントリーポイント、多重起動防止

**主要機能**:
- Mutexを使用した多重起動防止
- アプリケーションの起動・終了管理

### 3.2 MainWindow.xaml / MainWindow.xaml.cs
**役割**: メインタスクバーのUIと制御

**主要機能**:
- ウィンドウリストの表示と更新
- グローバルホットキーの登録と処理 (Win+Esc)
- マウスイベント処理（ドラッグ、リサイズ、エッジトリガー）
- 仮想デスクトップ操作（切り替え、作成、削除）
- 設定画面の表示
- System Trayアイコンの管理
- テーマ適用
- キーボードナビゲーションと選択状態管理

**依存関係**:
- AppBarController: AppBar機能の制御
- WindowStateManager: ウィンドウ状態の永続化
- WindowManager: ウィンドウ操作
- SettingsManager: 設定管理

### 3.3 SettingsWindow.xaml / SettingsWindow.xaml.cs
**役割**: 設定画面のUIと制御

**主要機能**:
- テーマ設定 (System/Light/Dark)
- レイアウトモード設定 (Simple/AllDesktops)
- モニターインジケーター設定 (None/Left/Right)
- スタートアップ設定
- ホットキー設定
- 設定の保存・キャンセル

### 3.4 Controllers

#### 3.4.1 AppBarController
**役割**: AppBar機能とエッジ検出の管理

**主要機能**:
- AppBarの登録・登録解除
- ピンモードの切り替え
- ウィンドウの表示/非表示（エッジトリガーモード）
- エッジ検出とドッキング
- 幅の更新とプレビュー

**依存関係**:
- AppBarManager: Windows APIとのやり取り

#### 3.4.2 WindowStateManager
**役割**: ウィンドウ状態の永続化と復元

**主要機能**:
- ウィンドウ設定の保存（位置、幅、エッジ、モニター）
- ウィンドウ設定の復元
- モニター構成変更の検出
- 設定のバリデーション

### 3.5 Managers

#### 3.5.1 AppBarManager
**役割**: Windows AppBar APIとのやり取り

**主要機能**:
- AppBarの登録・登録解除（SHAppBarMessage）
- 位置とサイズの設定（ABM_QUERYPOS, ABM_SETPOS）
- 幅の更新とプレビュー

**依存関係**:
- NativeMethods: Windows API呼び出し

#### 3.5.2 WindowManager
**役割**: 実行中ウィンドウの取得と操作

**主要機能**:
- 実行中ウィンドウの列挙とフィルタリング (2モード: Simple/AllDesktops)
- ウィンドウのアクティブ化、最小化、最大化、閉じる
- ウィンドウのモニター間移動
- ウィンドウアイコンの取得（UWP対応: PackageManager使用）
- 仮想デスクトップフィルタリング (IVirtualDesktopManager使用)
- アイコンキャッシュ管理

**依存関係**:
- NativeMethods: Windows API呼び出し
- Windows.Management.Deployment: UWPアイコン取得用

#### 3.5.3 SettingsManager
**役割**: 設定ファイルの読み書き

**主要機能**:
- JSON形式での設定の保存・読み込み (System.Text.Json使用)
- デフォルト設定の提供
- 設定ファイルの検証

**保存場所**: `%APPDATA%\YomogiTaskBar\settings.json`

#### 3.5.4 HotkeyListener
**役割**: グローバルホットキーの登録と処理

**主要機能**:
- ホットキーの登録（RegisterHotKey）
- ホットキーの解除
- 修飾キーの変換

#### 3.5.5 StartupManager
**役割**: スタートアップ起動の管理 (レジストリベース)

**主要機能**:
- レジストリ (`HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`) への登録・削除
- スタートアップ有効状態の確認

### 3.6 ViewModels

#### 3.6.1 WindowItemViewModel
**役割**: ウィンドウリストアイテムのデータモデル (INotifyPropertyChanged実装)

**プロパティ**:
- Handle: ウィンドウハンドル
- Title: ウィンドウタイトル
- ProcessId: プロセスID
- IconSource: アイコン (ImageSource)
- IsMinimized: 最小化状態
- IsSeparator: セパレータフラグ
- IsDesktopSeparator: デスクトップ区切りフラグ
- MonitorIndex: モニターインデックス
- IsActive: アクティブ状態
- DesktopId: 仮想デスクトップID
- DesktopName: デスクトップ名
- IsCurrentDesktop: 現在のデスクトップフラグ
- ShouldShowLeftIndicator: 左モニターインジケーター表示フラグ
- ShouldShowRightIndicator: 右モニターインジケーター表示フラグ

### 3.7 Models (Settings.cs)

#### 3.7.1 LayoutMode (Enum)
**役割**: アプリ一覧のレイアウトモード

**値**:
- Simple: シンプルレイアウト（現在のデスクトップのみ）
- AllDesktops: すべてのデスクトップのウィンドウを表示

#### 3.7.2 MonitorIndicatorDisplay (Enum)
**役割**: モニターインジケーターの表示設定

**値**:
- None: 表示しない
- Left: 左側に表示
- Right: 右側に表示

#### 3.7.3 ShortcutConfig
**役割**: ショートカットキー設定

**プロパティ**:
- Key: キー (System.Windows.Input.Key)
- Modifiers: 修飾キー (System.Windows.Input.ModifierKeys)

**メソッド**:
- ToString(): ショートカット表示文字列を生成 (例: "Ctrl + J")
- IsPressed(KeyEventArgs): キーイベントがこのショートカットと一致するか判定

#### 3.7.4 WindowSettings
**役割**: ウィンドウ位置と表示設定

**プロパティ**:
- IsAppBarMode: AppBarモードフラグ（常にtrue）
- Edge: 配置エッジ（ABEdge.Left/Right）
- MonitorIndex: モニターインデックス
- WindowWidth: ウィンドウ幅（MinWindowWidth〜MaxWindowWidthの範囲でクランプ）
- LastMonitorCount: 前回のモニター数

**メソッド**:
- ValidateAndFix(): 設定値の検証と修正

#### 3.7.5 AppSettings
**役割**: アプリケーション全体の設定

**プロパティ**:
- ThemeMode: テーマモード（"Light"/"Dark"/"System"）
- LaunchOnStartup: スタートアップ起動フラグ
- LayoutMode: レイアウトモード（Simple/AllDesktops）
- MonitorIndicatorDisplay: モニターインジケーター表示設定
- GlobalActivate: グローバルアクティベートホットキー（デフォルト: Win+Esc）
- Minimize: 最小化ホットキー（デフォルト: Ctrl+J）
- ToggleMaximize: 最大化切り替えホットキー（デフォルト: Ctrl+K）
- Close: 閉じるホットキー（デフォルト: Ctrl+L）
- NextMonitor: 次のモニターへ移動ホットキー（デフォルト: Ctrl+I）
- PrevMonitor: 前のモニターへ移動ホットキー（デフォルト: Ctrl+U）
- WindowSettings: ウィンドウ設定（WindowSettingsインスタンス）

### 3.8 Utilities

#### 3.8.1 NativeMethods
**役割**: Windows APIのP/Invoke宣言

**主要API**:
- EnumWindows, GetWindowText: ウィンドウ列挙
- SetForegroundWindow, ShowWindow: ウィンドウ操作
- SHAppBarMessage: AppBar管理
- RegisterHotKey, UnregisterHotKey: ホットキー
- DwmGetWindowAttribute, DwmSetWindowAttribute: DWM API
- その他多数のWindows API

#### 3.8.2 Logger
**役割**: ログ出力（Debug.WriteLineベース）

**メソッド**:
- LogInfo(string, string): 情報ログ
- LogWarning(string, string): 警告ログ
- LogError(string, Exception?, string): エラーログ
- LogDebug(string, string): デバッグログ（DEBUGビルドのみ）
- LogOperationStart(string, string): 操作開始ログ
- LogOperationComplete(string, string): 操作完了ログ
- LogOperationFailed(string, Exception?, string): 操作失敗ログ

---

## 4. データフロー

### 4.1 起動シーケンス
```
App.xaml (OnStartup)
  → 多重起動チェック
  → MainWindow生成
  → MainWindow (Window_Loaded)
    → 設定読み込み
    → テーマ適用
    → Controller初期化
    → AppBar登録
    → ウィンドウ設定復元
    → ホットキー登録
    → タイマー開始
    → NotifyIcon設定
```

### 4.2 ウィンドウリスト更新シーケンス
```
DispatcherTimer (Tick)
  → VirtualDesktopHelper.MoveToCurrentDesktop (MainWindow.xaml.cs内)
  → RefreshWindowList
    → WindowManager.GetRunningWindows
      → EnumWindows
      → フィルタリング（可視、クローク、仮想デスクトップ）
      → アイコン取得 (UWP対応込み)
      → ソート（通常→最小化、またはデスクトップ順）
    → ObservableCollection更新
```

### 4.3 アプリ切り替えシーケンス
```
ユーザー操作（クリック/Enter）
  → ActivateSelectedItem
    → WindowManager.ActivateWindow
      → ShowWindow (SW_RESTORE if minimized)
      → SetForegroundWindow
    → AppBarController.HideWindow (if unpinned)
```

### 4.4 設定保存シーケンス
```
MainWindow (Window_Closing)
  → WindowStateManager.SaveWindowSettings
    → 現在の位置・幅・エッジ・モニターを取得
    → SettingsManager.Save
      → JSONシリアライズ
      → ファイル書き込み
```

---

## 5. キー技術ポイント

### 5.1 AppBar実装
- WindowsのAppBar API（SHAppBarMessage）を使用
- システムにタスクバー領域を予約
- ABM_QUERYPOSでシステム承認済み位置を取得
- ABM_SETPOSで位置を設定

### 5.2 ウィンドウフィルタリング
- 可視ウィンドウのみ（IsWindowVisible）
- クロークされていないウィンドウ（DWMWA_CLOAKED）
- 現在の仮想デスクトップ上のウィンドウ
- 特定タイトル除外（Program Manager等）
- サイズ異常ウィンドウ除外

### 5.3 UWPアプリ対応
- ApplicationFrameHost.exeを検出
- 子ウィンドウから実際のプロセスIDを取得
- PackageManagerからアプリパッケージ情報を取得
- Logo URIからアイコンを取得

### 5.4 エッジトリガーモード
- ピン留め解除時、マウスが離れるとウィンドウを隠す
- 画面端に細い表示ストリップを残す
- マウスが戻るとウィンドウを表示

### 5.5 DPI対応
- PresentationSourceからDPIスケールを取得
- 座標計算時にDPI補正を行う

---

## 6. 設定ファイル構造

```json
{
  "ThemeMode": "System",
  "LaunchOnStartup": false,
  "LayoutMode": "Simple",
  "MonitorIndicatorDisplay": "Right",
  "GlobalActivate": {
    "Key": "Escape",
    "Modifiers": "Windows"
  },
  "Minimize": {
    "Key": "J",
    "Modifiers": "Control"
  },
  "ToggleMaximize": {
    "Key": "K",
    "Modifiers": "Control"
  },
  "Close": {
    "Key": "L",
    "Modifiers": "Control"
  },
  "NextMonitor": {
    "Key": "I",
    "Modifiers": "Control"
  },
  "PrevMonitor": {
    "Key": "U",
    "Modifiers": "Control"
  },
  "WindowSettings": {
    "IsAppBarMode": true,
    "Edge": 2,
    "MonitorIndex": 0,
    "WindowWidth": 300.0,
    "LastMonitorCount": 1
  }
}
```

---

## 7. 既知の制約・注意点

### 7.1 ウィンドウフィルタリング
- Edgeブラウザで検索した際にEdgeがタスクバーから見えなくなる問題があるため、一部のフィルタリングロジックをコメントアウトしている（WindowManager.cs:386-388）

### 7.2 アイコンキャッシュ
- 最小化されたUWPアプリは汎用アイコンを返すことがあるため、最小化状態のウィンドウはアイコンをキャッシュしない（WindowManager.cs:514-517）

### 7.3 ピンモード
- 安定性のため、起動時は常にピン留めモードで開始する（MainWindow.xaml.cs:66-68）

### 7.4 モニター構成
- モニター数が変更された場合、自動的に最初のモニターにリセットされる（WindowStateManager.cs:78-83）

---

## 8. 今後の改善案

### 8.1 アーキテクチャ
- MVVMパターンの完全な採用（現在はViewModelのみでModelとViewの分離が不完全）
- 依存性注入コンテナの導入
- イベントベースの疎結合化（Messengerパターン）
- VirtualDesktopHelperとThemeManagerの分離（独立したクラスとして実装）

### 8.2 機能
- ウィンドウのピン留め（特定のウィンドウを常にリストの上部に表示）
- ウィンドウのグループ化（同じアプリのウィンドウをグループ化）
- カスタムテーマのサポート（ユーザー定義カラー）
- プラグインシステム
- ウィンドウ検索機能

### 8.3 テスト
- 単体テストの追加
- 統合テストの追加
- UIテストの追加

---

## 9. 参考文献

### 9.1 Windows API
- [AppBar Documentation](https://docs.microsoft.com/en-us/windows/win32/shell/appbar)
- [Virtual Desktops API](https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ivirtualdesktopmanager)

### 9.2 WPF
- [WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)

### 9.3 UWP
- [PackageManager Class](https://docs.microsoft.com/en-us/uwp/api/windows.management.deployment.packagemanager)
