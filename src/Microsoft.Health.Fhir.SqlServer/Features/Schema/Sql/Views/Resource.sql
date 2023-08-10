﻿CREATE VIEW dbo.Resource
AS
SELECT ResourceTypeId
      ,ResourceSurrogateId
      ,ResourceId
      ,Version
      ,IsHistory
      ,IsDeleted
      ,RequestMethod
      ,RawResource
      ,IsRawResourceMetaSet
      ,SearchParamHash
      ,TransactionId
      ,HistoryTransactionId
  FROM dbo.ResourceHistory
UNION ALL
SELECT ResourceTypeId
      ,ResourceSurrogateId
      ,ResourceId
      ,Version
      ,IsHistory
      ,IsDeleted
      ,RequestMethod
      ,RawResource
      ,IsRawResourceMetaSet
      ,SearchParamHash
      ,TransactionId
      ,NULL
  FROM dbo.ResourceCurrent
GO
CREATE TRIGGER dbo.ResourceIns ON dbo.Resource INSTEAD OF INSERT
AS
BEGIN
  INSERT INTO dbo.ResourceCurrent
      (
           ResourceTypeId
          ,ResourceSurrogateId
          ,ResourceId
          ,Version
          ,IsDeleted
          ,RequestMethod
          ,RawResource
          ,IsRawResourceMetaSet
          ,SearchParamHash
          ,TransactionId
      )
    SELECT ResourceTypeId
          ,ResourceSurrogateId
          ,ResourceId
          ,Version
          ,IsDeleted
          ,RequestMethod
          ,RawResource
          ,IsRawResourceMetaSet
          ,SearchParamHash
          ,TransactionId
      FROM Inserted
      WHERE IsHistory = 0

  INSERT INTO dbo.ResourceHistory
      (
           ResourceTypeId
          ,ResourceSurrogateId
          ,ResourceId
          ,Version
          ,IsDeleted
          ,RequestMethod
          ,RawResource
          ,IsRawResourceMetaSet
          ,SearchParamHash
          ,TransactionId
          ,HistoryTransactionId
      )
    SELECT ResourceTypeId
          ,ResourceSurrogateId
          ,ResourceId
          ,Version
          ,IsDeleted
          ,RequestMethod
          ,RawResource
          ,IsRawResourceMetaSet
          ,SearchParamHash
          ,TransactionId
          ,HistoryTransactionId
      FROM Inserted
      WHERE IsHistory = 1

  RETURN
END
GO
CREATE TRIGGER dbo.ResourceUpd ON dbo.Resource INSTEAD OF UPDATE
AS
BEGIN
  DELETE FROM A
    FROM dbo.ResourceCurrent A
    WHERE EXISTS (SELECT * FROM Inserted B WHERE B.ResourceTypeId = A.ResourceTypeId AND B.ResourceSurrogateId = A.ResourceSurrogateId AND B.IsHistory = 1)

  INSERT INTO dbo.ResourceHistory
      (
           ResourceTypeId
          ,ResourceSurrogateId
          ,ResourceId
          ,Version
          ,IsDeleted
          ,RequestMethod
          ,RawResource
          ,IsRawResourceMetaSet
          ,SearchParamHash
          ,TransactionId
          ,HistoryTransactionId
      )
    SELECT ResourceTypeId
          ,ResourceSurrogateId
          ,ResourceId
          ,Version
          ,IsDeleted
          ,RequestMethod
          ,RawResource
          ,IsRawResourceMetaSet
          ,SearchParamHash
          ,TransactionId
          ,HistoryTransactionId
      FROM Inserted
      WHERE IsHistory = 1

  RETURN
END
GO
CREATE TRIGGER dbo.ResourceDel ON dbo.Resource INSTEAD OF DELETE
AS
BEGIN
  DELETE FROM A
    FROM dbo.ResourceCurrent A
    WHERE EXISTS (SELECT * FROM Deleted B WHERE B.ResourceTypeId = A.ResourceTypeId AND B.ResourceSurrogateId = A.ResourceSurrogateId AND B.IsHistory = 0)

  DELETE FROM A
    FROM dbo.ResourceHistory A
    WHERE EXISTS (SELECT * FROM Deleted B WHERE B.ResourceTypeId = A.ResourceTypeId AND B.ResourceSurrogateId = A.ResourceSurrogateId AND B.IsHistory = 1)

  RETURN
END
GO
