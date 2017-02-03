﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Config;
using FFImageLoading.Helpers;

namespace FFImageLoading.Work
{
    public class WorkScheduler : IWorkScheduler
    {
        protected readonly object _pendingTasksLock = new object();
        private int _currentPosition; // useful?
        private Task _dispatch = Task.FromResult<byte>(1);
        private long _loadCount;
        private int _statsTotalMemoryCacheHits;
        private int _statsTotalPending;
        private int _statsTotalRunning;
        private int _statsTotalWaiting;

        public WorkScheduler(Configuration configuration, IPlatformPerformance performance)
        {
            Configuration = configuration;
            Performance = performance;
            PendingTasks = new List<PendingTask>();
            RunningTasks = new Dictionary<string, PendingTask>();
        }

        protected int MaxParallelTasks
        {
            get
            {
                if (Configuration.SchedulerMaxParallelTasksFactory != null)
                    return Configuration.SchedulerMaxParallelTasksFactory(Configuration);

                return Configuration.SchedulerMaxParallelTasks;
            }
        }

        protected IPlatformPerformance Performance { get; }
        protected List<PendingTask> PendingTasks { get; }
        protected Dictionary<string, PendingTask> RunningTasks { get; }

        protected Configuration Configuration { get; }

        protected IMiniLogger Logger
        {
            get { return Configuration.Logger; }
        }

        public virtual void Cancel(IImageLoaderTask task)
        {
            try
            {
                if ((task != null) && !task.IsCancelled && !task.IsCompleted)
                    task.Cancel();
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("Cancelling task failed: {0}", task?.Key), e);
            }
            finally
            {
                task?.Dispose();
                EvictStaleTasks();
            }
        }

        public virtual void Cancel(Func<IImageLoaderTask, bool> predicate)
        {
            lock (_pendingTasksLock)
            {
                foreach (var task in PendingTasks.Where(p => predicate(p.ImageLoadingTask)).ToList())
                    // FMT: here we need a copy since cancelling will trigger them to be removed, hence collection is modified during enumeration
                    task.ImageLoadingTask.Cancel();
            }
        }

        public bool ExitTasksEarly { get; private set; }

        public void SetExitTasksEarly(bool exitTasksEarly)
        {
            if (ExitTasksEarly == exitTasksEarly)
                return;

            ExitTasksEarly = exitTasksEarly;

            if (exitTasksEarly)
            {
                Logger.Debug("ExitTasksEarly enabled.");

                lock (_pendingTasksLock)
                {
                    foreach (var task in PendingTasks.ToList())
                        // FMT: here we need a copy since cancelling will trigger them to be removed, hence collection is modified during enumeration
                        task.ImageLoadingTask.Cancel();

                    PendingTasks.Clear();
                }
            }
            else
            {
                Logger.Debug("ExitTasksEarly disabled.");
            }
        }

        public bool PauseWork { get; private set; }

        public void SetPauseWork(bool pauseWork)
        {
            if (PauseWork == pauseWork)
                return;

            PauseWork = pauseWork;

            if (pauseWork)
            {
                Logger.Debug("SetPauseWork enabled.");
            }
            else
            {
                Logger.Debug("SetPauseWork disabled.");
                TakeFromPendingTasksAndRun();
            }
        }

        public virtual void RemovePendingTask(IImageLoaderTask task)
        {
            lock (_pendingTasksLock)
            {
                PendingTasks.RemoveAll(p => p.ImageLoadingTask == task);
            }
        }

        public virtual async void LoadImage(IImageLoaderTask task)
        {
            Interlocked.Increment(ref _loadCount);

            EvictStaleTasks();

            if (Configuration.VerbosePerformanceLogging && (_loadCount%10 == 0))
                LogSchedulerStats();

            if ((task?.Parameters?.Source != ImageSource.Stream) && string.IsNullOrWhiteSpace(task?.Parameters?.Path))
            {
                Logger.Error("ImageService: null path ignored");
                task?.Dispose();
                return;
            }

            if (task == null)
                return;

            if (task.IsCancelled || task.IsCompleted || ExitTasksEarly)
            {
                task?.Dispose();
                return;
            }

            if ((task.Parameters.DelayInMs != null) && (task.Parameters.DelayInMs > 0))
                await Task.Delay(task.Parameters.DelayInMs.Value).ConfigureAwait(false);

            // If we have the image in memory then it's pointless to schedule the job: just display it straight away
            if (task.CanUseMemoryCache)
                if (await task.TryLoadFromMemoryCacheAsync().ConfigureAwait(false))
                {
                    Interlocked.Increment(ref _statsTotalMemoryCacheHits);
                    task?.Dispose();
                    return;
                }

            _dispatch = _dispatch.ContinueWith(t =>
            {
                try
                {
                    QueueImageLoadingTask(task);
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Image loaded failed: {0}", task?.Key), ex);
                }
            });
        }

