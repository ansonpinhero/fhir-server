﻿CREATE TABLE Resource
(
    ResourceTypeId              smallint                NOT NULL,
    ResourceId                  varchar(64)             NOT NULL,
    Version                     int                     NOT NULL,
    IsHistory                   bit                     NOT NULL,
    ResourceSurrogateId         bigint                  NOT NULL,
    IsDeleted                   bit                     NOT NULL,
    RequestMethod               varchar(10)             NULL,
    RawResource                 bytea          NOT NULL,
    IsRawResourceMetaSet        bit                     NOT NULL,
    SearchParamHash             varchar(64)             NULL,

    CONSTRAINT PKC_Resource PRIMARY KEY (ResourceTypeId, ResourceId, Version),
    CONSTRAINT CH_Resource_RawResource_Length CHECK (RawResource > '\000')
);

-- SQLINES LICENSE FOR EVALUATION USE ONLY
CREATE UNIQUE INDEX IX_Resource_ResourceTypeId_ResourceId_Version ON Resource
(
    ResourceTypeId,
    ResourceId,
    Version
);


-- SQLINES LICENSE FOR EVALUATION USE ONLY
CREATE UNIQUE INDEX IX_Resource_ResourceTypeId_ResourceId ON Resource
(
    ResourceTypeId,
    ResourceId
)
INCLUDE -- SQLINES DEMO ***  in UpsertResource, which is done with UPDLOCK AND HOLDLOCK, to not require a key lookup
(
    Version,
    IsDeleted
)
WHERE IsHistory = 0 :: bit;

SELECT create_distributed_table('resource', 'resourceid');
