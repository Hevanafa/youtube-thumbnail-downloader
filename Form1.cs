using AngleSharp.Html.Parser;
using System;
using System.Drawing;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Web;
using System.Diagnostics;
using System.Net.Http;

namespace YouTubeThumbnailDownloader
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            
        }

        HttpClient client = new HttpClient();

        private void btnDownload_Click(object sender, EventArgs e)
        {
            // Done: subsequent downloads use the same image as the first one
            // Todo: rewrite the WebClient below with HttpClient

            var thread = new Thread(async () => {
                try
                {
                    // https://stackoverflow.com/questions/9459225/
                    //var client = new WebClient();

                    ////client.DownloadProgressChanged += Client_DownloadProgressChanged;
                    //client.DownloadFileCompleted += Client_DownloadFileCompleted;
                    //await client.DownloadFileTaskAsync(new Uri(txbUrl.Text), "temp.html");

                    //ProcessHtmlFile();

                    //File.Delete("temp.html");

                    //client.BaseAddress = new Uri(txbUrl.Text);
                    //client.DefaultRequestHeaders.Accept.Clear();
                    //var res = await client.GetAsync(txbUrl.Text);

                    var req = new HttpRequestMessage(HttpMethod.Get, txbUrl.Text);
                    var res = await client.SendAsync(req);

                    if (res.IsSuccessStatusCode)
                    {
                        File.Delete("temp.html");

                        var content = res.Content;
                        //var stream = await content.ReadAsStreamAsync();
                        using (var stream = new FileStream("temp.html", FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                            await content.CopyToAsync(stream);

                        ProcessHtmlFile();
                    }
                    else {
                        WriteLog("Error while downloading");
                    }
                }
                catch (Exception ex) {
                    WriteLog("Error: " + ex.Message);
                }
            });

            WriteLog("Starting download...");

            if (!txbUrl.Text.Contains("https://www.youtube.com")) {
                WriteLog("Error: The URL isn't a valid YouTube link!");
                return;
            }

            thread.Start();
        }

        void CheckCreateDownloadsFolder() {
            if (!Directory.Exists("thumbs"))
                Directory.CreateDirectory("thumbs");
        }

        // https://stackoverflow.com/questions/49981433/
        void WriteLog(string text) {
            txbLog.BeginInvoke(new Action(() =>
                txbLog.Text += text + "\r\n"
            ));
        }

        async void ProcessHtmlFile() {
            //MessageBox.Show("Saved as temp.html");

            var uri = new Uri(txbUrl.Text);

            var hash = txbUrl.Text.Contains("shorts")
                ? Path.GetFileName(txbUrl.Text)
                // https://stackoverflow.com/questions/659887/
                : HttpUtility.ParseQueryString(uri.Query).Get("v");

            WriteLog("Hash: " + hash);

            var sr = new StreamReader("temp.html");
            var html = await sr.ReadToEndAsync();
            sr.Close();
            File.Delete("temp.html");

            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            // https://stackoverflow.com/questions/36368789/
            var node = document.QuerySelector("link[itemprop=\"thumbnailUrl\"]");

            // https://stackoverflow.com/questions/57705418/
            var imgHref = node.GetAttribute("href");
            WriteLog("Found the link: " + imgHref);

            var imgUri = new Uri(imgHref);

            // https://stackoverflow.com/questions/4630249/
            var imgUriPath = imgUri.GetLeftPart(UriPartial.Path);
            //WriteLog(imgUriPath);
            //return;

            var ext = Path.GetExtension(imgUriPath);
            newFilename = hash + ext;
            //WriteLog("Filename: " + newFilename);

            CheckCreateDownloadsFolder();
            // Actually download the image

            var client = new WebClient();
            //client.DownloadFileCompleted += Client_DownloadFileCompleted1;
            await client.DownloadFileTaskAsync(new Uri(imgUriPath), $"thumbs\\{newFilename}");

            WriteLog("Saved as " + newFilename);

            // https://stackoverflow.com/questions/17193825/
            pbPreview.BackgroundImage = Image.FromFile($"thumbs\\{newFilename}");


            // Done: handle the use case for livestreams
            // Done: handle the case for Shorts
            // https://i.ytimg.com/vi/lhfBIsHmhSs/oardefault.jpg?sqp=-oaymwEkCJUDENAFSFqQAgHyq4qpAxMIARUAAAAAJQAAyEI9AICiQ3gB&rs=AOn4CLCEtR7lA0dDWmwsYAppArMjQaNfaA
        }

        private void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            WriteLog("temp.html is available");
        }

        string newFilename;

        private void Client_DownloadFileCompleted1(object sender, AsyncCompletedEventArgs e)
        {
            WriteLog("Saved as " + newFilename);

            // https://stackoverflow.com/questions/17193825/
            pbPreview.BackgroundImage = Image.FromFile($"thumbs\\{newFilename}");
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            CheckCreateDownloadsFolder();

            Process.Start(".\\thumbs");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //pbPreview.BackgroundImage = Image.FromFile("thumbs\\kBdkwdK6VYA.jpg");
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txbLog.Clear();
        }
    }
}
