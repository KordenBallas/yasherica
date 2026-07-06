using UnityEngine;

namespace Core.Persistence
{
    /// <summary>
    /// JsonUtility-backed <see cref="ISaveSerializer"/>. The snapshot DTOs were shaped for it
    /// (fields, primitives, lists — no dictionaries), so no external JSON package is needed.
    /// </summary>
    public sealed class UnityJsonSaveSerializer : ISaveSerializer
    {
        public string ToJson(object data) => JsonUtility.ToJson(data, prettyPrint: true);

        public T FromJson<T>(string json) where T : class => JsonUtility.FromJson<T>(json);
    }
}
