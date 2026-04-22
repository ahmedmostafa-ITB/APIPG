# Known Issues & Pending Fixes

Tracked issues discovered during PG application form testing. Each entry has enough context to resume work without prior conversation history.

---

## 1. Transaction Scope Does Not Actually Roll Back on Failure

**Status:** Open — architectural issue, not yet started  
**Severity:** Critical — can cause partial submissions with missing data  
**Location:** `APIPG/SelfServiceAPI/Controllers/SubmitController.cs` (lines 478-573) and `APIPG/SelfServiceAPI/Classes/Submit.cs`

**Problem:**  
`InsertApplication()` in SubmitController wraps all insert calls in a `Database.BeginTransaction()` block with try/catch/rollback. However, each insert method (`Submit.InsertApplication`, `Submit.InsertApplicationPhone`, `Submit.InsertApplicationAddress`, etc.) creates its **own** `ApplicationFormEntities` context internally. The stored procedures execute and commit on those independent contexts, so the outer `tranScope.Rollback()` has no effect on already-committed data.

**Impact:**  
If any insert fails mid-way (e.g., `InsertApplicationProgram` throws after `InsertApplication` and `InsertApplicationPhone` succeed), the application record and phone record are committed to the database, but program, attachments, and education records are missing. The user sees an error but their data is partially saved in an inconsistent state. The incomplete application may also be deleted (line 259), making recovery impossible.

**Observed during testing:**  
ApplicationId 16292 was created with phone and address but no program, attachments, or education data after `InsertApplicationProgram` failed with a NullReferenceException on Level lookup.

**Fix approach:**  
Either:
1. Pass the outer `ApplicationFormEntities` context and transaction into each insert method instead of creating new ones, so all operations share one transaction
2. Or use `TransactionScope` (System.Transactions) which enlists all connections opened within its scope into a distributed transaction
3. Or at minimum, validate all data before starting any inserts (fail-fast before any writes)

**Related files:**
- `SubmitController.cs` — `InsertApplication()` method (the orchestrator, lines 470-581)
- `Submit.cs` — all `InsertApplication*` static methods (each creates own context)

---

## 2. Attachment Silently Skipped When Media Type Not Found

**Status:** Open — fix written but reverted, pending separate commit  
**Severity:** Critical — mandatory attachments can be lost without any error  
**Location:** `APIPG/SelfServiceAPI/Classes/Submit.cs` — `InsertApplicationAttachment()` (line 222)

**Problem:**  
When a file is uploaded, `InsertApplicationAttachment` looks up the file extension in `CODE_MEDIATYPE`. If no match is found (`mediaType == null`), the entire attachment is silently skipped — no exception, no log, no error returned. The submission succeeds but the file is gone.

The frontend `AttachmentCheck` (SubmitController.cs line 453) validates by checking if the `label` property exists in the request list — this passes because the file IS in the list, just its content won't be saved.

**Observed in production (UG form):**  
Mandatory attachments were lost — files were uploaded by applicants but never stored in the database.

