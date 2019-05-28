using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using SystemFile = System.IO.File;

namespace DevOps.Controllers.Scheduler
{
    public class SchedulerController : Controller
    {
        public SchedulerController(IDbConnection db)
        {
            this.db = db;
            Directory.CreateDirectory(BaseDirectory);
        }

        public async Task<IActionResult> UploadLinq(IFormFile uploadedFile)
        {
            if (!uploadedFile.FileName.EndsWith(".linq")) return BadRequest("File extension need be .linq");
            string directory = EnsureDirectory(Path.GetFileNameWithoutExtension(uploadedFile.FileName));

            using (FileStream file = SystemFile.OpenWrite(Path.Combine(directory, "Runner.linq")))
            using (Stream destStream = uploadedFile.OpenReadStream())
            {
                await destStream.CopyToAsync(file);
            }

            return Ok();
        }

        public async Task<IActionResult> DownloadLinq(string id)
        {
            string directory = EnsureDirectory(id);
            return File(Path.Combine(directory, "Runner.linq"), "application/octet-stream");
        }

        private static string EnsureDirectory(string segment)
        {
            string directory = Path.Combine(BaseDirectory, segment);
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static string BaseDirectory { get; } = Path.Combine(Program.BaseDirectory, "Scheduler");
        private IDbConnection db;
    }
}
