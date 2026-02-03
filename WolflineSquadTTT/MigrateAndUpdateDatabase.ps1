# AutoMigration.ps1
$envArg = "Development"  # default

if ($args.Length -gt 0) {
    # Accept --prod, --production, or any custom arg
    switch ($args[0].ToLower()) {
        "--prod" { $envArg = "Production" }
        "--production" { $envArg = "Production" }
        default { $envArg = $args[0] }  # allow custom environment name
    }
}

Write-Host "Target environment: $envArg"

# Generate a timestamped migration name
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$migrationName = "AutoMigration_$timestamp"

Write-Host "Creating migration: $migrationName"

# Create the migration
dotnet ef migrations add $migrationName

# Update the database
Write-Host "Updating database..."
dotnet ef database update -- --environment $envArg

Write-Host "Done!"
