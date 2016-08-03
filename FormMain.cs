using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Data.SqlClient;

namespace FileChangeNotifier
{
    public partial class frmNotifier : Form
    {
        private StringBuilder m_Sb;
        private bool m_bDirty;
        private System.IO.FileSystemWatcher m_Watcher;
        private bool m_bIsWatching;

        string connectionString = "Data Source=DESKTOP-ABEPKAT;Initial Catalog=Costco;Integrated Security=False;User ID=sa;Password=G4indigo;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";


        public frmNotifier()
        {
            InitializeComponent();
            m_Sb = new StringBuilder();
            m_bDirty = false;
            m_bIsWatching = false;
        }

        private void btnWatchFile_Click(object sender, EventArgs e)
        {
            if (m_bIsWatching)
            {
                m_bIsWatching = false;
                m_Watcher.EnableRaisingEvents = false;
                m_Watcher.Dispose();
                btnWatchFile.BackColor = Color.LightSkyBlue;
                btnWatchFile.Text = "Start Watching";

            }
            else
            {
                m_bIsWatching = true;
                btnWatchFile.BackColor = Color.Red;
                btnWatchFile.Text = "Stop Watching";

                m_Watcher = new System.IO.FileSystemWatcher();
                if (rdbDir.Checked)
                {
                    m_Watcher.Filter = "*.*";
                    m_Watcher.Path = txtFile.Text + "\\";
                }
                else
                {
                    m_Watcher.Filter = txtFile.Text.Substring(txtFile.Text.LastIndexOf('\\') + 1);
                    m_Watcher.Path = txtFile.Text.Substring(0, txtFile.Text.Length - m_Watcher.Filter.Length);
                }

                if (chkSubFolder.Checked)
                {
                    m_Watcher.IncludeSubdirectories = true;
                }

                m_Watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                     | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                m_Watcher.Changed += new FileSystemEventHandler(OnChanged);
                m_Watcher.Created += new FileSystemEventHandler(OnChanged);
                m_Watcher.Deleted += new FileSystemEventHandler(OnChanged);
                m_Watcher.Renamed += new RenamedEventHandler(OnRenamed);
                m_Watcher.EnableRaisingEvents = true;
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            //if (!m_bDirty)
            //{
            m_Sb.Remove(0, m_Sb.Length);
            m_Sb.Append(e.FullPath);
            m_Sb.Append(" ");
            m_Sb.Append(e.ChangeType.ToString());
            m_Sb.Append("    ");
            m_Sb.Append(DateTime.Now.ToString());
            m_bDirty = true;

            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                string name = e.Name;

                if (name.Split('\\').Length == 3)
                {
                    string folder = name.Split('\\')[1];
                    string filename = name.Split('\\')[2];

                    filename = filename.Substring(0, filename.LastIndexOf('.'));

                    if (filename.Split('-').Length == 4 || filename.Split('-').Length == 3)
                    {
                        SqlConnection cn = new SqlConnection(connectionString);
                        SqlCommand cmd = new SqlCommand();
                        cmd.Connection = cn;
                        cn.Open();

                        string sqlString;

                        string date = filename.Split('-')[0];
                        string year = date.Substring(0, 4);
                        string month = date.Substring(4, 2);
                        string day = date.Substring(6, 2);
                        date = month + "/" + day + "/" + year;
                        string amount = filename.Split('-')[1];
                        string categoryCode = filename.Split('-')[2];

                        string transactionName = string.Empty;
                        if (filename.Split('-').Length == 4)
                            transactionName = filename.Split('-')[3];

                        sqlString = @"INSERT INTO BookKeeping (Date, Name, CategoryCode, Amount, Receipt, Expense) 
                                      VALUES (@_Date, @_Name, @_CategoryCode, @_Amount, @_Receipt, @_Expense)";

                        cmd.CommandText = sqlString;
                        cmd.Parameters.AddWithValue("@_Date", date);
                        cmd.Parameters.AddWithValue("@_Name", transactionName);
                        cmd.Parameters.AddWithValue("@_CategoryCode", categoryCode);
                        cmd.Parameters.AddWithValue("@_Amount", amount);
                        cmd.Parameters.AddWithValue("@_Receipt", filename);
                        cmd.Parameters.AddWithValue("@_Expense", folder == "Cost" ? "0" : "1");

                        cmd.ExecuteNonQuery();

                        cn.Close();
                    }
                }
            }
            else if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                string name = e.Name;
                string folder = name.Split('\\')[0];
                string filename = name.Split('\\')[1];

                filename = filename.Substring(0, filename.LastIndexOf('.'));

                if (filename.Split('-').Length == 3)
                {
                    SqlConnection cn = new SqlConnection(connectionString);
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = cn;
                    cn.Open();

                    string sqlString;

                    string date = filename.Split('-')[0];
                    string year = date.Substring(0, 4);
                    string month = date.Substring(4, 2);
                    string day = date.Substring(6, 2);
                    date = month + "/" + day + "/" + year;
                    string amount = filename.Split('-')[1];
                    string note = filename.Split('-')[2];

                    sqlString = "DELETE FROM eBay_BookKeeping WHERE TransactionDate = '" + date + "' AND Amount = " +
                                amount + " AND Note = '" + note + "' AND Receipt = '" + filename + "' AND Gain = " + (folder == "Cost" ? "0" : "1");

                    cmd.CommandText = sqlString;
                    cmd.ExecuteNonQuery();

                    cn.Close();
                }
            }
            //}
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            //if (!m_bDirty)
            //{
            m_Sb.Remove(0, m_Sb.Length);
            m_Sb.Append(e.OldFullPath);
            m_Sb.Append(" ");
            m_Sb.Append(e.ChangeType.ToString());
            m_Sb.Append(" ");
            m_Sb.Append("to ");
            m_Sb.Append(e.Name);
            m_Sb.Append("    ");
            m_Sb.Append(DateTime.Now.ToString());
            m_bDirty = true;
            if (rdbFile.Checked)
            {
                m_Watcher.Filter = e.Name;
                m_Watcher.Path = e.FullPath.Substring(0, e.FullPath.Length - m_Watcher.Filter.Length);
            }

