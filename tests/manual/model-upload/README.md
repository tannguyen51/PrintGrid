# Manual test fixtures — model upload

Local trial: model upload, admin sample upload and quote slicing use the same
`ModelUpload:MaxFileBytes` setting (Docker: `MODEL_UPLOAD_MAX_FILE_BYTES`), default
104857600 bytes (100 MiB). Nginx and API request limits are 120 MiB to leave room
for multipart fields. Account storage quotas still apply.

Boundary checks: accept valid files at 51 MiB and exactly 100 MiB; reject
104857601 bytes. Retry quote calculation for an already uploaded file above
50 MiB: it must reach mesh/profile processing instead of failing the old 50 MB
size gate. The separate 2097150-face mesh limit and slicing timeout still apply;
passing the size check alone does not guarantee a successful slice.

- `valid-cube.obj`: valid 10 mm watertight cube; upload must succeed.
- `invalid-content.stl`: renamed/plain-text content; upload must fail with HTTP 415 and must not consume quota.

To exercise quota quickly, set `MODEL_UPLOAD_MAX_MODELS=2` in the root `.env`, recreate the API container,
then upload two valid files. The third upload must fail with `quota_exceeded`. Restore the value to `20`
and recreate the API after the test.
