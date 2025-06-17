# OpenAPI-MCP-Proxy スキーマフラット化修正

## 修正内容

### 問題
InstructionStore APIなど一部のOpenAPIスペックでは、リクエストボディのスキーマに`type: object`が明示的に指定されていないため、スキーマのフラット化が機能せず、すべてのパラメータが`body`オブジェクト内にネストされていました。

### 解決策
`OpenApiService.cs`の`GenerateInputSchema`メソッドで、スキーマタイプの判定ロジックを修正：

```csharp
// 修正前
if (contentEntry.Value.Schema.Type == "object" && contentEntry.Value.Schema.Properties?.Count > 0)

// 修正後
if ((contentEntry.Value.Schema.Type == "object" || contentEntry.Value.Schema.Type == null) && 
    contentEntry.Value.Schema.Properties?.Count > 0)
```

## 修正後の動作

### 1. スキーマがフラット化される場合
リクエストボディが以下の条件を満たす場合、パラメータがフラット化されます：
- スキーマが`properties`を持つ
- `type`が`"object"`または未指定

**修正前のMCP関数定義:**
```json
{
  "inputSchema": {
    "properties": {
      "body": {
        "type": "object",
        "properties": {
          "query": {...},
          "searchTarget": {...}
        }
      }
    }
  }
}
```

**修正後のMCP関数定義:**
```json
{
  "inputSchema": {
    "properties": {
      "query": {...},
      "searchTarget": {...}
    }
  }
}
```

### 2. 引数の指定方法

修正後は、以下の**どちらの形式でも**動作します：

**推奨：フラット形式**
```json
{
  "method": "tools/call",
  "params": {
    "name": "postapisearchsingle",
    "arguments": {
      "query": {
        "queryId": "123",
        "searchTarget": "documents"
      }
    }
  }
}
```

**後方互換性：ネスト形式**
```json
{
  "method": "tools/call",
  "params": {
    "name": "postapisearchsingle",
    "arguments": {
      "body": {
        "query": {
          "queryId": "123",
          "searchTarget": "documents"
        }
      }
    }
  }
}
```

## PrepareHttpRequestの処理ロジック

`PrepareHttpRequest`メソッドは以下の優先順位で引数を処理します：

1. **明示的な`body`パラメータ**: 引数に`body`キーがある場合、その値をリクエストボディとして使用
2. **パスパラメータ**: URLテンプレート内の`{param}`を置換
3. **クエリパラメータ**: OpenAPIで明示的に定義されたクエリパラメータ
4. **暗黙的なボディパラメータ**: 上記のいずれにも該当しない引数は、リクエストボディを期待するエンドポイントの場合、自動的にボディパラメータとして収集

## 利点

1. **直感的な使用**: ユーザーは`body`でラップする必要がない
2. **後方互換性**: 既存の`body`形式も引き続き動作
3. **柔軟性**: OpenAPIスペックの記述方法に依存しない
4. **MCP Inspector対応**: より自然なパラメータ入力が可能

## テスト確認事項

修正が正しく機能することを確認するには：

1. `tools/list`でスキーマがフラット化されているか確認
2. フラット形式で`tools/call`が成功するか確認
3. 従来のネスト形式でも動作するか確認
4. パスパラメータやクエリパラメータを持つエンドポイントでも正しく動作するか確認