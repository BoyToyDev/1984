using System.Drawing;
using System.Windows.Forms;

namespace ProductivityTracker;

internal sealed class FirstRunSetupForm : Form
{
    private readonly TextBox _passwordBox = new();
    private readonly TextBox _confirmPasswordBox = new();

    public string Password => _passwordBox.Text;

    public FirstRunSetupForm(string productName)
    {
        Text = $"Set up {productName}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(420, 210);

        var titleLabel = new Label
        {
            Text = "Create local access password",
            AutoSize = true,
            Left = 16,
            Top = 16,
            Font = new Font(Font, FontStyle.Bold)
        };

        var passwordLabel = new Label { Text = "Password:", AutoSize = true, Left = 16, Top = 58 };
        _passwordBox.Left = 150;
        _passwordBox.Top = 54;
        _passwordBox.Width = 240;
        _passwordBox.UseSystemPasswordChar = true;

        var confirmLabel = new Label { Text = "Confirm password:", AutoSize = true, Left = 16, Top = 94 };
        _confirmPasswordBox.Left = 150;
        _confirmPasswordBox.Top = 90;
        _confirmPasswordBox.Width = 240;
        _confirmPasswordBox.UseSystemPasswordChar = true;

        var noteLabel = new Label
        {
            Text = "This password is required to open the app UI and close the tray app.",
            AutoSize = false,
            Left = 16,
            Top = 130,
            Width = 374,
            Height = 34
        };

        var okButton = new Button
        {
            Text = "Save",
            Left = 220,
            Width = 80,
            Top = 170
        };
        okButton.Click += (_, _) => Save();

        var cancelButton = new Button
        {
            Text = "Exit",
            DialogResult = DialogResult.Cancel,
            Left = 310,
            Width = 80,
            Top = 170
        };

        AcceptButton = okButton;
        CancelButton = cancelButton;
        Controls.Add(titleLabel);
        Controls.Add(passwordLabel);
        Controls.Add(_passwordBox);
        Controls.Add(confirmLabel);
        Controls.Add(_confirmPasswordBox);
        Controls.Add(noteLabel);
        Controls.Add(okButton);
        Controls.Add(cancelButton);
    }

    private void Save()
    {
        if (_passwordBox.Text.Length < 8)
        {
            MessageBox.Show("Password must contain at least 8 characters.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_passwordBox.Text != _confirmPasswordBox.Text)
        {
            MessageBox.Show("Passwords do not match.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
