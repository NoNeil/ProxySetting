using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Microsoft.Win32;
using System.Runtime.InteropServices;

using System.IO;


namespace MyProxy
{
    public partial class Form1 : Form
    {
        private SQLite sqlite;

        private static string dbFileName = "Proxys.db";

        public Form1()
        {
            sqlite = new SQLite();
            InitializeComponent();
        }

        public struct Struct_INTERNET_PROXY_INFO
        {
            public int dwAccessType;
            public IntPtr proxy;
            public IntPtr proxyBypass;
        };
        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lPBuffer, int lpdwBufferLength);

        private const int INTERNET_OPTION_REFRESH = 37;
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;

        private bool Reflush()
        {
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0); //通知注册表中代理改变,下次连接时启动代理
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
            return true;
        }
        
        //应用代理
        private void btnApply_Click(object sender, EventArgs e)
        {
            string ip = dataGridView.CurrentCell.OwningRow.Cells["地址"].Value.ToString();
            string port = dataGridView.CurrentCell.OwningRow.Cells["端口"].Value.ToString();

            if (ProxySetting.SetProxy(ip + ":" + port))
            {
                lalMsg.ForeColor = Color.Black;
                lalMsg.Text = "已设置代理：" + ip + ":" + port;
            }
            else
            {
                lalMsg.ForeColor = Color.Red;
                lalMsg.Text = "设置代理失败. 原因：无效IP和端口.";
            }            
        }

        //取消代理
        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (ProxySetting.UnsetProxy())
            {
                lalMsg.ForeColor = Color.Black;
                lalMsg.Text = "已取消代理.";
            }
            else
            {
                lalMsg.ForeColor = Color.Red;
                lalMsg.Text = "取消代理失败.";
            }
        }

        //点击“确定”按钮
        private void btnConfirm_Click(object sender, EventArgs e)
        {
            this.btnApply_Click(null, null);
            this.Close();
        }
        
        //窗口加载
        private void Form1_Load(object sender, EventArgs e)
        {
            //读取是否已经设置代理
            if (ProxySetting.UsedProxy())
            {                
                lalMsg.ForeColor = Color.Black;
                lalMsg.Text = "当前正在使用代理：" + ProxySetting.GetProxyProxyServer();
            }
            else
            {                
                lalMsg.ForeColor = Color.Black;
                lalMsg.Text = "当前没有使用代理.";
            }

            //判断数据库是否已存在
            if (File.Exists(dbFileName) == false)
            {
                Console.WriteLine("数据库文件不存在");
                //open/create the database
                sqlite.OpenDatabase(dbFileName);

                //创建Proxy表
                string sql = "CREATE TABLE Proxy(ProxyName varchar(20) unique, IP varchar(20), Port varchar(10))";
                sqlite.ExecuteNonQuery(sql);

                //添加一个用户
                sql = "INSERT INTO Proxy VALUES('cs', 'cs.bit.edu.cn', '2804')";
                sqlite.ExecuteNonQuery(sql);
                sql = "INSERT INTO Proxy VALUES('108', '10.108.12.56', '8085')";
                sqlite.ExecuteNonQuery(sql);            
                
            }

            //初始化dataGridView
            dataGridView.RowHeadersWidth = 30;
            dataGridView.ColumnCount = 3;
            dataGridView.Columns[0].Name = "名称";
            dataGridView.Columns[1].Name = "地址";
            dataGridView.Columns[2].Name = "端口";
            dataGridView.Columns[2].Width = 88;
            
            //加载数据（代理）
            LoadData();
        }

        //添加代理
        private void AddProxy_Click(object sender, EventArgs e)
        {
            //添加一行空白
            string[] row = { "", "", "" };
            dataGridView.Rows.Add(row);
            lalMsg.ForeColor = Color.Black;
            lalMsg.Text = "请输入新的代理.";
            //滚动到最后一行
            dataGridView.CurrentCell = dataGridView.Rows[dataGridView.Rows.Count - 1].Cells[0];
        }

        //保存代理
        private void StoreProxy_Click(object sender, EventArgs e)
        {
            //打开数据库
            sqlite.OpenDatabase(dbFileName);
            //删除Proxy表
            string sql = "DROP TABLE Proxy";
            sqlite.ExecuteNonQuery(sql);
            //新建Proxy表
            sql = "CREATE TABLE Proxy(ProxyName varchar(20) unique, IP varchar(20), Port varchar(10))";
            sqlite.ExecuteNonQuery(sql);

            int failedCounter = 0;
            for (int i = 0; i < dataGridView.Rows.Count; i++)
            {   
                string name = dataGridView.Rows[i].Cells["名称"].Value.ToString();
                string ip = dataGridView.Rows[i].Cells["地址"].Value.ToString();
                string port = dataGridView.Rows[i].Cells["端口"].Value.ToString();
                if (name.Length == 0)
                {
                    failedCounter++;
                    continue;
                }
                //插入
                sql = "INSERT INTO Proxy VALUES(\'" + name + "\', \'" + ip + "\', \'" + port + "\')";
                if (sqlite.ExecuteNonQuery(sql) == false)
                {
                    failedCounter++;
                }
            }
            if (failedCounter == 0)
            {
                lalMsg.ForeColor = Color.Black;
                lalMsg.Text = "保存成功.";
            }
            else
            {
                lalMsg.ForeColor = Color.Red;
                lalMsg.Text = failedCounter.ToString() + "条记录保存失败 原因：名称重复或者空白";
            }
        }

        //删除代理
        private void DeleteProxy_Click(object sender, EventArgs e)
        {
            if (dataGridView.Rows.Count <= 0)
            {
                lalMsg.ForeColor = Color.Red;
                lalMsg.Text = "表已经为空";
                return;
            }
            string name = dataGridView.CurrentCell.OwningRow.Cells["名称"].Value.ToString();

            //打开数据库
            sqlite.OpenDatabase(dbFileName);

            //删除一条记录
            if (name.Length > 0)
            {
                String sql = "DELETE FROM Proxy where ProxyName = \'" + name + "\'";
                Console.WriteLine(sql);
                if (sqlite.ExecuteNonQuery(sql) == true)
                {
                    lalMsg.ForeColor = Color.Black;
                    lalMsg.Text = "成功删除一条记录";
                }
                else
                {
                    lalMsg.ForeColor = Color.Red;
                    lalMsg.Text = "删除失败!";
                }
            }
            dataGridView.Rows.Remove(dataGridView.CurrentRow);
            if (dataGridView.Rows.Count <= 5)
            {
                dataGridView.Columns[2].Width = 88;
            }
        }  
      
        //加载代理数据
        private void LoadData()
        {
            //打开数据库
            sqlite.OpenDatabase(dbFileName);
            String sql = "SELECT * FROM Proxy";
            List<Proxy> listProxy = sqlite.ExecuteQuery(sql);
            foreach (Proxy p in listProxy)
            {
                string[] row = { p.name, p.ip, p.port };
                dataGridView.Rows.Add(row);                         //添加一行
            }           
        }
        
        //点击最小化时，缩小到通知栏
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            //if (this.WindowState == FormWindowState.Minimized)
            //{
            //    this.Hide();
            //    this.notifyIcon1.Visible = true;
            //}
        }

        //双击通知栏小标，显示窗口
        private void  notifyIcon1_MouseDoubleClick(object sender, EventArgs e)
        {
            this.Visible = true;
            this.WindowState = FormWindowState.Normal;
            this.notifyIcon1.Visible = false;
        }

        //点击通知栏小标菜单，显示窗口
        private void ShowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = true;
            this.notifyIcon1.Visible = false;
            this.WindowState = FormWindowState.Normal;
        }

        //点击任务栏小标菜单，关闭软件
        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);//这是最彻底的退出方式，不管什么线程都被强制退出，把程序结束的很干净。
        }
        
    }
}
