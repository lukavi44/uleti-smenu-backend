# Fill in your Stripe TEST values, then run from repo root or any folder.
# Keys are stored in dotnet user-secrets (not committed).

$ApiProject = Join-Path $PSScriptRoot "..\API\API.csproj" | Resolve-Path

$StripeEnabled = "true"
$StripeSecretKey = "sk_test_REPLACE_ME"
$StripeWebhookSecret = "whsec_REPLACE_ME"   # from: stripe listen --forward-to http://localhost:5201/api/v1/Billing/webhooks/stripe
$BasicCreditPackPriceId = "price_REPLACE_ME"
$ProMonthlyPriceId = "price_REPLACE_ME"

if ($StripeSecretKey -like "*REPLACE*") {
    Write-Host "Edit scripts/set-stripe-dev-secrets.ps1 with your Stripe test keys first." -ForegroundColor Yellow
    exit 1
}

dotnet user-secrets set "Stripe:Enabled" $StripeEnabled --project $ApiProject
dotnet user-secrets set "Stripe:SecretKey" $StripeSecretKey --project $ApiProject
dotnet user-secrets set "Stripe:WebhookSecret" $StripeWebhookSecret --project $ApiProject
dotnet user-secrets set "Stripe:PriceIds:BasicCreditPack" $BasicCreditPackPriceId --project $ApiProject
dotnet user-secrets set "Stripe:PriceIds:ProMonthly" $ProMonthlyPriceId --project $ApiProject

Write-Host "Stripe user secrets saved. Restart the API." -ForegroundColor Green
dotnet user-secrets list --project $ApiProject
