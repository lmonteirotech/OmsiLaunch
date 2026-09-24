# Windows Session Indicator

> Superseded by [Windows Tray](../reference/windows-tray.md). This page is kept
> only so that existing links keep resolving; it is not normative.

Summary of the current behaviour, documented in full on the new page:

- A standalone session owner shows a localized Notification Area indicator from
  `StartSessionAsync` acceptance until the owner finishes, with `Status`
  (read-only) and `End session` actions.
- `End session` requests the canonical stop: OMSI is terminated (forced) and
  session-owned files are restored exactly; closing Status never stops OMSI.
- `SessionPresentationSpec.SuppressTrayIcon` (API only, default `false`) hides
  the indicator without disabling diagnostics, recovery or local control.
- Tray behaviour under Explorer restarts and `OmsiLaunchW.exe` failure dialogs
  still require runtime validation.
