using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Dapper;
using DevOps.Common;
using Microsoft.AspNetCore.Mvc;

namespace DevOps.Controllers.Database.SQLServer
{
    public class SQLServerController : Controller
    {
        public SQLServerController(IDbConnection db)
        {
            this.db = db;
        }

        public async Task<dynamic> DBList()
        {
            return await this.db.QueryAsync(@"
                SELECT name AS Name, database_id AS DatabaseId
                FROM master.sys.databases
                where database_id > 4");
        }

        public async Task<IActionResult> Backup(string dbName)
        {
            using (var backFolder = TempDirectory.CreateFromBase(await GetBackupDirectory(this.db), CategoryName))
            {
                string backFile = backFolder.Concat($"{dbName}.bak");
                await this.db.ExecuteAsync("BACKUP DATABASE @dbName TO DISK=@location WITH INIT", new
                {
                    DbName = dbName,
                    Location = backFile,
                });

                var ms = new MemoryStream();
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    zip.CreateEntryFromFile(backFile, $"{dbName}.bak");
                }
                ms.Position = 0;
                return File(ms, "application/octet-stream", $"{dbName}-{DateTime.Now:yyyy-MM-dd_HHmm}.zip");
            }
        }

        private static async Task<string> GetBackupDirectory(IDbConnection db)
        {
            dynamic value = await db.QueryFirstAsync(
                @"EXEC master.dbo.xp_instance_regread @store, @location, @item", new
                {
                    Store = "HKEY_LOCAL_MACHINE", 
                    Location = @"Software\Microsoft\MSSQLServer\MSSQLServer", 
                    Item = "BackupDirectory"
                });
            return value.Data;
        }

        private const string CategoryName = "SQLServer";
        private readonly IDbConnection db;
    }
}
