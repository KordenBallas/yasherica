using System;

namespace CharacterSystem.Core
{
    /// <summary>
    /// One validation finding. SubjectId names the offending part/socket/slot/bone
    /// so tooling can point the designer at the exact asset.
    /// </summary>
    public sealed class ValidationIssue
    {
        public ValidationSeverity Severity { get; }
        public ValidationIssueCode Code { get; }
        public string SubjectId { get; }
        public string Message { get; }

        public ValidationIssue(ValidationSeverity severity, ValidationIssueCode code, string subjectId, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("Validation issue message must be non-empty.", nameof(message));
            }

            Severity = severity;
            Code = code;
            SubjectId = subjectId;
            Message = message;
        }

        public override string ToString()
        {
            return $"[{Severity}] {Code} ({SubjectId}): {Message}";
        }
    }
}
