namespace Utility
{
    public interface IWorkerCancellable
    {
        string Usage { get; }
        void Cancel();
        bool IsRequestToCancel {get; }
        void ResetCancellationStatus();
        void CheckCancellatio();
    }
}