        protected virtual void EvictStaleTasks()
        {
            lock (_pendingTasksLock)
            {
                var toRemove = PendingTasks.Where(v => (v.FrameworkWrappingTask == null) || (v.ImageLoadingTask == null)
                                                       || v.ImageLoadingTask.IsCancelled ||
                                                       v.ImageLoadingTask.IsCompleted)
                    .ToList();

                foreach (var task in toRemove)
                {
                    task?.ImageLoadingTask?.Dispose();
                    PendingTasks.Remove(task);
                }
            }
        }

        protected void QueueImageLoadingTask(IImageLoaderTask task)
        {
            var position = Interlocked.Increment(ref _currentPosition);
            var currentPendingTask = new PendingTask
            {
                Position = position,
                ImageLoadingTask = task,
                FrameworkWrappingTask = CreateFrameworkTask(task)
            };

            if (task.IsCancelled || task.IsCompleted || ExitTasksEarly)
            {
                task?.Dispose();
                return;
            }

            PendingTask similarRunningTask = null;
            lock (_pendingTasksLock)
            {
                if (!task.Parameters.Preload)
                {
                    foreach (var pendingTask in PendingTasks.ToList())
                        // FMT: here we need a copy since cancelling will trigger them to be removed, hence collection is modified during enumeration
                        if ((pendingTask.ImageLoadingTask != null) &&
                            pendingTask.ImageLoadingTask.UsesSameNativeControl(task))
                            pendingTask.ImageLoadingTask.CancelIfNeeded();

                    EvictStaleTasks();
                }

                similarRunningTask = PendingTasks.FirstOrDefault(t => t.ImageLoadingTask.Key == task.Key);
                if (similarRunningTask == null)
                {
                    Interlocked.Increment(ref _statsTotalPending);
                    PendingTasks.Add(currentPendingTask);
                }
                else if (similarRunningTask.ImageLoadingTask != null)
                {
                    if (task.Parameters.Priority.HasValue &&
                        (!similarRunningTask.ImageLoadingTask.Parameters.Priority.HasValue
                         ||
                         (task.Parameters.Priority.Value > similarRunningTask.ImageLoadingTask.Parameters.Priority.Value)))
                        similarRunningTask.ImageLoadingTask.Parameters.WithPriority(task.Parameters.Priority.Value);

                    if (task.Parameters.OnDownloadProgress != null)
                    {
                        var similarTaskOnDownloadProgress =
                            similarRunningTask.ImageLoadingTask.Parameters.OnDownloadProgress;

                        similarRunningTask.ImageLoadingTask.Parameters.DownloadProgress(obj =>
                        {
                            similarTaskOnDownloadProgress?.Invoke(obj);
                            task.Parameters.OnDownloadProgress(obj);
                        });
                    }
                }
            }

            if (PauseWork)
                return;

            if ((similarRunningTask == null) || !currentPendingTask.ImageLoadingTask.CanUseMemoryCache)
                TakeFromPendingTasksAndRun();
            else
                WaitForSimilarTaskFinished(currentPendingTask, similarRunningTask);
        }

