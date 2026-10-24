# Azure infrastructure (Bicep)

Subscription-scoped templates for the Pymen demo environment (ADR 0009).

| File | Role |
|------|------|
| `main.bicep` | Resource group + modules + budget |
| `main.bicepparam` | Non-secret defaults (pass secrets at deploy time) |
| `modules/*` | Identity, monitoring, storage, Service Bus, SQL, Key Vault, Container Apps, Static Web Apps, budget |

**Do not commit real passwords, JWT keys, or connection strings.**

Human setup steps (subscription, `az login`, deploy): [docs/azure-setup.md](../docs/azure-setup.md).
