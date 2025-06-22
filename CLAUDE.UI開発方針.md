# CLAUDE.UI開発方針.md

## 開発者からの指示

> - 現状中途半端な実装はあるものの、新しく書き直したい
> - まずはbetrayer1と同じような画面構成で行こうと思います（マップとメニューを横並びに）
> - DevelopmentのIMGUIの開発パネル(TileInfoEditorWindow)の内容も参考にしてください

## 基本方針

### 1. 現状の扱い
- **現状の中途半端な実装は新しく書き直す**
- 既存のMainUI.cs等は参考程度に留め、一から設計し直す
- ただし、Rosalinaの自動生成システムは活用する

### 2. 全体レイアウト設計
- **betrayer1と同じような画面構成を採用**: マップとメニューを横並び配置
- 左側: ゲームマップ表示領域
- 右側: UI操作パネル（メニュー、情報表示等）

## betrayer1のUI構造を参考にした設計

### レイアウト構成
```
┌─────────────────┬─────────────────┐
│                 │                 │
│   ゲームマップ   │   UIパネル      │
│   (左側)        │   (右側)        │
│                 │                 │
│                 │ ┌─────────────┐ │
│                 │ │ 日付・ターン │ │
│                 │ ├─────────────┤ │
│                 │ │ プレイヤー情報│ │
│                 │ ├─────────────┤ │
│                 │ │ タイル詳細   │ │
│                 │ ├─────────────┤ │
│                 │ │ アクション   │ │
│                 │ │ ボタン       │ │
│                 │ ├─────────────┤ │
│                 │ │ キャラ情報   │ │
│                 │ └─────────────┘ │
└─────────────────┴─────────────────┘
```

### UI階層設計（betrayer1参考）
```
MainUI (新設計)
├── MapContainer (左側)
│   └── ゲームマップ表示
├── UIPanel (右側)
│   ├── DatePanel (日付・ターン情報)
│   ├── PlayerPanel (プレイヤー情報)
│   ├── TileDetailPanel (選択タイル詳細)
│   ├── ActionPanel (アクション選択)
│   └── CharacterInfoPanel (キャラクター情報)
└── WindowHost (モーダルウィンドウ)
    ├── BattleWindow
    ├── SystemWindow
    └── MessageWindow
```

### 再利用可能な部品設計
betrayer1のParts/フォルダを参考に、以下の部品を作成：

#### 基本部品
- **CharacterInfo**: キャラクター情報表示（顔画像、ステータス、兵士等）
- **SoldierIcon**: 兵士アイコン表示（HP、レベル等）
- **ActionButton**: アクション実行ボタン（コスト表示、実行可能判定等）
- **CountryInfo**: 国家情報表示（統治者、関係等）

#### リスト系部品
- **CharacterList**: キャラクター一覧表示
- **ActionList**: 利用可能アクション一覧
- **MessageList**: ログ・メッセージ履歴

## DevelopmentのIMGUI開発パネル参考項目

### TileInfoEditorWindowから参考にする機能
```csharp
// 主要な表示項目
- キャラクター情報表示（顔画像、ステータス、兵士状況）
- 城・町・領地情報の詳細表示
- 国家間関係の表示・編集
- 兵士HP・レベルの視覚的表示
- アクションボタンのレイアウト
- ページング機能（キャラクター一覧等）
```

### 参考にするUIパターン
1. **パラメータバー表示**: HP、ステータス等の視覚的表示
2. **キャラクター顔画像**: 統一的な表示サイズとレイアウト
3. **情報の階層化**: 城→町→キャラの関係表示
4. **編集可能フィールド**: 数値入力とスライダーの組み合わせ
5. **アクションボタン群**: 実行可能状態の視覚的表現

## 技術仕様

### Rosalina活用方針
- **UXML**: 各パネルのレイアウト定義
- **USS**: 統一的なスタイル設定
- **自動生成C#**: UI要素へのアクセッサー

### ファイル構成
```
Assets/Main/UI/ (新設計)
├── MainUI.cs              # 新メインUIコントローラー
├── MainUI.uxml            # 新メインレイアウト
├── MainUI.uss             # 新メインスタイル
├── Panels/                # パネル系（画面の一部）
│   ├── DatePanel.cs/.uxml
│   ├── PlayerPanel.cs/.uxml
│   ├── TileDetailPanel.cs/.uxml
│   ├── ActionPanel.cs/.uxml
│   └── CharacterInfoPanel.cs/.uxml
├── Parts/                 # 再利用部品
│   ├── CharacterInfo.cs/.uxml
│   ├── SoldierIcon.cs/.uxml
│   ├── ActionButton.cs/.uxml
│   └── CountryInfo.cs/.uxml
└── Windows/               # モーダルウィンドウ
    ├── BattleWindow.cs/.uxml
    ├── SystemWindow.cs/.uxml
    └── MessageWindow.cs/.uxml
```

## 開発の進め方

### フェーズ1: 基本レイアウト
1. MainUI.uxml で左右分割レイアウト作成
2. 基本的なパネル配置
3. Rosalina自動生成の動作確認

### フェーズ2: 部品作成
1. CharacterInfo等の基本部品実装
2. ActionButton等のインタラクティブ部品
3. データバインディング機能

### フェーズ3: 統合・調整
1. GameCoreとの連携
2. アクションシステムとの統合
3. スタイル・見た目の調整

## 重要な設計原則

1. **betrayer1のUI階層を参考にしつつ、betrayer2の機能に適応**
2. **開発パネルの豊富な情報表示機能を活用**
3. **Rosalinaの自動生成を最大限活用**
4. **再利用可能な部品設計で保守性向上**
5. **データバインディングによる効率的な更新**