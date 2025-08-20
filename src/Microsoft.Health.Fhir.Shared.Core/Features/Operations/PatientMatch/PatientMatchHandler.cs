// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Fhir.Core.Features.Security;
using Microsoft.Health.Fhir.Core.Messages.PatientMatch;

namespace Microsoft.Health.Fhir.Core.Features.Operations.PatientMatch
{
    public sealed class PatientMatchHandler : IRequestHandler<PatientMatchRequest, PatientMatchResponse>
    {
        public PatientMatchHandler()
        {
        }

        public Task<PatientMatchResponse> Handle(PatientMatchRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            return Task.FromResult(new PatientMatchResponse(request.Patient));
        }
    }
}
