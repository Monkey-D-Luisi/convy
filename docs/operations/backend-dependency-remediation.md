# Backend dependency correction

This correction is independent of the GPT-6 Luna migration and changes no provider configuration or accounting behavior.

| Advisory | Previous dependency path | Supported parent correction |
| --- | --- | --- |
| [GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc) | Microsoft.AspNetCore.OpenApi 10.0.5 → Microsoft.OpenApi 2.0.0 | Microsoft.AspNetCore.OpenApi 10.0.12 requires Microsoft.OpenApi `[2.12.0, 3.0.0)`. The project stays on .NET 10. |
| [GHSA-mggc-4xg6-vcxf](https://github.com/advisories/GHSA-mggc-4xg6-vcxf), [GHSA-q939-rpr3-3284](https://github.com/advisories/GHSA-q939-rpr3-3284) | Testcontainers.PostgreSql 4.11.0 → Testcontainers 4.11.0 → SSH.NET 2025.1.0 | Testcontainers.PostgreSql 4.15.0 → Testcontainers 4.15.0 → SSH.NET 2026.0.0. No direct transitive override is necessary. |

OpenAPI is used for generated API documentation. Testcontainers and SSH.NET are test-only. Both vulnerable versions are removed even though no untrusted OpenAPI reader or SCP client call was found in the application's execution path. NuGet warnings remain errors; no NU1903 suppression, audit reduction or framework major upgrade is introduced.

Validation: restore the solution, scan all transitive packages, build Release and run the entire backend solution including real PostgreSQL integration tests. Review the resolved NuGet graph and current advisory feed, rather than relying only on the absence of compiler errors. These projects do not enable package lock files; generated obj assets remain untracked build metadata.
