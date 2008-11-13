/*
  
   ELMAH - Error Logging Modules and Handlers for ASP.NET
   Copyright (c) 2007 Atif Aziz. All rights reserved.
  
    Author(s):
  
        Atif Aziz, http://www.raboof.com
        Phil Haacked, http://haacked.com
  
   This library is free software; you can redistribute it and/or modify it 
   under the terms of the New BSD License, a copy of which should have 
   been delivered along with this distribution.
  
   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
   "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
   LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
   PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
   OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
   SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
   LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
   DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
   THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
   OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
  
*/

-- ELMAH DDL script for Microsoft SQL Server 2000 or later.

DECLARE @DBCompatibilityLevel INT
DECLARE @DBCompatibilityLevelMajor INT
DECLARE @DBCompatibilityLevelMinor INT

SELECT 
    @DBCompatibilityLevel = compatibility_level 
FROM 
    sys.databases 
WHERE 
    name = DB_NAME()

IF @DBCompatibilityLevel <> 80
BEGIN

    SELECT @DBCompatibilityLevelMajor = @DBCompatibilityLevel / 10, 
           @DBCompatibilityLevelMinor = @DBCompatibilityLevel % 10
           
    PRINT N'
    ===========================================================================
    WARNING! 
    ---------------------------------------------------------------------------
    
    This script is designed for Microsoft SQL Server 2000 (8.0) but your 
    database is set up for compatibility with version ' 
    + CAST(@DBCompatibilityLevelMajor AS NVARCHAR(80)) 
    + N'.' 
    + CAST(@DBCompatibilityLevelMinor AS NVARCHAR(80)) 
    + N'. Although 
    the script should work with later versions of Microsoft SQL Server, 
    you can ensure compatibility by executing the following statement:
    
    ALTER DATABASE [' 
    + DB_NAME() 
    + N'] 
    SET COMPATIBILITY_LEVEL = 80

    If you are hosting ELMAH in the same database as your application 
    database and do not wish to change the compatibility option then you 
    should create a separate database to host ELMAH where you can set the 
    compatibility level more freely.
    
    If you continue with the current setup, please report any compatibility 
    issues you encounter over at:
    
    http://code.google.com/p/elmah/issues/list

    ===========================================================================
'
END
GO

CREATE TABLE [dbo].[ELMAH_Error]
(
    [ErrorId]     UNIQUEIDENTIFIER NOT NULL,
    [Application] NVARCHAR(60)  COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Host]        NVARCHAR(50)  COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Type]        NVARCHAR(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Source]      NVARCHAR(60)  COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Message]     NVARCHAR(500) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [User]        NVARCHAR(50)  COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [StatusCode]  INT NOT NULL,
    [TimeUtc]     DATETIME NOT NULL,
    [Sequence]    INT IDENTITY (1, 1) NOT NULL,
    [AllXml]      NTEXT COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL 
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[ELMAH_Error] WITH NOCHECK ADD 
    CONSTRAINT [PK_ELMAH_Error] PRIMARY KEY NONCLUSTERED
    (
        [ErrorId]
    )  ON [PRIMARY] 
GO

ALTER TABLE [dbo].[ELMAH_Error] ADD 
    CONSTRAINT [DF_ELMAH_Error_ErrorId] DEFAULT (newid()) FOR [ErrorId]
GO

CREATE NONCLUSTERED INDEX [IX_ELMAH_Error_App_Time_Seq] ON [dbo].[ELMAH_Error]
(
    [Application] ASC,
    [TimeUtc] DESC,
    [Sequence] DESC
) ON [PRIMARY]
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

CREATE PROCEDURE [dbo].[ELMAH_GetErrorXml]
(
    @Application NVARCHAR(60),
    @ErrorId UNIQUEIDENTIFIER
)
AS

SET NOCOUNT ON

SELECT 
    [AllXml]
FROM 
    [ELMAH_Error]
WHERE
    [ErrorId] = @ErrorId
AND
    [Application] = @Application

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

CREATE PROCEDURE [dbo].[ELMAH_GetErrorsXml]
(
    @Application NVARCHAR(60),
    @PageIndex INT = 0,
    @PageSize INT = 15,
    @TotalCount INT OUTPUT
)
AS 

SET NOCOUNT ON

DECLARE @FirstTimeUTC DateTime
DECLARE @FirstSequence int
DECLARE @StartRow int
DECLARE @StartRowIndex int

SELECT 
    @TotalCount = COUNT(1) 
FROM 
    [ELMAH_Error]
WHERE 
    [Application] = @Application

-- Get the ID of the first error for the requested page

SET @StartRowIndex = @PageIndex * @PageSize + 1

IF @StartRowIndex <= @TotalCount
BEGIN

    SET ROWCOUNT @StartRowIndex

    SELECT  
        @FirstTimeUTC = [TimeUtc],
        @FirstSequence = [Sequence]
    FROM 
        [ELMAH_Error]
    WHERE   
        [Application] = @Application
    ORDER BY 
        [TimeUtc] DESC, 
        [Sequence] DESC

END
ELSE
BEGIN

    SET @PageSize = 0

END

-- Now set the row count to the requested page size and get
-- all records below it for the pertaining application.

SET ROWCOUNT @PageSize

SELECT 
    errorId     = [ErrorId], 
    application = [Application],
    host        = [Host], 
    type        = [Type],
    source      = [Source],
    message     = [Message],
    [user]      = [User],
    statusCode  = [StatusCode], 
    time        = CONVERT(VARCHAR(50), [TimeUtc], 126) + 'Z'
FROM 
    [ELMAH_Error] error
WHERE
    [Application] = @Application
AND
    [TimeUtc] <= @FirstTimeUTC
AND 
    [Sequence] <= @FirstSequence
ORDER BY
    [TimeUtc] DESC, 
    [Sequence] DESC
FOR
    XML AUTO

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

CREATE PROCEDURE [dbo].[ELMAH_LogError]
(
    @ErrorId UNIQUEIDENTIFIER,
    @Application NVARCHAR(60),
    @Host NVARCHAR(30),
    @Type NVARCHAR(100),
    @Source NVARCHAR(60),
    @Message NVARCHAR(500),
    @User NVARCHAR(50),
    @AllXml NTEXT,
    @StatusCode INT,
    @TimeUtc DATETIME
)
AS

SET NOCOUNT ON

INSERT
INTO
    [ELMAH_Error]
    (
        [ErrorId],
        [Application],
        [Host],
        [Type],
        [Source],
        [Message],
        [User],
        [AllXml],
        [StatusCode],
        [TimeUtc]
    )
VALUES
    (
        @ErrorId,
        @Application,
        @Host,
        @Type,
        @Source,
        @Message,
        @User,
        @AllXml,
        @StatusCode,
        @TimeUtc
    )

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

