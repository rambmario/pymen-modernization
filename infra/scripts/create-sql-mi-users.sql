/*
  Post-deploy: map Container Apps managed identities as Azure SQL users (Entra-only).

  Prerequisites:
  - Connected as the Entra admin set in Bicep (sqlEntraAdminLogin / ObjectId).
  - Database: Pymen
  - Replace identity display names with deployment outputs:
      apiManagedIdentityName    (e.g. id-pymen-demo-api)
      workerManagedIdentityName (e.g. id-pymen-demo-worker)

  Example (Azure Data Studio / sqlcmd with Entra auth):
    sqlcmd -S sql-….database.windows.net -d Pymen -G -Q "…"
*/

-- API identity: read/write + DDL if the API runs EF migrations at startup.
CREATE USER [id-pymen-demo-api] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [id-pymen-demo-api];
ALTER ROLE db_datawriter ADD MEMBER [id-pymen-demo-api];
ALTER ROLE db_ddladmin ADD MEMBER [id-pymen-demo-api];

-- Worker identity: read/write only (ProcessedMessages, delivery-note reads).
CREATE USER [id-pymen-demo-worker] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [id-pymen-demo-worker];
ALTER ROLE db_datawriter ADD MEMBER [id-pymen-demo-worker];
GO
