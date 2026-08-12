# Contributing

Use a feature branch, keep dependencies pointing inward, and add focused tests.

Before opening a pull request:

```powershell
./scripts/verify.ps1
```

The verification script runs .NET unit/component/integration tests, TypeScript
checking, Vitest, Playwright browser workflows, release packaging, archive
integrity checks, and a packaged desktop startup smoke test. For a faster inner
loop, use `./scripts/test.ps1`; see `docs/testing-strategy.md` for test ownership
and scope. New domain rules belong in `MarkdownEditor.Domain`; use-case
coordination belongs in `MarkdownEditor.Application`; filesystem and persistence
implementations belong in `MarkdownEditor.Infrastructure`; WPF and WebView2 code
remain presentation adapters.

Use Conventional Commits and describe observable behavior changes in the pull
request.
