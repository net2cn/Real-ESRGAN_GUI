using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Real_ESRGAN_GUI
{
    class Utils
    {
        public static string[] SearchDirectory(string directory, string filetype)
        {
            string[] files;
            files = Directory.GetFiles(directory, filetype, SearchOption.AllDirectories).Select(x => Path.GetFileName(x)).ToArray();
            return files;
        }
    }
}
