# Telegram Registration Development Configuration Design

## Goal

Enable Telegram-assisted owner registration in development with valid, environment-specific cryptographic material and a completion URL that follows the active Cloudflare Quick Tunnel.

## Configuration

- Set `Telegram__RegistrationEnabled=true` in the ignored `.env` file.
- Set `Telegram__RegistrationCompletionUrl` to the current `Telegram__PublicWebhookUrl` plus `/telegram/registration/complete`.
- Generate independent random 32-byte Base64 peppers for appointment and contact verification.
- Preserve the seeded Agent catalog IDs and the existing JWT key ID because they are already valid.
- Leave Twilio credentials empty while `Twilio__Enabled=false`.

## Tunnel lifecycle

Extend `scripts/start-telegram-cloudflare-tunnel.ps1` so every new Quick Tunnel URL updates both `Telegram__PublicWebhookUrl` and `Telegram__RegistrationCompletionUrl`. This prevents registration links from retaining an expired tunnel hostname.

## Verification

Verify the script behavior without exposing values, confirm `.env` remains ignored, run targeted tests, and start the API long enough to prove startup option validation succeeds.

The API must load `.env` only outside the `Testing` host environment. Test factories already provide isolated environment variables; allowing the developer `.env` to leak into them makes their outcome depend on local feature flags.
