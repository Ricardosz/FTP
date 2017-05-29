using System;
using System.Text;
using System.Windows.Forms;
//添加所需的命名空间
using System.Net;
using System.IO;

namespace FTP
{
    public partial class Form1 : Form
    {   //默认FTP端口为21
        private const int ftpport = 21;
        private string ftpUristring = null;
        private NetworkCredential networkCredential;
        private string currentDir = "/";

        public Form1()
        {
            InitializeComponent();
            IPAddress[] ips = Dns.GetHostAddresses("");
            tbxServerIp.Text = ips[1].ToString();
            tbxServerIp.SelectAll();
            btndownload.Enabled = false;
            lstbxFtpResources.Enabled = false;
            lstbxFtpState.Enabled = false;
            btnUpload.Enabled = false;
            btndownload.Enabled = false;
            btnDelete.Enabled = false;
        }


        //获取键盘操作
        private void tbxServerIp_KeyPress(object sender, KeyPressEventArgs e)
        {
            //输入服务器IP后，焦点跳转到用户名文本框
            if (e.KeyChar == (char)13)
            {
                tbxUsername.Focus();
            }
        }

        private void tbxUsername_KeyPress(object sender, KeyPressEventArgs e)
        {
            //输入用户名后，焦点跳转到密码文本框
            if (e.KeyChar == (char)13)
            {
                tbxPassword.Focus();
            }
        }

        private void tbxPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            //输入密码后，焦点跳转到登陆按钮
            if (e.KeyChar == (char)13)
            {
                btnlogin.Focus();
            }

        }
        //匿名事件
        private void chkbxAnonymous_Click(object sender, EventArgs e)
        {
            //实名登录
            if (chkbxAnonymous.Checked == false)
            {
                tbxUsername.Enabled = true;
                tbxUsername.Text = "user";
                tbxPassword.Enabled = true;
                tbxPassword.Text = "";
                tbxUsername.Focus();
            }
            //匿名登录
            else
            {
                tbxUsername.Text = "anonymous";
                tbxUsername.Enabled = false;
                tbxPassword.Text = "";
                tbxPassword.Enabled = false;
            }
        }
        //服务器交互
        //创建FTP连接
        private FtpWebRequest CreateFtpWebRequest(string uri, string requestMethod)
        {
            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(uri);
            request.Credentials = networkCredential;
            request.KeepAlive = true;
            request.UseBinary = true;
            request.Method = requestMethod;
            return request;
        }
        //获取服务器返回信息
        private FtpWebResponse GetFtpResponse(FtpWebRequest request)
        {
            FtpWebResponse response = null;
            try
            {
                response = (FtpWebResponse)request.GetResponse();
                lstbxFtpState.Items.Add("验证完毕，服务器返回信息：[" + response.WelcomeMessage + "]");
                lstbxFtpState.Items.Add("正在连接：[" + response.BannerMessage + "]");
                lstbxFtpState.TopIndex = lstbxFtpState.Items.Count - 1;
                return response;
            }
            catch (WebException ex)
            {
                lstbxFtpState.Items.Add("发送错误。返回信息为：" + ex.Status);
                lstbxFtpState.TopIndex = lstbxFtpState.Items.Count - 1;
                return null;
            }
        }
        //登录模块的实现
        //登录服务器事件
        private void btnlogin_Click(object sender, EventArgs e)
        {
            if (tbxServerIp.Text == string.Empty)
            {
                MessageBox.Show("请先填写服务器地址", "提示");
                return;
            }
            ftpUristring = "ftp://" + tbxServerIp.Text;
            networkCredential = new NetworkCredential(tbxUsername.Text, tbxPassword.Text);
            if (ShowFtpFileAndDirectory() == true)
            {
                btnlogin.Enabled = false;
                btnlogout.Enabled = true;
                lstbxFtpResources.Enabled = true;
                lstbxFtpState.Enabled = true;
                tbxServerIp.Enabled = false;
                if (chkbxAnonymous.Checked == false)
                {
                    tbxUsername.Enabled = false;
                    tbxPassword.Enabled = false;
                    chkbxAnonymous.Enabled = false;
                }
                else
                {
                    chkbxAnonymous.Enabled = false;
                }
                tbxloginmessage.Text = "登录成功";
                btnUpload.Enabled = true;
                btndownload.Enabled = true;
                btnDelete.Enabled = true;
            }
            else
            {
                lstbxFtpState.Enabled = true;
                tbxloginmessage.Text = "登录失败";
            }
        }

