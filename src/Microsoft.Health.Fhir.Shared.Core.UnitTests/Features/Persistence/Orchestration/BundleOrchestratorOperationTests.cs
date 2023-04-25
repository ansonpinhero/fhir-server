﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Persistence.Orchestration;
using Microsoft.Health.Fhir.Tests.Common;
using Microsoft.Health.Test.Utilities;
using NSubstitute;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Shared.Core.UnitTests.Features.Persistence.Orchestration
{
    [Trait(Traits.OwningTeam, OwningTeam.Fhir)]
    [Trait(Traits.Category, Categories.Bundle)]
    [Trait(Traits.Category, Categories.BundleOrchestrator)]
    public class BundleOrchestratorOperationTests
    {
        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(500)]
        public async Task GivenABatchOperation_WhenAppendedMultipleResourcesInSequenceWaitForAllToBeAppended_ThenCompleteWithSuccess(int numberOfResources)
        {
            // When all resources in a bundle are properly appended to the operation sequentially and the operation is committed, then the expected state is 'Completed'.

            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            Func<IScoped<IFhirDataStore>> newDataStoreFunc = () =>
            {
                return Substitute.For<IScoped<IFhirDataStore>>();
            };

            var batchOrchestrator = new BundleOrchestrator(isEnabled: true, createDataStoreFunc: newDataStoreFunc);
            IBundleOrchestratorOperation operation = batchOrchestrator.CreateNewOperation(BundleOrchestratorOperationType.Batch, "PUT", numberOfResources);

            Assert.Equal(BundleOrchestratorOperationStatus.Open, operation.Status);

            // Append resources to an operation.
            Task[] tasksWaitingForMergeAsync = new Task[numberOfResources];
            for (int i = 0; i < numberOfResources; i++)
            {
                DomainResource resource = BundleTestsCommonFunctions.GetSamplePatient(Guid.NewGuid());
                ResourceWrapper resourceWrapper = await BundleTestsCommonFunctions.GetResourceWrapperAsync(resource);

                Task appendedResourceTask = operation.AppendResourceAsync(resourceWrapper, cts.Token);
                tasksWaitingForMergeAsync[i] = appendedResourceTask;

                if (i == (numberOfResources - 1))
                {
                    Task.WaitAll(tasksWaitingForMergeAsync);

                    // After waiting for all tasks, the operation should be completed.
                    Assert.Equal(BundleOrchestratorOperationStatus.Completed, operation.Status);
                }
                else
                {
                    Assert.Equal(BundleOrchestratorOperationStatus.WaitingForResources, operation.Status);
                }
            }

            Assert.Equal(numberOfResources, operation.OriginalExpectedNumberOfResources);
            Assert.Equal(numberOfResources, operation.CurrentExpectedNumberOfResources);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(500)]
        public void GivenABatchOperation_WhenAppendedMultipleResourcesInParallelWaitForAllToBeAppended_ThenCompleteWithSuccess(int numberOfResources)
        {
            // When all resources in a bundle are properly appended to the operation in parallel and the operation is committed, then the expected state is 'Completed'.

            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            Func<IScoped<IFhirDataStore>> newDataStoreFunc = () =>
            {
                return Substitute.For<IScoped<IFhirDataStore>>();
            };
            var batchOrchestrator = new BundleOrchestrator(isEnabled: true, createDataStoreFunc: newDataStoreFunc);

            IBundleOrchestratorOperation operation = batchOrchestrator.CreateNewOperation(BundleOrchestratorOperationType.Batch, "POST", numberOfResources);

            Assert.Equal(BundleOrchestratorOperationStatus.Open, operation.Status);

            // Append resources to an operation.
            List<Task> tasksWaitingForMergeAsync = new List<Task>(capacity: numberOfResources);
            Parallel.For(0, numberOfResources, async (i, task) =>
            {
                DomainResource resource = BundleTestsCommonFunctions.GetSamplePatient(Guid.NewGuid());
                ResourceWrapper resourceWrapper = await BundleTestsCommonFunctions.GetResourceWrapperAsync(resource);

                Task appendedResourceTask = operation.AppendResourceAsync(resourceWrapper, cts.Token);
                tasksWaitingForMergeAsync.Add(appendedResourceTask);
            });

            Task.WaitAll(tasksWaitingForMergeAsync.ToArray());

            Assert.Equal(BundleOrchestratorOperationStatus.Completed, operation.Status);
            Assert.Equal(numberOfResources, operation.OriginalExpectedNumberOfResources);
            Assert.Equal(numberOfResources, operation.CurrentExpectedNumberOfResources);
        }

        [Theory]
        [InlineData(10, BundleOrchestratorOperationType.Batch)]
        [InlineData(100, BundleOrchestratorOperationType.Transaction)]
        [InlineData(500, BundleOrchestratorOperationType.Batch)]
        [InlineData(1000, BundleOrchestratorOperationType.Transaction)]
        public void GivenABatchOperation_WhenAllResourcedAreReleasedInParallel_ThenCancelTheOperation(int numberOfResources, BundleOrchestratorOperationType operationType)
        {
            // When all resources in a bundle are released, then the operation state changes to 'Canceled'.

            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            Func<IScoped<IFhirDataStore>> newDataStoreFunc = () =>
            {
                return Substitute.For<IScoped<IFhirDataStore>>();
            };
            var batchOrchestrator = new BundleOrchestrator(isEnabled: true, createDataStoreFunc: newDataStoreFunc);

            IBundleOrchestratorOperation operation = batchOrchestrator.CreateNewOperation(operationType, "POST", numberOfResources);

            Assert.Equal(BundleOrchestratorOperationStatus.Open, operation.Status);
            Assert.Equal(operationType, operation.Type);

            // Append resources to an operation.
            List<Task> tasksWaitingForMergeAsync = new List<Task>(capacity: numberOfResources);
            Parallel.For(0, numberOfResources, (i, task) =>
            {
                Task releasedResourceTask = operation.ReleaseResourceAsync("Canceled due tests.", cts.Token);
                tasksWaitingForMergeAsync.Add(releasedResourceTask);
            });

            Task.WaitAll(tasksWaitingForMergeAsync.ToArray());

            Assert.Equal(BundleOrchestratorOperationStatus.Canceled, operation.Status);
            Assert.Equal(numberOfResources, operation.OriginalExpectedNumberOfResources);
            Assert.Equal(0, operation.CurrentExpectedNumberOfResources);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(500)]
        public void GivenABatchOperation_WhenHalfOfResourcesAreReleasedInParallel_ThenBatchShouldProcessTheRemainingResources(int numberOfResources)
        {
            // This test validates the logic when half of resources in a bundle are released due an expected behavior, and the other half is expected to be processed.

            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            Func<IScoped<IFhirDataStore>> newDataStoreFunc = () =>
            {
                return Substitute.For<IScoped<IFhirDataStore>>();
            };
            var batchOrchestrator = new BundleOrchestrator(isEnabled: true, createDataStoreFunc: newDataStoreFunc);

            IBundleOrchestratorOperation operation = batchOrchestrator.CreateNewOperation(BundleOrchestratorOperationType.Batch, "POST", numberOfResources);

            Assert.Equal(BundleOrchestratorOperationStatus.Open, operation.Status);

            // Append resources to an operation.
            List<Task> tasksWaitingForMergeAsync = new List<Task>(capacity: numberOfResources);
            Parallel.For(0, numberOfResources, async (i, task) =>
            {
                Task appendTask;
                if (i % 2 == 0)
                {
                    appendTask = operation.ReleaseResourceAsync("Canceled due tests.", cts.Token);
                }
                else
                {
                    DomainResource resource = BundleTestsCommonFunctions.GetSamplePatient(Guid.NewGuid());
                    ResourceWrapper resourceWrapper = await BundleTestsCommonFunctions.GetResourceWrapperAsync(resource);

                    appendTask = operation.AppendResourceAsync(resourceWrapper, cts.Token);
                }

                tasksWaitingForMergeAsync.Add(appendTask);
            });

            Task.WaitAll(tasksWaitingForMergeAsync.ToArray());

            Assert.Equal(BundleOrchestratorOperationStatus.Completed, operation.Status);
            Assert.Equal(numberOfResources, operation.OriginalExpectedNumberOfResources);
            Assert.Equal(numberOfResources / 2, operation.CurrentExpectedNumberOfResources);
        }

        [Fact]
        public async Task GivenABatchOperation_WhenAppendedOnlyOneOutOfAllSupposedResourcesIsAppended_ThenThrowATaskCanceledOperationDueTimeout()
        {
            // This test validated if the CancellationToken is respected and a Bundle Operation fails if waits for too long for all the resources.

            const int numberOfResources = 10;

            // Short cancellation time. This test should fail fast and throw a TaskCanceledException.
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            Func<IScoped<IFhirDataStore>> newDataStoreFunc = () =>
            {
                return Substitute.For<IScoped<IFhirDataStore>>();
            };
            var batchOrchestrator = new BundleOrchestrator(isEnabled: true, createDataStoreFunc: newDataStoreFunc);

            IBundleOrchestratorOperation operation = batchOrchestrator.CreateNewOperation(BundleOrchestratorOperationType.Batch, "POST", expectedNumberOfResources: numberOfResources);

            Assert.Equal(BundleOrchestratorOperationStatus.Open, operation.Status);

            // Append resources to an operation.
            List<Task> tasksWaitingForMergeAsync = new List<Task>(capacity: numberOfResources);

            DomainResource resource = BundleTestsCommonFunctions.GetSamplePatient(Guid.NewGuid());
            ResourceWrapper resourceWrapper = await BundleTestsCommonFunctions.GetResourceWrapperAsync(resource);

            // A single resource will be appended to this operation.
            // In this test, we are forcing the operation to timeout while waiting for the remain resources.
            tasksWaitingForMergeAsync.Add(operation.AppendResourceAsync(resourceWrapper, cts.Token));

            AggregateException age = Assert.Throws<AggregateException>(() => Task.WaitAll(tasksWaitingForMergeAsync.ToArray()));

            Assert.True(age.InnerException is TaskCanceledException);
            Assert.Equal(BundleOrchestratorOperationStatus.Canceled, operation.Status);
            Assert.Equal(numberOfResources, operation.OriginalExpectedNumberOfResources);
            Assert.Equal(numberOfResources, operation.CurrentExpectedNumberOfResources);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(500)]
        [InlineData(1000)]
        public void GivenMultipleBatchOperations_WhenAskedForNexOperations_ThenNewDataStoresWillBeProvided(int numberOfBundles)
        {
            // In this test, every call to create a new Bundle Operation should generate a new call to retrieve a data store.
            // The motivation for this test is to ensure that every operation will be executed in a isolated data store.

            const int numberOfResourcesPerBundle = 10;

            // Short cancellation time. This test should fail fast and throw a TaskCanceledException.
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            int numberOfCallsToCreateANewDataStore = 0;
            Func<IScoped<IFhirDataStore>> newDataStoreFunc = () =>
            {
                Interlocked.Increment(ref numberOfCallsToCreateANewDataStore);

                return Substitute.For<IScoped<IFhirDataStore>>();
            };
            var batchOrchestrator = new BundleOrchestrator(isEnabled: true, createDataStoreFunc: newDataStoreFunc);

            Parallel.For(0, numberOfBundles, (index) =>
            {
                IBundleOrchestratorOperation operation = batchOrchestrator.CreateNewOperation(BundleOrchestratorOperationType.Batch, "POST", expectedNumberOfResources: numberOfResourcesPerBundle);
                Assert.Equal(BundleOrchestratorOperationStatus.Open, operation.Status);

                batchOrchestrator.CompleteOperation(operation);
            });

            Assert.Equal(numberOfBundles, numberOfCallsToCreateANewDataStore);
        }
    }
}