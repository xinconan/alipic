using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace alipic46
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //保存到本地的路径
        private string path;

        private void btnBegin_Click(object sender, EventArgs e)
        {
            String url = tbUrl.Text;
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("请输入地址");
                return;
            }

            path = tbPath.Text;
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("请选择文件夹");
                return;
            }

            //d:\a\
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

           
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "text/html, application/xhtml+xml, *";
            request.KeepAlive = true;
            request.Referer = url;
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.85 Safari/537.36";
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("gb2312"));
                        string html = reader.ReadToEnd();
                        //获取详情页面的url地址
                        string detailUrl = getDetailUrl(html);
                        //获取详情页中具体的html
                        string detailhtml = getDetailHtml(detailUrl);
                        //处理获取详情页中图片
                        processDetailPics(detailhtml);
                        //处理获取展示页图片
                        processHeaderPics(html);
                        MessageBox.Show("下载完成");
                    }
                }
            }
            catch (WebException ex)
            {
                tbLog.AppendText("地址："+url+" 解析失败。"+ex.Message.ToString() + Environment.NewLine);
            }
        }

        /// <summary>
        /// 处理下载主图
        /// </summary>
        /// <param name="html"></param>
        private void processHeaderPics(string html)
        {
            var parser = new HtmlParser();
            var document = parser.Parse(html);
            var list = document.QuerySelectorAll("li.tab-trigger");
            var imgUrl = "";
            foreach (var item in list)
            {
                //tbLog.AppendText(item.GetAttribute("data-imgs"));
                string dataImgs = item.GetAttribute("data-imgs");
                JObject json = (JObject)JsonConvert.DeserializeObject(dataImgs);
                imgUrl = (string)json["original"];
                if (imgUrl == null)
                {
                    continue;
                }
                imgUrl = imgUrl.Trim();
                if (imgUrl == "")
                {
                    continue;
                }
                imgUrl = imgUrl.Replace("\\", "").Replace("\"", "");
                DownloadImages(imgUrl);
                //tbLog.AppendText("正在下载："+imgUrl +Environment.NewLine);
            }
        }

        /// <summary>
        /// 处理下载详细图
        /// </summary>
        /// <param name="detailhtml"></param>
        private void processDetailPics(string detailhtml)
        {
            var parser = new HtmlParser();
            var document = parser.Parse(detailhtml);
            var imgList = document.QuerySelectorAll("img");
            var srcUrl = "";
            
            foreach (var item in imgList)
            {
                srcUrl = item.GetAttribute("src");
                //TODO:在img有class为desc_anchor的情况下，图片不下载
                if (srcUrl == null)
                {
                    continue;
                }
                srcUrl = srcUrl.Trim();
                if (srcUrl == "")
                {
                    continue;
                }
                //有些url是这样的：\"http:1688.com/dd.jpg\"，需要去除
                srcUrl = srcUrl.Replace("\\", "").Replace("\"", "");
                //tbLog.AppendText(" " + srcUrl + Environment.NewLine);

                DownloadImages(srcUrl);
            }
        }

        /// <summary>
        /// 获取详情页面的url地址
        /// </summary>
        /// <param name="detailUrl"></param>
        /// <returns></returns>
        private string getDetailHtml(string detailUrl)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(detailUrl);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "text/html, application/xhtml+xml, *";
            request.KeepAlive = true;
            request.Referer = detailUrl;
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.85 Safari/537.36";
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("gb2312"));
                        string html = reader.ReadToEnd();
                        html = html.Substring(10);
                        return html.Substring(0,html.Length-3);
                        //html.Substring()
                    }
                }
                else
                {
                    tbLog.AppendText("保存图片:" + detailUrl + "失败," + response.StatusCode + Environment.NewLine);
                }
            }
            catch (WebException ex)
            {
                tbLog.AppendText( ex.Message.ToString());
            }
            return "";
        }

        /// <summary>
        /// 处理获取详情页data-tfs-url地址
        /// </summary>
        /// <param name="html"></param>
        private string getDetailUrl(string html)
        {
            var parser = new HtmlParser();
            var document = parser.Parse(html);
            var list = document.QuerySelectorAll("#desc-lazyload-container");
            return list[0].GetAttribute("data-tfs-url");
        }

        /// <summary>
        /// 根据url下载图片
        /// </summary>
        /// <param name="objUrl"></param>
        private void DownloadImages(string objUrl)
        {
            //UrlRefer:从哪个页面启动下载的

            //Path.GetFileName("aa/1.jpg")得到"1.jpg"
            //Path.Combine合并路径
            //保存的文件名
            string destFile = Path.Combine(path, Path.GetFileName(objUrl));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(objUrl);
            request.Referer = objUrl;//欺骗服务器
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (FileStream fstream = new FileStream(destFile, FileMode.Create))
                        {
                            stream.CopyTo(fstream);
                            tbLog.AppendText("成功下载：" + objUrl + Environment.NewLine);
                        }
                    }
                }
                else
                {
                    tbLog.AppendText("保存图片:" + objUrl + "失败," + response.StatusCode + Environment.NewLine);
                }
            }
            catch (WebException ex)
            {
                //2016-1-6 22:25:29增加捕获response异常
                tbLog.AppendText("保存图片:" + objUrl + "失败," + ex.Message.ToString() + Environment.NewLine);
            }
        }

        private void btnChoosePath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                tbPath.Text = dlg.SelectedPath;
            }
        }
    }
}
