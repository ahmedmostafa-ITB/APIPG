-- PG Application Form: Returns the maximum number of attachments allowed.
-- Update the SELECT value if new upload categories are added.
-- Called by AttachmentController.GetMaxAttachmentNumber()

CREATE PROCEDURE [dbo].[ITB_GetPGMaxAttachmentCount]
AS
BEGIN
    SET NOCOUNT ON;
    -- Base: 13 upload categories in PG form
    -- +2 buffer for Letters of Recommendation (min 2 required)
    -- Adjust this number if new upload categories are added
    SELECT 15 AS MaxAttachments
END
