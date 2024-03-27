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
using System.Threading.Tasks;

namespace YouTubeThumbnailDownloader
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            
        }

        void CheckCreateDownloadsFolder()
        {
            if (!Directory.Exists("thumbs"))
                Directory.CreateDirectory("thumbs");
        }

        // https://stackoverflow.com/questions/49981433/
        void WriteLog(string text)
        {
            txbLog.BeginInvoke(new Action(() =>
                txbLog.AppendText($"{text}\r\n")
            ));
        }

        HttpClient client = new HttpClient();

        private void btnDownload_Click(object sender, EventArgs e)
        {
            StartDownload();
        }

        async void StartDownload() {
            // Done: subsequent downloads use the same image as the first one
            // Done: rewrite the WebClient below with HttpClient

            WriteLog("Starting download...");

            if (!txbUrl.Text.Contains("https://www.youtube.com"))
            {
                WriteLog("Error: The URL isn't a valid YouTube link!");
                return;
            }

            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, txbUrl.Text);
                var res = await client.SendAsync(req);

                if (res.IsSuccessStatusCode)
                {
                    File.Delete("temp.html");

                    var content = res.Content;
                    
                    using (var stream = new FileStream("temp.html", FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                        await content.CopyToAsync(stream);

                    ProcessHtmlFile();
                }
                else
                {
                    WriteLog("Error while downloading");
                }
            }
            catch (Exception ex)
            {
                WriteLog("Error: " + ex.Message);
            }
        }

        string GetHash(string youtubeUrl) {
            var uri = new Uri(youtubeUrl);

            return youtubeUrl.Contains("shorts")
                ? Path.GetFileName(youtubeUrl)
                // https://stackoverflow.com/questions/659887/
                : HttpUtility.ParseQueryString(uri.Query).Get("v");
        }

        async Task<AngleSharp.Dom.IElement> GetThumbnailNode() {
            var sr = new StreamReader("temp.html");
            var html = await sr.ReadToEndAsync();
            sr.Close();

            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            // https://stackoverflow.com/questions/36368789/
            return document.QuerySelector("link[itemprop=\"thumbnailUrl\"]");
        }

        async void ProcessHtmlFile() {
            //MessageBox.Show("Saved as temp.html");

            var hash = GetHash(txbUrl.Text);
            WriteLog("Hash: " + hash);

            var node = await GetThumbnailNode();
            File.Delete("temp.html");

            // https://stackoverflow.com/questions/57705418/
            var imgHref = node.GetAttribute("href");
            WriteLog("Found the link: " + imgHref);

            var imgUri = new Uri(imgHref);

            // https://stackoverflow.com/questions/4630249/
            var imgUriPath = imgUri.GetLeftPart(UriPartial.Path);
            var ext = Path.GetExtension(imgUriPath);
            newFilename = hash + ext;
            //WriteLog("Filename: " + newFilename);

            CheckCreateDownloadsFolder();

            // Actually download the image
            var req = new HttpRequestMessage(HttpMethod.Get, imgUriPath);
            var res = await client.SendAsync(req);

            if (res.IsSuccessStatusCode) {
                var content = res.Content;
                var newFullPath = $"thumbs\\{newFilename}";

                if (File.Exists(newFullPath))
                    File.Delete(newFullPath);

                using (var stream = new FileStream(newFullPath, FileMode.CreateNew))
                    await content.CopyToAsync(stream);

                WriteLog("Saved as " + newFilename);

                // https://stackoverflow.com/questions/17193825/
                pbPreview.BackgroundImage = Image.FromFile(newFullPath);
            }

            // Done: handle the use case for livestreams
            // Done: handle the case for Shorts
            // https://i.ytimg.com/vi/lhfBIsHmhSs/oardefault.jpg?sqp=-oaymwEkCJUDENAFSFqQAgHyq4qpAxMIARUAAAAAJQAAyEI9AICiQ3gB&rs=AOn4CLCEtR7lA0dDWmwsYAppArMjQaNfaA
        }

        string newFilename;

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