        protected async void WaitForSimilarTaskFinished(PendingTask currentPendingTask, PendingTask taskForSimilarKey)
        {
            Interlocked.Increment(ref _statsTotalWaiting);

            if ((taskForSimilarKey?.FrameworkWrappingTask == null)
                || taskForSimilarKey.FrameworkWrappingTask.IsCanceled
                || taskForSimilarKey.FrameworkWrappingTask.IsFaulted)
            {
                lock (_pendingTasksLock)
                {
                    Interlocked.Increment(ref _statsTotalPending);
                    PendingTasks.Add(currentPendingTask);
                }

                TakeFromPendingTasksAndRun();
                return;
            }

            Logger.Debug(string.Format("Wait for similar request for key: {0}", taskForSimilarKey.ImageLoadingTask.Key));
            await taskForSimilarKey.FrameworkWrappingTask.ConfigureAwait(false);

            if ((currentPendingTask?.ImageLoadingTask == null) || currentPendingTask.ImageLoadingTask.IsCancelled)
                return;

            // Now our image should be in the cache
            var cacheFound =
                await currentPendingTask.ImageLoadingTask.TryLoadFromMemoryCacheAsync().ConfigureAwait(false);

            if (!cacheFound)
            {
                if ((currentPendingTask?.ImageLoadingTask == null) || currentPendingTask.ImageLoadingTask.IsCancelled)
                    return;

                lock (_pendingTasksLock)
                {
                    Interlocked.Increment(ref _statsTotalPending);
                    PendingTasks.Add(currentPendingTask);
                }

                TakeFromPendingTasksAndRun();
            }
            else
            {
                currentPendingTask?.ImageLoadingTask?.Dispose();
            }
        }

