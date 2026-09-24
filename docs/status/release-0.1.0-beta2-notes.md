# OmsiLaunch 0.1.0-beta2 Notes

This local build separates normal product sessions from explicit validation
policies. Normal `/new`, `/saved`, `/last`, and `/spec` sessions remain active
until OMSI exits naturally or a client sends `session stop`. `/observe-seconds:N`
is the explicit opt-in timed validation policy.

The package contains no Debug-runtime fallback. Product plugin artifacts are
loaded only from the permanent `plugins\OmsiLaunch.*` installation closure.
