using System;
using System.Threading;
using System.Threading.Tasks;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;

namespace Blockcore.AsyncWork
{
    /// <summary>
    /// Allows running application defined in a loop with specific timing.
    /// <para>
    /// It is possible to specify a startup delay, which will cause the first execution of the task to be delayed.
    /// It is also possible to specify a delay between two executions of the task. And finally, it is possible
    /// to make the task run only once. Running the task for other than one or infinite number of times is not supported.
    /// </para>
    /// </summary>
    public sealed class AsyncLoop : IAsyncLoop
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        /// Application defined task that will be called and awaited in the async loop.
        /// The task is given a cancellation token that allows it to recognize that the caller wishes to cancel it.
        /// </summary>
        private readonly Func<CancellationToken, Task> loopAsync;

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public Task RunningTask { get; private set; }

        /// <inheritdoc />
        public TimeSpan RepeatEvery { get; set; }

        /// <summary>
        /// Gets the uncaught exception, if available.
        /// </summary>
        /// <value>
        /// The uncaught exception.
        /// </value>
        internal Exception UncaughtException { get; private set; }

        /// <summary>
        /// Initializes a named instance of the object.
        /// </summary>
        /// <param name="name">Name of the loop.</param>
        /// <param name="logger">Logger for the new instance.</param>
        /// <param name="loop">Application defined task that will be called and awaited in the async loop.</param>
        public AsyncLoop(string name, ILogger logger, Func<CancellationToken, Task> loop)
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotNull(loop, nameof(loop));

            this.Name = name;
            this.logger = logger;
            this.loopAsync = loop;
            this.RepeatEvery = TimeSpan.FromMilliseconds(1000);
        }

        /// <inheritdoc />
        public IAsyncLoop Run(TimeSpan? repeatEvery = null, TimeSpan? startAfter = null)
        {
            return this.Run(CancellationToken.None, repeatEvery, startAfter);
        }

        /// <inheritdoc />
        public IAsyncLoop Run(CancellationToken cancellation, TimeSpan? repeatEvery = null, TimeSpan? startAfter = null)
        {
            Guard.NotNull(cancellation, nameof(cancellation));

            if (repeatEvery != null)
                this.RepeatEvery = repeatEvery.Value;

            this.RunningTask = Task.Run(async () => await this.StartAsync(cancellation, startAfter), cancellation);

            return this;
        }

        /// <summary>
        /// Starts an application defined task inside the async loop.
        /// </summary>
        /// <param name="cancellation">Cancellation token that triggers when the task and the loop should be cancelled.</param>
        /// <param name="delayStart">Delay before the first run of the task, or null if no startup delay is required.</param>
        private async Task StartAsync(CancellationToken cancellation, TimeSpan? delayStart = null)
        { 
            try
            {
                if (cancellation.IsCancellationRequested) return;

                if (delayStart != null)
                {
                    this.logger.LogInformation("{name} starting in {value} seconds.", this.Name, delayStart.Value.TotalSeconds);
                    await Task.Delay(delayStart.Value, cancellation).ConfigureAwait(false);
                }

                this.logger.LogInformation("{name} starting.", this.Name);

                if (this.RepeatEvery == TimeSpans.RunOnce)
                {
                    await this.loopAsync(cancellation).ConfigureAwait(false);
                    return;
                }

                while (!cancellation.IsCancellationRequested)
                {
                    await this.loopAsync(cancellation).ConfigureAwait(false);
                    if (!cancellation.IsCancellationRequested)
                        await Task.Delay(this.RepeatEvery, cancellation).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                if (!cancellation.IsCancellationRequested)
                {
                    this.logger.LogCritical(new EventId(0), ex, "{logName} threw an unhandled exception", this.Name);
                    this.logger.LogError("{logName} threw an unhandled exception: {logUncaughtException}", this.Name, ex.ToString());
                }
            }
            catch (Exception ex)
            {
                this.logger.LogCritical(new EventId(0), ex, "{logName} threw an unhandled exception", this.Name);
                this.logger.LogError("{0} threw an unhandled exception: {1}", this.Name, ex.ToString());
            }
            finally
            {
                this.logger.LogInformation("{logName} stopping.", this.Name);
            }
        }

        /// <summary>
        /// Wait for the loop task to complete.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (!this.RunningTask.IsCanceled)
            {
                try
                {
                    this.logger.LogInformation("Waiting for {name} to finish or be cancelled.", this.Name);
                    this.RunningTask.Wait();
                }
                catch (TaskCanceledException)
                {
                    this.logger.LogInformation("{name} cancelled.", this.Name);
                }
            }
        }
    }
}
