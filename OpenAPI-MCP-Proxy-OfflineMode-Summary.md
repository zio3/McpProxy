# OpenAPI-MCP-Proxy オフラインモード実装完了

## 実装内容

### 1. 新規追加ファイル
- `Models/OperationMode.cs` - Online/Offlineモードの列挙型
- `Services/CacheService.cs` - ツールキャッシュの保存・読み込み機能

### 2. 修正ファイル
- `Services/McpService.cs` - モード管理とキャッシュ連携
- `Services/HttpProxyService.cs` - オフライン時のエラーハンドリング
- `Program.cs` - 起動時のオフラインモード対応

### 3. 主な機能

#### オンラインモード時
- OpenAPI仕様を正常に読み込み
- ツール情報を生成し、キャッシュファイルに保存
- 通常のHTTPリクエストを実行

#### オフラインモード時
- サーバー接続失敗時に自動的にオフラインモードに切り替え
- キャッシュからツール一覧を読み込み（tools/list）
- tools/call実行時に適切な日本語エラーメッセージを表示
- サーバー復旧時に自動的にオンラインモードに戻る

### 4. キャッシュファイル
- 場所: 実行ファイルと同じディレクトリ
- ファイル名: `tools_cache_{url_hash}.json`
- 内容: ツール定義とメタデータ

### 5. エラーメッセージ
```json
{
  "error": {
    "code": -1,
    "message": "サーバーがオフラインです。WSLやサーバーが起動しているか確認してください。",
    "details": "接続先: {url}"
  }
}
```

### 6. ログ出力
- モード切り替え: `[INFO] Operation mode changed: Online -> Offline`
- キャッシュ使用: `[INFO] Using cached tools (Offline mode): {filename}`
- キャッシュ保存: `[INFO] Tools cached successfully: {filename}`

## 使用方法

1. 初回はオンラインでツールを読み込み（キャッシュ作成）
2. サーバーダウン時もツール一覧は表示可能
3. ツール実行時は適切なエラーメッセージ表示
4. サーバー復旧後は自動的に通常動作に戻る

## 制限事項

- 初回起動時にサーバーがダウンしている場合、キャッシュがなければ起動不可
- Program.cs での初期ロード失敗時は現在の実装では即座に終了
- より完全なオフライン対応には、起動フローの変更が必要