        protected void TakeFromPendingTasksAndRun()
        {
            //Task.Factory.StartNew(async () =>
            //{
            //    await TakeFromPendingTasksAndRunAsync().ConfigureAwait(false); // FMT: we limit concurrent work using MaxParallelTasks
            //}, TaskCreationOptions.LongRunning).ConfigureAwait(false);
            //Task.Factory.StartNew(async () =>
            //{
            //    await TakeFromPendingTasksAndRunAsync().ConfigureAwait(false); // FMT: we limit concurrent work using MaxParallelTasks
            //}, TaskCreationOptions.HideScheduler).ConfigureAwait(false);
            Task.Factory.StartNew(
                async () =>
                {
                    await TakeFromPendingTasksAndRunAsync().ConfigureAwait(false);
                        // FMT: we limit concurrent work using MaxParallelTasks
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler,
                TaskScheduler.Default).ConfigureAwait(false);
        }

        protected Task CreateFrameworkTask(IImageLoaderTask imageLoadingTask)
        {
            var parameters = imageLoadingTask.Parameters;

            var tcs = new TaskCompletionSource<bool>();

            var successCallback = parameters.OnSuccess;
            parameters.Success((size, result) =>
            {
                tcs.TrySetResult(true);
                successCallback?.Invoke(size, result);
            });

            var finishCallback = parameters.OnFinish;
            parameters.Finish(sw =>
            {
                tcs.TrySetResult(false);
                finishCallback?.Invoke(sw);
            });

            return tcs.Task;
        }

        protected int GetDefaultPriority(ImageSource source)
        {
            if ((source == ImageSource.ApplicationBundle) || (source == ImageSource.CompiledResource))
                return (int) LoadingPriority.Normal + 2;

            if (source == ImageSource.Filepath)
                return (int) LoadingPriority.Normal + 1;

            return (int) LoadingPriority.Normal;
        }

        protected async Task TakeFromPendingTasksAndRunAsync()
        {
            Dictionary<string, PendingTask> tasksToRun = null;

            lock (_pendingTasksLock)
            {
                var preloadOrUrlTasksCount = 0;
                var urlTasksCount = 0;
                var preloadTasksCount = 0;

                if (RunningTasks.Count >= MaxParallelTasks)
                {
                    urlTasksCount =
                        RunningTasks.Count(
                            v =>
                                (v.Value?.ImageLoadingTask != null) && !v.Value.ImageLoadingTask.Parameters.Preload &&
                                (v.Value.ImageLoadingTask.Parameters.Source == ImageSource.Url));

                    preloadTasksCount = RunningTasks.Count(v => (v.Value?.ImageLoadingTask != null)
                                                                && v.Value.ImageLoadingTask.Parameters.Preload);
                    preloadOrUrlTasksCount = preloadTasksCount + urlTasksCount;

                    if ((preloadOrUrlTasksCount == 0) || (preloadOrUrlTasksCount != MaxParallelTasks))
                        return;

                    // Allow only half of MaxParallelTasks as additional allowed tasks when preloading occurs to prevent starvation
                    if (RunningTasks.Count - Math.Max(1, Math.Min(preloadOrUrlTasksCount, MaxParallelTasks/2)) >=
                        MaxParallelTasks)
                        return;
                }

                var numberOfTasks = MaxParallelTasks - RunningTasks.Count +
                                    Math.Min(preloadOrUrlTasksCount, MaxParallelTasks/2);
                tasksToRun = new Dictionary<string, PendingTask>();

                foreach (
                    var task in
                    PendingTasks.Where(t => !t.ImageLoadingTask.IsCancelled && !t.ImageLoadingTask.IsCompleted)
                        .OrderByDescending(
                            t =>
                                t.ImageLoadingTask.Parameters.Priority ??
                                GetDefaultPriority(t.ImageLoadingTask.Parameters.Source))
                        .ThenBy(t => t.Position))
                {
                    // We don't want to load, at the same time, images that have same key or same raw key at the same time
                    // This way we prevent concurrent downloads and benefit from caches

                    var rawKey = task.ImageLoadingTask.KeyRaw;
                    if (RunningTasks.ContainsKey(rawKey) || tasksToRun.ContainsKey(rawKey))
                        continue;

                    if (preloadOrUrlTasksCount != 0)
                    {
                        if (!task.ImageLoadingTask.Parameters.Preload &&
                            ((urlTasksCount == 0) || (task.ImageLoadingTask.Parameters.Source != ImageSource.Url)))
                            tasksToRun.Add(rawKey, task);
                    }
                    else
                    {
                        tasksToRun.Add(rawKey, task);
                    }

                    if (tasksToRun.Count == numberOfTasks)
                        break;
                }
            }

            if ((tasksToRun != null) && (tasksToRun.Count > 0))
                if (tasksToRun.Count == 1)
                {
                    await RunImageLoadingTaskAsync(tasksToRun.Values.First()).ConfigureAwait(false);
                }
                else
                {
                    var tasks = tasksToRun.Select(p => RunImageLoadingTaskAsync(p.Value));
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
        }

        protected async Task RunImageLoadingTaskAsync(PendingTask pendingTask)
        {
            var key = pendingTask.ImageLoadingTask.Key;

            lock (_pendingTasksLock)
            {
                if (RunningTasks.ContainsKey(key))
                    return;

                RunningTasks.Add(key, pendingTask);
                Interlocked.Increment(ref _statsTotalRunning);
            }

            try
            {
                if (Configuration.VerbosePerformanceLogging)
                {
                    LogSchedulerStats();
                    var stopwatch = Stopwatch.StartNew();

                    await Task.Run(pendingTask.ImageLoadingTask.RunAsync).ConfigureAwait(false);

                    stopwatch.Stop();

                    Logger.Debug(
                        string.Format(
                            "[PERFORMANCE] RunAsync - NetManagedThreadId: {0}, NativeThreadId: {1}, Execution: {2} ms, Key: {3}",
                            Performance.GetCurrentManagedThreadId(),
                            Performance.GetCurrentSystemThreadId(),
                            stopwatch.Elapsed.Milliseconds,
                            key));
                }
                else
                {
                    await Task.Run(pendingTask.ImageLoadingTask.RunAsync).ConfigureAwait(false);
                }
            }
            finally
            {
                lock (_pendingTasksLock)
                {
                    RunningTasks.Remove(key);
                }
                pendingTask?.ImageLoadingTask?.Dispose();

                await TakeFromPendingTasksAndRunAsync().ConfigureAwait(false);
            }
        }

        protected void LogSchedulerStats()
        {
            Logger.Debug(
                string.Format(
                    "[PERFORMANCE] Scheduler - Max: {0}, Pending: {1}, Running: {2}, TotalPending: {3}, TotalRunning: {4}, TotalMemoryCacheHit: {5}, TotalWaiting: {6}",
                    MaxParallelTasks,
                    PendingTasks.Count,
                    RunningTasks.Count,
                    _statsTotalPending,
                    _statsTotalRunning,
                    _statsTotalMemoryCacheHits,
                    _statsTotalWaiting));

            Logger.Debug(Performance.GetMemoryInfo());
        }

        protected class PendingTask
        {
            public int Position { get; set; }

            public IImageLoaderTask ImageLoadingTask { get; set; }

            public Task FrameworkWrappingTask { get; set; }
        }
    }
}