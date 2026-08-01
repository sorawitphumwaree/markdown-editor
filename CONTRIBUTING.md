# Contributing

Use a feature branch, keep dependencies pointing inward, and add focused tests.

Before opening a pull request:

```powershell
./scripts/test.ps1
./scripts/build-app.ps1
```

The test script runs .NET tests, TypeScript checking, and Vitest through
Corepack. New domain rules belong in `MarkdownEditor.Domain`; use-case
coordination belongs in `MarkdownEditor.Application`; filesystem and persistence
implementations belong in `MarkdownEditor.Infrastructure`; WPF and WebView2 code
remain presentation adapters.

Use Conventional Commits and describe observable behavior changes in the pull
request.
