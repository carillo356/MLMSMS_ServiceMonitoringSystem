ALTER DATABASE [ServiceMonitor] SET ENABLE_BROKER;
GO

USE [ServiceMonitor]
GO
/****** Object:  UserDefinedTableType [dbo].[ServiceInfoTableType]    Script Date: 24/04/2023 2:38:35 pm ******/
CREATE TYPE [dbo].[ServiceInfoTableType] AS TABLE(
	[ServiceName] [nvarchar](255) NULL,
	[ServiceStatus] [nvarchar](255) NULL,
	[Description] [nvarchar](4000) NULL,
	[StartupType] [nvarchar](255) NULL,
	[LogOnAs] [nvarchar](255) NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[ServiceStatusUpdateType]    Script Date: 24/04/2023 2:38:35 pm ******/
CREATE TYPE [dbo].[ServiceStatusUpdateType] AS TABLE(
	[ServiceName] [nvarchar](100) NULL,
	[ServiceStatus] [nvarchar](50) NULL,
	[HostName] [nvarchar](100) NULL,
	[LogBy] [nvarchar](100) NULL,
	[LastStart] [datetime] NULL,
	[LastEventLog] [nvarchar](max) NULL
)
GO
/****** Object:  UserDefinedFunction [dbo].[GetMD5]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[GetMD5]
(
    @input NVARCHAR(4000)
)
RETURNS NVARCHAR(32)
AS
BEGIN
    DECLARE @md5Hash AS VARBINARY(16);
    SET @md5Hash = CONVERT(VARBINARY(16), HashBytes('MD5', @input), 2);
    DECLARE @md5String AS NVARCHAR(32);
    SET @md5String = LOWER(CONVERT(NVARCHAR(32), @md5Hash, 2));
    RETURN @md5String;
END
GO
/****** Object:  Table [dbo].[ServicesAvailable]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ServicesAvailable](
	[sa_ServiceName] [varchar](100) NOT NULL,
	[sa_ServiceStatus] [nvarchar](50) NOT NULL,
	[sa_HostName] [nvarchar](250) NOT NULL,
	[sa_LastUpdate] [datetime] NOT NULL,
	[sa_Description] [nvarchar](max) NULL,
	[sa_StartupType] [nvarchar](50) NULL,
	[sa_LogOnAs] [nvarchar](100) NULL,
 CONSTRAINT [PK_ServicesAvailable] PRIMARY KEY CLUSTERED 
(
	[sa_ServiceName] ASC,
	[sa_HostName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ServicesLogs]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ServicesLogs](
	[sl_LogID] [bigint] IDENTITY(1,1) NOT NULL,
	[sl_LogDate] [datetime] NOT NULL,
	[sl_LogBy] [varchar](100) NOT NULL,
	[sl_ServiceName] [varchar](100) NOT NULL,
	[sl_ServiceStatus] [varchar](50) NOT NULL,
	[sl_HostName] [varchar](100) NULL,
	[sl_LastStart] [datetime] NULL,
	[sl_LastEventLog] [varchar](max) NULL,
 CONSTRAINT [PK__Services__8D34A6E347DC4CC1] PRIMARY KEY CLUSTERED 
(
	[sl_LogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ServicesMonitored]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ServicesMonitored](
	[sm_ServiceName] [varchar](100) NOT NULL,
	[sm_ServiceStatus] [varchar](50) NULL,
	[sm_HostName] [varchar](100) NOT NULL,
	[sm_LastLogID] [bigint] NULL,
 CONSTRAINT [PK__Services__08D7C6789811F427] PRIMARY KEY CLUSTERED 
(
	[sm_ServiceName] ASC,
	[sm_HostName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SsoTokens]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SsoTokens](
	[st_Id] [int] IDENTITY(1,1) NOT NULL,
	[st_Email] [nvarchar](100) NOT NULL,
	[st_Token] [nvarchar](100) NOT NULL,
	[st_ExpirationTime] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[st_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[IdUser] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [nvarchar](50) NULL,
	[LastName] [nvarchar](50) NULL,
	[Email] [nvarchar](50) NULL,
	[Password] [nvarchar](50) NULL,
	[Email_Notification] [bit] NULL,
	[IsAdmin] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdUser] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [IX_ServicesMonitored]    Script Date: 24/04/2023 2:38:35 pm ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_ServicesMonitored] ON [dbo].[ServicesMonitored]
(
	[sm_LastLogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Users]    Script Date: 24/04/2023 2:38:35 pm ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users] ON [dbo].[Users]
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ((0)) FOR [Email_Notification]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ((0)) FOR [IsAdmin]
GO
/****** Object:  StoredProcedure [dbo].[AddNewUser]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[AddNewUser]
    @FirstName NVARCHAR(50),
    @LastName NVARCHAR(50),
    @Email NVARCHAR(50),
    @Password NVARCHAR(50),
    @Email_Notification BIT,
    @IsAdmin BIT
AS
BEGIN
    -- Start a transaction
    BEGIN TRANSACTION;

    BEGIN TRY
        -- Validate input parameters
        IF (LEN(@FirstName) < 3 OR LEN(@FirstName) > 50)
            RAISERROR('Invalid FirstName length. It must be between 3 and 50 characters.', 16, 1);

        IF (LEN(@LastName) < 3 OR LEN(@LastName) > 50)
            RAISERROR('Invalid LastName length. It must be between 3 and 50 characters.', 16, 1);

        -- Validate email
		IF (PATINDEX('%@%', @Email) = 0)
            RAISERROR('Email must contain "@" character.', 16, 1);
        IF (PATINDEX('%[.]%', @Email) = 0)
            RAISERROR('Email must contain "." character.', 16, 1);
		IF (PATINDEX('%@%.%', @Email) = 0)
			RAISERROR('Invalid email format.', 16, 1);
				

        -- Validate password
        IF (LEN(@Password) < 8 OR LEN(@Password) > 50)
            RAISERROR('Invalid password length. It must be between 8 and 50 characters.', 16, 1);
        IF (NOT EXISTS (SELECT 1 WHERE @Password COLLATE Latin1_General_BIN2 LIKE '%[A-Z]%'))
            RAISERROR('Password must contain at least one uppercase letter.', 16, 1);
        IF (NOT EXISTS (SELECT 1 WHERE @Password COLLATE Latin1_General_BIN2 LIKE '%[a-z]%'))
            RAISERROR('Password must contain at least one lowercase letter.', 16, 1);
        IF (PATINDEX('%[0-9]%', @Password) = 0)
            RAISERROR('Password must contain at least one digit.', 16, 1);
        IF (PATINDEX('%[^a-zA-Z0-9]%', @Password) = 0)
            RAISERROR('Password must contain at least one special character.', 16, 1);

        -- Hash the password using MD5 algorithm
        DECLARE @HashedPassword NVARCHAR(32);
        SET @HashedPassword = dbo.GetMD5(@Password);

        INSERT INTO [dbo].[Users]
                   ([FirstName], [LastName], [Email], [Password], [Email_Notification], [IsAdmin])
             VALUES
                   (@FirstName, @LastName, @Email, @HashedPassword, @Email_Notification, @IsAdmin);

        -- Commit the transaction
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- Rollback the transaction in case of an error
        ROLLBACK TRANSACTION;

        -- Rethrow the error
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH;
END
GO
/****** Object:  StoredProcedure [dbo].[DeleteServiceFromMonitored]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[DeleteServiceFromMonitored]
    @ServiceName NVARCHAR(255)
AS
BEGIN
    DELETE FROM ServicesMonitored WHERE sm_ServiceName = @ServiceName
END
GO
/****** Object:  StoredProcedure [dbo].[DeleteServiceToken]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[DeleteServiceToken]
    @Token NVARCHAR(36)
AS
BEGIN
    DELETE FROM SsoTokens
    WHERE st_Token = @Token;
END
GO
/****** Object:  StoredProcedure [dbo].[DeleteUser]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[DeleteUser]
    @IdUser INT
AS
BEGIN
    DELETE FROM Users
    WHERE IdUser = @IdUser
END
GO
/****** Object:  StoredProcedure [dbo].[GetLatestLogsForAllServices]    Script Date: 25/04/2023 9:37:43 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[GetLatestLogsForAllServices]
AS
BEGIN
    SELECT sl.sl_LogID, sl.sl_LogDate, sl.sl_LogBy, sm.sm_ServiceName, sl.sl_ServiceStatus, sl.sl_HostName, sl.sl_LastStart, sl.sl_LastEventLog, sa.sa_Description, sa.sa_StartupType, sa.sa_LogOnAs
    FROM ServicesMonitored sm
    LEFT JOIN ServicesLogs sl ON sm.sm_LastLogID = sl.sl_LogID
    INNER JOIN ServicesAvailable sa ON sm.sm_ServiceName = sa.sa_ServiceName
END
GO
/****** Object:  StoredProcedure [dbo].[GetServicesAvailable]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetServicesAvailable]
AS
BEGIN
    SELECT sa_ServiceName FROM ServicesAvailable;
END
GO
/****** Object:  StoredProcedure [dbo].[GetServicesMonitored]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetServicesMonitored]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT sm_ServiceName, sm_ServiceStatus, sm_HostName, sm_LastLogID
    FROM dbo.ServicesMonitored;
END
GO
/****** Object:  StoredProcedure [dbo].[GetServicesMonitoredRowCount]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetServicesMonitoredRowCount]
AS
BEGIN
    SELECT COUNT_BIG(*) FROM dbo.ServicesMonitored;
END
GO
/****** Object:  StoredProcedure [dbo].[GetServicesStatus]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetServicesStatus]
AS
BEGIN
    SELECT [sm_ServiceName], [sm_ServiceStatus], [sm_HostName]
    FROM ServicesMonitored
END
GO
/****** Object:  StoredProcedure [dbo].[GetUserEmailBySSOToken]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetUserEmailBySSOToken]
    @Token NVARCHAR(100),
    @Email NVARCHAR(100) OUTPUT
AS
BEGIN
    SELECT @Email = st_Email
    FROM SsoTokens
    WHERE st_Token = @Token
      AND st_ExpirationTime > GETUTCDATE();
END
GO
/****** Object:  StoredProcedure [dbo].[InsertServicesToken]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[InsertServicesToken]
    @Email NVARCHAR(100),
    @Token NVARCHAR(100),
    @ExpirationTime DATETIME
AS
BEGIN
    INSERT INTO SsoTokens (st_Email, st_Token, st_ExpirationTime)
    VALUES (@Email, @Token, @ExpirationTime);
END
GO
/****** Object:  StoredProcedure [dbo].[SqlQueryNotificationStoredProcedure-f100e403-773c-4cba-a7c7-d8c19ca60fe6]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SqlQueryNotificationStoredProcedure-f100e403-773c-4cba-a7c7-d8c19ca60fe6] AS BEGIN BEGIN TRANSACTION; RECEIVE TOP(0) conversation_handle FROM [SqlQueryNotificationService-f100e403-773c-4cba-a7c7-d8c19ca60fe6]; IF (SELECT COUNT(*) FROM [SqlQueryNotificationService-f100e403-773c-4cba-a7c7-d8c19ca60fe6] WHERE message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer') > 0 BEGIN if ((SELECT COUNT(*) FROM sys.services WHERE name = 'SqlQueryNotificationService-f100e403-773c-4cba-a7c7-d8c19ca60fe6') > 0)   DROP SERVICE [SqlQueryNotificationService-f100e403-773c-4cba-a7c7-d8c19ca60fe6]; if (OBJECT_ID('SqlQueryNotificationService-f100e403-773c-4cba-a7c7-d8c19ca60fe6', 'SQ') IS NOT NULL)   DROP QUEUE [SqlQueryNotificationService-f100e403-773c-4cba-a7c7-d8c19ca60fe6]; DROP PROCEDURE [SqlQueryNotificationStoredProcedure-f100e403-773c-4cba-a7c7-d8c19ca60fe6]; END COMMIT TRANSACTION; END
GO
/****** Object:  StoredProcedure [dbo].[UpdateServiceEventLogInfo]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[UpdateServiceEventLogInfo]
    @ServiceName NVARCHAR(100),
    @LastStart DATETIME,
    @LastEventLog NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Update ServicesLogs table
        UPDATE [dbo].[ServicesLogs]
        SET [sl_LastStart] = @LastStart,
            [sl_LastEventLog] = @LastEventLog
        WHERE [sl_ServiceName] = @ServiceName;

        -- Update ServicesMonitored table
        UPDATE [dbo].[ServicesMonitored]
        SET [sm_LastLogID] = sl.[sl_LogID],
            [sm_ServiceStatus] = sl.[sl_ServiceStatus]
        FROM [dbo].[ServicesMonitored] sm
        INNER JOIN [dbo].[ServicesLogs] sl ON sm.[sm_ServiceName] = sl.[sl_ServiceName]
        WHERE sl.[sl_ServiceName] = @ServiceName
        AND sl.[sl_LogID] = (SELECT MAX([sl_LogID]) FROM [dbo].[ServicesLogs] WHERE [sl_ServiceName] = @ServiceName);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- If an error occurs, roll back the transaction
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
/****** Object:  StoredProcedure [dbo].[UpdateServicesAvailable]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[UpdateServicesAvailable]
    @ServiceInfo ServiceInfoTableType READONLY,
    @HostName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Delete existing records for the given host name
        DELETE FROM ServicesAvailable WHERE sa_HostName = @HostName;

        -- Insert new records
        INSERT INTO ServicesAvailable (sa_ServiceName, sa_ServiceStatus, sa_HostName, sa_LastUpdate, sa_Description, sa_StartupType, sa_LogOnAs)
        SELECT ServiceName, ServiceStatus, @HostName, GETDATE(), Description, StartupType, LogOnAs
        FROM @ServiceInfo;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- If an error occurs, roll back the transaction
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
/****** Object:  StoredProcedure [dbo].[UpdateServiceStatus]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[UpdateServiceStatus]
    @ServiceName NVARCHAR(100),
    @ServiceStatus NVARCHAR(50),
    @HostName NVARCHAR(100),
    @LogBy NVARCHAR(100),
    @LastStart datetime,
    @LastEventLog NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[ServicesMonitored] (sm_ServiceName, sm_HostName) 
        SELECT @ServiceName, @HostName
        WHERE NOT EXISTS (
            SELECT 1 
            FROM ServicesMonitored 
            WHERE sm_ServiceName = @ServiceName AND sm_HostName = @HostName
        )

        INSERT INTO [dbo].[ServicesLogs]
               ([sl_LogDate]
               ,[sl_LogBy]
               ,[sl_ServiceName]
               ,[sl_ServiceStatus]
               ,[sl_HostName]
               ,[sl_LastStart]
               ,[sl_LastEventLog])
         VALUES
               (GETDATE()
               ,@LogBy
               ,@ServiceName
               ,@ServiceStatus
               ,@HostName
               ,@LastStart
               ,@LastEventLog)

        DECLARE @lastLogId bigint = SCOPE_IDENTITY()

        UPDATE [dbo].[ServicesMonitored]
           SET [sm_ServiceStatus] = @ServiceStatus
              ,[sm_HostName] = @HostName
              ,[sm_LastLogID] = @lastLogId
         WHERE [sm_ServiceName] = @ServiceName AND [sm_HostName] = @HostName

        UPDATE [dbo].[ServicesAvailable]
           SET [sa_ServiceStatus] = @ServiceStatus
              ,[sa_HostName] = @HostName
              ,[sa_LastUpdate] = GETDATE()
         WHERE [sa_ServiceName] = @ServiceName AND [sa_HostName] = @HostName

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- If an error occurs, roll back the transaction
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
/****** Object:  StoredProcedure [dbo].[UpdateServiceStatusBulk]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[UpdateServiceStatusBulk]
    @Updates dbo.ServiceStatusUpdateType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Insert into ServicesLogs
        INSERT INTO [dbo].[ServicesLogs]
               ([sl_LogDate]
               ,[sl_LogBy]
               ,[sl_ServiceName]
               ,[sl_ServiceStatus]
               ,[sl_HostName]
               ,[sl_LastStart]
               ,[sl_LastEventLog])
        SELECT GETDATE(),
               LogBy,
               ServiceName,
               ServiceStatus,
               HostName,
               LastStart,
               LastEventLog
        FROM @Updates

        -- Update ServicesAvailable
        UPDATE sa
        SET sa_ServiceStatus = u.ServiceStatus,
            sa_HostName = u.HostName,
            sa_LastUpdate = GETDATE()
        FROM [dbo].[ServicesAvailable] sa
        JOIN @Updates u ON sa.sa_ServiceName = u.ServiceName AND sa.sa_HostName = u.HostName

        -- Update ServicesMonitored
        UPDATE sm
        SET sm_ServiceStatus = u.ServiceStatus,
            sm_LastLogID = sl.LatestLogID
        FROM [dbo].[ServicesMonitored] sm
        JOIN @Updates u ON sm.sm_ServiceName = u.ServiceName AND sm.sm_HostName = u.HostName
        JOIN (
            SELECT sl_ServiceName, sl_HostName, MAX(sl_LogID) AS LatestLogID
            FROM [dbo].[ServicesLogs]
            GROUP BY sl_ServiceName, sl_HostName
        ) sl ON sl.sl_ServiceName = u.ServiceName AND sl.sl_HostName = u.HostName
        WHERE sm.sm_ServiceName = u.ServiceName AND sm.sm_HostName = u.HostName
          AND sm.sm_ServiceStatus <> u.ServiceStatus -- Add this condition to check for status change

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- If an error occurs, roll back the transaction
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
/****** Object:  StoredProcedure [dbo].[UpdateUserEmailNotification]    Script Date: 24/04/2023 2:38:35 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[UpdateUserEmailNotification]
    @IdUser int
AS
BEGIN
    -- Get the current value of the Email_Notification column for the user
    DECLARE @currentEmailNotification bit;
    SELECT @currentEmailNotification = Email_Notification
    FROM Users
    WHERE IdUser = @IdUser;

    -- Update the Email_Notification column to the opposite of the current value
    UPDATE Users
    SET Email_Notification = ~@currentEmailNotification
    WHERE IdUser = @IdUser;
END
GO

CREATE PROCEDURE [dbo].[IsServiceAvailable]
	@ServiceName varchar(100)
AS
BEGIN
    SELECT CAST(1 as bit) FROM ServicesAvailable WHERE UPPER(sa_ServiceName) = UPPER(@ServiceName);
END
GO