        // 显示资源列表
        private bool ShowFtpFileAndDirectory()
        {
            lstbxFtpResources.Items.Clear();
            string uri = string.Empty;
            if (currentDir == "/")
            {
                uri = ftpUristring;
            }
            else
            {
                uri = ftpUristring + currentDir;
            }

            string[] urifield = uri.Split(' ');
            uri = urifield[0];
            FtpWebRequest request = CreateFtpWebRequest(uri, WebRequestMethods.Ftp.ListDirectoryDetails);

            // 获得服务器返回的响应信息
            FtpWebResponse response = GetFtpResponse(request);
            if (response == null)
            {
                return false;
            }
            lstbxFtpState.Items.Add("连接成功，服务器返回的是：" + response.StatusCode + " " + response.StatusDescription);

            // 读取网络流数据
            Stream stream = response.GetResponseStream();
            StreamReader streamReader = new StreamReader(stream, Encoding.Default);
            lstbxFtpState.Items.Add("获取响应流....");
            string s = streamReader.ReadToEnd();
            streamReader.Close();
            stream.Close();
            response.Close();
            lstbxFtpState.Items.Add("传输完成");

            // 处理并显示文件目录列表
            string[] ftpdir = s.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            lstbxFtpResources.Items.Add("↑返回上层目录");
            int length = 0;
            for (int i = 0; i < ftpdir.Length; i++)
            {
                if (ftpdir[i].EndsWith("."))
                {
                    length = ftpdir[i].Length - 2;
                    break;
                }
            }

            for (int i = 0; i < ftpdir.Length; i++)
            {
                s = ftpdir[i];
                int index = s.LastIndexOf('\t');
                if (index == -1)
                {
                    if (length < s.Length)
                    {
                        index = length;
                    }
                    else
                    {
                        continue;
                    }
                }

                string name = s.Substring(index + 1);
                if (name == "." || name == "..")
                {
                    continue;
                }

                // 判断是否为目录，在名称前加"目录"来表示
                if (s[0] == 'd' || (s.ToLower()).Contains("<dir>"))
                {
                    string[] namefield = name.Split(' ');
                    int namefieldlength = namefield.Length;
                    string dirname;
                    dirname = namefield[namefieldlength - 1];

                    // 对齐
                    dirname = dirname.PadRight(34, ' ');
                    name = dirname;
                    // 显示目录
                    lstbxFtpResources.Items.Add("[目录]" + name);
                }
            }

            for (int i = 0; i < ftpdir.Length; i++)
            {
                s = ftpdir[i];
                int index = s.LastIndexOf('\t');
                if (index == -1)
                {
                    if (length < s.Length)
                    {
                        index = length;
                    }
                    else
                    {
                        continue;
                    }
                }

                string name = s.Substring(index + 1);
                if (name == "." || name == "..")
                {
                    continue;
                }

                // 判断是否为文件
                if (!(s[0] == 'd' || (s.ToLower()).Contains("<dir>")))
                {
                    string[] namefield = name.Split(' ');
                    int namefieldlength = namefield.Length;
                    string filename;

                    filename = namefield[namefieldlength - 1];

                    // 对齐
                    filename = filename.PadRight(34, ' ');
                    name = filename;

                    // 显示文件
                    lstbxFtpResources.Items.Add(name);
                }
            }

            return true;
        }

        //注销事件
        private void btnlogout_Click(object sender, EventArgs e)
        {
            btnlogin.Enabled = true;
            btnlogout.Enabled = false;
            tbxServerIp.Enabled = true;
            tbxServerIp.SelectAll();
            tbxServerIp.Focus();
            chkbxAnonymous.Enabled = true;
            if (chkbxAnonymous.Checked == false)
            {
                tbxUsername.Enabled = true;
                tbxPassword.Enabled = true;
            }
            tbxloginmessage.Text = "你已经退出";
            lstbxFtpResources.Items.Clear();
            lstbxFtpResources.Enabled = false;
            lstbxFtpState.Items.Clear();
            lstbxFtpState.Enabled = false;
            btnUpload.Enabled = false;
            btndownload.Enabled = false;
            btnDelete.Enabled = false;
        }


        //文件操作模块的实现
        private void btnUpload_Click(object sender, EventArgs e)
        {
            //选择要上传的文件
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.FileName = openFileDialog.FileName.ToString();
            openFileDialog.Filter = "所有文件(*.*)|*.*";
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            FileInfo fileinfo = new FileInfo(openFileDialog.FileName);
            try
            {
                string uri = GetUriString(fileinfo.Name);
                FtpWebRequest request = CreateFtpWebRequest(uri, WebRequestMethods.Ftp.UploadFile);
                request.ContentLength = fileinfo.Length;
                int buflength = 8196;
                byte[] buffer = new byte[buflength];
                FileStream filestream = fileinfo.OpenRead();
                Stream responseStream = request.GetRequestStream();
                lstbxFtpState.Items.Add("打开上传流，文件上传中...");
                int contenlength = filestream.Read(buffer, 0, buflength);
                while (contenlength != 0)
                {
                    responseStream.Write(buffer, 0, contenlength);
                    contenlength = filestream.Read(buffer, 0, buflength);
                }
                responseStream.Close();
                filestream.Close();
                FtpWebResponse response = GetFtpResponse(request);
                if (response == null)
                {
                    lstbxFtpState.Items.Add("服务器未响应...");
                    lstbxFtpState.TopIndex = lstbxFtpState.Items.Count - 1;
                    return;
                }
                lstbxFtpState.Items.Add("上传完毕，服务器返回:" + response.StatusCode + "" + response.StatusDescription);
                lstbxFtpState.TopIndex = lstbxFtpState.Items.Count - 1;
                MessageBox.Show("上传成功");
                //上传成功后，即时刷新服务器目录
                ShowFtpFileAndDirectory();
            }
            catch (WebException ex)
            {
                lstbxFtpState.Items.Add("上传失败，返回信息为:" + ex.Status);
                lstbxFtpState.TopIndex = lstbxFtpState.Items.Count - 1;
                MessageBox.Show(ex.Message, "上传失败");
            }

        }


        private string GetUriString(string filename)
        {
            string uri = string.Empty;
            if (currentDir.EndsWith("/"))
            {
                uri = ftpUristring + currentDir + filename;
            }
            else
            {
                uri = ftpUristring + currentDir + "/" + filename;
            }
            return uri;
        }
        //从服务器上下载文件到本地






    }

    }


