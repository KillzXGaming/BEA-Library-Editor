using System;
using Syroot.BinaryData;
using System.IO;

namespace BezelEngineArchive_Lib
{
    public class ASST : IFileData //File asset?
    {
        private const string _signature = "ASST";

        public ushort unk { get; set; }
        public ushort unk2 { get; set; }
        public byte[] FileData;
        public string FileName;
        public long UncompressedSize;

        void IFileData.Load(FileLoader loader)
        {
            loader.CheckSignature(_signature);
            loader.LoadBlockHeader();
            unk = loader.ReadUInt16();
            unk2 = loader.ReadUInt16();
            uint FileSize = loader.ReadUInt32();
            UncompressedSize = loader.ReadInt64();
            FileData = loader.LoadCustom(() => loader.ReadBytes((int)FileSize));
            FileName = loader.LoadString();
        }
        void IFileData.Save(FileSaver saver)
        {
            saver.WriteSignature(_signature);
            saver.SaveBlockHeader();
            saver.Write(unk);
            saver.Write(unk2);
            saver.Write((uint)FileData.Length);
            saver.Write(UncompressedSize);
            saver.SaveBlock(FileData, (uint)saver.BezelEngineArchive.RawAlignment, () => saver.Write(FileData));
            saver.SaveRelocateEntryToSection(saver.Position, 1, 1, 0, 1, "Asst File Name Offset"); //      <------------ Entry Set
            saver.SaveString(FileName);
        }
    }
}
