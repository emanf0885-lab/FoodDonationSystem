FOODSHARE STAGE 3 — IMAGE LOADING FIX

This package keeps the existing Form1.cs dashboard and workflows, and fixes image lookup so photos can load from either the application output folder or the project's Assets folder.

FILES
- Form1.cs
- Assets\foodshare_hero_photo.png
- Assets\foodshare_food_photo.png

INSTALL
1. Back up your current FoodDonation.Client\Form1.cs.
2. Replace FoodDonation.Client\Form1.cs with this package's Form1.cs.
3. Copy the included Assets folder into FoodDonation.Client\Assets. Ensure the two PNG files are physically present there.
4. Open FoodDonation.Client.csproj and add this ItemGroup immediately before </Project> (if it is not already there):

<ItemGroup>
  <Content Include="Assets\*.png">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>

5. Save the project file, then from the solution folder run:
   dotnet clean FoodDonationSystem.slnx
   dotnet build FoodDonationSystem.slnx
6. Run the app again. If it was already running, close it first.

IMPORTANT
- Do not replace Program.cs, API files, database files, or Form1.Designer.cs.
- This patch improves image file discovery and keeps the existing dashboard code. It does not change API endpoints or database logic.
- The image corners are rounded by the existing ApplyRoundedCorners helper. If the image area is still blank, check that both PNG files are in FoodDonation.Client\Assets and send the build output plus a screenshot.
