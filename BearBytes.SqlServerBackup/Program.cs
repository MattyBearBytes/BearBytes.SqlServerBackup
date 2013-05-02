using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using NLog;

namespace BearBytes.SqlServerBackup
{
    class Program
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            try
            {
                Log.Info("Starting Database Backup Utility...");

                var connectionString = ConfigurationManager.ConnectionStrings["MyConnection"].ToString();
                var databaseName = ConfigurationManager.AppSettings["DatabaseName"];
                var backupDirs = ConfigurationManager.AppSettings["BackupDirectories"].Split(',');

                //Make sure the backup directories exist
                foreach (var backupDir in backupDirs.Where(backupDir => !Directory.Exists(backupDir)))
                    throw new Exception("Backup directory '" + backupDir + "' doesn't exist.");

                Log.Info("Backing up database '{0}' to '{1}'", databaseName, backupDirs[0]);

                var backupFileName = "Backup_" + databaseName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".bak";
                var backupFilePath = Path.Combine(backupDirs[0], backupFileName);

                using (var conn = new SqlConnection(connectionString))
                {
                    //Generate backup command
                    var sql = string.Format("BACKUP DATABASE {0} TO DISK='{1}'", databaseName, backupFilePath);

                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        Log.Info("Executing database backup script...");
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();
                    }
                }

                //Copy backup to other locations if specified
                foreach (var backupDir in backupDirs.Where(x => x != backupDirs[0]))
                {
                    File.Copy(backupFilePath, Path.Combine(backupDir, backupFileName));
                    Log.Info("Copied backup to: {0}", backupDir);
                }

                Log.Info("Backup completed successfully!");
            }
            catch (Exception ex)
            {
                Log.Fatal("Error: Backup aborted.  Message: {0}", ex.Message);
            }

            Console.Read();
        }
    }
}
