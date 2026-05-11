# Browser extension

This Manifest V3 extension tracks only active tab metadata:

- URL
- title
- start timestamp
- end timestamp

It does not scrape page content, does not read form fields, and does not transmit data outside the local computer.

## Manual install for testing

1. Open `chrome://extensions` or `edge://extensions`.
2. Enable developer mode.
3. Click `Load unpacked`.
4. Select this `browser-extension` folder.

## Enterprise deployment

For production, package and deploy via Chrome/Edge enterprise extension policies. The local WinForms app must be running and listening on `127.0.0.1:39877`.
