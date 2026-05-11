# Product ideas

## Near-term improvements

- **Reports UI**: add a protected Reports tab with date selection, report generation, and open report folder actions.
- **Editable settings**: allow changing screenshot folder, screenshot interval, retention days, browser receiver port, and local access password from the protected UI.
- **Browser plugin status**: implement a silent heartbeat endpoint and show `active`, `limited`, or `not detected` status only inside the protected UI.
- **Retention cleanup**: delete old screenshots and old activity rows according to configured retention periods.
- **Screenshot review**: add thumbnails, open screenshot, open containing folder, and date filters.

## Data model improvements

- **Windows user**: add `windows_user` to activity and screenshot rows.
- **Browser source**: add `source` to browser rows with values such as `plugin` and `fallback_window_title`.
- **Domain extraction**: store `domain` separately from full URL for filtering and reporting.
- **App state**: add key/value state for last heartbeat, last cleanup, and diagnostic timestamps.

## UX improvements

- **Human-readable dashboard**: show cards such as application sessions, last browser event, screenshot status, and current tracking health.
- **Filters/search**: add date range, process, browser/domain, idle, and text search filters.
- **Diagnostics panel**: show local paths, database size, last errors, receiver port status, and extension status.

## Production hardening

- **Code signing**: sign the published executable before rollout.
- **Installer/package**: package the app and config for per-user deployment.
- **Enterprise extension packaging**: deploy Chrome/Edge extension through managed policies.
- **Antivirus validation**: test behavior on representative endpoints.
- **Policy documentation**: provide employee notice, retention policy, and administrator procedures.
