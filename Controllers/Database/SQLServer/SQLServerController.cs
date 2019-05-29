using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using DevOps.Common;
using DevOps.Controllers.Database.SQLServer.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DevOps.Controllers.Database.SQLServer
{
    public class SQLServerController : Controller
    {
        public SQLServerController(IDbConnection db)
        {
            this.db = db;
        }

        public async Task<IEnumerable<DatabaseDto>> DBList()
        {
            return await this.db.QueryAsync<DatabaseDto>(@"
                SELECT name AS Name, database_id AS Id
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
                }, commandTimeout: 120);

                var ms = new MemoryStream();
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    zip.CreateEntryFromFile(backFile, $"{dbName}.bak");
                }
                ms.Position = 0;
                return File(ms, "application/octet-stream", $"{dbName}-{DateTime.Now:yyyy-MM-dd_HHmm}.zip");
            }
        }

        public async Task<IActionResult> Drop([Required]string dbToDrop)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await this.db.ExecuteAsync($@"ALTER DATABASE [{dbToDrop}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
                                          DROP DATABASE [{dbToDrop}]");
            return Ok();
        }

        public async Task<IActionResult> Branch([Required]string dbFrom, [Required]string dbTo)
        {
            if (!ModelState.IsValid) return base.BadRequest(ModelState);
            if (dbFrom == dbTo) return base.BadRequest("新老数据库不能一样。");
            
            string backPath = await GetBackupDirectory(this.db);
            string dataPath = await GetDataDirectory(this.db);
            string backFile = Path.Combine(backPath, $"{dbFrom}_{DateTime.Now:yyyyMMdd_HHmm}.bak");

            await this.db.ExecuteAsync("BACKUP DATABASE @dbName TO DISK=@location WITH INIT", new
            {
                DbName = dbFrom,
                Location = backFile,
            });

            var files = this.db
                .Query("RESTORE FILELISTONLY FROM  DISK = @backFile", new { BackFile = backFile })
                .Select(x => new { Name = (string)x.LogicalName, Path = (string)x.PhysicalName });
            string moves = string.Join(", ", files.Select(x =>
            {
                string newFileName = Regex.Replace(Path.GetFileName(x.Path), dbTo, dbTo, RegexOptions.IgnoreCase);
                string dest = Path.Combine(dataPath, $"{newFileName}");
                return $"\nMOVE N'{x.Name}' TO N'{dest}'";
            }));

            await this.db.ExecuteAsync(@$"IF DB_ID(@to) IS NULL CREATE DATABASE {dbTo}
		                       ALTER DATABASE {dbFrom} SET SINGLE_USER WITH ROLLBACK IMMEDIATE
		                       RESTORE DATABASE {dbFrom} FROM DISK=@location WITH {moves}, REPLACE", new
            {
                To = dbTo,
                Location = backFile,
            });

            await this.db.ExecuteAsync($"EXEC master.dbo.xp_delete_file 0, @path", new { Path = backFile });
            return Ok();
        }

        private static async Task<string> GetBackupDirectory(IDbConnection db)
        {
            return
                await db.QueryFirstAsync<string>("SELECT ServerProperty(@prop)", new
                {
                    Prop = "InstanceDefaultBackupPath"
                }) ??
                db.QueryFirstOrDefault(@"EXEC xp_instance_regread @store, @path, @key", new
                {
                    Store = "HKEY_LOCAL_MACHINE",
                    Path = @"Software\Microsoft\MSSQLServer\MSSQLServer",
                    Key = "BackupDirectory"
                })?.Data;
        }

        private static async Task<string> GetDataDirectory(IDbConnection db)
        {
            return
                await db.QueryFirstAsync<string>("SELECT ServerProperty(@prop)", new
                {
                    Prop = "InstanceDefaultDataPath"
                }) ??
                db.QueryFirstOrDefault(@"EXEC xp_instance_regread @store, @path, @key", new
                {
                    Store = "HKEY_LOCAL_MACHINE",
                    Path = @"Software\Microsoft\MSSQLServer\MSSQLServer",
                    Key = "DefaultData"
                })?.Data;
        }

        private const string CategoryName = "SQLServer";
        private readonly IDbConnection db;
    }
}
