using AngleSharp.Html.Parser;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Web;
using System.Diagnostics;

namespace YouTubeThumbnailDownloader
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            var thread = new Thread(() => {
                try
                {
                    using (var client = new WebClient())
                    {
                        //client.DownloadProgressChanged += Client_DownloadProgressChanged;
                        client.DownloadFileCompleted += Client_DownloadFileCompleted;
                        client.DownloadFileAsync(new Uri(txbUrl.Text), "temp.html");
                    }
                }
                catch (Exception ex) {
                    WriteLog("Error: " + ex.Message);
                }
            });

            WriteLog("Starting download...");

            thread.Start();
        }

        //private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        // https://stackoverflow.com/questions/49981433/
        void WriteLog(string text) {
            txbLog.BeginInvoke(new Action(() =>
                txbLog.Text += text + "\r\n"
            ));
        }

        private async void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            //MessageBox.Show("Saved as temp.html");

            var uri = new Uri(txbUrl.Text);
            var hash = HttpUtility.ParseQueryString(uri.Query).Get("v");
            WriteLog(hash);

            var sr = new StreamReader("temp.html");
            var html = await sr.ReadToEndAsync();

            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            var node = document.QuerySelector("link[itemprop=\"thumbnailUrl\"]");
            var imgHref = node.GetAttribute("href");
            WriteLog("Found the link: " + imgHref);

            var ext = Path.GetExtension(imgHref);
            newFilename = hash + ext;
            //WriteLog("Filename: " + newFilename);

            // Actually download the image
            if (!Directory.Exists("thumbs"))
                Directory.CreateDirectory("thumbs");

            using (var client = new WebClient()) {
                client.DownloadFileCompleted += Client_DownloadFileCompleted1;
                client.DownloadFileAsync(new Uri(imgHref), $"thumbs\\{newFilename}");
            }

            // Todo: handle the use case for different resolutions
            // Done: handle the use case for livestreams
        }

        string newFilename;

        private void Client_DownloadFileCompleted1(object sender, AsyncCompletedEventArgs e)
        {
            WriteLog("Saved as " + newFilename);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists("thumbs"))
                Directory.CreateDirectory("thumbs");

            Process.Start(".\\thumbs");
        }
    }
}
