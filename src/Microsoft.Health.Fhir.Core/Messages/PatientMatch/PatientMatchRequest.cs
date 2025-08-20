// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Messages.PatientMatch
{
    public sealed class PatientMatchRequest : IRequest<PatientMatchResponse>, IRequest
    {
        public PatientMatchRequest(ResourceElement patient)
        {
            Patient = patient;
        }

        public ResourceElement Patient { get; }
    }
}
