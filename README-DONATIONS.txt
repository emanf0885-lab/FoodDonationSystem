FoodShare - Donation feature update

This update adds a persistent Donations table while preserving the existing Users table.
Donors can add food donations and view their own donation records. Receiver accounts can browse available, non-expired donations.

API routes added:
POST /api/donations (Donor role only)
GET  /api/donations/mine (Donor role only)
GET  /api/donations/available (Receiver role only)

Database note:
The API creates the Donations table with CREATE TABLE IF NOT EXISTS. Existing accounts are not deleted. Keep a backup of foodshare.db before updating.

Run from PowerShell in the solution folder:
1. dotnet build FoodDonationSystem.slnx
2. dotnet run --project FoodDonation.Api
3. In a second terminal: dotnet run --project FoodDonation.Client

Use the API on http://localhost:5233. This is a local development setup, not a public production deployment.
