using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarShell.Services
{
    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public string DownloadUrl { get; set; } = string.Empty;

        public bool HasUpdate { get; set; }
    }
}