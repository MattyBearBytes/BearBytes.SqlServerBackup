using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                var backupDir = ConfigurationManager.AppSettings["BackupDirectory"];

                if(Directory.Exists(backupDir))
                    Log.Info("Backup directory exists.");
                else
                {
                    Log.Info("Backup directory doesn't exist.  Creating directory.");
                    Directory.CreateDirectory(backupDir);
                }

                Log.Info("Backing up database '{0}' to '{1}'", databaseName, backupDir);

                var backupFileName = "Backup_" + databaseName + "_" + DateTime.Now.ToString("yyyymmdd") + ".bak";
                var backupFilePath = Path.Combine(backupDir, backupFileName);

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

                //Make sure that the new backup file exists
                if (File.Exists(backupFilePath))
                {
                    Log.Info("Backup Successful");
                }
                else
                {
                    Log.Warn("Backup file not found after completing without errors.");
                }
            }
            catch (Exception ex)
            {
                Log.Fatal("Error: Backup aborted.  Message: {0}", ex.Message);
            }
        }
    }
}
