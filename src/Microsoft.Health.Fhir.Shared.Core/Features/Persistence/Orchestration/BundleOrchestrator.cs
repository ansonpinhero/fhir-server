﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using EnsureThat;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Fhir.Core.Features.Persistence.Orchestration
{
    public sealed class BundleOrchestrator : IBundleOrchestrator
    {
        /// <summary>
        /// Dictionary of current operations. At the end of an operation, it should be removed from this dictionary.
        /// Operations are indexed by their respective IDs.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, IBundleOrchestratorOperation> _operationsById;

        private readonly Func<IScoped<IFhirDataStore>> _createDataStoreFunc;

        /// <summary>
        /// Creates a new instance of <see cref="BundleOrchestrator"/>.
        /// </summary>
        /// <param name="isEnabled">Enables or disables the Bundle Orchestrator functionality.</param>
        /// <param name="createDataStoreFunc">Function creating a new instances of the data store.</param>
        public BundleOrchestrator(bool isEnabled, Func<IScoped<IFhirDataStore>> createDataStoreFunc)
        {
            EnsureArg.IsNotNull(createDataStoreFunc, nameof(createDataStoreFunc));

            _createDataStoreFunc = createDataStoreFunc;
            _operationsById = new ConcurrentDictionary<Guid, IBundleOrchestratorOperation>();

            IsEnabled = isEnabled;
        }

        public bool IsEnabled { get; }

        public IBundleOrchestratorOperation CreateNewOperation(BundleOrchestratorOperationType type, string label, int expectedNumberOfResources)
        {
            EnsureArg.IsNotNullOrWhiteSpace(label, nameof(label));
            EnsureArg.IsGt(expectedNumberOfResources, 0, nameof(expectedNumberOfResources));

            // Every bundle operation requires a new instance of the data store.
            IScoped<IFhirDataStore> dataStore = _createDataStoreFunc();
            BundleOrchestratorOperation newOperation = new BundleOrchestratorOperation(type, label, expectedNumberOfResources, dataStore: dataStore.Value);

            if (!_operationsById.TryAdd(newOperation.Id, newOperation))
            {
                throw new BundleOrchestratorException($"An operation with ID '{newOperation.Id}' was already added to the queue.");
            }

            return newOperation;
        }

        public bool CompleteOperation(IBundleOrchestratorOperation operation)
        {
            EnsureArg.IsNotNull(operation, nameof(operation));

            if (!_operationsById.TryRemove(operation.Id, out IBundleOrchestratorOperation job))
            {
                throw new BundleOrchestratorException($"A job with ID '{operation.Id}' was not found or unable to be completed.");
            }

            return true;
        }
    }
}