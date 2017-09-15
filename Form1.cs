using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Threading;
namespace Lightshot_Dumper
{
    
    
    public partial class Form1 : Form
    {
        bool bRun = false;
        static Dumper oDumper = new Dumper();
        Thread oThread = new Thread(new ThreadStart(oDumper.Dump));
        public Form1()
        {
            InitializeComponent();

        }

        private void btnStart_Click(object sender, EventArgs e)
        {

            if (!Directory.Exists(txtDest.Text)) // check if path is valid
            {
                MessageBox.Show("Destination path invalid.");
                return;
            }

            bRun = !bRun;
            if (bRun)
            {
                globals.destination = txtDest.Text;
                oThread.Start();
                btnStart.Text = "Stop";
            }
            else
            {
                oThread.Abort();
                oThread = new Thread(new ThreadStart(oDumper.Dump));
                btnStart.Text = "Start";
            }
               
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label2.Text = "Images downloaded: " + globals.downloadCount.ToString(); // misc. stats
        }

        private void btnChooseDest_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    txtDest.Text = fbd.SelectedPath;
                }
            }
        }
    }
    public class Dumper
    {
        private static Random random = new Random();
        private static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        // This method that will be called when the thread is started
        public void Dump()
        {
            
            while (true)
            {
                try
                {
                    string picID = RandomString(6); // generate an id for the image
                    string urlAddress = "https://prnt.sc/" + picID;

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddress);
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36"; // common user-agent
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Stream receiveStream = response.GetResponseStream();
                        StreamReader readStream = null;

                        if (response.CharacterSet == null)
                        {
                            readStream = new StreamReader(receiveStream);
                        }
                        else
                        {
                            readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                        }

                        string data = readStream.ReadToEnd();

                        response.Close();
                        readStream.Close();

                        int marker1 = data.IndexOf("\"image__pic js-image-pic\" src=\""); // we search for the beginning of the <img> tag and then search for the '"' ending the src attribute which gives us the URL.
                        int marker2 = data.IndexOf("\"", marker1 + 31); // 31 is the length of what we searched above (indexOf gives the beginning of the searched str)
                        string img = data.Substring(marker1 + 31, marker2 - marker1 - 31);
                        if (img == "//st.prntscr.com/2017/09/07/1522/img/0_173a7b_211be8ff.png") // check if screenshot was removed or never existed
                            continue;
                        string ext = Path.GetExtension(img);
                        string filePath = Path.Combine(globals.destination, (picID + ext));
                        if (File.Exists(filePath)) // in the rare chance that we are downloading an image we already have downloaded before, continue.
                            continue;
                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(img,  filePath); // download the image
                        globals.downloadCount++; // DownloadFile did not fail, add 1 to our download count
                    }

                }
                catch { }
            }
        }
    }
    public static class globals
    {
        public static string destination; // path to save images
        public static int downloadCount = 0; // count for downloads
    }
}
