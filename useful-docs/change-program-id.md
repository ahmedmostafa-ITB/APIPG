# How to Change the Program ID

When the application form needs to target a different program (e.g. switching from Graduate to Postgraduate), update the following locations.

## Step 1: Identify the new ProgramId

Query the database:
```sql
SELECT CODE_VALUE, SHORT_DESC, LONG_DESC, ProgramId FROM CODE_PROGRAM
```

Current mapping:
| CODE_VALUE | LONG_DESC | ProgramId |
|---|---|---|
| UG | Undergraduate | 1 |
| PG | Postgraduate | 2 |
| GR | Graduate | 3 |

## Step 2: Update the API

### Web.config
```xml
<add key="GraduateProgram" value="NEW_PROGRAM_ID" />
```

### AcademicInterestController.cs
Two locations with hardcoded ProgramId:

1. **GetPGSchools** — SQL query:
```csharp
WHERE pos.Program = NEW_PROGRAM_ID
```

2. **GetPGMajorsBySchool** — getMajorList call:
```csharp
var allMajors = entities.getMajorList(NEW_PROGRAM_ID, populationId, ...);
```

## Step 3: Update the Frontend

### PGAcademicInterest.js
Three locations:

1. **Constructor** default state:
```javascript
selectedProgram: NEW_PROGRAM_ID,
```

2. **publish()** method:
```javascript
selectedProgram: NEW_PROGRAM_ID,
```

3. **GetMajors API call**:
```javascript
/api/AcademicInterest/GetMajors?programId=NEW_PROGRAM_ID&...
```

4. **defaultProps**:
```javascript
selectedProgram: NEW_PROGRAM_ID,
```

### Main.js
**academicInterestCheck()** — isPG flag:
```javascript
const isPG = ai.selectedProgram === NEW_PROGRAM_ID || ai.selectedProgram === 'NEW_PROGRAM_ID'
```

## Step 4: Verify Database Setup

Ensure the programs are linked to the correct ApplicationFormSettingId:
```sql
-- Check programs linked to the form setting
SELECT pos.ProgramOfStudyId, pos.Program, pos.Degree, pos.Curriculum, pos.PopulationId
FROM PROGRAMOFSTUDY pos
JOIN ApplicationProgramSetting aps ON pos.ProgramOfStudyId = aps.ProgramOfStudyId
WHERE aps.ApplicationFormSettingId = YOUR_SETTING_ID
  AND pos.Program = NEW_PROGRAM_ID
```

If empty, link the programs:
```sql
INSERT INTO ApplicationProgramSetting (ApplicationFormSettingId, ProgramOfStudyId)
SELECT YOUR_SETTING_ID, ProgramOfStudyId
FROM PROGRAMOFSTUDY
WHERE Program = NEW_PROGRAM_ID
```

## Step 5: Rebuild and Deploy

1. Publish API from Visual Studio
2. Copy Web.config and System.Net.Http.dll
3. Rebuild frontend: `npm run build`
4. Deploy both to IIS
5. Test: verify Program of Study dropdown shows correct majors
