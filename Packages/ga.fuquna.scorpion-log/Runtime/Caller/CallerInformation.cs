using System.IO;

namespace ScorpionLog;

public readonly record struct CallerInformation(string FilePath, int LineNumber, string MemberName)
{
    public string FileName => Path.GetFileName(FilePath);

    public override string ToString() => $"{MemberName}@{FileName}:{LineNumber}";
}