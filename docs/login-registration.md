# In-client test registration

The login dialog now has Login and Register tabs. The active tab is disabled. Registration has username, email, password and confirmation fields, plus Submit and Cancel. Cancel returns to Login; successful registration keeps the username and clears both passwords. Passwords are not saved by registration.

While Register is open, the game-server connection is deliberately suspended so its unauthenticated login timer cannot expire. The form stays open through late disconnect/error events. Returning to Login reconnects through the connect server and requests the previously selected game server. Registration HTTP failures stay on the form and can be retried.

## VPS setup (not deployed)

1. Deploy the updated OpenMU server build containing the AdminPanel API and middleware changes. The existing administrator-only `POST /api/accounts` remains protected.
2. Serve the API over HTTPS with a valid certificate. A website registration page is not required. If TLS terminates at a reverse proxy, configure trusted forwarded headers so ASP.NET sees the original HTTPS scheme and client IP. Only trust your proxy addresses.
3. In the server host configuration, enable `TestRegistration:Enabled` (or set the environment variable `TestRegistration__Enabled=true`) and restart the host. Without this setting the endpoint returns 404.
4. In the client's `MuOnlineSettings` configuration, set `RegistrationUrl` to the full address, for example `https://YOUR-HOST/api/accounts/test-registration`. Leave it empty until HTTPS is ready. For desktop local configuration this can go in `appsettings.local.json` beside the running build; for mobile set the bundled client configuration before building.
5. Create a disposable test account in the client, then log in with it. No administrator API key belongs in the client.

Example client override:

```json
{
  "MuOnlineSettings": {
    "RegistrationUrl": "https://YOUR-HOST/api/accounts/test-registration"
  }
}
```

The test endpoint permits five requests per IP per hour and queues none. Duplicate usernames return 409; invalid fields return 400; rate limits return 429. Usernames are 3–10 printable ASCII characters without spaces; passwords are 3–20 printable ASCII characters. Email is required. The server stores a BCrypt password hash. The normal game connection and this HTTPS endpoint use different ports/protocols; the connect-server address cannot be used as the registration URL.

Disable `TestRegistration:Enabled` when direct test registration is no longer wanted. Deployment, certificate setup and live VPS account creation remain pending.

## Windows VPS batch setup

The portable RegistrationSetup package now contains Setup-Registration.bat, its PowerShell installer, a tested AdminPanel patch, and the official Caddy Windows x64 executable. Copy the whole extracted package to the VPS and run the batch as administrator. See deploy/windows-registration/README.txt in the server source for details. The installer keeps IIS on port 8088 and adds a registration-only HTTPS gateway on TCP 443 to the local OpenMU API on port 5000. It refuses to take over an occupied port 443.

The gateway uses Let's Encrypt public IP certificates (shortlived profile, TLS-ALPN on 443) and runs as an automatically restarted startup task for continuous renewal. No website registration page or administrator API key is needed. Open inbound TCP 443 in the VPS provider firewall; the installer adds the Windows rule. Stop/start OpenMU when prompted so the DLL patch loads. Server configuration and DLL are backed up under C:/ProgramData/TwinMURegistration/backups.

TestRegistration.TrustLoopbackProxy is opt-in and accepts X-Forwarded-Proto and X-Forwarded-For only from 127.0.0.1 or ::1 with a single proxy hop. Public callers cannot mark HTTP requests as HTTPS by supplying those headers. Only /api/accounts/test-registration is exposed by the new gateway. The installer performs normal certificate validation and expects GET/405; live account creation must be checked from the client afterwards.

Local validation: all 23 account/proxy tests passed; Windows PowerShell 5.1 plan/configuration-preservation/idempotence/invalid-input checks and official Caddy configuration adaptation passed. No firewall/task/gateway was installed on the development PC. VPS certificate issuance and live registration are pending execution there.
