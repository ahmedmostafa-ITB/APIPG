-- PG Application Form: Returns professional exam test codes for the dropdown.
-- Filtered by ApplicationFormSettingId and a configurable list of CODE_VALUE keys.
-- To add/remove exam types, update the @ProfExamCodes variable below.
-- Called by EducationHistoryController.GetProfessionalExamTests()

CREATE PROCEDURE [dbo].[ITB_GetPGProfessionalExamTests]
    @ApplicationFormSettingId int
AS
BEGIN
    SET NOCOUNT ON;

    -- Configure which test CODE_VALUEs are professional exams.
    -- Add or remove values here to control the dropdown options.
    DECLARE @ProfExamCodes TABLE (CodeValue nvarchar(20))
    INSERT INTO @ProfExamCodes VALUES ('GRE'), ('GMAT'), ('Other')

    SELECT ct.TestId AS Id, ct.LONG_DESC AS value
    FROM CODE_TEST ct
    JOIN ApplicationTestSetting ats ON ct.TestId = ats.TestId
    WHERE ats.ApplicationFormSettingId = @ApplicationFormSettingId
      AND ct.STATUS = 'A'
      AND ct.CODE_VALUE IN (SELECT CodeValue FROM @ProfExamCodes)
    ORDER BY ct.LONG_DESC
END
