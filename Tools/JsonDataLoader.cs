using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tools
{
    public static class JsonDataLoader
    {
        public static List<T> LoadJsonFile<T>(IWebHostEnvironment env, string relativePath)
        {
            var path = Path.Combine(env.WebRootPath, relativePath);
            var json = System.IO.File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<T>>(json);
        }
    }
}