            string oldName = e.OldName;
            string oldFolder = oldName.Split('\\')[0];
            string oldFilename = oldName.Split('\\')[1];
            oldFilename = oldFilename.Substring(0, oldFilename.LastIndexOf('.'));

            string newName = e.Name;
            string newFolder = newName.Split('\\')[0];
            string newFilename = newName.Split('\\')[1];
            newFilename = newFilename.Substring(0, newFilename.LastIndexOf('.'));

            if (oldFilename.Split('-').Length == 3 && newFilename.Split('-').Length == 3)
            {
                string oldDate = oldFilename.Split('-')[0];
                string oldYear = oldDate.Substring(0, 4);
                string oldMonth = oldDate.Substring(4, 2);
                string oldDay = oldDate.Substring(6, 2);
                oldDate = oldMonth + "/" + oldDay + "/" + oldYear;
                string oldAmount = oldFilename.Split('-')[1];
                string oldNote = oldFilename.Split('-')[2];

                string newDate = newFilename.Split('-')[0];
                string newYear = newDate.Substring(0, 4);
                string newMonth = newDate.Substring(4, 2);
                string newDay = newDate.Substring(6, 2);
                newDate = newMonth + "/" + newDay + "/" + newYear;
                string newAmount = newFilename.Split('-')[1];
                string newNote = newFilename.Split('-')[2];

                SqlConnection cn = new SqlConnection(connectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = cn;
                cn.Open();

                string sqlString = @"UPDATE eBay_BookKeeping 
                                        SET TransactionDate = @_newTransactionDate, 
                                            Amount = @_newAmount, 
                                            Note = @_newNote,
                                            Receipt = @_newReceipt
                                        WHERE TransactionDate = @_oldTransactionDate
                                        AND Amount = @_oldAmount
                                        AND Note = @_oldNote
                                        AND Receipt = @_oldReceipt";

                cmd.CommandText = sqlString;

                cmd.Parameters.AddWithValue("@_newTransactionDate", newDate);
                cmd.Parameters.AddWithValue("@_newAmount", newAmount);
                cmd.Parameters.AddWithValue("@_newNote", newNote);
                cmd.Parameters.AddWithValue("@_newReceipt", newFilename);
                cmd.Parameters.AddWithValue("@_oldTransactionDate", oldDate);
                cmd.Parameters.AddWithValue("@_oldAmount", oldAmount);
                cmd.Parameters.AddWithValue("@_oldNote", oldNote);
                cmd.Parameters.AddWithValue("@_oldReceipt", oldFilename);

                cmd.ExecuteNonQuery();

                cn.Close();
            }
            //}            
        }

        private void tmrEditNotify_Tick(object sender, EventArgs e)
        {
            if (m_bDirty)
            {
                lstNotification.BeginUpdate();
                lstNotification.Items.Add(m_Sb.ToString());
                lstNotification.EndUpdate();
                m_bDirty = false;
            }
        }

        private void btnBrowseFile_Click(object sender, EventArgs e)
        {
            if (rdbDir.Checked)
            {
                DialogResult resDialog = dlgOpenDir.ShowDialog();
                if (resDialog.ToString() == "OK")
                {
                    txtFile.Text = dlgOpenDir.SelectedPath;
                }
            }
            else
            {
                DialogResult resDialog = dlgOpenFile.ShowDialog();
                if (resDialog.ToString() == "OK")
                {
                    txtFile.Text = dlgOpenFile.FileName;
                }
            }
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            DialogResult resDialog = dlgSaveFile.ShowDialog();
            if (resDialog.ToString() == "OK")
            {
                FileInfo fi = new FileInfo(dlgSaveFile.FileName);
                StreamWriter sw = fi.CreateText();
                foreach (string sItem in lstNotification.Items)
                {
                    sw.WriteLine(sItem);
                }
                sw.Close();
            }
        }

        private void rdbFile_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbFile.Checked == true)
            {
                chkSubFolder.Enabled = false;
                chkSubFolder.Checked = false;
            }
        }

        private void rdbDir_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbDir.Checked == true)
            {
                chkSubFolder.Enabled = true;
            }
        }

        private void frmNotifier_Load(object sender, EventArgs e)
        {
            rdbDir.Checked = true;
            chkSubFolder.Checked = true;
            txtFile.Text = @"C:\Users\Jason Ding\Dropbox\Bookkeeping\";
            btnWatchFile.PerformClick();
        }
    }
}