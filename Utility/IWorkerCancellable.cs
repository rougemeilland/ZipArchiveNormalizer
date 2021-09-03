namespace Utility
{
    public interface IWorkerCancellable
    {
        string Usage { get; }
        bool IsRequestToCancel {get; }
        void ResetCancellationStatus();
        void CheckCancellatio();
    }
}
