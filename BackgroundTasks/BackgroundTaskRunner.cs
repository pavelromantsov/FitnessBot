using System.Collections.Concurrent;

namespace FitnessBot.BackgroundTasks
{
    public class BackgroundTaskRunner : IDisposable
    {
        private readonly ConcurrentBag<IBackgroundTask> _tasks = new();
        private Task? _runningTasks;
        private CancellationTokenSource? _stoppingCts;

        public void AddTask(IBackgroundTask task)
        {
            if (_runningTasks is not null)
                throw new InvalidOperationException("Задачи уже запущены");

            _tasks.Add(task);
        }

        public void StartTasks(CancellationToken ct)
        {
            if (_runningTasks is not null)
                throw new InvalidOperationException("Задачи уже запущены");

            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            static async Task RunSafe(IBackgroundTask task, CancellationToken ct)
            {
                try
                {
                    await task.Start(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // нормально завершаемся при отмене
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Ошибка в {task.GetType().Name}: {ex}");
                }
            }

            _runningTasks = Task.WhenAll(_tasks.Select(t => RunSafe(t, _stoppingCts.Token)));
        }

        public async Task StopTasks(CancellationToken ct)
        {
            if (_runningTasks is null)
                return;

            try
            {
                _stoppingCts?.Cancel();
            }
            finally
            {
                await _runningTasks.WaitAsync(ct).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }

        public void Dispose()
        {
            _stoppingCts?.Cancel();
            _stoppingCts?.Dispose();
        }
    }
}
