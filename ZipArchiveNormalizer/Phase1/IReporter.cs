namespace ZipArchiveNormalizer.Phase1
{
    interface IReporter
    {
        void ReportInformationMessage(string message);
        void ReportWarningMessage(string message);
    }
}
