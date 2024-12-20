using csDBPF;
using SQLite;

namespace SC4CleanitolEngine {
    internal static class DatabaseBuilder {

        public static SQLiteConnection CreateTGIdb(string dbpath) {
            //Open a new StreamWriter to create a file then immediately close - writing is handled by the SQLite functions
            File.CreateText(dbpath).Dispose();
            SQLiteConnection db = new SQLiteConnection(dbpath);
            db.CreateTable<TGIItem>();
            return db;
        }



    }


    /// <summary>
    /// A TGI found in a scanned folder.
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
    /// An item for the undo batch file.
    /// </summary>
    [Table("Backups")]
    internal class BackupItem {
        [PrimaryKey, AutoIncrement]
        [Column("Id")]
        public uint Id { get; set; }
        [Column("From")]
        public required string From { get; set; }
        [Column("To")]
        public required string To { get; set; }

        public override string ToString() {
            return $"{Id}: {From}  →  {To}";
        }
    }
}
