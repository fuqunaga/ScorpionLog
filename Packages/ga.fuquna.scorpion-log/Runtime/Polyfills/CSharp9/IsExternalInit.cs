using System.ComponentModel;

namespace System.Runtime.CompilerServices;

/// <summary>
/// C# 9.0で追加された init-only セッターを使用するために必要なクラス。
/// Unity では C# 9.0 がサポートされていないため、同名のクラスを定義して使用する。
/// record 型や init プロパティを使用する場合に必要となる。
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit { }