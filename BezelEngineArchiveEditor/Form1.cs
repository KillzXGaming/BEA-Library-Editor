using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using BezelEngineArchive_Lib;
using System.IO;
using ZstdNet;

namespace BezelEngineArchiveEditor
{
    public partial class Form1 : Form
    {
        private Thread Thread;
        public static bool IsCompressed = true;

        public Form1()
        {
            InitializeComponent();
        }

        public static BezelEngineArchive beaFile;
        SCNE_Editor scneEditor;

        public void OpenFile(string FileName, byte[] data = null, bool Compressed = false)
        {
            Reset();

            if (data == null)
                data = File.ReadAllBytes(FileName);

            LoadBezelEngineArchive(data, FileName, Compressed);
        }
        public void Reset()
        {
            if (scneEditor != null)
                scneEditor.Dispose();
            scneEditor = null;
            beaFile = null;
            panel1.Controls.Clear();
            GC.Collect();
        }
        public void LoadBezelEngineArchive(byte[] data, string FileName = "", bool Compressed = false)
        {
            beaFile = new BezelEngineArchive(new MemoryStream(data));

            scneEditor = new SCNE_Editor();
            scneEditor.Dock = DockStyle.Fill;
            scneEditor.LoadFile(beaFile, Compressed);
            scneEditor.Text = Path.GetFileName(FileName);
            panel1.Controls.Add(scneEditor);

            BtnExtract.Visible = true;
            BtnRePack.Visible = true;

            scneEditor.Show();
        }
        static void Save(string FilePath)
        {
            beaFile.Save(FilePath);

            MessageBox.Show("Saved file!");
        }
        public static byte[] GetASSTData(string path)
        {
            if (beaFile.FileList.ContainsKey(path))
            {
                if (IsCompressed)
                {
                    using (var decompressor = new Decompressor())
                    {
                        return decompressor.Unwrap(beaFile.FileList[path].FileData);
                    }
                }
                else
                {
                    return beaFile.FileList[path].FileData;
                }

            }
            return null;
        }
        public static byte[] SetASST(byte[] data, string path)
        {
            if (beaFile.FileList.ContainsKey(path))
            {
                ASST asst = beaFile.FileList[path];
                Console.WriteLine(path + " A match!");

                asst.UncompressedSize = data.Length;
                using (var compressor = new Compressor())
                {
                    asst.FileData = compressor.Wrap(data);
                }
            }
            return data;
        }
        void Repack(string FolderName)
        {
            progressBar = new ProgressBar();
            progressBar.Task = "Reading Directory...";
            progressBar.Value = 0;
            progressBar.StartPosition = FormStartPosition.CenterScreen;
            progressBar.Show();
            progressBar.Refresh();

            List<string> flist = new List<string>();
            List<byte[]> fdata = new List<byte[]>();

            readFiles(FolderName, flist, fdata);
            progressBar.Task = "Repacking Files...";
            progressBar.Refresh();

            int i = 0;
            foreach (string f in flist)
            {
                int value = (i * 100) / flist.Count;
                progressBar.Value = value;

                if (beaFile.FileList.ContainsKey(f))
                {
                    ASST asst = beaFile.FileList[f];

                    if (f == asst.FileName)
                    {
                        Console.WriteLine(f + " A match!");

                        asst.UncompressedSize = fdata[i].Length;
                        progressBar.Task = "Compressing " + Path.GetFileName(f);
                        progressBar.Refresh();
                        using (var compressor = new Compressor())
                        {
                            asst.FileData = compressor.Wrap(fdata[i]);
                        }
                    }
                }

                if (value == 99)
                {
                    value = 100;

                    progressBar.Value = value;
                    progressBar.Refresh();
                }
                i++;
            }

            fdata.Clear();
            flist.Clear();

            GC.Collect();

            ReloadTree();
        }
        void ReloadTree()
        {
            scneEditor.LoadFile(beaFile, false);
        }
        void Extract(string Folder, ProgressBar progressBar)
        {
            int Curfile = 0;
            foreach (ASST asst in beaFile.FileList.Values)
            {
                Console.WriteLine($"{Folder}/{beaFile.Name}/{asst.FileName}");

                int value = (Curfile * 100) / beaFile.FileList.Count;
                progressBar.Value = value;
                progressBar.Refresh();

                try
                {
                    if (!String.IsNullOrWhiteSpace(Path.GetDirectoryName($"{Folder}/{beaFile.Name}/{asst.FileName}")))
                    {
                        if (!File.Exists(asst.FileName))
                        {
                            if (!Directory.Exists($"{Folder}/{beaFile.Name}/{asst.FileName}"))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName($"{Folder}/{beaFile.Name}/{asst.FileName}"));
                            }
                        }
                        else
                        {

                        }
                    }


                    byte[] data = asst.FileData;

                    using (var decompressor = new Decompressor())
                    {
                        data = decompressor.Unwrap(data);
                    }

                    System.IO.File.WriteAllBytes($"{Folder}/{beaFile.Name}/{asst.FileName}", data);
                }
                catch
                {

                }
              
