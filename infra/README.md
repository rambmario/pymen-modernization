# Azure infrastructure (Bicep)

Subscription-scoped templates for the Pymen demo environment (ADR 0009).

| File | Role |
|------|------|
| `main.bicep` | Resource group + modules + budget |
| `main.bicepparam` | Non-secret defaults (pass Entra admin + JWT + budget email at deploy time) |
| `modules/*` | Identity (API+worker), monitoring, storage, Service Bus, SQL Entra-only, Key Vault (JWT), Container Apps, Static Web Apps, budget |
| `scripts/create-sql-mi-users.sql` | Post-deploy: `CREATE USER … FROM EXTERNAL PROVIDER` for both MIs |

**Do not commit real JWT keys, Entra IDs of unrelated people, or connection strings.**

There is **no** SQL password parameter. Service Bus and Storage do **not** use `listKeys` / shared keys in these templates.

Human setup steps (subscription, `az login`, deploy, SQL MI users): [docs/azure-setup.md](../docs/azure-setup.md).
