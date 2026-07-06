namespace Core.Persistence
{
    /// <summary>
    /// Serializer seam for save files, so the file gateway's atomicity/corruption logic stays
    /// pure C# (unit-testable with a stub) while the Unity JsonUtility implementation lives in
    /// one adapter class.
    /// </summary>
    public interface ISaveSerializer
    {
        string ToJson(object data);
        T FromJson<T>(string json) where T : class;
    }
}
