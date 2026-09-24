# Local Control Protocol

> Superseded by [Local Control / IPC](../reference/local-control.md). This page
> is kept only so that existing links keep resolving; it is not normative.

Summary of the current behaviour, documented in full on the new page:

- Every normal launch owns one session and exposes one named pipe per
  installation, `OmsiLaunch.Control.0.1.<hash>`, current-user only, protocol
  version `0.1`, length-prefixed JSON bounded to 64 KiB.
- `session.stop` and `runtime.execute` must carry the `session_id` of the
  active session; `session.status` and `session.events` are read-only.
- Without an active owner, clients report `OL_E_NO_ACTIVE_SESSION` and exit
  with code `4`; a client never starts OMSI.
- Handler faults are answered as typed errors, never as a dropped connection.
