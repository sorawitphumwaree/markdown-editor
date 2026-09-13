# Testing strategy

Automated verification protects user behavior at four complementary levels.
The complete release gate is:

```powershell
./scripts/verify.ps1
```

## Test layers

| Layer | Scope | Location | Runner |
| --- | --- | --- | --- |
| Unit | Domain rules, application policies, shortcuts, and rendering helpers | `tests/MarkdownEditor.Domain.Tests`, `tests/MarkdownEditor.Application.Tests`, `src/MarkdownEditor.Web/src/*.test.ts` | xUnit, Vitest |
| Component | WPF view-model behavior without opening windows | `tests/MarkdownEditor.App.Tests` | xUnit |
| Integration | Filesystem persistence, settings, recovery, path safety, and the browser/native message contract | `tests/MarkdownEditor.Infrastructure.Tests`, `src/MarkdownEditor.Web/tests/browser` | xUnit, Playwright with Edge |
| System | Production web build, portable ZIP contents, and packaged desktop startup | `scripts/smoke-package.ps1` | PowerShell on Windows |

Pull requests and pushes run all four layers on `windows-latest`. The manual
release workflow uses the same verification command before it uploads an
artifact, so a release cannot bypass the regression suite.

## What remains manual

Automation cannot reliably judge visual quality. Manual acceptance remains for
pixel-level WebView2 rendering, long-document PDF pagination, native file-picker
and confirmation-dialog usability, drag feel, accessibility, and behavior on
representative Windows display scaling. When a manual check finds a defect, add
the narrowest deterministic regression test possible before fixing it.

## Adding tests

- Put pure business behavior in unit tests and avoid filesystem or UI setup.
- Test view-model behavior through public commands and observable state.
- Use temporary directories for real persistence integration tests.
- Use Playwright when behavior crosses the browser/native message boundary.
- Extend the package smoke test only with deterministic startup or artifact
  invariants; do not assert timing-sensitive UI appearance.
- Tests must be isolated, deterministic, and independent of external services.
