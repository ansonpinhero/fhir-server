﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Client;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Tests.Common;
using Microsoft.Health.Fhir.Tests.Common.FixtureParameters;
using Microsoft.Health.Fhir.Tests.E2E.Common;
using Microsoft.Health.Fhir.Web;
using Microsoft.Health.Test.Utilities;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Tests.E2E.Rest
{
    [Trait(Traits.OwningTeam, OwningTeam.Fhir)]
    [Trait(Traits.Category, Categories.ConditionalOperations)]
    [HttpIntegrationFixtureArgumentSets(DataStore.All, Format.All)]
    public class ConditionalCreateTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly TestFhirClient _client;

        public ConditionalCreateTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.TestFhirClient;
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task GivenAResource_WhenCreatingConditionallyWithNoIdAndNoExisting_TheServerShouldReturnTheResourceSuccessfully()
        {
            var observation = Samples.GetDefaultObservation().ToPoco<Observation>();
            observation.Id = null;

            using FhirResponse<Observation> updateResponse = await _client.CreateAsync(
                observation,
                $"identifier={Guid.NewGuid().ToString()}");

            Assert.Equal(HttpStatusCode.Created, updateResponse.StatusCode);

            Observation updatedResource = updateResponse.Resource;

            Assert.NotNull(updatedResource);
            Assert.NotNull(updatedResource.Id);
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task GivenAResourceWithNoId_WhenCreatingConditionallyWithOneMatch_TheServerShouldReturnOK()
        {
            var observation = Samples.GetDefaultObservation().ToPoco<Observation>();
            var identifier = Guid.NewGuid().ToString();

            observation.Identifier.Add(new Identifier("http://e2etests", identifier));
            using FhirResponse<Observation> response = await _client.CreateAsync(observation);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var observation2 = Samples.GetDefaultObservation().ToPoco<Observation>();

            using FhirResponse<Observation> updateResponse = await _client.CreateAsync(
                observation2,
                $"identifier={identifier}");

            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            Assert.Null(updateResponse.Resource);
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task GivenAResourceAndProvenanceHeader_WhenCreatingConditionallyWithNoIdAndNoExisting_TheServerShouldRespondSuccessfully()
        {
            var observation = Samples.GetDefaultObservation().ToPoco<Observation>();
            observation.Id = null;

            using FhirResponse<Observation> updateResponse = await _client.CreateAsync(
                observation,
                $"identifier={Guid.NewGuid().ToString()}",
                Samples.GetProvenanceHeader());

            Assert.Equal(HttpStatusCode.Created, updateResponse.StatusCode);

            Observation updatedResource = updateResponse.Resource;

            Assert.NotNull(updatedResource);
            Assert.NotNull(updatedResource.Id);

            using var provenanceResponse = await _client.SearchAsync(ResourceType.Provenance, $"target={observation.Id}");
            Assert.Equal(HttpStatusCode.OK, provenanceResponse.StatusCode);
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task GivenAResourceAndMalformedProvenanceHeader_WhenPostingToHttp_TheServerShouldRespondSuccessfully()
        {
            var observation = Samples.GetDefaultObservation().ToPoco<Observation>();
            observation.Id = null;
            var exception = await Assert.ThrowsAsync<FhirClientException>(() => _client.CreateAsync(Samples.GetDefaultObservation().ToPoco<Observation>(), $"identifier={Guid.NewGuid().ToString()}", "Jibberish"));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task GivenAResource_WhenCreatingConditionallyWithMultipleMatches_TheServerShouldFail()
        {
            var observation = Samples.GetDefaultObservation().ToPoco<Observation>();
            var identifier = Guid.NewGuid().ToString();

            observation.Identifier.Add(new Identifier("http://e2etests", identifier));

            using FhirResponse<Observation> response = await _client.CreateAsync(observation);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            using FhirResponse<Observation> response2 = await _client.CreateAsync(observation);
            Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

            var observation2 = Samples.GetDefaultObservation().ToPoco<Observation>();
            observation2.Id = Guid.NewGuid().ToString();

            var exception = await Assert.ThrowsAsync<FhirClientException>(() => _client.CreateAsync(
                observation2,
                $"identifier={identifier}"));

            Assert.Equal(HttpStatusCode.PreconditionFailed, exception.Response.StatusCode);
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task GivenAResource_WhenCreatingConditionallyWithEmptyIfNoneHeader_TheServerShouldFail()
        {
            var exception = await Assert.ThrowsAsync<FhirClientException>(() => _client.CreateAsync(
                Samples.GetDefaultObservation().ToPoco<Observation>(),
                "&"));

            Assert.Equal(HttpStatusCode.BadRequest, exception.Response.StatusCode);
            Assert.Single(exception.OperationOutcome.Issue);
            Assert.Equal(exception.Response.Resource.Issue[0].Diagnostics, string.Format(Core.Resources.ConditionalOperationNotSelectiveEnough, "Observation"));
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task GivenAResource_WhenCreatingConditionallyANewDuplicatedSearchParameterResourceWithSameUrl_TheServerShouldFail()
        {
            /* When the server starts, search-parameters.json files are loaded and the default search parameters
             * are created. The search parameter with the code 'code' and base 'Observation' already exists with
             * the url http://hl7.org/fhir/SearchParameter/clinical-code */

            var resourceToCreate = Samples.GetJsonSample<SearchParameter>("SearchParameterDuplicatedConditionalCreate");
            resourceToCreate.Id = null;
            resourceToCreate.Url = "http://hl7.org/fhir/SearchParameter/clinical-code"; // Same Url than the default one

            // For calling a Conditional Create we do need to send a conditionalCreateCriteria which in this case is the url.
            using FhirClientException ex = await Assert.ThrowsAsync<FhirClientException>(() => _client.CreateAsync(
                resourceToCreate,
                $"url={resourceToCreate.Url}"));

            var expectedSubstring = "A search parameter with the same definition URL 'http://hl7.org/fhir/SearchParameter/clinical-code' already exists.";
            Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
            Assert.Contains(expectedSubstring, ex.Message);
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task GivenAResource_WhenCreatingConditionallyANewDuplicatedSearchParameterResourceWithUrl_TheServerShouldFail()
        {
            /* When the server starts, search-parameters.json files are loaded and the default search parameters
             * are created. The search parameter with the code 'code' and base 'Observation' already exists with
             * the url http://hl7.org/fhir/SearchParameter/clinical-code */

            var resourceToCreate = Samples.GetJsonSample<SearchParameter>("SearchParameterDuplicatedConditionalCreate");
            resourceToCreate.Id = null;
            resourceToCreate.Url = "http://fhir.medlix.org/SearchParameter/code-observation-test-conditional-create-url";

            // For calling a Conditional Create we do need to send a conditionalCreateCriteria which in this case is the url.
            using FhirClientException ex = await Assert.ThrowsAsync<FhirClientException>(() => _client.CreateAsync(
                resourceToCreate,
                $"url={resourceToCreate.Url}"));

            var expectedSubstring = "A search parameter with the same code value 'code' already exists for base type 'Observation'";
            Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
            Assert.Contains(expectedSubstring, ex.Message);

            /* If there is a Search parameter alredy defined with the same url, this test will fail because the ex.Message is different,
             * in that case we should received (example, URL may change):
             * "A search parameter with the same definition URL 'http://fhir.medlix.org/SearchParameter/code-observation-test-conditional-create-url' already exists.
             */
        }

        [Fact]
        [Trait(Traits.Priority, Priority.One)]
        public async Task GivenAResource_WhenCreatingConditionallyANewDuplicatedSearchParameterResourceWithCode_TheServerShouldFail()
        {
            /* When the server starts, search-parameters.json files are loaded and the default search parameters
             * are created. The search parameter with the code 'code' and base 'Observation' already exists with
             * the url http://hl7.org/fhir/SearchParameter/clinical-code */

            var resourceToCreate = Samples.GetJsonSample<SearchParameter>("SearchParameterDuplicatedConditionalCreate");
            resourceToCreate.Id = null;
            resourceToCreate.Url = "http://hl7.org/fhir/SearchParameter/code-observation-test-conditional-create-code";

            // For calling a Conditional Create we do need to send a conditionalCreateCriteria which in this case is the base.
            using FhirClientException ex = await Assert.ThrowsAsync<FhirClientException>(() => _client.CreateAsync(
                resourceToCreate,
                $"code=code"));

            var expectedSubstring = "A search parameter with the same code value 'code' already exists for base type 'Observation'";
            Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
            Assert.Contains(expectedSubstring, ex.Message);

            /* If there is a Search parameter alredy defined with the same url, this test will fail because the ex.Message is different,
             * in that case we should received (example, URL may change):
             * "A search parameter with the same definition URL 'http://fhir.medlix.org/SearchParameter/code-observation-test-conditional-create-code' already exists.
             */
        }
    }
}
