using System;
using System.Collections.Generic;
using System.Linq;
using Cloo;
using Microsoft.Extensions.Logging;

namespace Blockcore.Networks.X1.Components
{
    /// <summary>
    /// The famous SpartaCrypt OpenCLMiner, visit the original here:
    /// https://github.com/spartacrypt/xds-1/blob/master/src/components/Fullnode/UnnamedCoin.Bitcoin.Features.Miner/OpenCLMiner.cs
    /// </summary>
    public class OpenCLMiner : IDisposable
    {
        private const string KernelFunction = "kernel_find_pow";

        private readonly ILogger logger;
        private readonly ComputeDevice computeDevice;

        private List<ComputeKernel> computeKernels = new List<ComputeKernel>();
        private ComputeProgram computeProgram;
        private ComputeContext computeContext;
        private ComputeKernel computeKernel;
        private string[] openCLSources;
        private bool isDisposed;

        /// <summary>
        /// Create a new OpenCLMiner instance.
        /// </summary>
        /// <param name="minerSettings">the minerSettings</param>
        /// <param name="loggerFactory">the loggerFactory</param>
        public OpenCLMiner(X1MinerSettings minerSettings, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            var devices = ComputePlatform.Platforms.SelectMany(p => p.Devices).Where(d => d.Available && d.CompilerAvailable).ToList();

            if (!devices.Any())
            {
                this.logger.LogWarning($"No OpenCL Devices Found!");
            }
            else
            {
                foreach (ComputeDevice device in devices)
                {
                    this.logger.LogInformation($"Found OpenCL Device: Name={device.Name}, MaxClockFrequency{device.MaxClockFrequency}");
                }

                this.computeDevice = devices.FirstOrDefault(d => d.Name.Equals(minerSettings.OpenCLDevice, StringComparison.OrdinalIgnoreCase)) ?? devices.FirstOrDefault();
                if (this.computeDevice != null)
                {
                    this.logger.LogInformation($"Using OpenCL Device: Name={this.computeDevice.Name}");
                }
            }
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~OpenCLMiner()
        {
            this.DisposeOpenCLResources();
        }

        /// <summary>
        /// If a compute device for mining is available.
        /// </summary>
        /// <returns>true if a usable device is found, otherwise false</returns>
        public bool CanMine()
        {
            return this.computeDevice != null;
        }

        /// <summary>
        /// Gets the currently used device name.
        /// </summary>
        /// <returns></returns>
        public string GetDeviceName()
        {
            if (this.computeDevice == null)
            {
                throw new InvalidOperationException("GPU not found");
            }

            return this.computeDevice.Name;
        }

        /// <summary>
        /// Finds the nonce for a block header hash that meets the given target.
        /// </summary>
        /// <param name="header">serialized block header</param>
        /// <param name="bits">the target</param>
        /// <param name="nonceStart">the first nonce value to try</param>
        /// <param name="iterations">the number of iterations</param>
        /// <returns></returns>
        public uint FindPow(byte[] header, byte[] bits, uint nonceStart, uint iterations)
        {
            if (this.computeDevice == null)
            {
                throw new InvalidOperationException("GPU not found");
            }

            this.ConstructOpenCLResources();

            using var headerBuffer = new ComputeBuffer<byte>(this.computeContext, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, header);
            using var bitsBuffer = new ComputeBuffer<byte>(this.computeContext, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, bits);
            using var powBuffer = new ComputeBuffer<uint>(this.computeContext, ComputeMemoryFlags.WriteOnly, 1);

            this.computeKernel.SetMemoryArgument(0, headerBuffer);
            this.computeKernel.SetMemoryArgument(1, bitsBuffer);
            this.computeKernel.SetValueArgument(2, nonceStart);
            this.computeKernel.SetMemoryArgument(3, powBuffer);

            using var commands = new ComputeCommandQueue(this.computeContext, this.computeDevice, ComputeCommandQueueFlags.None);
            commands.Execute(this.computeKernel, null, new long[] { iterations }, null, null);

            var nonceOut = new uint[1];
            commands.ReadFromBuffer(powBuffer, ref nonceOut, true, null);
            commands.Finish();

            this.DisposeOpenCLResources();

            return nonceOut[0];
        }

        private void ConstructOpenCLResources()
        {
            if (this.computeDevice != null)
            {
                if (this.openCLSources == null)
                {
                    GetOpenCLSources();
                }
                var properties = new ComputeContextPropertyList(this.computeDevice.Platform);
                this.computeContext = new ComputeContext(new[] { this.computeDevice }, properties, null, IntPtr.Zero);
                this.computeProgram = new ComputeProgram(this.computeContext, this.openCLSources);
                this.computeProgram.Build(new[] { this.computeDevice }, null, null, IntPtr.Zero);
                this.computeKernels = this.computeProgram.CreateAllKernels().ToList();
                this.computeKernel = this.computeKernels.First((k) => k.FunctionName == KernelFunction);
            }
        }

        private void GetOpenCLSources()
        {
            this.openCLSources = new[]
            {
                Properties.Resources.SpartacryptOpenCLMiner_opencl_device_info_h,
                Properties.Resources.SpartacryptOpenCLMiner_opencl_misc_h,
                Properties.Resources.SpartacryptOpenCLMiner_opencl_sha2_common_h,
                Properties.Resources.SpartacryptOpenCLMiner_opencl_sha512_h,
                Properties.Resources.SpartacryptOpenCLMiner_sha512_miner_cl
            };
        }

        private void DisposeOpenCLResources()
        {
            this.computeKernels.ForEach(k => k.Dispose());
            this.computeKernels.Clear();
            this.computeProgram?.Dispose();
            this.computeProgram = null;
            this.computeContext?.Dispose();
            this.computeContext = null;
        }

        /// <summary>
        /// Releases the OpenCL resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (this.isDisposed)
                return;

            if (disposing)
            {
                DisposeOpenCLResources();
            }

            this.isDisposed = true;
        }
    }
}
