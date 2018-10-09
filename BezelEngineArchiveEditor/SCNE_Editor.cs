using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using BezelEngineArchive_Lib;
using ZstdNet;
using SARCExt;
using Be.Windows.Forms;

namespace BezelEngineArchiveEditor
{
    public partial class SCNE_Editor : UserControl
    {
        public SCNE_Editor()
        {
            InitializeComponent();

            ImageList imgList = new ImageList();
            imgList.ColorDepth = ColorDepth.Depth32Bit;
            imgList.ImageSize = new Size(34, 34);
            
            imgList.Images.Add("folder", Properties.Resources.folder);
            imgList.Images.Add("fileBlank", Properties.Resources.FileBank);
            imgList.Images.Add("bfres", Properties.Resources.Bfres);
            imgList.Images.Add("byaml", Properties.Resources.Byaml);
            imgList.Images.Add("aamp", Properties.Resources.Aamp);
            imgList.Images.Add("bntx", Properties.Resources.Bntx);
            imgList.Images.Add("bfsha", Properties.Resources.Bfsha);
            imgList.Images.Add("bnsh", Properties.Resources.Bnsh);

            treeView1.ImageList = imgList;
            treeView1.ForeColor = Color.White;
        }

        public bool Compressed;
        public class FolderEntry : TreeNode
        {
            public FolderEntry()
            {
                ImageKey = "folder";
                SelectedImageKey = "folder";
            }

            public FolderEntry(string Name)
            {
                Text = Name;
            }
        }
        public class FileEntry : TreeNode
        {
            public FileEntry()
            {
                ImageKey = "fileBlank";
                SelectedImageKey = "fileBlank";

                ContextMenu = new ContextMenu();
                MenuItem export = new MenuItem("Export");
                ContextMenu.MenuItems.Add(export);
                export.Click += Export;

                MenuItem replace = new MenuItem("Replace");
                ContextMenu.MenuItems.Add(replace);
                replace.Click += Replace;
            }

            public string FullName;

            private void Export(object sender, EventArgs args)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = Text;
                sfd.DefaultExt = Path.GetExtension(Text);
                sfd.Filter = "All files(*.*)|*.*";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(sfd.FileName, Form1.GetASSTData(FullName));
                }
            }

            private void Replace(object sender, EventArgs args)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.FileName = Text;
                ofd.DefaultExt = Path.GetExtension(Text);
                ofd.Filter = "All files(*.*)|*.*";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Form1.SetASST(File.ReadAllBytes(ofd.FileName), FullName);
                }
            }
        }

        void FillTreeNodes(FolderEntry root, Dictionary<string, ASST> files)
        {
            var rootText = root.Text;
            var rootTextLength = rootText.Length;
            var nodeStrings = files;
            foreach (var node in nodeStrings)
            {
                string nodeString = node.Value.FileName;

                var roots = nodeString.Split(new char[] { '/' },
                    StringSplitOptions.RemoveEmptyEntries);

                // The initial parent is the root node
                TreeNode parentNode = root;
                var sb = new StringBuilder(rootText, nodeString.Length + rootTextLength);
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    // Build the node name
                    var parentName = roots[rootIndex];
                    sb.Append("/");
                    sb.Append(parentName);
                    var nodeName = sb.ToString();

                    // Search for the node
                    var index = parentNode.Nodes.IndexOfKey(nodeName);
                    if (index == -1)
                    {
                        // Node was not found, add it

                        var temp = new TreeNode(parentName, 0, 0);
                        if (rootIndex == roots.Length - 1)
                            temp = SetupFileEntry(node.Value.FileData, parentName, node.Value.FileName);
                        else
                            temp = SetupFolderEntry(temp);

                        temp.Name = nodeName;
                        parentNode.Nodes.Add(temp);
                        parentNode = temp;
                    }
                    else
                    {
                        // Node was found, set that as parent and continue
                        parentNode = parentNode.Nodes[index];
                    }
                }
                parentNode = SetupFolderEntry(parentNode);
            }
        }

        public FolderEntry SetupFolderEntry(TreeNode node)
        {
            FolderEntry folder = new FolderEntry();
            folder.Text = node.Text;

            return folder;
        }

        List<string> BuildFinalList(List<string> paths)
        {
            var finalList = new List<string>();
            foreach (var path in paths)
            {
                bool found = false;
                foreach (var item in finalList)
                {
                    if (item.StartsWith(path, StringComparison.Ordinal))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    finalList.Add(path);
                }
            }
            return finalList;
        }

        public FileEntry SetupFileEntry(byte[] data, string name, string fullName)
        {
            FileEntry fileEntry = new FileEntry();
            fileEntry.FullName = fullName;
            fileEntry.Name = name;

            try
            {
                using (var decompressor = new Decompressor())
                {
                    data = decompressor.Unwrap(data);
                }
            }
            catch
            {
                Console.WriteLine("Unkwon compression for file " + fileEntry.Name);
            }
          

        //    fileEntry.data = data;
            fileEntry.Text = name;

            string ext = Path.GetExtension(name);
            if (name.EndsWith("bfres") || name.EndsWith("fmdb") || name.EndsWith("fskb") ||
                name.EndsWith("ftsb") || name.EndsWith("fvmb") || name.EndsWith("fvbb") ||
                name.EndsWith("fspb") || name.EndsWith("fsnb"))
            {
                fileEntry.ImageKey = "bfres";
                fileEntry.SelectedImageKey = "bfres";
            }
            if (name.EndsWith("byaml")|| name.EndsWith("byml") || name.EndsWith("yaml"))
            {
                fileEntry.ImageKey = "byaml";
                fileEntry.SelectedImageKey = "byaml";
            }
            if (name.EndsWith("aamp"))
            {
                fileEntry.ImageKey = "aamp";
                fileEntry.SelectedImageKey = "aamp";
            }
            if (name.EndsWith("bntx") || name.EndsWith("ftxb"))
            {
                fileEntry.ImageKey = "bntx";
                fileEntry.SelectedImageKey = "bntx";
            }
            return fileEntry;
        }

        public Dictionary<string, byte[]> FileData = new Dictionary<string, byte[]>();

        public void LoadFile(BezelEngineArchive beaFile, bool IsCompressed)
        {
            treeView1.Nodes.Clear();
            hexBox1.ByteProvider = null;
            Compressed = IsCompressed;

            FolderEntry root = new FolderEntry("Root");
            FillTreeNodes(root, beaFile.FileList);
            treeView1.Nodes.Add(root);

            treeView1.Sort();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;

            if (node is FileEntry)
            {
                if (Form1.GetASSTData(((FileEntry)node).FullName) != null)
                    UpdateHexEditor(Form1.GetASSTData(((FileEntry)node).FullName));
            }
            else if (node is FolderEntry)
            {
                hexBox1.ByteProvider = null;
            }
            else
            {
                hexBox1.ByteProvider = null;
            }
        }

        private void UpdateHexEditor(byte[] data)
        {
            hexBox1.ByteProvider = new DynamicByteProvider(data);
        }

        private void treeView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (treeView1.SelectedNode == null)
                return;

            string ext = Path.GetExtension(treeView1.SelectedNode.Text);

            TreeNode node = treeView1.SelectedNode;

            //This would open a file but nah it's just a bea editor
            if (node is FileEntry)
            {
            }
        }
    }
}
