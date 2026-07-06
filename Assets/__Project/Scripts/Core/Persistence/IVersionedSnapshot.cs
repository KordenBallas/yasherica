namespace Core.Persistence
{
    /// <summary>
    /// A root save DTO carrying its file-format version. The version gate is the whole FR14
    /// story: a file whose version does not match the current build's constant is treated as
    /// corrupt (discard-and-continue), never parsed on faith.
    /// </summary>
    public interface IVersionedSnapshot
    {
        int Version { get; }
    }
}
