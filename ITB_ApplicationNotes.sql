-- ITB_ApplicationNotes: Holds notes data during the application phase.
-- When the applicant is accepted, ITB_spAcceptApplicationNotes moves
-- these records to the NOTES table (PowerCampus).
-- Called by SubmitController during submission for Academic Awards, etc.

-- TABLE
CREATE TABLE [dbo].[ITB_ApplicationNotes] (
    ApplicationNoteId INT IDENTITY(1,1) PRIMARY KEY,
    ApplicationId INT NOT NULL,
    Office NVARCHAR(20) NOT NULL,
    NoteType NVARCHAR(12) NOT NULL,
    Notes NVARCHAR(MAX) NULL,
    CreateDatetime DATETIME NOT NULL DEFAULT GETDATE(),
    RevisionDatetime DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_ITB_ApplicationNotes_Application
        FOREIGN KEY (ApplicationId) REFERENCES Application(ApplicationId) ON DELETE CASCADE
)
GO

-- INSERT PROCEDURE
CREATE PROCEDURE [dbo].[ITB_spInsApplicationNotes]
    @ApplicationNoteId INT OUTPUT,
    @ApplicationId INT,
    @Office NVARCHAR(20),
    @NoteType NVARCHAR(12),
    @Notes NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ITB_ApplicationNotes (ApplicationId, Office, NoteType, Notes)
    VALUES (@ApplicationId, @Office, @NoteType, @Notes)

    SET @ApplicationNoteId = SCOPE_IDENTITY()
    RETURN @@ROWCOUNT
END
GO

-- ACCEPT PROCEDURE (called from spAcceptApplication when applicant is accepted)
-- Copies ITB_ApplicationNotes → NOTES table for the accepted person
CREATE PROCEDURE [dbo].[ITB_spAcceptApplicationNotes]
    @ApplicationId INT,
    @PersonId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PCID NVARCHAR(10)
    DECLARE @opid NVARCHAR(8) = 'APPLCANT'
    DECLARE @terminal NVARCHAR(4) = '0001'
    DECLARE @date DATETIME = GETDATE()

    -- Get the PCID from PersonId
    SET @PCID = dbo.fnGetPeopleCodeId(@PersonId)
    IF @PCID IS NULL RETURN -1

    -- Do not proceed if there are no notes for this application
    IF NOT EXISTS (SELECT 1 FROM dbo.ITB_ApplicationNotes WHERE ApplicationId = @ApplicationId)
        RETURN 0

    -- Insert each note into the NOTES table
    INSERT INTO dbo.NOTES (
        PEOPLE_ORG_CODE,
        PEOPLE_ORG_ID,
        PEOPLE_ORG_CODE_ID,
        OFFICE,
        NOTE_TYPE,
        NOTE_DATE,
        CREATE_DATE,
        CREATE_TIME,
        CREATE_OPID,
        CREATE_TERMINAL,
        REVISION_DATE,
        REVISION_TIME,
        REVISION_OPID,
        REVISION_TERMINAL,
        NOTES,
        ABT_JOIN,
        PRINT_ON_TRANS
    )
    SELECT
        'P',
        RIGHT(@PCID, 9),
        @PCID,
        n.Office,
        n.NoteType,
        n.CreateDatetime,
        dbo.fnMakeDate(@date),
        dbo.fnMakeTime(@date),
        @opid,
        @terminal,
        dbo.fnMakeDate(@date),
        dbo.fnMakeTime(@date),
        @opid,
        @terminal,
        n.Notes,
        '*',
        'N'
    FROM dbo.ITB_ApplicationNotes n
    WHERE n.ApplicationId = @ApplicationId

    RETURN @@ROWCOUNT
END
GO
