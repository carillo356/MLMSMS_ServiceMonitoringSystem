USE [master]
GO
/****** Object:  Database [ServiceDB]    Script Date: 13/04/2023 2:18:08 pm ******/
CREATE DATABASE [ServiceDB] ON  PRIMARY 
( NAME = N'ServiceDB', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SERVICESERVER\MSSQL\DATA\ServiceDB.mdf' , SIZE = 73728KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'ServiceDB_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SERVICESERVER\MSSQL\DATA\ServiceDB_log.ldf' , SIZE = 401408KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [ServiceDB].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [ServiceDB] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [ServiceDB] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [ServiceDB] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [ServiceDB] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [ServiceDB] SET ARITHABORT OFF 
GO
ALTER DATABASE [ServiceDB] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [ServiceDB] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [ServiceDB] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [ServiceDB] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [ServiceDB] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [ServiceDB] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [ServiceDB] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [ServiceDB] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [ServiceDB] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [ServiceDB] SET  DISABLE_BROKER 
GO
ALTER DATABASE [ServiceDB] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [ServiceDB] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [ServiceDB] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [ServiceDB] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [ServiceDB] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [ServiceDB] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [ServiceDB] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [ServiceDB] SET RECOVERY FULL 
GO
ALTER DATABASE [ServiceDB] SET  MULTI_USER 
GO
ALTER DATABASE [ServiceDB] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [ServiceDB] SET DB_CHAINING OFF 
GO
EXEC sys.sp_db_vardecimal_storage_format N'ServiceDB', N'ON'
GO
USE [ServiceDB]
GO

CREATE TYPE [dbo].[ServiceInfoTableType] AS TABLE(
	[ServiceName] [nvarchar](255) NULL,
	[ServiceStatus] [nvarchar](255) NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[ServiceStatusUpdateType]    Script Date: 13/04/2023 2:18:08 pm ******/
CREATE TYPE [dbo].[ServiceStatusUpdateType] AS TABLE(
	[ServiceName] [nvarchar](100) NULL,
	[ServiceStatus] [nvarchar](50) NULL,
	[HostName] [nvarchar](100) NULL,
	[LogBy] [nvarchar](100) NULL,
	[LastStart] [datetime] NULL,
	[LastEventLog] [nvarchar](max) NULL
)
GO
/****** Object:  Table [dbo].[ServicesAvailable]    Script Date: 13/04/2023 2:18:08 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ServicesAvailable](
	[sa_ServiceName] [varchar](100) NOT NULL,
	[sa_ServiceStatus] [nvarchar](50) NOT NULL,
	[sa_HostName] [nvarchar](250) NOT NULL,
	[sa_LastUpdate] [datetime] NOT NULL,
 CONSTRAINT [PK_ServicesAvailable] PRIMARY KEY CLUSTERED 
(
	[sa_ServiceName] ASC,
	[sa_HostName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ServicesLogs]    Script Date: 13/04/2023 2:18:08 pm ******/
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
/****** Object:  Table [dbo].[ServicesMonitored]    Script Date: 13/04/2023 2:18:08 pm ******/
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
/****** Object:  Table [dbo].[ServicesTokens]    Script Date: 13/04/2023 2:18:08 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ServicesTokens](
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
/****** Object:  Table [dbo].[Users]    Script Date: 13/04/2023 2:18:08 pm ******/
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
/****** Object:  Index [IX_ServicesMonitored]    Script Date: 13/04/2023 2:18:08 pm ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_ServicesMonitored] ON [dbo].[ServicesMonitored]
(
	[sm_LastLogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Users]    Script Date: 13/04/2023 2:18:08 pm ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users] ON [dbo].[Users]
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ((0)) FOR [Email_Notification]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ((0)) FOR [IsAdmin]
GO
/****** Object:  StoredProcedure [dbo].[DeleteServiceFromMonitored]    Script Date: 13/04/2023 2:18:08 pm ******/
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
/****** Object:  StoredProcedure [dbo].[DeleteServiceToken]    Script Date: 13/04/2023 2:18:08 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[DeleteServiceToken]
    @Token NVARCHAR(36)
AS
BEGIN
    DELETE FROM ServicesTokens
    WHERE st_Token = @Token;
END
GO
/****** Object:  StoredProcedure [dbo].[DeleteUser]    Script Date: 13/04/2023 2:18:08 pm ******/
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
/****** Object:  StoredProcedure [dbo].[GetLatestLogsForAllServices]    Script Date: 13/04/2023 2:18:08 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetLatestLogsForAllServices]
AS
BEGIN
    SELECT sl.sl_LogID, sl.sl_LogDate, sl.sl_LogBy, sm.sm_ServiceName, sl.sl_ServiceStatus, sl.sl_HostName, sl.sl_LastStart, sl.sl_LastEventLog
    FROM ServicesMonitored sm
    LEFT JOIN ServicesLogs sl ON sm.sm_LastLogID = sl.sl_LogID
END
GO
/****** Object:  StoredProcedure [dbo].[GetServiceLogsByServiceName]    Script Date: 13/04/2023 2:18:08 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetServiceLogsByServiceName]
    @ServiceName VARCHAR(50)
AS
BEGIN
    SELECT sl_LogID, sl_LogDate, sl_LogBy, sl_ServiceName, sl_ServiceStatus, sl_HostName, sl_LastStart, sl_LastEventLog
    FROM ServicesLogs
    WHERE sl_LogID = (SELECT sm_LastLogID FROM ServicesMonitored WHERE sm_ServiceName = @ServiceName)
END
GO
/****** Object:  StoredProcedure [dbo].[GetServicesAvailable]    Script Date: 13/04/2023 2:18:08 pm ******/
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
/****** Object:  StoredProcedure [dbo].[GetServicesStatus]    Script Date: 13/04/2023 2:18:08 pm ******/
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
/****** Object:  StoredProcedure [dbo].[GetUserEmailBySSOToken]    Script Date: 13/04/2023 2:18:08 pm ******/
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
    FROM ServicesTokens
    WHERE st_Token = @Token
      AND st_ExpirationTime > GETUTCDATE();
END
GO
/****** Object:  StoredProcedure [dbo].[InsertServicesToken]    Script Date: 13/04/2023 2:18:08 pm ******/
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
    INSERT INTO ServicesTokens (st_Email, st_Token, st_ExpirationTime)
    VALUES (@Email, @Token, @ExpirationTime);
END
GO
/****** Object:  StoredProcedure [dbo].[UpdateServiceEventLogInfo]    Script Date: 13/04/2023 2:18:08 pm ******/
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
/****** Object:  StoredProcedure [dbo].[UpdateServicesAvailable]    Script Date: 13/04/2023 2:18:08 pm ******/
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
        INSERT INTO ServicesAvailable (sa_ServiceName, sa_ServiceStatus, sa_HostName, sa_LastUpdate)
        SELECT ServiceName, ServiceStatus, @HostName, GETDATE()
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
/****** Object:  StoredProcedure [dbo].[UpdateServiceStatus]    Script Date: 13/04/2023 2:18:08 pm ******/
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
/****** Object:  StoredProcedure [dbo].[UpdateServiceStatusBulk]    Script Date: 13/04/2023 2:18:08 pm ******/
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
/****** Object:  StoredProcedure [dbo].[UpdateUserEmailNotification]    Script Date: 13/04/2023 2:18:08 pm ******/
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
USE [master]
GO
ALTER DATABASE [ServiceDB] SET  READ_WRITE 
GO
