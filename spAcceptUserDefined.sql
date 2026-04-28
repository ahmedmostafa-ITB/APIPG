--Fixes spAccept Issue Ali Reported.
USE [PCDB_TESTING]
GO
/****** Object:  StoredProcedure [dbo].[spAcceptApplicationUserDefined]    Script Date: 27/04/2026 11:39:07 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER procedure [dbo].[spAcceptApplicationUserDefined]  
 @ApplicationId int  
 , @PersonId int  
AS  
SET NOCOUNT ON  

declare @ReturnStatus int  
 , @opid nvarchar(8)  
 , @terminal nvarchar(4)  
 , @date nvarchar(23)  
 , @time nvarchar(23)  
 , @PCID nvarchar(10)  
 , @id int, @rc int  
 , @exists bit  
 , @column nvarchar(128)   -- AM: FIXED (was 18)
 , @value nvarchar(4000)   -- AM: safer size
 , @type tinyint  
 , @sql nvarchar(MAX), @insValues nvarchar(MAX);  
  
--Do not proceed if there are no user defined entries for the application  
if not exists (select 1 from dbo.ApplicationUserDefined   
    where IsUploading = 1   
    and ApplicationId = @ApplicationId)  
 return 0;  
  
select @ReturnStatus = 0  
 , @opid = 'APPLCANT'  
 , @terminal = '0001'  
 , @exists = 0  
 , @date = convert(nvarchar(23), dbo.fnMakeDate( getdate() ), 126)  
 , @time = convert(nvarchar(23), dbo.fnMakeTime( getdate() ), 126);  
  
--get the pcid  
select @PCID = dbo.fnGetPeopleCodeId(@PersonId);  
if @PCID is null   
 return -1;  
  
-- Check if record exists
if exists (select 1 from dbo.USERDEFINEDIND where PEOPLE_CODE_ID = @PCID)  
begin  
 select @exists = 1  
   ,@sql = N'update dbo.USERDEFINEDIND SET 
        REVISION_OPID = N''' + @opid + ''', 
        REVISION_TERMINAL = N''' + @terminal + ''', 
        REVISION_DATE = N''' + @date + ''', 
        REVISION_TIME = N''' + @time + '''';  
end  
else  
begin  
 select @exists = 0  
   ,@sql = N'insert into dbo.USERDEFINEDIND (
        PEOPLE_CODE, PEOPLE_ID, PEOPLE_CODE_ID, 
        CREATE_DATE, CREATE_TIME, CREATE_OPID, CREATE_TERMINAL, 
        REVISION_DATE, REVISION_TIME, REVISION_OPID, REVISION_TERMINAL, ABT_JOIN'  
   ,@insValues = N'''P'', N''' + Right(@PCID,9) + ''', N''' + @PCID +   
      ''', N'''+ @date + ''', N'''+ @time + ''', N'''+ @opid + ''', N'''+ @terminal +   
      ''', N'''+ @date + ''', N'''+ @time + ''', N'''+ @opid + ''', N'''+ @terminal +   
      ''', ''*''';  
end  
  
-- Get first record
select @id = min(ApplicationUserDefinedId)   
from dbo.ApplicationUserDefined   
where ApplicationId = @ApplicationId  
and IsUploading = 1;  
  
while @id is not null  
begin  
 -- Get data
 select @column = ColumnName  
   ,@value = ColumnValue  
   ,@type = ColumnType   
 from dbo.ApplicationUserDefined   
 where ApplicationUserDefinedId = @id;  

 -- AM: VALIDATE COLUMN EXISTS 
 IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.USERDEFINEDIND')
    AND name = @column COLLATE SQL_Latin1_General_Cp1_CI_AI
 )
 BEGIN
    SELECT @column = name
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.USERDEFINEDIND')
    AND name = @column COLLATE SQL_Latin1_General_Cp1_CI_AI;
 END
 ELSE
 BEGIN
    -- AM: Skip invalid/truncated column safely
    SELECT @id = min(ApplicationUserDefinedId)   
    FROM dbo.ApplicationUserDefined   
    WHERE ApplicationId = @ApplicationId   
    AND ApplicationUserDefinedId > @id  
    AND IsUploading = 1;

    CONTINUE;
 END

 -- Format date/time
 IF @type IN (2,3)
 BEGIN
    BEGIN TRY
        SET @value = CONVERT(nvarchar(23), CAST(@value AS datetime), 126);
    END TRY
    BEGIN CATCH
        SET @value = NULL;
    END CATCH
 END
  
 -- Build SQL
 IF @exists = 1  
    SET @sql = @sql + ', [' + @column + '] = N''' + ISNULL(REPLACE(@value,'''',''''''),'') + '''';  
 ELSE  
 BEGIN  
    SET @sql = @sql + ', [' + @column + ']';  
    SET @insValues = @insValues + ', N''' + ISNULL(REPLACE(@value,'''',''''''),'') + '''';  
 END  
  
 -- Next record
 select @id = min(ApplicationUserDefinedId)   
 from dbo.ApplicationUserDefined   
 where ApplicationId = @ApplicationId   
 and ApplicationUserDefinedId > @id  
 and IsUploading = 1;  
  
 set @ReturnStatus = @ReturnStatus + 1;  
end  
  
-- Finalize SQL
IF @exists = 1  
    SET @sql = @sql + ' WHERE PEOPLE_CODE_ID = N''' + @PCID + '''';  
ELSE  
    SET @sql = @sql + ') VALUES (' + @insValues + ')';  
  
-- Execute safely
BEGIN TRY
    EXEC @rc = sp_executesql @sql;  
END TRY
BEGIN CATCH
    SET @ReturnStatus = -1;
    RETURN @ReturnStatus;
END CATCH
  
IF @rc <> 0  
    SET @ReturnStatus = -1;  
  
return @ReturnStatus;