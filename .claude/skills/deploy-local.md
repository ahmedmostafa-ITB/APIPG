# Deploy Local

Rebuild and deploy frontend and/or API to local IIS (TKH site on port 8010).

## Frontend

```bash
# 1. Delete old build (CRA caches aggressively)
rm -rf ApplicationFormPG/build/

# 2. Rebuild (NODE_OPTIONS required for Node 17+)
cd ApplicationFormPG
NODE_OPTIONS=--openssl-legacy-provider npm run build

# 3. Copy to IIS
cp -r build/* C:/inetpub/TKH/PGApplicationForm/

# 4. Hard-refresh browser (Ctrl+Shift+R)
```

## API

1. Open `APIPG/SelfServiceAPI.sln` in Visual Studio
2. Right-click SelfServiceAPI → Publish → target `C:\inetpub\TKH\APITest`
3. After publish, copy local Web.config:
   ```bash
   cp APIPG/SelfServiceAPI/Web.config C:/inetpub/TKH/APITest/Web.config
   ```
4. Copy System.Net.Http.dll (not included by publish):
   ```bash
   cp APIPG/packages/System.Net.Http.4.3.4/lib/net46/System.Net.Http.dll C:/inetpub/TKH/APITest/bin/
   ```
5. Restart app pool (elevated PowerShell): `Restart-WebAppPool -Name "TKH"`

## URLs

- IIS Frontend: http://localhost:8010/PGApplicationForm
- IIS API: http://localhost:8010/APITest
- Dev Frontend (hot reload): `npm start` → http://localhost:3000/PGApplicationForm
