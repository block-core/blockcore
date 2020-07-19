﻿using System.Collections.Generic;
using x42.Features.xServer.Models;

namespace x42.Features.xServer.Interfaces
{
    /// <summary>
    ///     Interface for a manager providing operations for xServers.
    /// </summary>
    public interface IxServerManager
    {
        /// <summary>
        ///     Starts any processes for the xServer manager.
        /// </summary>
        void Start();

        /// <summary>
        ///     Runs any steps nessesary to stop the xServer manager.
        /// </summary>
        void Stop();

        /// <summary>
        ///     A count of connected xServer seeds.
        /// </summary>
        List<xServerPeer> ConnectedSeeds { get; }

        /// <summary>
        ///     Registers the xServer and returns the result.
        /// </summary>
        RegisterResult RegisterXServer(RegisterRequest registerRequest);

        /// <summary>
        ///     Tests the xServer Ports, and returns the result
        /// </summary>
        TestResult TestXServerPorts(TestRequest testRequest);

        /// <summary>
        ///     Get available price lock pairs
        /// </summary>
        List<PairResult> GetAvailablePairs();

        /// <summary>
        ///     Create a price lock.
        /// </summary>
        PriceLockResult CreatePriceLock(CreatePriceLockRequest priceLockRequest);

        /// <summary>
        ///     Get a price lock.
        /// </summary>
        PriceLockResult GetPriceLock(string priceLockId);

        /// <summary>
        ///     Submit the payment for a price lock.
        /// </summary>
        SubmitPaymentResult SubmitPayment(SubmitPaymentRequest submitPaymentRequest);
    }
}