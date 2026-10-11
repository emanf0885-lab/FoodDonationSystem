
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is missing from configuration.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT key must contain at least 32 bytes.");
}

builder.Services.AddDbContext<FoodShareDbContext>(options =>
    options.UseSqlite("Data Source=foodshare.db"));

builder.Services.AddScoped<IPasswordHasher<FoodShareUser>, PasswordHasher<FoodShareUser>>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider
        .GetRequiredService<FoodShareDbContext>();

    await database.Database.EnsureCreatedAsync();

    // EnsureCreated does not add tables to an existing database, so create
    // the new Donations table separately without touching existing accounts.
    await database.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS Donations (
            Id INTEGER NOT NULL CONSTRAINT PK_Donations PRIMARY KEY AUTOINCREMENT,
            DonorUserId INTEGER NOT NULL,
            FoodName TEXT NOT NULL,
            Category TEXT NOT NULL,
            Quantity TEXT NOT NULL,
            PickupLocation TEXT NOT NULL,
            ExpiryDateUtc TEXT NOT NULL,
            Notes TEXT NOT NULL DEFAULT '',
            Status TEXT NOT NULL DEFAULT 'Available',
            CreatedAtUtc TEXT NOT NULL,
            CONSTRAINT FK_Donations_Users_DonorUserId
                FOREIGN KEY (DonorUserId) REFERENCES Users (Id) ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS IX_Donations_DonorUserId ON Donations (DonorUserId);
        CREATE INDEX IF NOT EXISTS IX_Donations_Status ON Donations (Status);

        -- Add the new request table without deleting or rebuilding existing data.
        CREATE TABLE IF NOT EXISTS DonationRequests (
            Id INTEGER NOT NULL CONSTRAINT PK_DonationRequests PRIMARY KEY AUTOINCREMENT,
            DonationId INTEGER NOT NULL,
            ReceiverUserId INTEGER NOT NULL,
            Message TEXT NOT NULL DEFAULT '',
            Status TEXT NOT NULL DEFAULT 'Pending',
            CreatedAtUtc TEXT NOT NULL,
            UpdatedAtUtc TEXT NULL,
            CONSTRAINT FK_DonationRequests_Donations_DonationId
                FOREIGN KEY (DonationId) REFERENCES Donations (Id) ON DELETE RESTRICT,
            CONSTRAINT FK_DonationRequests_Users_ReceiverUserId
                FOREIGN KEY (ReceiverUserId) REFERENCES Users (Id) ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS IX_DonationRequests_DonationId ON DonationRequests (DonationId);
        CREATE INDEX IF NOT EXISTS IX_DonationRequests_ReceiverUserId ON DonationRequests (ReceiverUserId);
        CREATE INDEX IF NOT EXISTS IX_DonationRequests_Status ON DonationRequests (Status);
        CREATE UNIQUE INDEX IF NOT EXISTS IX_DonationRequests_ActiveReceiverPerDonation
            ON DonationRequests (DonationId, ReceiverUserId)
            WHERE Status IN ('Pending', 'Approved');
        """);
}

app.MapGet("/", () => Results.Ok(new
{
    application = "FoodShare API",
    status = "Running",
    message = "FoodShare backend is ready."
}));

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "Healthy"
}));

// REGISTER A DONOR OR RECEIVER

app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    FoodShareDbContext database,
    IPasswordHasher<FoodShareUser> passwordHasher) =>
{
    string name = request.FullName?.Trim() ?? "";
    string email = request.Email?.Trim() ?? "";
    string normalizedEmail = email.ToUpperInvariant();
    string phone = request.Phone?.Trim() ?? "";
    string countryCode = request.CountryCode?.Trim() ?? "";
    string role = request.Role?.Trim() ?? "";
    string password = request.Password ?? "";

    if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
    {
        return Results.BadRequest(new
        {
            message = "Enter a valid full name (maximum 100 characters)."
        });
    }

    if (!IsValidEmail(email))
    {
        return Results.BadRequest(new
        {
            message = "Enter a valid email address."
        });
    }

    if (phone.Length < 5 || phone.Length > 20 ||
        !phone.All(char.IsDigit))
    {
        return Results.BadRequest(new
        {
            message = "Phone number must contain 5 to 20 digits."
        });
    }

    if (countryCode.Length < 2 || countryCode.Length > 6 ||
        countryCode[0] != '+' ||
        !countryCode.Skip(1).All(char.IsDigit))
    {
        return Results.BadRequest(new
        {
            message = "Select a valid country calling code."
        });
    }

    if (!role.Equals("Donor", StringComparison.OrdinalIgnoreCase) &&
        !role.Equals("Receiver", StringComparison.OrdinalIgnoreCase) &&
        !role.Equals("Receiver / Charity", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new
        {
            message = "Choose Donor or Receiver / Charity."
        });
    }

    if (!IsStrongPassword(password))
    {
        return Results.BadRequest(new
        {
            message =
                "Password must contain at least 8 characters, " +
                "an uppercase letter, a lowercase letter, a digit, " +
                "and a special character."
        });
    }

    bool emailExists = await database.Users
        .AnyAsync(user => user.NormalizedEmail == normalizedEmail);

    if (emailExists)
    {
        return Results.Conflict(new
        {
            message = "An account with this email already exists."
        });
    }

    string savedRole = role.Equals(
        "Donor", StringComparison.OrdinalIgnoreCase)
        ? "Donor"
        : "Receiver";

    var user = new FoodShareUser
    {
        FullName = name,
        Email = email,
        NormalizedEmail = normalizedEmail,
        Phone = countryCode + phone,
        Role = savedRole,
        CreatedAtUtc = DateTime.UtcNow
    };

    user.PasswordHash = passwordHasher.HashPassword(user, password);

    database.Users.Add(user);
    await database.SaveChangesAsync();

    return Results.Created("/api/auth/login", new
    {
        message = "Your FoodShare account has been created.",
        user = new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.Role
        }
    });
});

// LOG IN TO AN EXISTING ACCOUNT

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    FoodShareDbContext database,
    IPasswordHasher<FoodShareUser> passwordHasher) =>
{
    string email = request.Email?.Trim() ?? "";
    string password = request.Password ?? "";

    if (!IsValidEmail(email) || string.IsNullOrEmpty(password))
    {
        return Results.BadRequest(new
        {
            message = "Enter a valid email address and password."
        });
    }

    string normalizedEmail = email.ToUpperInvariant();

    var user = await database.Users
        .SingleOrDefaultAsync(
            item => item.NormalizedEmail == normalizedEmail);

    if (user == null)
    {
        return Results.Json(
            new { message = "Invalid email or password." },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    var verification = passwordHasher.VerifyHashedPassword(
        user, user.PasswordHash, password);

    if (verification == PasswordVerificationResult.Failed)
    {
        return Results.Json(
            new { message = "Invalid email or password." },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    if (verification == PasswordVerificationResult.SuccessRehashNeeded)
    {
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        await database.SaveChangesAsync();
    }

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

    var signingKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(jwtKey));

    var signingCredentials = new SigningCredentials(
        signingKey, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: builder.Configuration["Jwt:Issuer"],
        audience: builder.Configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(60),
        signingCredentials: signingCredentials);

    string tokenText = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new
    {
        message = "Login successful.",
        token = tokenText,
        expiresAtUtc = token.ValidTo,
        user = new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.Role
        }
    });
});


// CREATE A FOOD DONATION (DONORS ONLY)
app.MapPost("/api/donations", async (
    CreateDonationRequest request,
    HttpContext httpContext,
    FoodShareDbContext database) =>
{
    string? userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!int.TryParse(userIdText, out int donorUserId))
        return Results.Unauthorized();

    string foodName = request.FoodName?.Trim() ?? "";
    string category = request.Category?.Trim() ?? "";
    string quantity = request.Quantity?.Trim() ?? "";
    string pickupLocation = request.PickupLocation?.Trim() ?? "";
    string notes = request.Notes?.Trim() ?? "";

    if (foodName.Length < 2 || foodName.Length > 100)
        return Results.BadRequest(new { message = "Food name must be between 2 and 100 characters." });
    if (category.Length < 2 || category.Length > 50)
        return Results.BadRequest(new { message = "Choose a valid food category." });
    if (quantity.Length < 1 || quantity.Length > 50)
        return Results.BadRequest(new { message = "Enter a quantity, for example 10 meal boxes or 5 kg." });
    if (pickupLocation.Length < 3 || pickupLocation.Length > 200)
        return Results.BadRequest(new { message = "Pickup location must be between 3 and 200 characters." });
    if (!DateTime.TryParseExact(request.ExpiryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
        DateTimeStyles.None, out DateTime parsedExpiryDate))
        return Results.BadRequest(new { message = "Choose a valid expiry date." });
    if (parsedExpiryDate.Date < DateTime.UtcNow.Date)
        return Results.BadRequest(new { message = "Expiry date cannot be in the past." });
    if (notes.Length > 500)
        return Results.BadRequest(new { message = "Notes must be 500 characters or fewer." });

    var donation = new FoodShareDonation
    {
        DonorUserId = donorUserId,
        FoodName = foodName,
        Category = category,
        Quantity = quantity,
        PickupLocation = pickupLocation,
        ExpiryDateUtc = DateTime.SpecifyKind(parsedExpiryDate.Date, DateTimeKind.Utc),
        Notes = notes,
        Status = "Available",
        CreatedAtUtc = DateTime.UtcNow
    };

    database.Donations.Add(donation);
    await database.SaveChangesAsync();

    return Results.Created($"/api/donations/{donation.Id}", new
    {
        message = "Food donation added successfully.",
        donation = new
        {
            donation.Id,
            donation.FoodName,
            donation.Category,
            donation.Quantity,
            donation.PickupLocation,
            donation.ExpiryDateUtc,
            donation.Notes,
            donation.Status,
            donation.CreatedAtUtc
        }
    });
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Donor" });

// SHOW DONATIONS CREATED BY THE SIGNED-IN DONOR
app.MapGet("/api/donations/mine", async (
    HttpContext httpContext,
    FoodShareDbContext database) =>
{
    string? userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!int.TryParse(userIdText, out int donorUserId))
        return Results.Unauthorized();

    var donations = await database.Donations
        .Where(donation => donation.DonorUserId == donorUserId)
        .OrderByDescending(donation => donation.CreatedAtUtc)
        .Select(donation => new
        {
            donation.Id,
            donation.FoodName,
            donation.Category,
            donation.Quantity,
            donation.PickupLocation,
            donation.ExpiryDateUtc,
            donation.Notes,
            donation.Status,
            donation.CreatedAtUtc
        })
        .ToListAsync();

    return Results.Ok(donations);
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Donor" });

// SHOW AVAILABLE DONATIONS TO RECEIVERS / CHARITIES
app.MapGet("/api/donations/available", async (FoodShareDbContext database) =>
{
    var donations = await database.Donations
        .Where(donation => donation.Status == "Available" && donation.ExpiryDateUtc >= DateTime.UtcNow.Date)
        .OrderBy(donation => donation.ExpiryDateUtc)
        .Select(donation => new
        {
            donation.Id,
            donation.FoodName,
            donation.Category,
            donation.Quantity,
            donation.PickupLocation,
            donation.ExpiryDateUtc,
            donation.Notes,
            donation.Status,
            DonorName = donation.Donor.FullName
        })
        .ToListAsync();

    return Results.Ok(donations);
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Receiver" });

// RECEIVER REQUESTS AN AVAILABLE DONATION
app.MapPost("/api/donations/{donationId:int}/requests", async (
    int donationId,
    CreateDonationRequestMessage requestBody,
    HttpContext httpContext,
    FoodShareDbContext database) =>
{
    if (!TryGetCurrentUserId(httpContext, out int receiverUserId))
        return Results.Unauthorized();

    string message = requestBody.Message?.Trim() ?? "";
    if (message.Length > 500)
        return Results.BadRequest(new { message = "Request message must be 500 characters or fewer." });

    var donation = await database.Donations
        .SingleOrDefaultAsync(item => item.Id == donationId);

    if (donation == null)
        return Results.NotFound(new { message = "Donation was not found." });

    if (donation.Status != "Available" || donation.ExpiryDateUtc.Date < DateTime.UtcNow.Date)
        return Results.Conflict(new { message = "This donation is no longer available to request." });

    bool existingActiveRequest = await database.DonationRequests.AnyAsync(item =>
        item.DonationId == donationId &&
        item.ReceiverUserId == receiverUserId &&
        (item.Status == "Pending" || item.Status == "Approved"));

    if (existingActiveRequest)
        return Results.Conflict(new { message = "You already have an active request for this donation." });

    var donationRequest = new FoodShareDonationRequest
    {
        DonationId = donationId,
        ReceiverUserId = receiverUserId,
        Message = message,
        Status = "Pending",
        CreatedAtUtc = DateTime.UtcNow
    };

    database.DonationRequests.Add(donationRequest);
    try
    {
        await database.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        return Results.Conflict(new { message = "An active request for this donation already exists." });
    }

    return Results.Created($"/api/donation-requests/{donationRequest.Id}", new
    {
        message = "Your food request has been sent to the donor.",
        request = new
        {
            donationRequest.Id,
            donationRequest.DonationId,
            donationRequest.ReceiverUserId,
            donationRequest.Message,
            donationRequest.Status,
            donationRequest.CreatedAtUtc
        }
    });
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Receiver" });

// RECEIVER VIEWS ONLY THEIR OWN REQUESTS
app.MapGet("/api/donation-requests/mine", async (
    HttpContext httpContext,
    FoodShareDbContext database) =>
{
    if (!TryGetCurrentUserId(httpContext, out int receiverUserId))
        return Results.Unauthorized();

    var requests = await database.DonationRequests
        .Where(item => item.ReceiverUserId == receiverUserId)
        .OrderByDescending(item => item.CreatedAtUtc)
        .Select(item => new
        {
            item.Id,
            item.DonationId,
            FoodName = item.Donation.FoodName,
            DonorName = item.Donation.Donor.FullName,
            item.Message,
            item.Status,
            item.CreatedAtUtc,
            item.UpdatedAtUtc
        })
        .ToListAsync();

    return Results.Ok(requests);
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Receiver" });

// DONOR VIEWS REQUESTS FOR A DONATION THEY OWN
app.MapGet("/api/donations/{donationId:int}/requests", async (
    int donationId,
    HttpContext httpContext,
    FoodShareDbContext database) =>
{
    if (!TryGetCurrentUserId(httpContext, out int donorUserId))
        return Results.Unauthorized();

    bool ownsDonation = await database.Donations.AnyAsync(item =>
        item.Id == donationId && item.DonorUserId == donorUserId);

    if (!ownsDonation)
        return Results.NotFound(new { message = "Donation was not found for your account." });

    var requests = await database.DonationRequests
        .Where(item => item.DonationId == donationId)
        .OrderByDescending(item => item.CreatedAtUtc)
        .Select(item => new
        {
            item.Id,
            item.DonationId,
            FoodName = item.Donation.FoodName,
            ReceiverName = item.Receiver.FullName,
            item.Message,
            item.Status,
            item.CreatedAtUtc,
            item.UpdatedAtUtc
        })
        .ToListAsync();

    return Results.Ok(requests);
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Donor" });

// DONOR APPROVES A PENDING REQUEST; THE DONATION BECOMES RESERVED
app.MapPost("/api/donation-requests/{requestId:int}/approve", async (
    int requestId,
    HttpContext httpContext,
    FoodShareDbContext database) =>
{
    if (!TryGetCurrentUserId(httpContext, out int donorUserId))
        return Results.Unauthorized();

    var donationRequest = await database.DonationRequests
        .Include(item => item.Donation)
        .SingleOrDefaultAsync(item => item.Id == requestId);

    if (donationRequest == null || donationRequest.Donation.DonorUserId != donorUserId)
        return Results.NotFound(new { message = "Request was not found for your donations." });

    if (donationRequest.Status != "Pending")
        return Results.Conflict(new { message = "Only pending requests can be approved." });

    if (donationRequest.Donation.Status != "Available" ||
        donationRequest.Donation.ExpiryDateUtc.Date < DateTime.UtcNow.Date)
        return Results.Conflict(new { message = "This donation is no longer available." });

    var otherPendingRequests = await database.DonationRequests
        .Where(item => item.DonationId == donationRequest.DonationId &&
                       item.Id != donationRequest.Id && item.Status == "Pending")
        .ToListAsync();

    DateTime now = DateTime.UtcNow;
    donationRequest.Status = "Approved";
    donationRequest.UpdatedAtUtc = now;
    donationRequest.Donation.Status = "Reserved";

    // One donation can be allocated to only one receiver.
    foreach (var otherRequest in otherPendingRequests)
    {
        otherRequest.Status = "Rejected";
        otherRequest.UpdatedAtUtc = now;
    }

    await database.SaveChangesAsync();

    return Results.Ok(new
    {
        message = "Request approved. The donation is now reserved for the receiver.",
        requestId = donationRequest.Id,
        donationId = donationRequest.DonationId,
        status = donationRequest.Status,
        donationStatus = donationRequest.Donation.Status
    });
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Donor" });

// DONOR REJECTS A PENDING REQUEST
app.MapPost("/api/donation-requests/{requestId:int}/reject", async (
    int requestId,
    HttpContext httpContext,
    FoodShareDbContext database) =>
{
    if (!TryGetCurrentUserId(httpContext, out int donorUserId))
        return Results.Unauthorized();

    var donationRequest = await database.DonationRequests
        .Include(item => item.Donation)
        .SingleOrDefaultAsync(item => item.Id == requestId);

    if (donationRequest == null || donationRequest.Donation.DonorUserId != donorUserId)
        return Results.NotFound(new { message = "Request was not found for your donations." });

    if (donationRequest.Status != "Pending")
        return Results.Conflict(new { message = "Only pending requests can be rejected." });

    donationRequest.Status = "Rejected";
    donationRequest.UpdatedAtUtc = DateTime.UtcNow;
    await database.SaveChangesAsync();

    return Results.Ok(new
    {
        message = "The request has been rejected.",
        requestId = donationRequest.Id,
        status = donationRequest.Status
    });
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Donor" });

app.Run();

static bool TryGetCurrentUserId(HttpContext httpContext, out int userId)
{
    string? userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    return int.TryParse(userIdText, out userId);
}

static bool IsValidEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
    {
        return false;
    }

    try
    {
        var address = new MailAddress(email);
        return address.Address.Equals(
            email, StringComparison.OrdinalIgnoreCase);
    }
    catch
    {
        return false;
    }
}

static bool IsStrongPassword(string password)
{
    return password.Length >= 8 &&
           password.Any(char.IsUpper) &&
           password.Any(char.IsLower) &&
           password.Any(char.IsDigit) &&
           password.Any(character => !char.IsLetterOrDigit(character));
}

public class FoodShareUser
{
    public int Id { get; set; }

    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    public string NormalizedEmail { get; set; } = "";

    public string Phone { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Role { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }
}

public class FoodShareDbContext : DbContext
{
    public FoodShareDbContext(DbContextOptions<FoodShareDbContext> options)
        : base(options)
    {
    }

    public DbSet<FoodShareUser> Users => Set<FoodShareUser>();

    public DbSet<FoodShareDonation> Donations => Set<FoodShareDonation>();

    public DbSet<FoodShareDonationRequest> DonationRequests => Set<FoodShareDonationRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FoodShareUser>()
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique();

        modelBuilder.Entity<FoodShareUser>()
            .Property(user => user.FullName)
            .IsRequired();

        modelBuilder.Entity<FoodShareUser>()
            .Property(user => user.PasswordHash)
            .IsRequired();

        modelBuilder.Entity<FoodShareDonation>()
            .HasOne(donation => donation.Donor)
            .WithMany()
            .HasForeignKey(donation => donation.DonorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FoodShareDonation>()
            .Property(donation => donation.FoodName).IsRequired();
        modelBuilder.Entity<FoodShareDonation>()
            .Property(donation => donation.Category).IsRequired();
        modelBuilder.Entity<FoodShareDonation>()
            .Property(donation => donation.Quantity).IsRequired();
        modelBuilder.Entity<FoodShareDonation>()
            .Property(donation => donation.PickupLocation).IsRequired();
        modelBuilder.Entity<FoodShareDonation>()
            .Property(donation => donation.Status).IsRequired();

        modelBuilder.Entity<FoodShareDonationRequest>()
            .HasOne(request => request.Donation)
            .WithMany()
            .HasForeignKey(request => request.DonationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FoodShareDonationRequest>()
            .HasOne(request => request.Receiver)
            .WithMany()
            .HasForeignKey(request => request.ReceiverUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FoodShareDonationRequest>()
            .Property(request => request.Message).IsRequired();
        modelBuilder.Entity<FoodShareDonationRequest>()
            .Property(request => request.Status).IsRequired();
    }
}

public class RegisterRequest
{
    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? CountryCode { get; set; }

    public string? Role { get; set; }

    public string? Password { get; set; }
}

public class LoginRequest
{
    public string? Email { get; set; }

    public string? Password { get; set; }
}

public class FoodShareDonation
{
    public int Id { get; set; }
    public int DonorUserId { get; set; }
    public FoodShareUser Donor { get; set; } = null!;
    public string FoodName { get; set; } = "";
    public string Category { get; set; } = "";
    public string Quantity { get; set; } = "";
    public string PickupLocation { get; set; } = "";
    public DateTime ExpiryDateUtc { get; set; }
    public string Notes { get; set; } = "";
    public string Status { get; set; } = "Available";
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateDonationRequest
{
    public string? FoodName { get; set; }
    public string? Category { get; set; }
    public string? Quantity { get; set; }
    public string? PickupLocation { get; set; }
    public string? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

public class FoodShareDonationRequest
{
    public int Id { get; set; }
    public int DonationId { get; set; }
    public FoodShareDonation Donation { get; set; } = null!;
    public int ReceiverUserId { get; set; }
    public FoodShareUser Receiver { get; set; } = null!;
    public string Message { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public class CreateDonationRequestMessage
{
    public string? Message { get; set; }
}
