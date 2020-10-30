using System;
using System.Threading;
using System.Threading.Tasks;

namespace Blockcore.Builder.Feature
{
    public enum FeatureInitializationState
    {
        Uninitialized,
        Initializing,
        Initialized,
        Disposing,
        Disposed
    }

    /// <summary>
    /// Defines methods for features that are managed by the FullNode.
    /// </summary>
    public interface IFullNodeFeature : IDisposable
    {
        /// <summary>
        /// Instructs the <see cref="FullNodeFeatureExecutor"/> to start this feature before the <see cref="Base.BaseFeature"/>.
        /// </summary>
        bool InitializeBeforeBase { get; set; }

        /// <summary>
        /// The state in which the feature currently is.
        /// </summary>
        FeatureInitializationState State { get; set; }

        /// <summary>
        /// Triggered when the FullNode host has fully started.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Validates the feature's required dependencies are all present.
        /// </summary>
        /// <exception cref="MissingDependencyException">should be thrown if dependency is missing</exception>
        /// <param name="services">Services and features registered to node.</param>
        void ValidateDependencies(IFullNodeServiceProvider services);

        bool IsEnabled();

        void WaitInitialized();
    }

    /// <summary>
    /// A feature is used to extend functionality into the full node.
    /// It can manage its life time or use the full node disposable resources.
    /// <para>
    /// If a feature adds an option of a certain functionality to be available to be used by the node
    /// (it may be disabled/enabled by the configuration) the naming convention is
    /// <c>Add[Feature]()</c>. Conversely, when a feature is inclined to be used if included,
    /// the naming convention should be <c>Use[Feature]()</c>.
    /// </para>
    /// </summary>
    public abstract class FullNodeFeature : IFullNodeFeature
    {
        /// <inheritdoc />
        public bool InitializeBeforeBase { get; set; }

        /// <inheritdoc />
        public FeatureInitializationState State { get; set; }

        /// <inheritdoc />
        public abstract Task InitializeAsync();

        /// <inheritdoc />
        public virtual void Dispose()
        {
        }

        /// <inheritdoc />
        public virtual void ValidateDependencies(IFullNodeServiceProvider services)
        {
        }

        public virtual bool IsEnabled()
        {
            return true;
        }

        public void WaitInitialized()
        {
            if (!this.IsEnabled())
                throw new NotSupportedException($"{this.GetType()} is not enabled.");

            // If this feature is awaiting initialization then wait a bit.
            while (this.State == FeatureInitializationState.Uninitialized || this.State == FeatureInitializationState.Initializing)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
