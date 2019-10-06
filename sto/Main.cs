using CefSharp;
using CefSharp.WinForms;
using Newtonsoft.Json;
using sto.DataObjects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace sto
{
    public partial class Main : Form
    {
        string workingDir;
        List<SerieHolder> SerieHolders = new List<SerieHolder>();
        public bool Proxy { get; set; }
        public string ProxyString { get; set; }
        public static int KeepingHost { get; set; }
        internal static List<HostPriority> HostPriorities { get; set; } = new List<HostPriority>();
        internal readonly BiDictionary<HostPriority, ListViewItem> ListViewItemDic = new BiDictionary<HostPriority, ListViewItem>();
        internal readonly BiDictionary<SerieHolder, ListViewItem> ListViewItemDic2 = new BiDictionary<SerieHolder, ListViewItem>();
        public Main()
        {
            InitializeComponent();
            Captcha.Main = this;
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            foreach (var item in textBox1.Text.Split('\n').Where(str => str != null && str.Length > 0))
            {
                var holder = new SerieHolder(item) { Writer = AppendTextBox };
                SerieHolders.Add(holder);
                holder.WorkinDir = workingDir;
                holder.Start();
                ListViewItemDic2.Add(holder, new ListViewItem());
                listView2.Items.Add(ListViewItemDic2[holder]);
            }
            textBox1.Text = "";
        }
        public void AppendTextBox(string value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AppendTextBox), new object[] { value });
                return;
            }
            var selectionStart = textBox2.SelectionStart;
            var selectionLength = textBox2.SelectionLength;
            textBox2.Text += value + "\r\n";
            textBox2.SelectionStart = selectionStart;
            textBox2.SelectionLength = selectionLength;
        }
        private void Main_Shown(object sender, EventArgs e)
        {
            var d = new FolderBrowserDialog() { SelectedPath = @"C:\Users\kian\Desktop\workdir" };
            d.ShowDialog();
            workingDir = d.SelectedPath;
            LoadSetting();

            var setting = new CefSettings();
            if (Proxy)
                setting.CefCommandLineArgs.Add("proxy-server", ProxyString);
            Cef.Initialize(setting);

            foreach (var item in HostPriorities)
                ListViewItemDic.Add(item, new ListViewItem());
            listView1.Items.Clear();
            listView1.Items.AddRange(ListViewItemDic.Values.ToArray());
            foreach (var item in Directory.GetDirectories(workingDir))
                if (File.GetAttributes(item).HasFlag(FileAttributes.Directory))
                    SerieHolders.Add(SerieHolder.Load(item, workingDir));

            foreach (var item in SerieHolders)
                ListViewItemDic2.Add(item, new ListViewItem());
            listView2.Items.Clear();
            listView2.Items.AddRange(ListViewItemDic2.Values.ToArray());
            timer1.Enabled = true;
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            Enabled = false;
            new Setting(this).Show();
        }
        private void Button3_Click(object sender, EventArgs e)
        {
            textBox2.Text = "";
        }
        private void Button4_Click(object sender, EventArgs e)
        {
            var form = new Form() { Size = Size };
            var tb = new TextBox()
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Height = form.Height - 50,
                Width = form.Width - 30,
                Text = textBox2.Text,
                Top = 10,
                Left = 10
            };
            form.Controls.Add(tb);
            form.Show();

        }
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            Process.End();
            Captcha.End();
        }
        private void Button5_Click(object sender, EventArgs e)
        {
            Enabled = false;
            new Selector(this, SerieHolders).Show();
        }
        private void Button6_Click(object sender, EventArgs e)
        {
            foreach (var item in SerieHolders)
                item.StartCaptcha();
            button6.Text = "Add captcha solver";
            Captcha.AddCapcha();
        }
        public void SaveAll()
        {
            foreach (var item in SerieHolders)
                item.Save();
        }
        public void SaveSetting()
        {
            if (File.Exists(workingDir + "\\setting.json"))
                File.Delete(workingDir + "\\setting.json");
            var sw = new StreamWriter(workingDir + "\\setting.json");
            HostPriorities.Sort();
            sw.WriteLine(JsonConvert.SerializeObject(new SettingData(), Formatting.Indented));
            sw.Close();
        }
        public void LoadSetting()
        {
            if (!File.Exists(workingDir + "\\setting.json"))
                File.Copy(Directory.GetCurrentDirectory() + "\\setting.json", workingDir + "\\setting.json");
            var sr = new StreamReader(workingDir + "\\setting.json");
            JsonConvert.DeserializeObject<SettingData>(sr.ReadToEnd());
            HostPriority.Import(HostPriorities);
            sr.Close();
        }
        private void Timer1_Tick(object sender, EventArgs e)
        {
            foreach (var item in HostPriorities)
            {
                try
                {
                    if (ListViewItemDic[item].Text != item.ToString())
                        ListViewItemDic[item].Text = item.ToString();
                }
                catch (KeyNotFoundException)
                {
                    ListViewItemDic.Add(item, new ListViewItem(item.ToString()));
                    listView1.Items.Add(ListViewItemDic[item]);
                }
            }
            foreach (var item in SerieHolders)
            {
                try
                {
                    if (ListViewItemDic2[item].Text != item.ToString())
                        ListViewItemDic2[item].Text = item.ToString();
                }
                catch (KeyNotFoundException)
                {
                    ListViewItemDic2.Add(item, new ListViewItem(item.ToString()));
                    listView2.Items.Add(ListViewItemDic2[item]);
                }
            }
        }
        private void ListView1_DoubleClick(object sender, EventArgs e)
        {
            var t = ListViewItemDic[listView1.SelectedItems[0]];
            t.Enable = !t.Enable;
            if (t.Enable)
                listView1.SelectedItems[0].ForeColor = Color.Black;
            else
                listView1.SelectedItems[0].ForeColor = Color.Gray;
        }
        private void ListView2_DoubleClick(object sender, EventArgs e)
        {
            var t = ListViewItemDic2[listView2.SelectedItems[0]];
            if (t.WorkingStat != Stat.Loading)
            {
                Enabled = false;
                new Selector(this, new List<SerieHolder>() { t }).Show();
            }
        }
        private void Main_Resize(object sender, EventArgs e)
        {
            var width = Width - 45;
            if (width % 2 == 1)
                textBox1.Width = width / 2 + 1;
            else
                textBox1.Width = width / 2;
            textBox2.Width = textBox1.Width;
            listView1.Width = width / 2;
            listView2.Width = width / 2;
            listView2.Left = textBox1.Width + textBox1.Left + 5;
            listView1.Left = textBox1.Width + textBox1.Left + 5;
            var height = Height - 96;
            if (height % 2 == 1)
                textBox1.Height = height / 2 + 1;
            else
                textBox1.Height = height / 2;
            listView1.Height = textBox1.Height;
            listView2.Height = height / 2;
            textBox2.Height = height / 2;
            listView2.Top = listView1.Height + listView1.Top + 5;
            textBox2.Top = listView1.Height + listView1.Top + 5;
        }
        public void RefeshHosts()
        {
            foreach (var item in SerieHolders)
                item.RefeshHosts();
        }
    }
}
