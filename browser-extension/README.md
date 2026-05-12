# Browser extension

This Manifest V3 extension tracks only active tab metadata:

- URL
- title
- start timestamp
- end timestamp

It does not scrape page content, does not read form fields, and does not transmit data outside the local computer.

## Structure

- `chromium/` — Chrome, Edge, Brave, Opera (Chromium-based)
- `firefox/` — Firefox (Gecko-based)

## Manual install for testing

**Chrome / Edge / Brave / Opera:**

1. Open `chrome://extensions` or `edge://extensions`.
2. Enable developer mode.
3. Click `Load unpacked`.
4. Select the `chromium` folder.

**Firefox:**

1. Open `about:debugging#/runtime/this-firefox`.
2. Click `Load Temporary Add-on`.
3. Select `manifest.json` from the `firefox` folder.

## Enterprise deployment

For production, package and deploy via Chrome/Edge enterprise extension policies. The local WinForms app must be running and listening on `127.0.0.1:39877`.
