namespace Keyhooker_V2;

public class ConfigForm : Form
{
    private readonly DataGridView _grid;
    private readonly List<Keybinding> _bindings;
    private bool _isRecording;

    public ConfigForm()
    {
        Text = "Keyhooker V2 - Configure";
        Size = new Size(620, 400);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _bindings = RegistryConfig.LoadBindings();

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.None
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Keys (click to record)",
            Name = "Keys",
            FillWeight = 30
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Command",
            Name = "Command",
            FillWeight = 40
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Arguments",
            Name = "Args",
            FillWeight = 30
        });

        _grid.EditingControlShowing += OnEditingControlShowing;

        RefreshGrid();

        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 45
        };

        var addBtn = new Button { Text = "Add", Width = 80, Location = new Point(10, 8) };
        var removeBtn = new Button { Text = "Remove", Width = 80, Location = new Point(96, 8) };
        var saveBtn = new Button { Text = "Save", Width = 80, Location = new Point(424, 8) };
        var cancelBtn = new Button { Text = "Cancel", Width = 80, Location = new Point(510, 8) };

        addBtn.Click += (_, _) => AddBinding();
        removeBtn.Click += (_, _) => RemoveBinding();
        saveBtn.Click += (_, _) => Save();
        cancelBtn.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        bottomPanel.Controls.AddRange([addBtn, removeBtn, saveBtn, cancelBtn]);

        Controls.Add(_grid);
        Controls.Add(bottomPanel);

        AcceptButton = saveBtn;
        CancelButton = cancelBtn;
    }

    private void OnEditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (e.Control is not TextBox tb) return;

        // Unhook previous handlers to prevent stacking across edits
        tb.KeyDown -= OnKeysKeyDown;
        tb.PreviewKeyDown -= OnKeysPreviewKeyDown;
        tb.ReadOnly = false;

        bool isKeysColumn = _grid.CurrentCell?.ColumnIndex == _grid.Columns["Keys"]!.Index;
        _isRecording = isKeysColumn;

        if (!isKeysColumn) return;

        tb.Text = "Press a key combination...";
        tb.ReadOnly = true;
        tb.KeyDown += OnKeysKeyDown;
        tb.PreviewKeyDown += OnKeysPreviewKeyDown;
    }

    private void OnKeysPreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
    {
        // Claim all keys as input so they reach our KeyDown handler
        // instead of being processed by the grid or form (including Escape)
        e.IsInputKey = true;
    }

    private void OnKeysKeyDown(object? sender, KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        e.Handled = true;

        // Escape cancels recording without closing the form
        if (e.KeyCode == Keys.Escape)
        {
            if (sender is TextBox tb)
                tb.ReadOnly = false;
            _grid.CancelEdit();
            return;
        }

        // Ignore standalone modifier presses — just show what's held so far
        if (IsModifierKey(e.KeyCode))
        {
            if (sender is TextBox tb)
                tb.Text = FormatModifiers(e) is string mods and not ""
                    ? mods + "+..."
                    : "Press a key combination...";
            return;
        }

        // Build the full combination string
        string? keyName = MapKeyName(e.KeyCode);
        if (keyName == null) return;

        string combo = FormatModifiers(e) is string prefix and not ""
            ? prefix + "+" + keyName
            : keyName;

        // Commit to cell and end edit
        if (sender is TextBox textBox)
        {
            textBox.ReadOnly = false;
            textBox.Text = combo;
        }

        if (_grid.CurrentCell != null)
        {
            _grid.CurrentCell.Value = combo;
            _grid.EndEdit();
        }
    }

    private static bool IsModifierKey(Keys key) =>
        key is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
            or Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.Menu or Keys.LMenu or Keys.RMenu
            or Keys.LWin or Keys.RWin;

    private static string FormatModifiers(KeyEventArgs e)
    {
        var parts = new List<string>(3);
        if (e.Control) parts.Add("Ctrl");
        if (e.Alt) parts.Add("Alt");
        if (e.Shift) parts.Add("Shift");
        return string.Join("+", parts);
    }

    private static string? MapKeyName(Keys key) => key switch
    {
        >= Keys.A and <= Keys.Z => key.ToString(),
        >= Keys.D0 and <= Keys.D9 => ((char)('0' + (key - Keys.D0))).ToString(),
        >= Keys.F1 and <= Keys.F12 => key.ToString(),
        >= Keys.NumPad0 and <= Keys.NumPad9 => "Numpad" + (key - Keys.NumPad0),
        Keys.Space => "Space",
        Keys.Enter or Keys.Return => "Enter",
        Keys.Tab => "Tab",
        Keys.Back => "Backspace",
        Keys.Delete => "Delete",
        Keys.Insert => "Insert",
        Keys.Home => "Home",
        Keys.End => "End",
        Keys.PageUp => "PageUp",
        Keys.PageDown => "PageDown",
        Keys.Up => "Up",
        Keys.Down => "Down",
        Keys.Left => "Left",
        Keys.Right => "Right",
        Keys.PrintScreen => "PrintScreen",
        Keys.Scroll => "ScrollLock",
        Keys.Pause => "Pause",
        _ => null
    };

    private void RefreshGrid()
    {
        _grid.Rows.Clear();
        foreach (var b in _bindings)
            _grid.Rows.Add(b.Keys, b.Command, b.Args);
    }

    private void AddBinding()
    {
        _bindings.Add(new Keybinding());
        int rowIdx = _grid.Rows.Add("", "", "");
        _grid.CurrentCell = _grid.Rows[rowIdx].Cells[0];
        _grid.BeginEdit(true);
    }

    private void RemoveBinding()
    {
        if (_grid.CurrentRow == null || _grid.CurrentRow.Index >= _bindings.Count)
            return;

        int idx = _grid.CurrentRow.Index;
        _bindings.RemoveAt(idx);
        _grid.Rows.RemoveAt(idx);
    }

    private void Save()
    {
        _grid.EndEdit();

        var validated = new List<Keybinding>();
        for (int i = 0; i < _grid.Rows.Count; i++)
        {
            var row = _grid.Rows[i];
            var keys = row.Cells["Keys"].Value?.ToString()?.Trim() ?? "";
            var cmd = row.Cells["Command"].Value?.ToString()?.Trim() ?? "";
            var args = row.Cells["Args"].Value?.ToString()?.Trim() ?? "";

            if (string.IsNullOrEmpty(keys) && string.IsNullOrEmpty(cmd))
                continue;

            if (string.IsNullOrEmpty(keys) || string.IsNullOrEmpty(cmd))
            {
                MessageBox.Show($"Row {i + 1}: Keys and Command are both required.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _grid.CurrentCell = row.Cells[string.IsNullOrEmpty(keys) ? "Keys" : "Command"];
                return;
            }

            if (!HotkeyManager.TryParseKeys(keys, out _, out _))
            {
                MessageBox.Show(
                    $"Row {i + 1}: \"{keys}\" is not a valid key combination.\n\n" +
                    "Examples: Ctrl+Alt+T, Ctrl+Shift+F1, Alt+Space",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _grid.CurrentCell = row.Cells["Keys"];
                return;
            }

            validated.Add(new Keybinding { Id = i, Keys = keys, Command = cmd, Args = args });
        }

        RegistryConfig.SaveBindings(validated);
        DialogResult = DialogResult.OK;
        Close();
    }
}
