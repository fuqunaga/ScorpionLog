namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// C# 10.0で追加された属性。引数の式を文字列として取得するために使用される。
    /// Unity では C# 10.0 がサポートされていないため、同名の属性を定義して使用する。
    /// csc.rsp に -langversion:10 を追加して C# 10.0 の機能を有効にすることで、C# 10.0 の機能を使用できるようになる。
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }
}