# Fix Bug

Standard workflow for fixing and documenting bugs in this project.

## Steps

1. **Investigate**: Read the relevant code, trace the error from console/logs to the source.
2. **Confirm with user**: Describe the root cause and proposed fix before changing code.
3. **Fix**: Make the minimal change needed. Preserve backward compatibility with existing data when changing formats.
4. **Test**: Rebuild and deploy to local IIS:
   ```bash
   rm -rf build/
   NODE_OPTIONS=--openssl-legacy-provider npm run build
   cp -r build/* C:/inetpub/TKH/PGApplicationForm/
   ```
   For API changes, re-publish from Visual Studio then copy Web.config.
5. **Commit**: One bug per commit. Use this format:
   ```
   Fix <short description of what broke>

   Problem:
   <What was happening and why — include the error message if relevant>

   Fix:
   <What was changed and why this resolves it>
   ```
6. **GitHub Issue** (when access is available): Create an issue per bug, link to commit:
   ```bash
   gh issue create --repo ahmedmostafa-ITB/ApplicationFormPG \
     --title "Bug: <short title>" \
     --body "$(cat <<'EOF'
   ## Problem
   <description>

   ## Root Cause
   <code location and why it fails>

   ## Fix
   <what was changed>

   Resolved in commit <hash>
   EOF
   )"
   ```
   Then close it: `gh issue close <number>`
