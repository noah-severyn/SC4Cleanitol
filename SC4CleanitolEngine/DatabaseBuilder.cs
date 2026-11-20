using csDBPF;
using SQLite;

namespace SC4CleanitolEngine {
    internal static class DatabaseBuilder {

        /// <summary>
        /// Create a SQLite database with a single table of a generic type, and add all items specified.
        /// </summary>
        /// <typeparam name="T">Object type to add.</typeparam>
        /// <param name="dbpath">Database file path to create</param>
        /// <param name="items">Items to add to the database</param>
        public static void CreateDb<T>(string dbpath, IEnumerable<T> items) {
            File.CreateText(dbpath).Dispose();
            var db = new SQLiteConnection(dbpath);
            db.CreateTable<T>();
            db.RunInTransaction(() => {
                db.InsertAll(items);
            });
        }
    }


    /// <summary>
    /// A pair with a TGI and the file it was found in.
    /// </summary>
    [Table("TGIs")]
    internal class FileTgiPair(string filepath, TGI tgi) {
        public string FilePath { get; set; } = filepath;
        public string Tgi { get; set; } = tgi.ToString();

        public override string ToString() {
            return FilePath + " : " + Tgi;
        }
    }
}
