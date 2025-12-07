# HideTaskBar - アーキテクチャ設計

## システム概要

```mermaid
flowchart TB
    subgraph User["👤 ユーザー操作"]
        WinKey["⌨️ Winキー押下"]
        Click["🖱️ スタートメニュー以外の場所をクリック"]
        TrayMenu["📋 トレイメニュー→終了"]
    end

    subgraph App["🔧 HideTaskBar.exe"]
        KeyboardHook["KeyboardHook"]
        StartMenuMonitor["StartMenuMonitor"]
        TaskBarController["TaskBarController"]
        TrayIcon["TrayIcon"]
    end

    subgraph Windows["🪟 Windows"]
        TaskBar["タスクバー"]
        StartMenu["スタートメニュー"]
    end

    WinKey --> KeyboardHook
    KeyboardHook -->|"Show()"| TaskBarController
    TaskBarController -->|"ShowWindow API"| TaskBar

    StartMenu -->|"フォーカス変化"| StartMenuMonitor
    Click --> StartMenu
    StartMenuMonitor -->|"Hide()"| TaskBarController

    TrayMenu --> TrayIcon
    TrayIcon -->|"Application.Exit()"| App
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
    T->>W: Hide() - タスクバー非表示

    Note over U,W: Winキー押下
    U->>K: Winキー押下
    K->>T: WinKeyPressed イベント
    T->>W: Show() - タスクバー表示
    W->>U: スタートメニュー表示

    Note over U,W: スタートメニュー終了
    U->>W: スタートメニュー以外の場所をクリック
    W->>S: フォアグラウンド変更
    S->>T: StartMenuClosed イベント
    T->>W: Hide() - タスクバー非表示

    Note over U,W: アプリ終了
    U->>T: トレイ→終了
    T->>W: Show() - タスクバー復元
```

---

## コンポーネント構成

```mermaid
classDiagram
    class Program {
        +Main()
    }

    class TaskBarController {
        -List~IntPtr~ _taskBarHandles
        +Hide()
        +Show()
        +Dispose()
    }

    class KeyboardHook {
        -IntPtr _hookId
        +event WinKeyPressed
        +Dispose()
    }

    class StartMenuMonitor {
        -IntPtr _hookHandle
        -bool _startMenuWasOpen
        +event StartMenuClosed
        +Dispose()
    }

    class TrayIcon {
        -NotifyIcon _notifyIcon
        -ContextMenuStrip _contextMenu
        +Dispose()
    }

    Program --> TaskBarController
    Program --> KeyboardHook
    Program --> StartMenuMonitor
    Program --> TrayIcon

    KeyboardHook ..> TaskBarController : WinKeyPressed→Show()
    StartMenuMonitor ..> TaskBarController : StartMenuClosed→Hide()
    TrayIcon ..> Program : 終了→Application.Exit()
```

---

## Win32 API 使用一覧

```mermaid
mindmap
  root((Win32 API))
    TaskBarController
      FindWindow
      FindWindowEx
      ShowWindow
    KeyboardHook
      SetWindowsHookEx
      UnhookWindowsHookEx
      CallNextHookEx
      GetModuleHandle
    StartMenuMonitor
      SetWinEventHook
      UnhookWinEvent
      GetForegroundWindow
      GetClassName
      GetWindowText
```

---

## 機能要件マッピング

| 要件 | コンポーネント | 実装 |
|------|----------------|------|
| FR-1 | TaskBarController | `ShowWindow(hwnd, SW_HIDE)` |
| FR-2 | KeyboardHook | `WH_KEYBOARD_LL` フック |
| FR-3 | StartMenuMonitor | `EVENT_SYSTEM_FOREGROUND` 監視 |
| FR-4 | TrayIcon | `NotifyIcon` + コンテキストメニュー |
