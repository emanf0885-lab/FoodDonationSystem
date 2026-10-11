
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FoodDonation.Client
{
    public partial class Form1 : Form
    {
        // FoodShare visual theme. These colours are used only by the GUI;
        // the API, authentication, and database workflow are unchanged.
        private readonly Color darkGreen = Color.FromArgb(28, 86, 65);
        private readonly Color accentGreen = Color.FromArgb(49, 126, 91);
        private readonly Color softGreen = Color.FromArgb(226, 241, 231);
        private readonly Color lightBackground = Color.FromArgb(246, 249, 246);
        private readonly Color surfaceBorder = Color.FromArgb(222, 231, 224);
        private readonly Color textColor = Color.FromArgb(42, 55, 48);
        private readonly Color mutedText = Color.FromArgb(105, 119, 110);

        private static readonly HttpClient apiClient = new HttpClient();

        private readonly string apiBaseUrl = "http://localhost:5233";

        private string? currentToken;
        private UserResponse? currentUser;
        private DataGridView? donationsGrid;
        private Label? donationsStatusLabel;
        private Label? statPrimaryValueLabel;
        private Label? statSecondaryValueLabel;
        private Label? statTertiaryValueLabel;
        private Label? statPrimaryTitleLabel;
        private Label? statSecondaryTitleLabel;
        private Label? statTertiaryTitleLabel;

        public Form1()
        {
            InitializeComponent();

            Text = "FoodShare | Food Donation Coordination";
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            MinimumSize = new Size(1050, 650);
            Size = new Size(1450, 900);
            BackColor = lightBackground;
            Font = new Font("Segoe UI", 10);

            BuildLoginScreen();
        }

        private void BuildLoginScreen()
        {
            BuildScreen(false);
        }

        private void BuildRegistrationScreen()
        {
            BuildScreen(true);
        }

        private void BuildScreen(bool registrationMode)
        {
            Controls.Clear();

            TableLayoutPanel mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Margin = new Padding(0);
            mainLayout.Padding = new Padding(0);
            mainLayout.ColumnCount = 2;
            mainLayout.RowCount = 1;

            mainLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 43));
            mainLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 57));
            mainLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            Controls.Add(mainLayout);

            // LEFT BRANDING PANEL

            Panel brandingPanel = new Panel();
            brandingPanel.Dock = DockStyle.Fill;
            brandingPanel.BackColor = darkGreen;
            brandingPanel.Padding = new Padding(30);

            mainLayout.Controls.Add(brandingPanel, 0, 0);

            TableLayoutPanel brandingLayout = new TableLayoutPanel();
            brandingLayout.Dock = DockStyle.Fill;
            brandingLayout.ColumnCount = 1;
            brandingLayout.RowCount = 3;

            brandingLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));
            brandingLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 25));
            brandingLayout.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));
            brandingLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 75));

            brandingPanel.Controls.Add(brandingLayout);

            Label logoLabel = new Label();
            logoLabel.Text = "FoodShare";
            logoLabel.Dock = DockStyle.Fill;
            logoLabel.ForeColor = Color.White;
            logoLabel.Font = new Font("Segoe UI", 25, FontStyle.Bold);
            logoLabel.TextAlign = ContentAlignment.BottomLeft;
            logoLabel.AutoSize = false;

            brandingLayout.Controls.Add(logoLabel, 0, 0);

            Label headingLabel = new Label();
            headingLabel.Text = registrationMode
                ? "Be Part of\na Better Food\nSharing Community."
                : "Share Food.\nReduce Waste.\nSupport Communities.";

            headingLabel.AutoSize = true;
            headingLabel.MaximumSize = new Size(400, 0);
            headingLabel.ForeColor = Color.White;
            headingLabel.Font = new Font("Segoe UI", 21, FontStyle.Bold);
            headingLabel.Margin = new Padding(0, 20, 0, 15);

            brandingLayout.Controls.Add(headingLabel, 0, 1);

            Label descriptionLabel = new Label();
            descriptionLabel.Text =
                "Connecting food donors with charities " +
                "and communities in need.";
            descriptionLabel.AutoSize = true;
            descriptionLabel.MaximumSize = new Size(400, 0);
            descriptionLabel.ForeColor = Color.FromArgb(222, 239, 228);
            descriptionLabel.Font = new Font("Segoe UI", 11);
            descriptionLabel.Margin = new Padding(0, 10, 0, 0);

            brandingLayout.Controls.Add(descriptionLabel, 0, 2);

            // RIGHT FORM PANEL

            Panel rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.BackColor = lightBackground;
            rightPanel.Padding = new Padding(22);

            mainLayout.Controls.Add(rightPanel, 1, 0);

            TableLayoutPanel rightLayout = new TableLayoutPanel();
            rightLayout.Dock = DockStyle.Fill;
            rightLayout.ColumnCount = 3;
            rightLayout.RowCount = 3;

            rightLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 3));
            rightLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 94));
            rightLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 3));

            rightLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 3));
            rightLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 94));
            rightLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 3));

            rightPanel.Controls.Add(rightLayout);

            Panel card = new Panel();
            card.Dock = DockStyle.Fill;
            card.BackColor = Color.White;
            card.Padding = new Padding(28);
            card.BorderStyle = BorderStyle.FixedSingle;

            rightLayout.Controls.Add(card, 1, 1);

            TableLayoutPanel formLayout = new TableLayoutPanel();
            formLayout.Dock = DockStyle.Fill;
            formLayout.ColumnCount = 1;
            formLayout.RowCount = 0;
            formLayout.AutoScroll = true;
            formLayout.GrowStyle = TableLayoutPanelGrowStyle.AddRows;

            formLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            card.Controls.Add(formLayout);

            Label titleLabel = new Label();
            titleLabel.Text = registrationMode
                ? "Create Account"
                : "Welcome Back";
            titleLabel.AutoSize = true;
            titleLabel.ForeColor = darkGreen;
            titleLabel.Font = new Font("Segoe UI", 22, FontStyle.Bold);
            titleLabel.Margin = new Padding(0, 0, 0, 5);

            AddControl(formLayout, titleLabel);

            Label subtitleLabel = new Label();
            subtitleLabel.Text = registrationMode
                ? "Join FoodShare and make a difference."
                : "Sign in to continue to FoodShare.";
            subtitleLabel.AutoSize = true;
            subtitleLabel.ForeColor = mutedText;
            subtitleLabel.Margin = new Padding(0, 0, 0, 20);

            AddControl(formLayout, subtitleLabel);

            TextBox? nameBox = null;
            TextBox emailBox;
            TextBox? phoneBox = null;
            TextBox passwordBox;
            TextBox? confirmPasswordBox = null;
            ComboBox? countryComboBox = null;
            ComboBox? roleComboBox = null;

            if (registrationMode)
            {
                nameBox = AddField(
                    formLayout, "Full name", "Enter your full name");

                emailBox = AddField(
                    formLayout, "Email address", "Enter your email address");

                Label phoneLabel = new Label();
                phoneLabel.Text = "Phone number";
                phoneLabel.AutoSize = true;
                phoneLabel.ForeColor = textColor;
                phoneLabel.Margin = new Padding(0, 0, 0, 5);

                AddControl(formLayout, phoneLabel);

                TableLayoutPanel phoneLayout = new TableLayoutPanel();
                phoneLayout.Dock = DockStyle.Top;
                phoneLayout.Height = 34;
                phoneLayout.Margin = new Padding(0, 0, 0, 14);
                phoneLayout.ColumnCount = 2;
                phoneLayout.RowCount = 1;

                phoneLayout.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Absolute, 115));
                phoneLayout.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 100));
                phoneLayout.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 100));

                countryComboBox = new ComboBox();
                countryComboBox.Dock = DockStyle.Fill;
                countryComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                countryComboBox.Font = new Font("Segoe UI", 10);
                countryComboBox.BackColor = Color.White;
                countryComboBox.ForeColor = textColor;

                countryComboBox.Items.Add("Pakistan (+92)");
                countryComboBox.Items.Add("United Kingdom (+44)");
                countryComboBox.Items.Add("United States (+1)");
                countryComboBox.Items.Add("Canada (+1)");
                countryComboBox.Items.Add("UAE (+971)");
                countryComboBox.Items.Add("Saudi Arabia (+966)");
                countryComboBox.Items.Add("India (+91)");
                countryComboBox.Items.Add("Australia (+61)");
                countryComboBox.Items.Add("Turkey (+90)");
                countryComboBox.Items.Add("Germany (+49)");
                countryComboBox.Items.Add("France (+33)");
                countryComboBox.Items.Add("Other");
                countryComboBox.SelectedIndex = 0;

                phoneBox = new TextBox();
                phoneBox.Dock = DockStyle.Fill;
                phoneBox.Font = new Font("Segoe UI", 11);
                phoneBox.PlaceholderText = "3001234567";

                phoneLayout.Controls.Add(countryComboBox, 0, 0);
                phoneLayout.Controls.Add(phoneBox, 1, 0);

                AddControl(formLayout, phoneLayout);

                Label roleLabel = new Label();
                roleLabel.Text = "Account type";
                roleLabel.AutoSize = true;
                roleLabel.ForeColor = textColor;
                roleLabel.Margin = new Padding(0, 0, 0, 5);

                AddControl(formLayout, roleLabel);

                roleComboBox = new ComboBox();
                roleComboBox.Dock = DockStyle.Top;
                roleComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                roleComboBox.Font = new Font("Segoe UI", 10);
                roleComboBox.BackColor = Color.White;
                roleComboBox.ForeColor = textColor;
                roleComboBox.Items.Add("Donor");
                roleComboBox.Items.Add("Receiver / Charity");
                roleComboBox.SelectedIndex = 0;
                roleComboBox.Margin = new Padding(0, 0, 0, 14);

                AddControl(formLayout, roleComboBox);

                passwordBox = AddField(
                    formLayout, "Password", "Create a strong password", true);

                Label passwordHint = new Label();
                passwordHint.Text =
                    "Use 8+ characters, uppercase and lowercase " +
                    "letters, a number, and a special character.";
                passwordHint.AutoSize = true;
                passwordHint.MaximumSize = new Size(450, 0);
                passwordHint.ForeColor = Color.Gray;
                passwordHint.Font = new Font("Segoe UI", 8.5f);
                passwordHint.Margin = new Padding(0, -8, 0, 12);

                AddControl(formLayout, passwordHint);

                confirmPasswordBox = AddField(
                    formLayout, "Confirm password", "Re-enter your password", true);
            }
            else
            {
                emailBox = AddField(
                    formLayout, "Email address", "Enter your email address");

                passwordBox = AddField(
                    formLayout, "Password", "Enter your password", true);
            }

            CheckBox captchaCheckBox = new CheckBox();
            captchaCheckBox.Text = "I'm not a robot";
            captchaCheckBox.AutoSize = true;
            captchaCheckBox.ForeColor = textColor;
            captchaCheckBox.Margin = new Padding(0, 2, 0, 15);

            AddControl(formLayout, captchaCheckBox);

            Button primaryButton = new Button();
            primaryButton.Text = registrationMode ? "Create Account" : "Sign In";
            primaryButton.Dock = DockStyle.Top;
            primaryButton.Height = 44;
            primaryButton.BackColor = darkGreen;
            primaryButton.ForeColor = Color.White;
            primaryButton.FlatStyle = FlatStyle.Flat;
            primaryButton.FlatAppearance.BorderSize = 0;
            primaryButton.FlatAppearance.MouseOverBackColor = accentGreen;
            primaryButton.FlatAppearance.MouseDownBackColor = darkGreen;
            primaryButton.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            primaryButton.Cursor = Cursors.Hand;
            primaryButton.Margin = new Padding(0, 8, 0, 15);

            primaryButton.Click += async (sender, e) =>
            {
                primaryButton.Enabled = false;
                primaryButton.Text = "Please wait...";

                try
                {
                    if (!ValidateForm(
                        registrationMode, nameBox, emailBox, phoneBox,
                        passwordBox, confirmPasswordBox, countryComboBox,
                        captchaCheckBox))
                    {
                        return;
                    }

                    if (registrationMode)
                    {
                        await RegisterAccount(
                            nameBox!.Text.Trim(),
                            emailBox.Text.Trim(),
                            phoneBox!.Text.Trim(),
                            GetCountryCode(countryComboBox!.SelectedItem!.ToString()!),
                            roleComboBox!.SelectedIndex == 0 ? "Donor" : "Receiver",
                            passwordBox.Text);
                    }
                    else
                    {
                        await LoginAccount(emailBox.Text.Trim(), passwordBox.Text);
                    }
                }
                catch (HttpRequestException)
                {
                    ShowValidationError(
                        "Cannot connect to FoodShare API.\n\n" +
                        "Make sure the API is running at " + apiBaseUrl + ".");
                }
                catch (TaskCanceledException)
                {
                    ShowValidationError("The request took too long. Please try again.");
                }
                catch (Exception ex)
                {
                    ShowValidationError("An unexpected error occurred:\n" + ex.Message);
                }
                finally
                {
                    if (!IsDisposed && !Disposing)
                    {
                        primaryButton.Enabled = true;
                        primaryButton.Text = registrationMode
                            ? "Create Account" : "Sign In";
                    }
                }
            };

            AddControl(formLayout, primaryButton);

            Label switchPrompt = new Label();
            switchPrompt.Text = registrationMode
                ? "Already have an account?"
                : "New to FoodShare?";
            switchPrompt.AutoSize = true;
            switchPrompt.ForeColor = mutedText;
            switchPrompt.Margin = new Padding(0, 0, 0, 5);

            AddControl(formLayout, switchPrompt);

            LinkLabel switchLink = new LinkLabel();
            switchLink.Text = registrationMode
                ? "Back to Sign In" : "Create an account";
            switchLink.AutoSize = true;
            switchLink.LinkColor = darkGreen;
            switchLink.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            switchLink.Click += (sender, e) =>
            {
                if (registrationMode)
                    BuildLoginScreen();
                else
                    BuildRegistrationScreen();
            };

            AddControl(formLayout, switchLink);
        }

        private bool ValidateForm(
            bool registrationMode,
            TextBox? nameBox,
            TextBox emailBox,
            TextBox? phoneBox,
            TextBox passwordBox,
            TextBox? confirmPasswordBox,
            ComboBox? countryComboBox,
            CheckBox captchaCheckBox)
        {
            if (string.IsNullOrWhiteSpace(emailBox.Text) ||
                !IsValidEmail(emailBox.Text.Trim()))
            {
                ShowValidationError("Please enter a valid email address.");
                emailBox.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(passwordBox.Text))
            {
                ShowValidationError("Please enter your password.");
                passwordBox.Focus();
                return false;
            }

            if (!captchaCheckBox.Checked)
            {
                ShowValidationError("Please tick the 'I'm not a robot' checkbox.");
                return false;
            }

            if (!registrationMode)
                return true;

            if (string.IsNullOrWhiteSpace(nameBox!.Text))
            {
                ShowValidationError("Please enter your full name.");
                nameBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(phoneBox!.Text) ||
                !ContainsOnlyDigits(phoneBox.Text.Trim()))
            {
                ShowValidationError("Enter a phone number containing digits only.");
                phoneBox.Focus();
                return false;
            }

            if (!IsStrongPassword(passwordBox.Text))
            {
                ShowValidationError(
                    "Password requirements:\n\n" +
                    "- At least 8 characters\n" +
                    "- One uppercase letter\n" +
                    "- One lowercase letter\n" +
                    "- One number\n" +
                    "- One special character, such as @ or #");
                passwordBox.Focus();
                return false;
            }

            if (passwordBox.Text != confirmPasswordBox!.Text)
            {
                ShowValidationError("Your passwords do not match.");
                confirmPasswordBox.Focus();
                return false;
            }

            return true;
        }

        private async Task RegisterAccount(
            string fullName, string email, string phone,
            string countryCode, string role, string password)
        {
            var request = new RegisterRequest
            {
                FullName = fullName,
                Email = email,
                Phone = phone,
                CountryCode = countryCode,
                Role = role,
                Password = password
            };

            HttpResponseMessage response = await apiClient.PostAsJsonAsync(
                apiBaseUrl + "/api/auth/register", request);

            if (!response.IsSuccessStatusCode)
            {
                ShowValidationError(await GetErrorMessage(response));
                return;
            }

            MessageBox.Show(
                "Your FoodShare account has been created successfully.\n\n" +
                "You can now sign in using your email and password.",
                "Registration Successful",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            BuildLoginScreen();
        }

        private async Task LoginAccount(string email, string password)
        {
            var request = new LoginRequest
            {
                Email = email,
                Password = password
            };

            HttpResponseMessage response = await apiClient.PostAsJsonAsync(
                apiBaseUrl + "/api/auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                ShowValidationError(await GetErrorMessage(response));
                return;
            }

            LoginResponse? result =
                await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (result == null ||
                string.IsNullOrWhiteSpace(result.Token) ||
                result.User == null)
            {
                ShowValidationError("The server returned an invalid login response.");
                return;
            }

            currentToken = result.Token;
            currentUser = result.User;

            BuildDashboard();
        }

        private async Task<string> GetErrorMessage(HttpResponseMessage response)
        {
            string responseText = await response.Content.ReadAsStringAsync();

            try
            {
                using JsonDocument document = JsonDocument.Parse(responseText);

                if (document.RootElement.TryGetProperty(
                    "message", out JsonElement messageElement))
                {
                    return messageElement.GetString()
                        ?? "The request could not be completed.";
                }
            }
            catch (JsonException)
            {
                // Use a general message for non-JSON responses.
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return "An account with this email already exists.";

            return "The request failed. Please check your information.";
        }

        // RESPONSIVE DASHBOARD: HEADER AND CONTENT USE SEPARATE ROWS

        private void BuildDashboard()
        {
            Controls.Clear();

            TableLayoutPanel dashboardLayout = new TableLayoutPanel();
            dashboardLayout.Dock = DockStyle.Fill;
            dashboardLayout.Margin = new Padding(0);
            dashboardLayout.Padding = new Padding(0);
            dashboardLayout.ColumnCount = 1;
            dashboardLayout.RowCount = 2;

            dashboardLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));
            dashboardLayout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 82));
            dashboardLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            Controls.Add(dashboardLayout);

            // HEADER

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.Margin = new Padding(8, 5, 8, 5);
            header.BackColor = darkGreen;
            header.Padding = new Padding(24, 5, 18, 5);
            ApplyRoundedCorners(header, 22);

            dashboardLayout.Controls.Add(header, 0, 0);

            TableLayoutPanel headerLayout = new TableLayoutPanel();
            headerLayout.Dock = DockStyle.Fill;
            headerLayout.ColumnCount = 3;
            headerLayout.RowCount = 1;

            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            headerLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            header.Controls.Add(headerLayout);

            Panel brandMark = new Panel();
            brandMark.Dock = DockStyle.Fill;
            brandMark.BackColor = darkGreen;
            brandMark.Margin = new Padding(0, 4, 6, 4);
            brandMark.Paint += (sender, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using Pen leafPen = new Pen(Color.FromArgb(178, 220, 126), 3.2f);
                leafPen.StartCap = LineCap.Round;
                leafPen.EndCap = LineCap.Round;
                int cx = brandMark.ClientSize.Width / 2;
                int cy = brandMark.ClientSize.Height / 2 + 3;
                e.Graphics.DrawArc(leafPen, cx - 15, cy - 17, 19, 25, 195, 250);
                e.Graphics.DrawArc(leafPen, cx - 2, cy - 22, 20, 26, 285, 250);
                e.Graphics.DrawArc(leafPen, cx - 12, cy - 2, 25, 17, 15, 150);
                e.Graphics.DrawLine(leafPen, cx - 1, cy + 12, cx + 9, cy - 12);
            };
            headerLayout.Controls.Add(brandMark, 0, 0);

            Label logo = new Label();
            logo.Text = "FoodShare";
            logo.Dock = DockStyle.Fill;
            logo.ForeColor = Color.White;
            logo.Font = new Font("Segoe UI", 23, FontStyle.Bold);
            logo.TextAlign = ContentAlignment.MiddleLeft;
            logo.AutoSize = false;

            headerLayout.Controls.Add(logo, 1, 0);

            Button logoutButton = new Button();
            logoutButton.Text = "Sign Out";
            logoutButton.Dock = DockStyle.Fill;
            logoutButton.Margin = new Padding(5, 8, 0, 8);
            logoutButton.BackColor = Color.White;
            logoutButton.ForeColor = darkGreen;
            logoutButton.FlatStyle = FlatStyle.Flat;
            logoutButton.FlatAppearance.BorderSize = 0;
            logoutButton.FlatAppearance.MouseOverBackColor = softGreen;
            logoutButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(211, 231, 217);
            logoutButton.Cursor = Cursors.Hand;
            logoutButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            logoutButton.Click += (sender, e) =>
            {
                currentToken = null;
                currentUser = null;
                BuildLoginScreen();
            };

            headerLayout.Controls.Add(logoutButton, 2, 0);

            // SCROLLABLE MAIN CONTENT, BELOW THE HEADER

            Panel contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.AutoScroll = true;
            contentPanel.BackColor = lightBackground;
            contentPanel.Padding = new Padding(30, 26, 30, 30);

            dashboardLayout.Controls.Add(contentPanel, 0, 1);

            TableLayoutPanel contentLayout = new TableLayoutPanel();
            contentLayout.Dock = DockStyle.Top;
            contentLayout.AutoSize = true;
            contentLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            contentLayout.ColumnCount = 1;
            contentLayout.RowCount = 0;
            contentLayout.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
            contentLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            contentPanel.Controls.Add(contentLayout);

            // WELCOME HERO BANNER
            Panel heroPanel = new Panel();
            heroPanel.Dock = DockStyle.Top;
            heroPanel.Height = 158;
            heroPanel.Margin = new Padding(0, 0, 0, 18);
            heroPanel.Padding = new Padding(26, 16, 8, 16);
            heroPanel.BackColor = darkGreen;
            ApplyRoundedCorners(heroPanel, 22);
            AddControl(contentLayout, heroPanel);

            TableLayoutPanel heroLayout = new TableLayoutPanel();
            heroLayout.Dock = DockStyle.Fill;
            heroLayout.Margin = new Padding(0);
            heroLayout.ColumnCount = 2;
            heroLayout.RowCount = 1;
            heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            heroLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            heroPanel.Controls.Add(heroLayout);

            Panel heroTextPanel = new Panel();
            heroTextPanel.Dock = DockStyle.Fill;
            heroTextPanel.BackColor = darkGreen;
            heroTextPanel.Padding = new Padding(0, 0, 8, 0);
            heroLayout.Controls.Add(heroTextPanel, 0, 0);

            Label welcomeLabel = new Label();
            welcomeLabel.Text = "Welcome, " + currentUser!.FullName + "!";
            welcomeLabel.AutoSize = false;
            welcomeLabel.Dock = DockStyle.Top;
            welcomeLabel.Height = 62;
            welcomeLabel.ForeColor = Color.White;
            welcomeLabel.Font = new Font("Segoe UI", 23, FontStyle.Bold);
            welcomeLabel.TextAlign = ContentAlignment.MiddleLeft;
            heroTextPanel.Controls.Add(welcomeLabel);

            Label subtitle = new Label();
            subtitle.Text = currentUser.Role == "Donor"
                ? "Your extra food can make a meaningful difference."
                : "Discover food donations and help good food reach people.";
            subtitle.AutoSize = false;
            subtitle.Dock = DockStyle.Top;
            subtitle.Height = 40;
            subtitle.ForeColor = Color.FromArgb(224, 240, 229);
            subtitle.Font = new Font("Segoe UI", 11);
            subtitle.TextAlign = ContentAlignment.MiddleLeft;
            heroTextPanel.Controls.Add(subtitle);

            PictureBox heroPhoto = new PictureBox();
            heroPhoto.Dock = DockStyle.Fill;
            heroPhoto.Margin = new Padding(12, 2, 0, 2);
            heroPhoto.SizeMode = PictureBoxSizeMode.Zoom;
            heroPhoto.BackColor = accentGreen;
            heroPhoto.Image = LoadDashboardImage("foodshare_hero_photo.png");
            ApplyRoundedCorners(heroPhoto, 18);
            heroLayout.Controls.Add(heroPhoto, 1, 0);

            // SUMMARY CARDS + FOOD IMPACT PANEL
            TableLayoutPanel summaryRow = new TableLayoutPanel();
            summaryRow.Dock = DockStyle.Top;
            summaryRow.AutoSize = true;
            summaryRow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            summaryRow.Margin = new Padding(0, 0, 0, 18);
            summaryRow.Padding = new Padding(0);
            summaryRow.ColumnCount = 2;
            summaryRow.RowCount = 1;
            summaryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            summaryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            summaryRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            AddControl(contentLayout, summaryRow);

            FlowLayoutPanel statsPanel = new FlowLayoutPanel();
            statsPanel.Dock = DockStyle.Fill;
            statsPanel.AutoSize = true;
            statsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            statsPanel.WrapContents = true;
            statsPanel.Margin = new Padding(0, 0, 12, 0);
            statsPanel.Padding = new Padding(0);
            summaryRow.Controls.Add(statsPanel, 0, 0);

            CreateSummaryCard(statsPanel, "", "Total Donations", out statPrimaryTitleLabel, out statPrimaryValueLabel);
            CreateSummaryCard(statsPanel, "", "Available Food", out statSecondaryTitleLabel, out statSecondaryValueLabel);
            CreateSummaryCard(statsPanel, "", "Reserved / Shared", out statTertiaryTitleLabel, out statTertiaryValueLabel);

            Panel impactCard = new Panel();
            impactCard.Dock = DockStyle.Fill;
            impactCard.MinimumSize = new Size(250, 108);
            impactCard.Height = 112;
            impactCard.Margin = new Padding(0, 0, 0, 0);
            impactCard.Padding = new Padding(18, 12, 10, 12);
            impactCard.BackColor = Color.FromArgb(231, 245, 232);
            ApplyRoundedCorners(impactCard, 18);
            summaryRow.Controls.Add(impactCard, 1, 0);

            TableLayoutPanel impactLayout = new TableLayoutPanel();
            impactLayout.Dock = DockStyle.Fill;
            impactLayout.Margin = new Padding(0);
            impactLayout.Padding = new Padding(0);
            impactLayout.ColumnCount = 2;
            impactLayout.RowCount = 1;
            impactLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63));
            impactLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
            impactLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            impactCard.Controls.Add(impactLayout);

            Panel impactText = new Panel();
            impactText.Dock = DockStyle.Fill;
            impactText.BackColor = Color.Transparent;
            impactLayout.Controls.Add(impactText, 0, 0);

            Label impactTitle = new Label();
            impactTitle.Text = "Good Food, Brighter Tomorrows";
            impactTitle.Dock = DockStyle.Top;
            impactTitle.Height = 42;
            impactTitle.ForeColor = darkGreen;
            impactTitle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            impactTitle.TextAlign = ContentAlignment.MiddleLeft;
            impactText.Controls.Add(impactTitle);

            Label impactDescription = new Label();
            impactDescription.Text = "Your donations help provide meals and bring hope to people in need.";
            impactDescription.Dock = DockStyle.Fill;
            impactDescription.ForeColor = textColor;
            impactDescription.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            impactDescription.TextAlign = ContentAlignment.MiddleLeft;
            impactText.Controls.Add(impactDescription);

            PictureBox foodPhoto = new PictureBox();
            foodPhoto.Dock = DockStyle.Fill;
            foodPhoto.Margin = new Padding(8, 0, 0, 0);
            foodPhoto.SizeMode = PictureBoxSizeMode.Zoom;
            foodPhoto.BackColor = Color.FromArgb(231, 245, 232);
            foodPhoto.Image = LoadDashboardImage("foodshare_food_photo.png");
            ApplyRoundedCorners(foodPhoto, 14);
            impactLayout.Controls.Add(foodPhoto, 1, 0);

            // ACCOUNT CARD

            Panel accountCard = new Panel();
            accountCard.Dock = DockStyle.Top;
            accountCard.Height = 128;
            accountCard.Margin = new Padding(0, 0, 0, 18);
            accountCard.Padding = new Padding(22, 10, 22, 8);
            accountCard.BackColor = Color.White;
            accountCard.BorderStyle = BorderStyle.FixedSingle;
            ApplyRoundedCorners(accountCard, 18);

            AddControl(contentLayout, accountCard);

            // Use a top-docked, auto-sized layout so the account text cannot be clipped.
            TableLayoutPanel accountLayout = new TableLayoutPanel();
            accountLayout.Dock = DockStyle.Top;
            accountLayout.AutoSize = true;
            accountLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            accountLayout.ColumnCount = 1;
            accountLayout.RowCount = 2;
            accountLayout.Margin = new Padding(0);
            accountLayout.Padding = new Padding(0);
            accountLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            accountLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            accountLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            accountCard.Controls.Add(accountLayout);

            Label accountTitle = new Label();
            accountTitle.Text = "Your Account";
            accountTitle.AutoSize = true;
            accountTitle.ForeColor = darkGreen;
            accountTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            accountTitle.Margin = new Padding(0, 0, 0, 8);

            accountLayout.Controls.Add(accountTitle, 0, 0);

            Label accountDetails = new Label();
            accountDetails.Text =
                "Name: " + currentUser.FullName + "\n" +
                "Email: " + currentUser.Email + "\n" +
                "Account type: " + currentUser.Role;
            accountDetails.AutoSize = true;
            accountDetails.MaximumSize = new Size(1100, 0);
            accountDetails.ForeColor = textColor;
            accountDetails.Font = new Font("Segoe UI", 10.5f);
            accountDetails.Margin = new Padding(0);
            accountDetails.TextAlign = ContentAlignment.TopLeft;

            accountLayout.Controls.Add(accountDetails, 0, 1);

            Label workspaceTitle = new Label();
            workspaceTitle.Text = currentUser.Role == "Donor"
                ? "Your Donor Workspace"
                : "Available Food Donations";
            workspaceTitle.AutoSize = true;
            workspaceTitle.MaximumSize = new Size(850, 0);
            workspaceTitle.ForeColor = darkGreen;
            workspaceTitle.Font = new Font("Segoe UI", 17, FontStyle.Bold);
            workspaceTitle.Margin = new Padding(6, 0, 0, 6);
            AddControl(contentLayout, workspaceTitle);

            Label workspaceDescription = new Label();
            workspaceDescription.Text = currentUser.Role == "Donor"
                ? "Add surplus food and track the donations you have listed."
                : "Browse food currently listed by FoodShare donors.";
            workspaceDescription.AutoSize = true;
            workspaceDescription.MaximumSize = new Size(850, 0);
            workspaceDescription.ForeColor = textColor;
            workspaceDescription.Font = new Font("Segoe UI", 11);
            workspaceDescription.Margin = new Padding(6, 0, 0, 18);
            AddControl(contentLayout, workspaceDescription);

            FlowLayoutPanel actionsPanel = new FlowLayoutPanel();
            actionsPanel.AutoSize = true;
            actionsPanel.Dock = DockStyle.Top;
            actionsPanel.WrapContents = true;
            actionsPanel.Padding = new Padding(0);
            actionsPanel.Margin = new Padding(0, 0, 0, 16);

            if (currentUser.Role == "Donor")
            {
                Button addDonationButton = CreateActionButton("+ Add Food Donation", darkGreen, Color.White);
                addDonationButton.Click += async (sender, e) => await OpenCreateDonationForm();
                actionsPanel.Controls.Add(addDonationButton);

                Button refreshButton = CreateActionButton("Refresh My Donations", Color.White, darkGreen);
                refreshButton.Click += async (sender, e) => await LoadDonationsAsync();
                actionsPanel.Controls.Add(refreshButton);

                Button requestsButton = CreateActionButton("View Incoming Requests", Color.White, darkGreen);
                requestsButton.Click += async (sender, e) => await OpenDonorRequestsAsync();
                actionsPanel.Controls.Add(requestsButton);
            }
            else
            {
                Button refreshButton = CreateActionButton("Refresh Available Food", darkGreen, Color.White);
                refreshButton.Click += async (sender, e) => await LoadDonationsAsync();
                actionsPanel.Controls.Add(refreshButton);

                Button requestButton = CreateActionButton("Request Selected Food", Color.White, darkGreen);
                requestButton.Click += async (sender, e) => await RequestSelectedDonationAsync();
                actionsPanel.Controls.Add(requestButton);

                Button myRequestsButton = CreateActionButton("My Requests", Color.White, darkGreen);
                myRequestsButton.Click += async (sender, e) => await OpenMyRequestsAsync();
                actionsPanel.Controls.Add(myRequestsButton);
            }

            AddControl(contentLayout, actionsPanel);

            donationsStatusLabel = new Label();
            donationsStatusLabel.Text = "Loading donations...";
            donationsStatusLabel.AutoSize = true;
            donationsStatusLabel.MaximumSize = new Size(850, 0);
            donationsStatusLabel.ForeColor = mutedText;
            donationsStatusLabel.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            donationsStatusLabel.Margin = new Padding(0, 0, 0, 10);
            AddControl(contentLayout, donationsStatusLabel);

            donationsGrid = new DataGridView();
            donationsGrid.Dock = DockStyle.Top;
            donationsGrid.Height = 260;
            donationsGrid.MinimumSize = new Size(500, 200);
            donationsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            donationsGrid.AllowUserToAddRows = false;
            donationsGrid.AllowUserToDeleteRows = false;
            donationsGrid.ReadOnly = true;
            donationsGrid.MultiSelect = false;
            donationsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            donationsGrid.RowHeadersVisible = false;
            donationsGrid.BackgroundColor = Color.White;
            donationsGrid.BorderStyle = BorderStyle.None;
            donationsGrid.GridColor = surfaceBorder;
            donationsGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            donationsGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            donationsGrid.EnableHeadersVisualStyles = false;
            donationsGrid.ColumnHeadersHeight = 42;
            donationsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            donationsGrid.RowTemplate.Height = 38;
            donationsGrid.ColumnHeadersDefaultCellStyle.BackColor = darkGreen;
            donationsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            donationsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            donationsGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            donationsGrid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
            donationsGrid.DefaultCellStyle.ForeColor = textColor;
            donationsGrid.DefaultCellStyle.BackColor = Color.White;
            donationsGrid.DefaultCellStyle.SelectionBackColor = softGreen;
            donationsGrid.DefaultCellStyle.SelectionForeColor = darkGreen;
            donationsGrid.DefaultCellStyle.Padding = new Padding(8, 2, 8, 2);
            donationsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 252, 249);
            donationsGrid.RowHeadersDefaultCellStyle.BackColor = lightBackground;
            donationsGrid.CellFormatting += DonationsGrid_CellFormatting;
            donationsGrid.AutoGenerateColumns = false;
            donationsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false });
            donationsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Food", DataPropertyName = "FoodName", FillWeight = 115 });
            donationsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Category", DataPropertyName = "Category", FillWeight = 85 });
            donationsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Quantity", DataPropertyName = "Quantity", FillWeight = 85 });
            donationsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Pickup Location", DataPropertyName = "PickupLocation", FillWeight = 130 });
            donationsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Expiry Date", DataPropertyName = "ExpiryDateUtc", FillWeight = 85 });
            donationsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "Status", FillWeight = 75 });
            AddControl(contentLayout, donationsGrid);

            Label privacyNote = new Label();
            privacyNote.Text = currentUser.Role == "Donor"
                ? "Your donation records are saved in the FoodShare database and linked to your account."
                : "Only available, non-expired donations are shown. Contact details are not displayed publicly.";
            privacyNote.AutoSize = true;
            privacyNote.MaximumSize = new Size(850, 0);
            privacyNote.ForeColor = Color.Gray;
            privacyNote.Font = new Font("Segoe UI", 9);
            privacyNote.Margin = new Padding(0, 12, 0, 15);
            AddControl(contentLayout, privacyNote);

         _ = LoadDonationsAsync();
        }

        // Display-only styling for donation statuses. This does not change
        // the status values returned by the API or saved in SQLite.
        private void DonationsGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (donationsGrid == null || e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            string propertyName = donationsGrid.Columns[e.ColumnIndex].DataPropertyName;
            if (propertyName == "ExpiryDateUtc")
            {
                string dateText = Convert.ToString(e.Value) ?? string.Empty;
                if (DateTime.TryParse(dateText, out DateTime expiryDate))
                {
                    DateTime expiryDay = expiryDate.Date;
                    if (expiryDay < DateTime.Today)
                    {
                        e.CellStyle.ForeColor = Color.FromArgb(150, 55, 55);
                        e.CellStyle.BackColor = Color.FromArgb(253, 235, 235);
                        e.CellStyle.SelectionForeColor = Color.FromArgb(150, 55, 55);
                        e.CellStyle.SelectionBackColor = Color.FromArgb(248, 220, 220);
                    }
                    else if (expiryDay <= DateTime.Today.AddDays(2))
                    {
                        e.CellStyle.ForeColor = Color.FromArgb(125, 83, 26);
                        e.CellStyle.BackColor = Color.FromArgb(252, 244, 226);
                        e.CellStyle.SelectionForeColor = Color.FromArgb(125, 83, 26);
                        e.CellStyle.SelectionBackColor = Color.FromArgb(247, 232, 199);
                    }
                }
                return;
            }
            if (propertyName != "Status") return;

            string status = Convert.ToString(e.Value) ?? string.Empty;
            if (status.Equals("Available", StringComparison.OrdinalIgnoreCase))
            {
                e.CellStyle.ForeColor = Color.FromArgb(30, 110, 66);
                e.CellStyle.BackColor = Color.FromArgb(235, 247, 238);
                e.CellStyle.SelectionForeColor = Color.FromArgb(30, 110, 66);
                e.CellStyle.SelectionBackColor = softGreen;
            }
            else if (status.Equals("Reserved", StringComparison.OrdinalIgnoreCase))
            {
                e.CellStyle.ForeColor = Color.FromArgb(125, 83, 26);
                e.CellStyle.BackColor = Color.FromArgb(252, 244, 226);
                e.CellStyle.SelectionForeColor = Color.FromArgb(125, 83, 26);
                e.CellStyle.SelectionBackColor = Color.FromArgb(247, 232, 199);
            }
        }

        private Button CreateActionButton(string text, Color backColor, Color foreColor)
        {
            Button button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.MinimumSize = new Size(170, 42);
            button.Padding = new Padding(10, 4, 10, 4);
            button.Margin = new Padding(0, 0, 10, 0);
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = backColor == darkGreen ? darkGreen : surfaceBorder;
            button.FlatAppearance.BorderSize = backColor == darkGreen ? 0 : 1;
            button.FlatAppearance.MouseOverBackColor = backColor == darkGreen
                ? accentGreen : softGreen;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(211, 231, 217);
            button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private async Task OpenCreateDonationForm()
        {
            using Form dialog = new Form();
            dialog.Text = "Add Food Donation";
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            dialog.ShowInTaskbar = false;
            dialog.ClientSize = new Size(520, 610);
            dialog.BackColor = lightBackground;
            dialog.Font = new Font("Segoe UI", 10);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(24);
            layout.ColumnCount = 1;
            layout.RowCount = 0;
            layout.AutoScroll = true;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            dialog.Controls.Add(layout);

            Label title = new Label();
            title.Text = "Share Surplus Food";
            title.AutoSize = true;
            title.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            title.ForeColor = darkGreen;
            title.Margin = new Padding(0, 0, 0, 4);
            AddControl(layout, title);

            Label subtitle = new Label();
            subtitle.Text = "Enter clear details so a receiver can understand the donation.";
            subtitle.AutoSize = true;
            subtitle.MaximumSize = new Size(440, 0);
            subtitle.ForeColor = Color.Gray;
            subtitle.Margin = new Padding(0, 0, 0, 16);
            AddControl(layout, subtitle);

            TextBox foodNameBox = AddField(layout, "Food name", "e.g. Vegetable rice boxes");

            Label categoryLabel = new Label { Text = "Food category", AutoSize = true, ForeColor = textColor, Margin = new Padding(0, 0, 0, 5) };
            AddControl(layout, categoryLabel);
            ComboBox categoryBox = new ComboBox();
            categoryBox.DropDownStyle = ComboBoxStyle.DropDownList;
            categoryBox.Items.AddRange(new object[] { "Prepared Meals", "Bakery", "Fruits", "Vegetables", "Packaged Food", "Dairy", "Other" });
            categoryBox.SelectedIndex = 0;
            categoryBox.Dock = DockStyle.Top;
            categoryBox.Margin = new Padding(0, 0, 0, 14);
            AddControl(layout, categoryBox);

            TextBox quantityBox = AddField(layout, "Quantity", "e.g. 10 meal boxes or 5 kg");
            TextBox locationBox = AddField(layout, "Pickup location", "Area, street, or pickup address");

            Label expiryLabel = new Label { Text = "Expiry / use-by date", AutoSize = true, ForeColor = textColor, Margin = new Padding(0, 0, 0, 5) };
            AddControl(layout, expiryLabel);
            DateTimePicker expiryPicker = new DateTimePicker();
            expiryPicker.Format = DateTimePickerFormat.Short;
            expiryPicker.MinDate = DateTime.Today;
            expiryPicker.Value = DateTime.Today.AddDays(1);
            expiryPicker.Dock = DockStyle.Top;
            expiryPicker.Margin = new Padding(0, 0, 0, 14);
            AddControl(layout, expiryPicker);

            Label notesLabel = new Label { Text = "Additional notes (optional)", AutoSize = true, ForeColor = textColor, Margin = new Padding(0, 0, 0, 5) };
            AddControl(layout, notesLabel);
            TextBox notesBox = new TextBox();
            notesBox.Multiline = true;
            notesBox.Height = 70;
            notesBox.MaxLength = 500;
            notesBox.ScrollBars = ScrollBars.Vertical;
            notesBox.Dock = DockStyle.Top;
            notesBox.Margin = new Padding(0, 0, 0, 18);
            AddControl(layout, notesBox);

            FlowLayoutPanel buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, FlowDirection = FlowDirection.RightToLeft, WrapContents = true };
            Button saveButton = CreateActionButton("Save Donation", darkGreen, Color.White);
            Button cancelButton = CreateActionButton("Cancel", Color.White, darkGreen);
            cancelButton.Click += (sender, e) => dialog.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(saveButton);
            buttons.Controls.Add(cancelButton);
            AddControl(layout, buttons);

            saveButton.Click += async (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(foodNameBox.Text) ||
                    string.IsNullOrWhiteSpace(quantityBox.Text) ||
                    string.IsNullOrWhiteSpace(locationBox.Text))
                {
                    ShowValidationError("Complete the food name, quantity, and pickup location.");
                    return;
                }

                saveButton.Enabled = false;
                saveButton.Text = "Saving...";
                try
                {
                    apiClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", currentToken);
                    var request = new CreateDonationRequest
                    {
                        FoodName = foodNameBox.Text.Trim(),
                        Category = categoryBox.SelectedItem?.ToString() ?? "Other",
                        Quantity = quantityBox.Text.Trim(),
                        PickupLocation = locationBox.Text.Trim(),
                        ExpiryDate = expiryPicker.Value.Date.ToString("yyyy-MM-dd"),
                        Notes = notesBox.Text.Trim()
                    };

                    HttpResponseMessage response = await apiClient.PostAsJsonAsync(apiBaseUrl + "/api/donations", request);
                    if (!response.IsSuccessStatusCode)
                    {
                        ShowValidationError(await GetErrorMessage(response));
                        return;
                    }

                    MessageBox.Show("Your food donation has been saved.", "FoodShare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dialog.DialogResult = DialogResult.OK;
                    await LoadDonationsAsync();
                }
                catch (HttpRequestException)
                {
                    ShowValidationError("Could not connect to the FoodShare API. Make sure the API is running.");
                }
                finally
                {
                    saveButton.Enabled = true;
                    saveButton.Text = "Save Donation";
                }
            };

            dialog.AcceptButton = saveButton;
            dialog.CancelButton = cancelButton;
            dialog.ShowDialog(this);
        }

        private int? GetSelectedDonationId()
        {
            if (donationsGrid == null || donationsGrid.SelectedRows.Count == 0)
            {
                ShowValidationError("Please select a donation from the table first.");
                return null;
            }

            object? value = donationsGrid.SelectedRows[0].Cells["Id"].Value;
            if (value == null || !int.TryParse(value.ToString(), out int donationId))
            {
                ShowValidationError("Could not read the selected donation ID. Refresh the list and try again.");
                return null;
            }

            return donationId;
        }

        private async Task RequestSelectedDonationAsync()
        {
            if (currentUser?.Role != "Receiver")
            {
                ShowValidationError("Only receiver accounts can request food.");
                return;
            }

            int? donationId = GetSelectedDonationId();
            if (donationId == null) return;

            string message = Microsoft.VisualBasic.Interaction.InputBox(
                "Add a short message for the donor (optional):",
                "Request Food",
                "I would like to request this food donation.");

            if (string.IsNullOrWhiteSpace(message))
            {
                if (MessageBox.Show("Send the request without a message?", "Request Food",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                message = "";
            }

            try
            {
                SetAuthorizationHeader();
                HttpResponseMessage response = await apiClient.PostAsJsonAsync(
                    $"{apiBaseUrl}/api/donations/{donationId.Value}/requests",
                    new CreateDonationRequestMessage { Message = message.Trim() });

                if (!response.IsSuccessStatusCode)
                {
                    ShowValidationError(await GetErrorMessage(response));
                    return;
                }

                MessageBox.Show("Your food request has been sent to the donor.",
                    "FoodShare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadDonationsAsync();
            }
            catch (HttpRequestException)
            {
                ShowValidationError("Could not connect to the FoodShare API. Make sure it is running.");
            }
        }

        private async Task OpenMyRequestsAsync()
        {
            try
            {
                SetAuthorizationHeader();
                HttpResponseMessage response = await apiClient.GetAsync(
                    apiBaseUrl + "/api/donation-requests/mine");

                if (!response.IsSuccessStatusCode)
                {
                    ShowValidationError(await GetErrorMessage(response));
                    return;
                }

                List<DonationRequestResponse> requests =
                    await response.Content.ReadFromJsonAsync<List<DonationRequestResponse>>()
                    ?? new List<DonationRequestResponse>();

                ShowRequestsDialog("My Food Requests", requests, false);
            }
            catch (HttpRequestException)
            {
                ShowValidationError("Could not connect to the FoodShare API.");
            }
            catch (JsonException)
            {
                ShowValidationError("The API returned an unexpected response.");
            }
        }

        private async Task OpenDonorRequestsAsync()
        {
            if (currentUser?.Role != "Donor")
            {
                ShowValidationError("Only donor accounts can review incoming requests.");
                return;
            }

            try
            {
                SetAuthorizationHeader();

                // Load all donations owned by this donor first. The previous version
                // only fetched requests for the currently selected donation row, which
                // made requests on other donations appear to be missing.
                HttpResponseMessage donationsResponse = await apiClient.GetAsync(
                    apiBaseUrl + "/api/donations/mine");

                if (!donationsResponse.IsSuccessStatusCode)
                {
                    ShowValidationError(await GetErrorMessage(donationsResponse));
                    return;
                }

                List<DonationResponse> donorDonations =
                    await donationsResponse.Content.ReadFromJsonAsync<List<DonationResponse>>()
                    ?? new List<DonationResponse>();

                if (donorDonations.Count == 0)
                {
                    ShowRequestsDialog("Incoming Donation Requests", new List<DonationRequestResponse>(), true);
                    return;
                }

                List<DonationRequestResponse> allRequests = new List<DonationRequestResponse>();

                foreach (DonationResponse donation in donorDonations)
                {
                    HttpResponseMessage requestsResponse = await apiClient.GetAsync(
                        $"{apiBaseUrl}/api/donations/{donation.Id}/requests");

                    if (!requestsResponse.IsSuccessStatusCode)
                    {
                        // Keep requests successfully loaded for other donations, but report
                        // a useful error if an endpoint fails rather than silently hiding it.
                        ShowValidationError(
                            $"Could not load requests for donation '{donation.FoodName}'.\n" +
                            await GetErrorMessage(requestsResponse));
                        return;
                    }

                    List<DonationRequestResponse> donationRequests =
                        await requestsResponse.Content.ReadFromJsonAsync<List<DonationRequestResponse>>()
                        ?? new List<DonationRequestResponse>();

                    allRequests.AddRange(donationRequests);
                }

                // Protect the view from duplicate rows if the API ever returns an overlap.
                List<DonationRequestResponse> uniqueRequests = allRequests
                    .GroupBy(request => request.Id)
                    .Select(group => group.First())
                    .OrderByDescending(request => request.CreatedAtUtc)
                    .ToList();

                ShowRequestsDialog("Incoming Donation Requests", uniqueRequests, true);
            }
            catch (HttpRequestException)
            {
                ShowValidationError("Could not connect to the FoodShare API. Make sure it is running.");
            }
            catch (JsonException)
            {
                ShowValidationError("The API returned an unexpected response while loading incoming requests.");
            }
        }

        private void SetAuthorizationHeader()
        {
            apiClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", currentToken);
        }

        private void ShowRequestsDialog(string title, List<DonationRequestResponse> requests, bool donorMode)
        {
            using Form dialog = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(950, 560),
                MinimumSize = new Size(740, 400),
                BackColor = lightBackground,
                Font = new Font("Segoe UI", 10)
            };

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                ColumnCount = 1,
                RowCount = 4,
                BackColor = lightBackground
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            dialog.Controls.Add(layout);

            Label heading = new Label
            {
                Text = title,
                AutoSize = true,
                ForeColor = darkGreen,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 5)
            };
            layout.Controls.Add(heading, 0, 0);

            FlowLayoutPanel filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 4, 0, 14),
                Padding = new Padding(0, 4, 0, 4)
            };
            Label filterLabel = new Label
            {
                Text = "Filter by status:", AutoSize = true, ForeColor = textColor,
                TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 8, 10, 0)
            };
            ComboBox statusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 190,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.White,
                ForeColor = darkGreen,
                Margin = new Padding(0, 0, 16, 0)
            };
            statusFilter.Items.AddRange(new object[] { "All statuses", "Pending", "Approved", "Rejected" });
            statusFilter.SelectedIndex = 0;
            filterPanel.Controls.Add(filterLabel);
            filterPanel.Controls.Add(statusFilter);
            layout.Controls.Add(filterPanel, 0, 1);

            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 42,
                RowTemplate = { Height = 36 },
                GridColor = surfaceBorder
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = darkGreen;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            grid.DefaultCellStyle.Padding = new Padding(6, 2, 6, 2);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 250, 247);
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Request ID", DataPropertyName = "Id", FillWeight = 55 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Food", DataPropertyName = "FoodName", FillWeight = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = donorMode ? "Receiver" : "Donor",
                DataPropertyName = donorMode ? "ReceiverName" : "DonorName",
                FillWeight = 100
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Message", DataPropertyName = "Message", FillWeight = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "Status", FillWeight = 75 });

            BindingSource requestSource = new BindingSource();
            requestSource.DataSource = requests;
            grid.DataSource = requestSource;
            grid.CellFormatting += (sender, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].DataPropertyName != "Status") return;
                string status = Convert.ToString(e.Value) ?? "";
                if (status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(30, 110, 66);
                    e.CellStyle.BackColor = Color.FromArgb(235, 247, 238);
                }
                else if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(125, 83, 26);
                    e.CellStyle.BackColor = Color.FromArgb(252, 244, 226);
                }
                else if (status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(150, 55, 55);
                    e.CellStyle.BackColor = Color.FromArgb(253, 235, 235);
                }
            };
            layout.Controls.Add(grid, 0, 2);

            void ApplyRequestFilter()
            {
                string selected = Convert.ToString(statusFilter.SelectedItem) ?? "All statuses";
                List<DonationRequestResponse> filtered = selected == "All statuses"
                    ? requests
                    : requests.Where(r => string.Equals(r.Status, selected, StringComparison.OrdinalIgnoreCase)).ToList();
                requestSource.DataSource = filtered;
                heading.Text = $"{title}  ·  {filtered.Count} request(s)";
                if (filtered.Count == 0)
                    heading.Text += " — no requests match this filter";
            }
            statusFilter.SelectedIndexChanged += (sender, e) => ApplyRequestFilter();
            ApplyRequestFilter();

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true,
                Margin = new Padding(0, 14, 0, 0)
            };
            if (donorMode)
            {
                Button approveButton = CreateActionButton("Approve Selected", darkGreen, Color.White);
                Button rejectButton = CreateActionButton("Reject Selected", Color.White, darkGreen);
                approveButton.Click += async (sender, e) => await UpdateRequestStatusAsync(dialog, grid, true);
                rejectButton.Click += async (sender, e) => await UpdateRequestStatusAsync(dialog, grid, false);
                buttons.Controls.Add(approveButton);
                buttons.Controls.Add(rejectButton);
            }
            Button closeButton = CreateActionButton("Close", Color.White, darkGreen);
            closeButton.Click += (sender, e) => dialog.Close();
            buttons.Controls.Add(closeButton);
            layout.Controls.Add(buttons, 0, 3);

            dialog.ShowDialog(this);
        }

        private async Task UpdateRequestStatusAsync(Form dialog, DataGridView grid, bool approve)
        {
            if (grid.SelectedRows.Count == 0)
            {
                ShowValidationError("Select a request first.");
                return;
            }

            object? value = grid.SelectedRows[0].Cells["Id"].Value;
            if (value == null || !int.TryParse(value.ToString(), out int requestId))
            {
                ShowValidationError("Could not read the request ID.");
                return;
            }

            string action = approve ? "approve" : "reject";
            DialogResult confirm = MessageBox.Show(
                $"Are you sure you want to {action} request #{requestId}?",
                "Confirm Request Action", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                SetAuthorizationHeader();
                HttpResponseMessage response = await apiClient.PostAsync(
                    $"{apiBaseUrl}/api/donation-requests/{requestId}/{action}", null);

                if (!response.IsSuccessStatusCode)
                {
                    ShowValidationError(await GetErrorMessage(response));
                    return;
                }

                MessageBox.Show(
                    approve ? "Request approved. The donation is now reserved." : "Request rejected.",
                    "FoodShare", MessageBoxButtons.OK, MessageBoxIcon.Information);

                dialog.Close();
                await LoadDonationsAsync();
            }
            catch (HttpRequestException)
            {
                ShowValidationError("Could not connect to the FoodShare API.");
            }
        }

        private void CreateSummaryCard(
            FlowLayoutPanel parent,
            string iconText,
            string titleText,
            out Label titleLabel,
            out Label valueLabel)
        {
            // Simple, spacious summary cards: no narrow text badges, so labels never wrap awkwardly.
            Panel card = new Panel();
            card.Width = 202;
            card.Height = 112;
            card.Margin = new Padding(0, 0, 10, 10);
            card.Padding = new Padding(14, 9, 12, 9);
            card.BackColor = Color.White;
            card.BorderStyle = BorderStyle.FixedSingle;
            ApplyRoundedCorners(card, 16);

            Panel accentLine = new Panel();
            accentLine.Dock = DockStyle.Left;
            accentLine.Width = 5;
            accentLine.BackColor = accentGreen;
            accentLine.Margin = new Padding(0);
            card.Controls.Add(accentLine);

            TableLayoutPanel cardLayout = new TableLayoutPanel();
            cardLayout.Dock = DockStyle.Fill;
            cardLayout.Margin = new Padding(8, 0, 0, 0);
            cardLayout.Padding = new Padding(4, 0, 0, 0);
            cardLayout.ColumnCount = 1;
            cardLayout.RowCount = 2;
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            card.Controls.Add(cardLayout);

            valueLabel = new Label();
            valueLabel.Text = "—";
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.TextAlign = ContentAlignment.BottomLeft;
            valueLabel.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            valueLabel.ForeColor = darkGreen;
            valueLabel.Margin = new Padding(0);
            cardLayout.Controls.Add(valueLabel, 0, 0);

            titleLabel = new Label();
            titleLabel.Text = titleText;
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.TextAlign = ContentAlignment.TopLeft;
            titleLabel.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            titleLabel.ForeColor = mutedText;
            titleLabel.AutoEllipsis = true;
            titleLabel.Margin = new Padding(0, 2, 0, 0);
            cardLayout.Controls.Add(titleLabel, 0, 1);

            parent.Controls.Add(card);
        }

        private void UpdateSummaryCards(List<DonationResponse> donations)
        {
            if (statPrimaryValueLabel == null || statSecondaryValueLabel == null || statTertiaryValueLabel == null)
                return;

            if (currentUser?.Role == "Donor")
            {
                int total = donations.Count;
                int available = donations.Count(d => string.Equals(d.Status, "Available", StringComparison.OrdinalIgnoreCase));
                int reserved = donations.Count(d => string.Equals(d.Status, "Reserved", StringComparison.OrdinalIgnoreCase));

                statPrimaryTitleLabel!.Text = "Your listed donations";
                statSecondaryTitleLabel!.Text = "Currently available";
                statTertiaryTitleLabel!.Text = "Reserved by receivers";
                statPrimaryValueLabel.Text = total.ToString();
                statSecondaryValueLabel.Text = available.ToString();
                statTertiaryValueLabel.Text = reserved.ToString();
            }
            else
            {
                int available = donations.Count;
                int categories = donations.Select(d => d.Category).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                int expiringSoon = donations.Count(d => d.ExpiryDateUtc.ToLocalTime().Date <= DateTime.Now.Date.AddDays(2));

                statPrimaryTitleLabel!.Text = "Food listings to explore";
                statSecondaryTitleLabel!.Text = "Food categories";
                statTertiaryTitleLabel!.Text = "Expiring within 2 days";
                statPrimaryValueLabel.Text = available.ToString();
                statSecondaryValueLabel.Text = categories.ToString();
                statTertiaryValueLabel.Text = expiringSoon.ToString();
            }
        }

        private async Task LoadDonationsAsync()
        {
            if (currentUser == null || donationsGrid == null || donationsStatusLabel == null)
                return;

            donationsStatusLabel.Text = "Loading donations...";
            try
            {
                apiClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", currentToken);

                string endpoint = currentUser.Role == "Donor"
                    ? "/api/donations/mine"
                    : "/api/donations/available";

                HttpResponseMessage response = await apiClient.GetAsync(apiBaseUrl + endpoint);
                if (!response.IsSuccessStatusCode)
                {
                    donationsStatusLabel.Text = await GetErrorMessage(response);
                    return;
                }

                List<DonationResponse>? donations = await response.Content.ReadFromJsonAsync<List<DonationResponse>>();
                donations ??= new List<DonationResponse>();
                UpdateSummaryCards(donations);
                donationsGrid.DataSource = null;
                donationsGrid.DataSource = donations.Select(donation => new DonationGridRow
                {
                    Id = donation.Id,
                    FoodName = donation.FoodName,
                    Category = donation.Category,
                    Quantity = donation.Quantity,
                    PickupLocation = donation.PickupLocation,
                    ExpiryDateUtc = donation.ExpiryDateUtc.ToLocalTime().ToString("d"),
                    Status = donation.Status
                }).ToList();

                donationsStatusLabel.Text = donations.Count == 0
                    ? (currentUser.Role == "Donor" ? "You have not added any donations yet. Select ‘Add Food Donation’ to begin." : "No available donations were found. Check again later.")
                    : $"Showing {donations.Count} donation(s).";
            }
            catch (HttpRequestException)
            {
                donationsStatusLabel.Text = "Could not connect to the FoodShare API. Make sure it is running.";
            }
            catch (System.Text.Json.JsonException)
            {
                donationsStatusLabel.Text = "The API returned an unexpected response. Please restart the API and try again.";
            }
        }

        private TextBox AddField(
            TableLayoutPanel layout,
            string labelText,
            string placeholder,
            bool password = false)
        {
            Label label = new Label();
            label.Text = labelText;
            label.AutoSize = true;
            label.ForeColor = textColor;
            label.Margin = new Padding(0, 0, 0, 5);

            AddControl(layout, label);

            TextBox textBox = new TextBox();
            textBox.Dock = DockStyle.Top;
            textBox.Font = new Font("Segoe UI", 11);
            textBox.BackColor = Color.White;
            textBox.ForeColor = textColor;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.PlaceholderText = placeholder;
            textBox.UseSystemPasswordChar = password;
            textBox.Margin = new Padding(0, 0, 0, 16);

            AddControl(layout, textBox);

            return textBox;
        }

        private Image? LoadDashboardImage(string fileName)
        {
            // Search both the build output folder and the source project folder.
            // This also works when Visual Studio/dotnet has not copied Assets yet.
            try
            {
                List<string> candidatePaths = new List<string>
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", fileName),
                    Path.Combine(Environment.CurrentDirectory, "Assets", fileName),
                    Path.Combine(Environment.CurrentDirectory, "FoodDonation.Client", "Assets", fileName)
                };

                string? basePath = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 8 && !string.IsNullOrWhiteSpace(basePath); i++)
                {
                    candidatePaths.Add(Path.Combine(basePath, "Assets", fileName));
                    candidatePaths.Add(Path.Combine(basePath, "FoodDonation.Client", "Assets", fileName));
                    DirectoryInfo? parent = Directory.GetParent(basePath);
                    basePath = parent?.FullName;
                }

                string? path = candidatePaths.FirstOrDefault(File.Exists);
                if (path == null)
                    return null;

                // Detach the bitmap from its stream so it remains valid after loading.
                using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using MemoryStream memory = new MemoryStream();
                stream.CopyTo(memory);
                memory.Position = 0;
                using Image source = Image.FromStream(memory);
                return new Bitmap(source);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FoodShare image load failed for {fileName}: {ex.Message}");
                return null;
            }
        }

        private void ApplyRoundedCorners(Control control, int radius)
        {
            void UpdateRegion(object? sender, EventArgs e)
            {
                if (control.Width <= 0 || control.Height <= 0)
                    return;

                int diameter = Math.Min(radius * 2, Math.Min(control.Width, control.Height));
                using GraphicsPath path = new GraphicsPath();
                path.StartFigure();
                path.AddArc(0, 0, diameter, diameter, 180, 90);
                path.AddArc(control.Width - diameter, 0, diameter, diameter, 270, 90);
                path.AddArc(control.Width - diameter, control.Height - diameter, diameter, diameter, 0, 90);
                path.AddArc(0, control.Height - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                Region? oldRegion = control.Region;
                control.Region = new Region(path);
                oldRegion?.Dispose();
            }

            control.Resize += UpdateRegion;
            UpdateRegion(control, EventArgs.Empty);
        }

        private void AddControl(TableLayoutPanel layout, Control control)
        {
            int row = layout.RowCount;

            layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            control.Anchor = AnchorStyles.Left |
                             AnchorStyles.Right |
                             AnchorStyles.Top;

            layout.Controls.Add(control, 0, row);
        }

        private string GetCountryCode(string country)
        {
            int start = country.LastIndexOf('(');
            int end = country.LastIndexOf(')');

            if (start >= 0 && end > start)
                return country.Substring(start + 1, end - start - 1);

            return "+92";
        }

        private bool IsStrongPassword(string password)
        {
            return password.Length >= 8 &&
                   password.Length <= 128 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(c => !char.IsLetterOrDigit(c));
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                MailAddress address = new MailAddress(email);
                return address.Address.Equals(
                    email, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool ContainsOnlyDigits(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.All(char.IsDigit);
        }

        private void ShowValidationError(string message)
        {
            MessageBox.Show(
                message,
                "FoodShare",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    public class CreateDonationRequestMessage
    {
        public string Message { get; set; } = "";
    }

    public class DonationRequestResponse
    {
        public int Id { get; set; }
        public int DonationId { get; set; }
        public int ReceiverUserId { get; set; }
        public string FoodName { get; set; } = "";
        public string ReceiverName { get; set; } = "";
        public string DonorName { get; set; } = "";
        public string Message { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }

    public class CreateDonationRequest
    {
        public string FoodName { get; set; } = "";
        public string Category { get; set; } = "";
        public string Quantity { get; set; } = "";
        public string PickupLocation { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public string Notes { get; set; } = "";
    }

    public class DonationResponse
    {
        public int Id { get; set; }
        public string FoodName { get; set; } = "";
        public string Category { get; set; } = "";
        public string Quantity { get; set; } = "";
        public string PickupLocation { get; set; } = "";
        public DateTime ExpiryDateUtc { get; set; }
        public string Notes { get; set; } = "";
        public string Status { get; set; } = "Available";
        public string? DonorName { get; set; }
    }

    public class DonationGridRow
    {
        public int Id { get; set; }
        public string FoodName { get; set; } = "";
        public string Category { get; set; } = "";
        public string Quantity { get; set; } = "";
        public string PickupLocation { get; set; } = "";
        public string ExpiryDateUtc { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public class RegisterRequest
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string CountryCode { get; set; } = "";
        public string Role { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LoginResponse
    {
        public string Message { get; set; } = "";
        public string Token { get; set; } = "";
        public UserResponse? User { get; set; }
    }

    public class UserResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Role { get; set; } = "";
    }
}