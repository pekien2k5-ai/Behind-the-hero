namespace BTH.Core.Actions
{
    /// <summary>Kết quả kiểm tra một ActionRequest trước khi chạy.</summary>
    public readonly struct ValidationResult
    {
        private ValidationResult(bool isValid, ActionError error, string message)
        {
            IsValid = isValid;
            Error = error;
            Message = message ?? string.Empty;
        }

        public bool IsValid { get; }

        public ActionError Error { get; }

        public string Message { get; }

        public static ValidationResult Ok()
        {
            return new ValidationResult(true, ActionError.None, string.Empty);
        }

        public static ValidationResult Fail(ActionError error, string message = "")
        {
            return new ValidationResult(false, error, message);
        }

        public override string ToString()
        {
            return IsValid ? "OK" : Error + ": " + Message;
        }
    }
}