                Curfile++;

                if (value == 99)
                    value = 100;

                progressBar.Value = value;
                progressBar.Refresh();
            }
        }
        static void readFiles(string dir, List<string> flist, List<byte[]> fdata)
        {
            processDirectory(dir, flist, fdata);
        }

        static IEnumerable<string> GetRelativePaths(string root)
        {
            int rootLength = root.Length + (root[root.Length - 1] == '\\' ? 0 : 1);

            foreach (string path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                yield return path.Remove(0, rootLength);
            }
        }

        static void processDirectory(string targetDirectory, List<string> flist, List<byte[]> fdata)
        {
         //   string[] fileEntries = Directory.GetFiles(targetDirectory);
            var fileEntries = GetRelativePaths(targetDirectory);

            foreach (string fileName in fileEntries)
            {
                processFile(fileName, fdata, targetDirectory);

                char[] sep = { '\\' };
                string[] fn = fileName.Split(sep);
                string tempf = "";
                for (int i = 0; i < fn.Length; i++)
                {
                    tempf += fn[i];
                    if (fn.Length > 2 && (i != fn.Length - 1))
                    {
                        tempf += "/";
                    }
                }
                flist.Add(tempf);
            }

            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                processDirectory(subdirectory, flist, fdata);
        }
        static void processFile(string path, List<byte[]> fdata, string targetDirectory)
        {
            byte[] temp = File.ReadAllBytes($"{targetDirectory}/{path}");
            fdata.Add(temp);
        }
        private static string CleanFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Supported Formats|*.bea|" +
                         "All files(*.*)|*.*";

            ofd.Multiselect = false;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                foreach (string filename in ofd.FileNames)
                {
                    OpenFile(filename);
                }
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string filename in files)
            {
                OpenFile(filename);
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
            {
                String[] strGetFormats = e.Data.GetFormats();
                e.Effect = DragDropEffects.None;
            }
        }

        ProgressBar progressBar;

        private void BtnExtract_Click(object sender, EventArgs e)
        {
            FolderSelectDialog fsd = new FolderSelectDialog();

            if (fsd.ShowDialog() == DialogResult.OK)
            {
                string folderPath = fsd.SelectedPath;
   
                progressBar = new ProgressBar();
                progressBar.Task ="Extracing Files...";
                progressBar.Refresh();
                progressBar.Value = 0;
                progressBar.StartPosition = FormStartPosition.CenterScreen;
                progressBar.Show();

                Extract(folderPath, progressBar);
            }
        }

        private void BtnRePack_Click(object sender, EventArgs e)
        {
            FolderSelectDialog fsd = new FolderSelectDialog();

            if (fsd.ShowDialog() == DialogResult.OK)
            {
                string folderPath = fsd.SelectedPath;
                Repack(folderPath);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void creditsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Credits credits = new Credits();
            credits.Show();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (beaFile == null)
                return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "bea";
            sfd.Filter = "Supported Formats|*.bea;";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                Save(sfd.FileName);
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (progressBar != null)
                progressBar.Value = e.ProgressPercentage;
        }
    }
}
