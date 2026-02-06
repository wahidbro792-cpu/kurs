using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AtlasServiceCenter
{
    public static class UserPhotoHelper
    {
        public static string GetPhotosFolder()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string folder = Path.Combine(baseDir, "UserPhotos");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }

        public static string GetUserPhotoPath(int userId)
        {
            string folder = GetPhotosFolder();
            string fileName = $"User_{userId}.png";
            return Path.Combine(folder, fileName);
        }

        public static bool UserPhotoExists(int userId)
        {
            return File.Exists(GetUserPhotoPath(userId));
        }
    }
}
