namespace Combat.Execution
{
    /// <summary>
    /// Result of action validation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the action is valid.
        /// </summary>
        public bool IsValid { get; }
        
        /// <summary>
        /// Reason for failure if not valid.
        /// </summary>
        public string FailureReason { get; }
        
        /// <summary>
        /// Additional details about the validation.
        /// </summary>
        public string Details { get; }
        
        private ValidationResult(bool isValid, string failureReason = null, string details = null)
        {
            IsValid = isValid;
            FailureReason = failureReason;
            Details = details;
        }
        
        public static ValidationResult Success()
        {
            return new ValidationResult(true);
        }
        
        public static ValidationResult Failure(string reason, string details = null)
        {
            return new ValidationResult(false, reason, details);
        }
    }
}

