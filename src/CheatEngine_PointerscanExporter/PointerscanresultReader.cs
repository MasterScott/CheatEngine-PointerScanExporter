using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;

namespace CheatEngine_PointerscanExporter
{
    public class PointerscanresultReader
    {
        public string FileName;
        public List<string> Modules = new List<string>();
        public List<string> LinkedFiles = new List<string>();
        public List<RecordOffset> TableResults = new List<RecordOffset>();
        public int MaxOffsetCount;

        private int MaxBitCountModuleIndex => fMaxBitCountModuleIndex;
        private int fMaxBitCountModuleIndex;
        private int MaxBitCountModuleOffset => fMaxBitCountModuleOffset;
        private int fMaxBitCountModuleOffset;
        private int MaxBitCountLevel => fMaxBitCountLevel;
        private int fMaxBitCountLevel;
        private int MaxBitCountOffset => fMaxBitCountOffset;
        private int fMaxBitCountOffset;
        private bool fAligned;
        private int sizeofentry;

        private int MaskModuleIndex;
        private int MaskLevel;
        private int MaskOffset;

        private bool fCompressedPtr;
        private int[] fEndsWithOffsetList;

        public void ParseFile(string fileName)
        {
            FileName = fileName;
            using (FileStream FS = new FileStream(fileName, FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(FS))
                {

                    if (br.ReadByte() != 0xce)
                    {
                        MessageBox.Show("This is not Cheat Engine pointerscan result file!");
                        return;
                    }

                    byte pscanversion = br.ReadByte();//Version
                    int modulelistlength = br.ReadInt32();



                    for (int i = 0; i < modulelistlength; i++)
                    {
                        int moduleNameLength = br.ReadInt32();
                        char[] moduleNameCh = new char[0];
                        moduleNameCh = br.ReadChars(moduleNameLength);
                        string moduleName = string.Join("", moduleNameCh);

                        Modules.Add(moduleName);

                        var a = br.ReadInt64();//TODO: Define what a hell to do with this
                    }

                    int maxlevel = br.ReadInt32();

                    fCompressedPtr = br.ReadByte() == 1;

                    if (fCompressedPtr)
                    {
                        fAligned = br.ReadByte() == 1;

                        fMaxBitCountModuleIndex = br.ReadByte();
                        fMaxBitCountModuleOffset = br.ReadByte();
                        fMaxBitCountLevel = br.ReadByte();
                        fMaxBitCountOffset = br.ReadByte();


                        fEndsWithOffsetList = new int[br.ReadByte()];

                        for (int i = 0; i < fEndsWithOffsetList.Length; i++)
                        {
                            fEndsWithOffsetList[i] = br.ReadInt32();
                        }



                        sizeofentry = fMaxBitCountModuleOffset + fMaxBitCountModuleIndex + fMaxBitCountLevel + fMaxBitCountOffset * (maxlevel - fEndsWithOffsetList.Length);
                        sizeofentry = (sizeofentry + 7) / 8;


                        MaskModuleIndex = 0;
                        for (int i = 0; i < fMaxBitCountModuleIndex; i++)
                        {
                            MaskModuleIndex = (MaskModuleIndex << 1) ^ 1;
                        }

                        MaskLevel = 0;
                        for (int i = 0; i < fMaxBitCountLevel; i++)
                        {
                            MaskLevel = (MaskLevel << 1) ^ 1;
                        }

                        MaskOffset = 0;
                        for (int i = 0; i < fMaxBitCountOffset; i++)
                        {
                            MaskOffset = (MaskOffset << 1) ^ 1;
                        }
                    }
                    else
                    {
                        sizeofentry = 16 + (4 * maxlevel);
                    }


                    long foriginalBaseScanRange;
                    if (pscanversion >= 2)
                    {
                        var fdidBaseRangeScan = br.ReadByte();

                        if (fdidBaseRangeScan == 1)
                        {
                            foriginalBaseScanRange = br.ReadInt64();
                        }
                    }
                }
            }

            FindAllResultFilesForThisPtr(fileName, LinkedFiles);

            for (int i = 0; i < LinkedFiles.Count; i++)
            {

                using (FileStream curFS = new FileStream(LinkedFiles[i], FileMode.Open))
                {
                    using (BinaryReader curBR = new BinaryReader(curFS))
                    {
                        for (int j = 0; j < curFS.Length / sizeofentry; j++)
                        {
                            var result = new RecordOffset();

                            if (fCompressedPtr)
                            {
                                byte[] tempBuffer = curBR.ReadBytes(sizeofentry);

                                if (MaxBitCountModuleOffset == 32)
                                    result.moduleoffset = BitConverter.ToInt32(tempBuffer, 0);
                                else
                                    result.moduleoffset = BitConverter.ToInt64(tempBuffer, 0);


                                var bit = MaxBitCountModuleOffset;

                                result.modulenr = tempBuffer[bit >> 3];
                                result.modulenr = result.modulenr & MaskModuleIndex;

                                bit += fMaxBitCountModuleIndex;

                                result.offsetcount = tempBuffer[bit >> 3];
                                result.offsetcount = result.offsetcount >> (bit & 7);
                                result.offsetcount = result.offsetcount & MaskLevel;

                                result.offsetcount += fEndsWithOffsetList.Length;

                                bit += fMaxBitCountLevel;


                                var offsetsCount = fEndsWithOffsetList.Length + result.offsetcount;
                                result.offsets = new int[offsetsCount];


                                if (MaxOffsetCount < offsetsCount)
                                    MaxOffsetCount = offsetsCount;

                                for (int k = 0; k < fEndsWithOffsetList.Length; k++)
                                {
                                    result.offsets[k] = fEndsWithOffsetList[k];
                                }

                                for (int k = fEndsWithOffsetList.Length; k < result.offsetcount; k++)
                                {
                                    int pos = bit >> 3;
                                    var tempBuffer2 = new byte[4];

                                    Buffer.BlockCopy(tempBuffer, pos, tempBuffer2, 0, Math.Min(tempBuffer.Length - pos, 4));

                                    result.offsets[k] = BitConverter.ToInt32(tempBuffer2, 0);
                                    result.offsets[k] = result.offsets[k] >> (bit & 7);
                                    result.offsets[k] = result.offsets[k] & MaskOffset;

                                    if (fAligned)
                                        result.offsets[k] = result.offsets[k] << 2;

                                    bit += fMaxBitCountOffset;
                                }
                            }
                            else
                            {

                            }

                            TableResults.Add(result);
                        }

                    }

                }
            }
        }

        private void FindAllResultFilesForThisPtr(string fileName, List<string> result)
        {
            string searchFileName = fileName + ".results.";

            var dir = Path.GetDirectoryName(fileName);
            DirectoryInfo di = new DirectoryInfo(dir);
            foreach (var file in di.GetFiles())
            {
                if (file.FullName.StartsWith(searchFileName))
                    result.Add(file.FullName);
            }
        }
    }

    public class RecordOffset
    {
        public long moduleoffset;
        public int modulenr;
        public int offsetcount;
        public int[] offsets;
    }
}

