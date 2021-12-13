using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PenTabletNotebook {
    class SaveLoad {
        const int FILE_FOURCC = 0x424e5450;
        const int FILE_VERSION = 5;

        List<DOPage> mPageList = new List<DOPage>();
        List<PageTag> mPageTagList = new List<PageTag>();

        public static void SerializeString(string s, BinaryWriter bw) {
            var utf8 = System.Text.Encoding.UTF8.GetBytes(s);
            int bytes = utf8.Length;
            bw.Write(bytes);
            bw.Write(utf8);
        }

        public static string DeserializeString(BinaryReader br) {
            int bytes = br.ReadInt32();
            var utf8 = br.ReadBytes(bytes);
            return System.Text.Encoding.UTF8.GetString(utf8);
        }

        private void SerializePageTag(PageTag pt, BinaryWriter bw) {
            // page nr
            // name
            int pgNr = pt.PageNr;

            bw.Write(pgNr);
            SerializeString(pt.Name, bw);
        }

        private PageTag DeserializePageTag(BinaryReader br) {
            int pgNr = br.ReadInt32();
            string name = DeserializeString(br);
            return new PageTag(name, pgNr);
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        /// <summary>
        /// ページの数だけSaveAddPageを呼んでからSave()します。
        /// </summary>
        public void SaveAddPage(DOPage p) {
            mPageList.Add(p);
        }

        public void SaveAddPageTag(PageTag tnp) {
            mPageTagList.Add(tnp);
        }

        /// <summary>
        /// ページの数だけSaveAddPageしてSaveします。
        /// </summary>
        public bool Save(string path, int curPageNr) {
            // ファイルに保存します。
            try {
                using (var bw = new BinaryWriter(File.Open(path, FileMode.Create))) {
                    bw.Write(FILE_FOURCC);
                    bw.Write(FILE_VERSION);
                    bw.Write(curPageNr);

                    // 保存ファイル名を記録。
                    SerializeString(path, bw);

                    // PageTagを保存。
                    int ptCount = mPageTagList.Count;
                    bw.Write(ptCount);
                    for (int i=0; i<mPageTagList.Count; ++i) {
                        var p = mPageTagList[i];
                        SerializePageTag(p, bw);
                    }

                    // ページを保存。
                    int plCount = mPageList.Count;
                    bw.Write(plCount);
                    for (int i=0; i<mPageList.Count;++i) {
                        var p = mPageList[i];
                        p.Save(bw);
                        Console.WriteLine("Page {0} saved", i);
                    }
                }
            } catch (IOException ex) {
                Console.WriteLine("IOException {0}\n{1}", path, ex);
                return false;
            }

            return true;
        }

        public bool Load(string path, ref SaveCtx sc) {
            string loadDir = System.IO.Path.GetDirectoryName(path);

            sc.pageList.Clear();
            sc.pageTagList.Clear();

            try {
                using (var br = new BinaryReader(File.Open(path, FileMode.Open))) {
                    int fourcc = br.ReadInt32();
                    if (FILE_FOURCC != fourcc) {
                        Console.WriteLine("FourCC mismatch error {0:x8} should be {1:x8}", fourcc, FILE_FOURCC);
                        return false;
                    }
                    int version = br.ReadInt32();
                    if (4 != version && FILE_VERSION != version) {
                        Console.WriteLine("Version unsupported {0} should be {1}", version, FILE_VERSION);
                        return false;
                    }

                    sc.curPageNr = br.ReadInt32();

                    // 保存ファイル名。
                    string savePath = DeserializeString(br);
                    string saveDir = System.IO.Path.GetDirectoryName(savePath);

                    if (5 <= version) {
                        // PageTagを読み出し。
                        int ptCount = br.ReadInt32();
                        for (int i=0; i<ptCount; ++i) {
                            var p = DeserializePageTag(br);
                            sc.pageTagList.Add(p);
                        }
                    }

                    // ページを読み出し。
                    int plCount = br.ReadInt32();
                    if (plCount < 0) {
                        Console.WriteLine("Page count is negative {0}", plCount);
                        return false;
                    }

                    for (int i=0; i<plCount; ++i) {
                        var p = new DOPage();
                        p.Load(br, saveDir, loadDir);
                        Console.WriteLine("Page {0} loaded. {1}", i, System.IO.Path.GetFileName(p.ImgFilename));
                        sc.pageList.Add(p);
                    }
                }
            } catch (IOException ex) {
                Console.WriteLine("IOException {0}\n{1}", path, ex);
                return false;
            }

            return true;
        }
    }
}
