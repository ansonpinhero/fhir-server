// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Messages.PatientMatch
{
    public sealed class PatientMatchResponse
    {
        public PatientMatchResponse(ResourceElement patient)
        {
            EnsureArg.IsNotNull(patient, nameof(patient));
            Patient = patient;
        }

        public ResourceElement Patient { get; }
    }
}