**Planned fix (already written, reverted for separate commit):**
```csharp
// In InsertApplicationAttachment, after both mediaType lookups:
if (mediaType == null)
{
    throw new Exception("Unsupported file type '" + attachment.FileExtension + "' for file '" + attachment.FileName + "'. The file was not saved. Please check CODE_MEDIATYPE configuration.");
}
```
This causes the whole submission to fail and roll back (once Issue #1 is also fixed), keeping the incomplete application intact for retry.

**Also fix `InsertApplicationAttachment` to throw when `fileType <= 0`** — same silent skip pattern.

---

## 3. AttachmentCheck Bypassed When First Item Has No Label

**Status:** Open — fix written but reverted, pending separate commit  
**Severity:** High — backend validation for mandatory attachments can be completely skipped  
**Location:** `APIPG/SelfServiceAPI/Controllers/SubmitController.cs` — `AttachmentCheck()` (line 453)

**Problem:**
```csharp
if (lstRequest[0].label != null)
{
    Submit submit = lstRequest.Find(item => item.label.Equals(attachmentName));
    ...
}
```
If the first item in `lstRequest` has a null label, the entire check is bypassed and returns `false`. But more critically, this means the mandatory attachment validation (lines 111-156 for PG, 161-237 for UG) would flag the missing attachment and block submission — so in practice this bug causes false negatives (reports attachment as missing even when present) rather than false positives, IF the first item happens to have no label.

However, the real risk is if the list order changes or the fallback empty object `[{ IncompleteApplicationId: ... }]` (sent when no files exist) is the first item.

**Planned fix (already written, reverted for separate commit):**
```csharp
private bool AttachmentCheck(List<Submit> lstRequest, string attachmentName)
{
    Submit submit = lstRequest.Find(item => item.label != null && item.label.Equals(attachmentName));
    return submit != null;
}
```

---

## 4. Error Messages Swallowed as "Application Already Submitted"

**Status:** Open — related to Issue #1  
**Severity:** High — users see wrong error message, can't understand what went wrong  
**Location:** `APIPG/SelfServiceAPI/Controllers/SubmitController.cs` — lines 360-370, 568-573

**Problem:**  
When `InsertApplication()` (the private method in SubmitController) fails internally, the inner catch (line 568) catches the exception, rolls back (ineffectively — see Issue #1), logs it, and returns `insertedApplicationId = 0`. The calling code (line 360) interprets `0` as "application already submitted" and returns `StatusCode: 409, "Application already submitted"` to the frontend.

The real error (e.g., FK violation, NullReferenceException) is only in the log file — the user sees a completely misleading message.

**Planned fix:**  
Re-throw from the inner catch so the exception propagates to the outer catch (line 422) which returns `"An error has occured. Please contact your administrator."` — still generic but at least not misleading:
```csharp
catch (Exception exception)
{
    tranScope.Rollback();
    LoggingManager.LogException(exception.Message, exception.StackTrace, DateTime.Now, string.Empty, string.Empty);
    throw;  // propagate to outer catch
}
```

Ideally, the outer catch should also include the actual error detail in the response for admin/debugging purposes (not raw stack traces, but a meaningful message like "Failed to save program data" or "Unsupported file type").

---

## 5. PayTabs Payment Gateway 401 Unauthorized on Local

**Status:** Open — not yet investigated  
**Severity:** Medium — blocks full end-to-end submission testing locally  
**Location:** `APIPG/SelfServiceAPI/Controllers/SubmitController.cs` — line 312

**Problem:**  
After successful application insert, the API calls PayTabs payment gateway which returns 401 Unauthorized. The merchant ID in Web.config (`109627`) may not be authorized for local/test requests, or the callback URL (`http://localhost:8010/APITest`) isn't whitelisted.

**Log entry:**
```
System.Net.Http.HttpRequestException: Response status code does not indicate success: 401 (Unauthorized).
```

**To investigate:**
- Check if PayTabs has a sandbox/test mode
- Check if the merchant ID and callback URL need to match the registered domain
- May need a test merchant ID from TKH or PayTabs sandbox credentials

---

## 6. PG-Specific Data Not Saved to Database on Final Submission

**Status:** Open — major feature gap, needs design discussion  
**Severity:** Critical — 26 PG fields are lost on submission  
**Location:** `APIPG/SelfServiceAPI/Controllers/SubmitController.cs` (lines 484-561) and `APIPG/SelfServiceAPI/Classes/ApplicationInfo.cs` (PostgraduateInfo class, line 286)

**Problem:**  
The `PostgraduateInfo` class holds 26 PG-specific fields that are saved correctly in the `IncompleteApplication` XML blob (draft saves work). However, when the application is **submitted** via `SubmitIncompleteApplication`, the `PostgraduateInfo` data is only read for attachment validation checks (lines 106-156). **None of the PG fields are written to any final database table.**

**PG fields NOT saved on submit:**
- MilitaryStatus
- MaritalStatus
- EmploymentStatus, EmployerCompanyName, EmployerPosition, EmployerStartDate, EmployerDuties, EmployerIdNumber
- PgEmergencyContactRelationship, PgEmergencyContactGivenName, PgEmergencyContactMiddleName, PgEmergencyContactFamilyName, PgEmergencyContactMobile, PgEmergencyContactEmail
- CovAlumni, CoventryId
- BachelorTaughtInEnglish
- PgBachelorUniversity, PgBachelorUniversityName, PgBachelorDegree, PgBachelorFieldOfStudy, PgBachelorYearOfGrad
- PgHasAcademicAward, PgAcademicAwards
- HasProfExam, PgProfExamsData
- PgSchoolName

**What IS saved (from the UG-oriented flow):**
- Application record (personal info, demographics, government IDs) → `Application` table
- Phone → `ApplicationPhone`
- Address → `ApplicationAddress`
- Sources → `ApplicationSource`
- Test scores (English proficiency) → `ApplicationTestScore`
- Program of study → `ApplicationProgram`
- High school education → `ApplicationEducation` + `ApplicationEducationEnrollment`
- Arabic names + Disabilities → `ApplicationUserDefined` (only 4 rows)
- Attachments → `ApplicationAttachment`

**What's NOT saved (UG methods exist but PG data doesn't map to them):**
- `InsertApplicationEmployment` reads from `applicationInfo.Employment` (UG format: list of EmploymentInfo with EmployerName, Position as int FK, StartDate, EndDate) — PG employment uses flat strings in PostgraduateInfo (EmployerCompanyName, EmployerPosition as free text, etc.)
- `InsertApplicationRelationShip` reads from `applicationInfo.ApplicationRelations` (UG parent/guardian format) — PG emergency contact uses flat fields in PostgraduateInfo
- Bachelor details, academic awards, prof exams, military/marital status have no insert methods at all

**Fix approaches:**

**Option A: Store as ApplicationUserDefined rows**
- Simplest — no schema changes needed
- Add each PG field as a new `ApplicationUserDefinedInfo` row in the UserDefined list before submission
- Each row has ColumnName (e.g. "PG_MILITARY_STATUS"), ColumnValue, ColumnType, ColumnLabel, IsUploading, Description
- Downside: data is flat key-value, harder to query/report on

**Option B: New PG-specific tables + stored procedures**
- Cleanest — proper schema for PG data
- New tables: `ApplicationPostgraduate`, `ApplicationPGEmergencyContact`, `ApplicationPGEducation`, `ApplicationPGProfExam`
- New SPs: `spInsApplicationPostgraduate`, etc.
- New insert methods in Submit.cs
- Downside: requires DB schema changes on all environments (dev, test, prod)

**Option C: Map PG data to existing UG tables where possible**
- Map PG employment → `InsertApplicationEmployment` (need adapter for field name differences)
- Map PG emergency contact → `InsertApplicationRelationShip` (as a guardian-type relation)
- Store remaining PG-only fields (military, marital, bachelor, awards, prof exams) as ApplicationUserDefined
- Middle ground between A and B

**Decision needed from client/team:**
- Which approach fits the PowerCampus reporting requirements?
- Are there existing PowerCampus tables that expect PG data in a specific format?
- Does the admissions team need to query individual PG fields (favors Option B) or is the XML blob sufficient for review (favors Option A)?

---

## 7. Professional Examinations — GRE Validation Not Working

**Status:** Open — not yet investigated  
**Severity:** Medium — users can proceed with incomplete professional exam data  
**Location:** Frontend — likely `conditionCheck()` in `Main.js` and/or `PGProfessionalExams.js`

**Problem:**  
When "Has Professional Examinations" is set to "Yes" and GRE is selected, the validation does not enforce required fields. The user can proceed to the next page without filling in the GRE score, date, or other required fields.

**To investigate:**
- Check `conditionCheck()` in Main.js for professional exams validation (similar to how academic awards validation was missing)
- Check `PGProfessionalExams.js` for how exam fields are published back to Main via `collectState`
- Check if the issue is missing validation, wrong field names, or the `hasProfExam` flag not being read correctly
- Compare with the academic awards validation pattern (Issue fixed earlier — indexed error keys like `awardType_0`)
