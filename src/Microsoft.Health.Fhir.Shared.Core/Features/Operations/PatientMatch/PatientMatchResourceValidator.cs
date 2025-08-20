// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Context;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features.Context;
using Microsoft.Health.Fhir.Core.Features.Validation;
using Microsoft.Health.Fhir.Core.Features.Validation.Narratives;
using Microsoft.Health.Fhir.Core.Messages.PatientMatch;

namespace Microsoft.Health.Fhir.Core.Features.Operations.PatientMatch
{
    public class PatientMatchResourceValidator : AbstractValidator<PatientMatchRequest>
    {
        public PatientMatchResourceValidator(
            IModelAttributeValidator modelAttributeValidator,
            INarrativeHtmlSanitizer narrativeHtmlSanitizer,
            IProfileValidator profileValidator,
            RequestContextAccessor<IFhirRequestContext> fhirRequestContextAccessor,
            IOptions<CoreFeatureConfiguration> config)
        {
            var contentValidator = new ResourceProfileValidator(
                modelAttributeValidator,
                profileValidator,
                fhirRequestContextAccessor,
                config.Value.ProfileValidationOnCreate);

            RuleFor(x => x.Patient)
                  .SetValidator(new ResourceElementValidator(contentValidator, narrativeHtmlSanitizer));
        }
    }
}
