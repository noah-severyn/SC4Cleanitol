using csDBPF;
using SQLite;

namespace SC4CleanitolEngine {
    internal static class DatabaseBuilder {


        /// <summary>
        /// Initialize and create the TGI database.
        /// </summary>
        /// <param name="dbpath"></param>
        /// <returns></returns>
        public static SQLiteConnection CreateTGIdb(string dbpath) {
            //Open a new StreamWriter to create a file then immediately close - writing is handled by the SQLite functions
            File.CreateText(dbpath).Dispose();
            SQLiteConnection db = new SQLiteConnection(dbpath);
            db.CreateTable<TGIItem>();
            return db;
        }


        public static SQLiteConnection CreateBackupdb(string dbpath) {
            //Open a new StreamWriter to create a file then immediately close - writing is handled by the SQLite functions
            File.CreateText(dbpath).Dispose();
            SQLiteConnection db = new SQLiteConnection(dbpath);
            db.CreateTable<BackupItem>();
            return db;
        }

    }


    /// <summary>
    /// A TGI found in a scanned folder. Maps to a record in the <c>TGI</c> table created when scanning folders for TGIs.
    /// </summary>
    [Table("TGIs")]
    internal class TGIItem {
        [PrimaryKey, AutoIncrement]
        [Column("Id")]
        public uint Id { get; set; }
        [Column("Type")]
        public uint Type { get; set; }
        [Column("Group")]
        public uint Group { get; set; }
        [Column("Instance")]
        public uint Instance { get; set; }

        public override string ToString() {
            return $"{DBPFUtil.ToHexString(Type)}, {DBPFUtil.ToHexString(Group)}, {DBPFUtil.ToHexString(Instance)}";
        }

        public TGIItem(TGI tgi) {
            Type = tgi.TypeID;
            Group = tgi.GroupID;
            Instance = tgi.InstanceID;
        }
    }

    /// <summary>
    /// An item for the undo batch file. Maps to a record in the <c>Backups</c> table created when files are found that should be removed.
    /// </summary>
    [Table("Backups")]
    public class BackupItem {
        [PrimaryKey, AutoIncrement]
        [Column("Id")]
        public uint Id { get; set; }
        /// <summary>
        /// The full path of the file in the current directory removed from its original location.
        /// </summary>
        [Column("From")]
        public string From { get; set; }
        /// <summary>
        /// The full path of the original location of the file (where it will be moved back to).
        /// </summary>
        [Column("To")]
        public string To { get; set; }

        public override string ToString() {
            return $"{Id}: {From}  →  {To}";
        }

        /// <summary>
        /// Instantiate a new instance of this class
        /// </summary>
        /// <param name="from">The full path of the file in the current directory removed from its original location.</param>
        /// <param name="to">The full path of the original location of the file (where it will be moved back to).</param>
        public BackupItem(string from, string to) {
            From = from;
            To = to;
        }

        /// <summary>
        /// Instantiate a new instance of this class
        /// </summary>
        /// <remarks>A parameterless constructor is required for SQLiteConnection query to return a list of this type.</remarks>
        BackupItem() {
            From = string.Empty;
            To = string.Empty;
        }
    }
}
