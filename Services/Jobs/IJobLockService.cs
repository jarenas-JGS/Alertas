namespace Alertas.Services.Jobs
{
    public interface IJobLockService
    {
        Task<bool> IntentarTomarLockAsync(
            string nombreJob,
            string lockedBy,
            TimeSpan duracionLock,
            CancellationToken cancellationToken);

        Task LiberarLockAsync(
            string nombreJob,
            string lockedBy,
            CancellationToken cancellationToken);
    }
}