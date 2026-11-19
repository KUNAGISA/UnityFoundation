namespace Gameplay
{
    public readonly struct SaveResult
    {
        public SaveOperationStatus Status { get; }
        public string Error { get; }

        private SaveResult(SaveOperationStatus status, string error)
        {
            Status = status;
            Error = error;
        }

        internal static SaveResult Succeeded()
        {
            return new SaveResult(SaveOperationStatus.Succeeded, null);
        }

        internal static SaveResult FromCancellation()
        {
            return new SaveResult(SaveOperationStatus.Canceled, null);
        }

        internal static SaveResult Failed(string error)
        {
            return new SaveResult(SaveOperationStatus.Failed, error);
        }
    }
}
