using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ByteWriter
{
    public partial class Form_main : Form
    {
        Color primary_foreground = Color.White;
        Color primary_background = Color.FromArgb(255, 50, 50, 50);
        Color secondary_background = Color.FromArgb(255, 40, 40, 40);

        private string filepath = null;
        private bool _saved = true;
        private bool Saved
        {
            get
            {
                return _saved;
            }
            set
            {
                toolStripStatusLabel_saved.Text = value ? "Saved" : "Not saved";
                _saved = value;
            }
        }

        public Form_main()
        {
            InitializeComponent();

            ForeColor = primary_foreground;
            BackColor = primary_background;
            statusStrip_main.BackColor = primary_background;
            richTextBox_editor.ForeColor = primary_foreground;
            richTextBox_editor.BackColor = secondary_background;
            richTextBox_result.ForeColor = primary_foreground;
            richTextBox_result.BackColor = secondary_background;
            menuStrip_main.ForeColor = primary_foreground;
            menuStrip_main.BackColor = primary_background;
        }

        private void richTextBox_main_TextChanged(object sender, EventArgs e)
        {
            richTextBox_editor.Focus();

            int index = richTextBox_editor.SelectionStart;
            string f = Format(richTextBox_editor.Text, richTextBox_editor.SelectionStart, ref index);
            if (richTextBox_editor.Text != f) richTextBox_editor.Text = f;
            richTextBox_editor.SelectionStart = index;

            UpdateResult();

            Saved = false;
        }

        private void UpdateResult()
        {
            richTextBox_result.Text = Encoding.ASCII.GetString(ReadBinary(richTextBox_editor.Text));
        }

        private string Format(string text, int cursor, ref int index)
        {
            int len;
            for (len = text.Length - 1; len >= 0 ; len--)
            {
                char c = text[len];
                if (c == '1' || c == '0') break;
            }

            string r = string.Empty;
            int bits = 0;
            for (int i = 0; i <= len; i++)
            {
                char d = text[i];
                if (d == '1' || d == '0')
                {
                    r += d;
                    bits++;
                    if (bits >= 4 && i < len)
                    {
                        r += ' ';
                        bits = 0;
                    }
                }
            }
            if (cursor == r.Length - 1) index++;

            return r;
        }

        private byte[] ReadBinary(string text)
        {
            List<byte> content = new List<byte>();
            byte cb = 0;
            int bit = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char d = text[i];
                if (d == '1' || d == '0')
                {
                    byte b = (byte)(d == '1' ? 1 : 0);
                    cb = (byte)((cb << 1) + b);
                    bit++;
                    if (bit == 8)
                    {
                        content.Add(cb);
                        cb = 0;
                        bit = 0;
                    }
                    else if (i == text.Length - 1)
                    {
                        cb <<= 8 - bit;
                        content.Add(cb);
                    }
                }
            }
            return content.ToArray();
        }

        private void PrintBinary(byte[] data)
        {
            richTextBox_editor.Text = "";
            for (int i = 0; i < data.Length; i++)
            {
                string b1 = Convert.ToString(data[i] >> 4, 2).PadLeft(4, '0');
                string b2 = Convert.ToString(data[i] & 0xF, 2).PadLeft(4, '0');
                richTextBox_editor.Text += b1 + ' ' + b2;
                if (i < data.Length - 1) richTextBox_editor.Text += ' ';

                /*
                string binary = Convert.ToString(data[i], 2).PadLeft(8, '0');
                richTextBox_main.Text += binary.Substring(0, 4) + ' ' + binary.Substring(4, 4);
                if (i < data.Length - 1) richTextBox_main.Text += ' ';
                */
            }
        }

        private void richTextBox_main_KeyDown(object sender, KeyEventArgs e)
        {
            if (
                e.KeyCode == Keys.D1     ||
                e.KeyCode == Keys.D0     ||
                e.KeyCode == Keys.Back   ||
                e.KeyCode == Keys.Delete ||
                e.KeyCode == Keys.Left   ||
                e.KeyCode == Keys.Up     ||
                e.KeyCode == Keys.Right  ||
                e.KeyCode == Keys.Down   ||
                (e.Control && (
                e.KeyCode == Keys.A ||
                e.KeyCode == Keys.C ||
                e.KeyCode == Keys.V ||
                e.KeyCode == Keys.Z ||
                e.KeyCode == Keys.Y
                ))
                ) return;
            else if (e.KeyCode == Keys.Space && richTextBox_editor.SelectionStart < richTextBox_editor.Text.Length)
            {
                char[] t = richTextBox_editor.Text.ToArray();
                int i = richTextBox_editor.SelectionStart;
                int l = richTextBox_editor.SelectionLength;
                if (l > 0)
                {
                    for (int j = 0; j < l; j++)
                        if (t[i + j] == '0' || t[i + j] == '1')
                            t[i + j] = (t[i + j] == '0' ? '1' : '0');
                }
                else if (t[i] == '0' || t[i] == '1')
                    t[i] = (t[i] == '0' ? '1' : '0');
                richTextBox_editor.Text = new string(t);
                richTextBox_editor.SelectionStart = i;
                richTextBox_editor.SelectionLength = l;
            }
            e.SuppressKeyPress = true;
        }

        private int GetBitIndex()
        {
            int index = 0;
            for (int i = 0; i < richTextBox_editor.SelectionStart; i++)
            {
                char d = richTextBox_editor.Text[i];
                if (d == '0' || d == '1') index++;
            }
            return index;
        }
        private void UpdateIndex()
        {
            toolStripStatusLabel_linecol.Text = "Index: " + GetBitIndex();
        }

        private void richTextBox_main_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateIndex();
        }

        private void richTextBox_main_MouseDown(object sender, MouseEventArgs e)
        {
            UpdateIndex();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void Save()
        {
            if (filepath == null)
            {
                SaveFileDialog sfd = new SaveFileDialog();

                if (sfd.ShowDialog() == DialogResult.OK) filepath = sfd.FileName;
                else return;
            }
            UpdateTitle();

            byte[] data = ReadBinary(richTextBox_editor.Text);
            File.WriteAllBytes(filepath, data);
            Saved = true;
        }

        private void PromptSave()
        {
            if (!Saved && MessageBox.Show("Do you want to save your changes?", "Not saved!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) Save();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PromptSave();

            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                filepath = ofd.FileName;
                UpdateTitle();
                PrintBinary(File.ReadAllBytes(filepath));
                Saved = true;
            }

            UpdateResult();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();

            if (sfd.ShowDialog() == DialogResult.OK) filepath = sfd.FileName;
            Save();
        }

        private void UpdateTitle()
        {
            Text = "Byte Writer | " + Path.GetFileName(filepath);
        }

        private void Form_main_Load(object sender, EventArgs e)
        {

        }

        private void richTextBox_result_Enter(object sender, EventArgs e)
        {
            richTextBox_editor.Focus();
        }

        private void richTextBox_editor_SelectionChanged(object sender, EventArgs e)
        {
            int l = richTextBox_editor.SelectionLength;
            if (l == 0) return;
            int i = GetBitIndex();
            int spaces = richTextBox_editor.SelectionStart - i;
            int bitsSelected = l - spaces;
            if (bitsSelected % 8 == 0)
            {
                richTextBox_result.SelectionStart = i / 8;
                richTextBox_result.SelectionLength = l / 8;
            }
        }
    }
}