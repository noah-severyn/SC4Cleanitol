using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
//using SQLite;
using System.IO;

//package: sqlite-net-pcl

//namespace SC4CleanitolWPF {
//    /// <summary>
//    /// An item in the TGITable, which tracks which TGIs are in which dependency pack. 
//    /// </summary>s
//    [Table("TGITable")]
//    public class TGIItem {
//        [PrimaryKey, AutoIncrement]
//        [Column("TGIID")]
//        public int ID { get; set; }

//        [Column("PackID")]
//        public int PackID { get; set; }

//        [Column("TGI")]
//        public string TGI { get; set; }

//        public override string ToString() {
//            return $"{ID}: {PackID}, {TGI}";
//        }
//    }

//    /// <summary>
//    /// An item in the FileTable, which tracks information about each file in plugins.
//    /// </summary>
//    [Table("FileTable")]
//    public class FileTable {
//        [PrimaryKey, AutoIncrement]
//        [Column("FileID")]
//        public int FileID { get; set; }

//        [Column("FileName")]
//        public string FileName { get; set; }

//        public override string ToString() {
//            return $"{FileID}: {FileName}";
//        }
//    }



//    /// <summary>
//    /// Create and operate on the Prop Texture Catalog database.
//    /// </summary>
//    public class DatabaseBuilder {
//        private readonly SQLiteConnection _db;

//        public DatabaseBuilder(string dbpath) {
//            dbpath = Path.Combine(dbpath, "PluginsTGIs.db");
//            if (File.Exists(dbpath)) {
//                File.Delete(dbpath);
//            }

//            //Open a new StreamWriter to create a file then immediately close - writing is handled by the SQLite functions
//            File.CreateText(dbpath).Dispose();

//            //Initialize database tables and schema
//            _db = new SQLiteConnection(dbpath);
//            _db.CreateTable<TGIItem>();
//            _db.CreateTable<FileTable>();
//        }


//        /// <summary>
//        /// Add a TGI item with associated information to the database.
//        /// </summary>
//        /// <param name="fileName">File name the TGI is contained in</param>
//        /// <param name="tgi">String representation of the TGI in the format 0x00000000, 0x00000000, 0x00000000 </param>
//        /// <remarks>The path in TGITable is stored as a reference to the full path in PathTable. This dramatically reduces file size as the long path string only needs to be stored once.</remarks>
//        public void AddTGI(string fileName, string tgi) {
//            //check if we already have a matching FileID, create new FileTable if not, otherwise use found FileID
//            int fileID = GetFileID(fileName);
//            if (fileID <= 0) {

//                FileTable newPack = new FileTable {
//                    FileName = fileName
//                };
//                _db.Insert(newPack);
//                fileID = newPack.FileID;
//            }

//            //once we know our FileID then add the new TGI with that FileID
//            TGIItem newTGI = new TGIItem {
//                PackID = fileID,
//                TGI = tgi
//            };
//            _db.Insert(newTGI);
//        }


//        /// <summary>
//        /// Lookup and return the PathID for the provided PathName.
//        /// </summary>
//        /// <param name="name">Path name (file name) to lookup</param>
//        /// <returns>PathID if name is found; -1 if item is not found or if multiple matches were found</returns>
//        /// <remarks>PathItems table should always have unique items, so we might have an issue if the return is more than one item.</remarks>
//        private int GetFileID(string name) {
//            string searchName = name.Replace("'", "''");
//            List<FileTable> result = _db.Query<FileTable>($"SELECT * FROM FileTable WHERE FileName = '{searchName}'");

//            if (result.Count == 1) {
//                return result[0].FileID;
//            } else {
//                return -1;
//            }
//        }


//        private bool DoesTGIExist(string tgi) {
//            List<TGIItem> result = _db.Query<TGIItem>($"SELECT * FROM TGITable WHERE TGI = '{tgi}'");
//            return result.Count == 0;
//        }
//    }
//}
