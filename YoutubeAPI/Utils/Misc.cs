using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeAPI.Utils
{
    public class Misc
    {
        public static string GetProjectDirectoryPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\");
        }

        public static bool AreAllPropertiesNull(object obj)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties();

            foreach (var property in properties)
                if (property.GetValue(obj) != null)
                    return false;

            return true;
        }
    }
}
