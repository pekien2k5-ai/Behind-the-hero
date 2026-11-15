namespace BTH.Core.Actions
{
    /// <summary>Kết quả thực thi một ActionRequest; UI chỉ hiển thị, không tự đoán lỗi.</summary>
    public readonly struct ActionResult
    {
        private ActionResult(bool success, ActionError error, string message)
        {
            Success = success;
            Error = error;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }

        public ActionError Error { get; }

        public string Message { get; }

        public static ActionResult Ok()
        {
            return new ActionResult(true, ActionError.None, string.Empty);
        }

        public static ActionResult Fail(ActionError error, string message = "")
        {
            return new ActionResult(false, error, message);
        }

        public override string ToString()
        {
            return Success ? "OK" : Error + ": " + Message;
        }
    }
}
