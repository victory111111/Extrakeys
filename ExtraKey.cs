using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

public class TouchDetector
{
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    public const int VK_LBUTTON = 0x01;
    public const int VK_SHIFT = 0x10;
    public const int VK_TAB = 0x09;

    public const int KEYEVENTF_EXTENDEDKEY = 0x0001;
    public const int KEYEVENTF_KEYUP = 0x0002;

    private static readonly int TopScreenTolerance = 20;
    private static readonly int DragThreshold = 100;
    private static bool isMouseDown = false;
    private static int startY = 0;
    private static Keys keyModifier = Keys.Shift;
    private static Keys keyToPress = Keys.Tab;
    private static string settingsFilePath = "settings.txt";

    private static NotifyIcon trayIcon; // Tray icon

    public static void Main()
    {
        LoadSettings();
        StartTouchDetection();

        // Initialize NotifyIcon
        trayIcon = new NotifyIcon()
        {
            Icon = System.Drawing.SystemIcons.Information,
            ContextMenu = new ContextMenu(new MenuItem[] {
                new MenuItem("Exit", Exit)
            }),
            Visible = true
        };

        // Open the settings form on double click
        trayIcon.DoubleClick += (s, e) => ShowKeySelectionWindow();

        Application.Run();
    }

    private static void StartTouchDetection()
    {
        Thread thread = new Thread(() =>
        {
            bool hasExecutedKeyCombination = false;

            while (true)
            {
                if ((GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0)
                {
                    int y = Cursor.Position.Y;
                    if (y < TopScreenTolerance)
                    {
                        if (!isMouseDown)
                        {
                            isMouseDown = true;
                            startY = y;
                            hasExecutedKeyCombination = false;
                        }
                    }
                    else
                    {
                        if (isMouseDown && y - startY > DragThreshold && !hasExecutedKeyCombination)
                        {
                            ExecuteKeyCombination();
                            hasExecutedKeyCombination = true;
                        }
                    }
                }
                else
                {
                    isMouseDown = false;
                }
            }
        });

        thread.IsBackground = true;
        thread.Start();
    }

    private static void ExecuteKeyCombination()
    {
        keybd_event((byte)keyModifier, 0, KEYEVENTF_EXTENDEDKEY | 0, 0);
        keybd_event((byte)keyToPress, 0, KEYEVENTF_EXTENDEDKEY | 0, 0);
        Thread.Sleep(100);
        keybd_event((byte)keyToPress, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
        keybd_event((byte)keyModifier, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
    }

    private static void ShowKeySelectionWindow()
    {
        var form = new Form();
        form.Text = "Key Combination Customization";
        form.Width = 300;
        form.Height = 200;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.FormClosing += (s, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                form.Hide();
            }
        };

        var label1 = new Label();
        label1.Text = "Select Key Modifier:";
        label1.Left = 20;
        label1.Top = 20;
        form.Controls.Add(label1);

        var comboBox1 = new ComboBox();
        comboBox1.Left = 150;
        comboBox1.Top = 20;
        comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox1.Items.AddRange(Enum.GetNames(typeof(Keys)));
        comboBox1.SelectedItem = keyModifier.ToString();
        form.Controls.Add(comboBox1);

        var label2 = new Label();
        label2.Text = "Select Key to Press:";
        label2.Left = 20;
        label2.Top = 60;
        form.Controls.Add(label2);

        var comboBox2 = new ComboBox();
        comboBox2.Left = 150;
        comboBox2.Top = 60;
        comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox2.Items.AddRange(Enum.GetNames(typeof(Keys)));
        comboBox2.SelectedItem = keyToPress.ToString();
        form.Controls.Add(comboBox2);

        var button = new Button();
        button.Text = "Save";
        button.Left = 100;
        button.Top = 100;
        button.Click += (sender, e) =>
        {
            keyModifier = (Keys)Enum.Parse(typeof(Keys), comboBox1.SelectedItem.ToString());
            keyToPress = (Keys)Enum.Parse(typeof(Keys), comboBox2.SelectedItem.ToString());
            SaveSettings();
            form.Close();
        };
        form.Controls.Add(button);

        form.Show(); // Changed to show instead of Application.Run
    }

    private static void SaveSettings()
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(settingsFilePath))
            {
                writer.WriteLine(keyModifier.ToString());
                writer.WriteLine(keyToPress.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error saving settings: " + ex.Message);
        }
    }

    private static void LoadSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            try
            {
                using (StreamReader reader = new StreamReader(settingsFilePath))
                {
                    string modifier = reader.ReadLine();
                    string key = reader.ReadLine();

                    if (!string.IsNullOrEmpty(modifier) && !string.IsNullOrEmpty(key))
                    {
                        keyModifier = (Keys)Enum.Parse(typeof(Keys), modifier);
                        keyToPress = (Keys)Enum.Parse(typeof(Keys), key);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading settings: " + ex.Message);
            }
        }
    }

    // Exit method
    private static void Exit(object sender, EventArgs e)
    {
        trayIcon.Visible = false;
        Application.Exit();
    }
}
