# HideTaskBar - アーキテクチャ設計

## システム概要

```mermaid
flowchart TB
    subgraph User["👤 ユーザー操作"]
        WinKey["⌨️ Winキー押下"]
        Click["🖱️ スタートメニュー以外をクリック"]
        TrayMenu["📋 トレイメニュー"]
    end

    subgraph App["🔧 HideTaskBar.exe"]
        KeyboardHook["KeyboardHook"]
        StartMenuMonitor["StartMenuMonitor"]
        TaskBarController["TaskBarController"]
        TrayIcon["TrayIcon"]
    end

    subgraph Windows["🪟 Windows"]
        TaskBar["タスクバー"]
    end

    WinKey --> KeyboardHook
    KeyboardHook -->|"Show()"| TaskBarController
    TaskBarController -->|"ShowWindow + SetWindowPos"| TaskBar

    Click --> StartMenuMonitor
    StartMenuMonitor -->|"Hide()"| TaskBarController

    TrayMenu --> TrayIcon
    TrayIcon -->|"終了"| App
```

---

## イベントフロー

```mermaid
sequenceDiagram
    participant U as ユーザー
    participant K as KeyboardHook
    participant S as StartMenuMonitor
    participant T as TaskBarController
    participant W as Windows

    Note over T,W: 起動時
    T->>W: Hide() - ShowWindow(SW_HIDE) + SetWindowPos(Y=-10000)
    Note over T: 50ms間隔で強制維持

    Note over U,W: Winキー押下
    U->>K: Winキー押下
    K->>T: WinKeyPressed
    T->>W: Show() - SetWindowPos(元の位置) + ShowWindow(SW_SHOW)
    K->>S: StartWaitingForClose()

    Note over U,W: スタートメニュー終了
    U->>W: 他の場所をクリック
    W->>S: EVENT_SYSTEM_FOREGROUND
    S->>T: StartMenuClosed
    T->>W: Hide()
```

---

## タスクバー非表示の仕組み

```mermaid
flowchart LR
    subgraph Hide["Hide() 処理"]
        A[ShowWindow SW_HIDE] --> B[SetWindowPos Y=-10000]
        B --> C[50ms後に再実行]
        C --> A
    end
    
    subgraph Show["Show() 処理"]
        D[SetWindowPos 元の位置] --> E[ShowWindow SW_SHOW]
    end
```

**2つのAPIを併用する理由:**
- `ShowWindow(SW_HIDE)`: ウィンドウを非表示にし、マウスホバー判定を無効化
- `SetWindowPos(Y=-10000)`: 念のため画面外に移動してバックアップ

---

## コンポーネント構成

```mermaid
classDiagram
    class TaskBarController {
        -bool _shouldBeHidden
        -Timer _enforceTimer
        +Hide()
        +Show()
        -EnforceHide()
    }

    class KeyboardHook {
        -GCHandle _gcHandle
        +event WinKeyPressed
    }

    class StartMenuMonitor {
        -bool _waitingForClose
        +StartWaitingForClose()
        +event StartMenuClosed
    }

    class TrayIcon {
        +event EnabledChanged
        +bool IsEnabled
    }

    KeyboardHook ..> TaskBarController : Show()
    KeyboardHook ..> StartMenuMonitor : StartWaitingForClose()
    StartMenuMonitor ..> TaskBarController : Hide()
```

---

## Win32 API 使用一覧

| コンポーネント | API | 用途 |
|----------------|-----|------|
| TaskBarController | `FindWindow` | タスクバーハンドル取得 |
| TaskBarController | `ShowWindow` | 表示/非表示切り替え |
| TaskBarController | `SetWindowPos` | 位置移動 |
| KeyboardHook | `SetWindowsHookEx` | キーボードフック |
| StartMenuMonitor | `SetWinEventHook` | フォアグラウンド変更監視 |

---

## 機能要件マッピング

| 要件 | 実装 |
|------|------|
| FR-1 | `ShowWindow(SW_HIDE)` + `SetWindowPos(Y=-10000)` 50ms間隔強制 |
| FR-2 | `WH_KEYBOARD_LL` フック |
| FR-3 | `EVENT_SYSTEM_FOREGROUND` 監視 |
| FR-4 | `NotifyIcon` + コンテキストメニュー |
| FR-5 | トレイメニュー「有効/無効」 |
| FR-6 | レジストリ `HKCU\...\Run` |
| FR-7 | `Shell_SecondaryTrayWnd` 対応 |
