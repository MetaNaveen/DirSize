class Program {
   static void Main (string[] args) {
      if (args.Length == 0) {
         Console.WriteLine ("Usage: Dirsize <path> [--exclude=bin,obj,*.user] [--unit=KB|MB|GB]");
         return;
      }

      string path = args[0];
      string[] excludePatterns = ["bin", "obj", "properties", ".git", ".vscode", ".vs", "TData"]; // Default exclude patterns
      string[] fileFormats = []; // eg: ".cs", ".csproj"
      string unit = "KB";
      bool isVerbose = false;

      foreach (var arg in args.Skip (1).Select (x => x.ToLower ())) {
         if (arg.StartsWith ("--exclude=")) {
            excludePatterns = arg.Substring ("--exclude=".Length)
                                 .Split (',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select (p => p.Trim ())
                                 .ToArray ();
         } else if (arg.StartsWith ("--fileformats=")) {
            fileFormats = arg.Substring ("--fileFormats=".Length)
                                 .Split (',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select (p => p.Trim ())
                                 .ToArray ();
         } else if (arg.StartsWith ("--unit=")) {
            unit = arg.Substring ("--unit=".Length).ToUpperInvariant ();
            if (unit != "KB" && unit != "MB" && unit != "GB")
               unit = "KB";
         } else if (arg.Equals ("--verbose")) isVerbose = true;
      }

      // Handle drive letter input like "C:"
      if (path.Length == 2 && path[1] == ':') path += @"\";  // Convert "C:" to "C:\"

      if (File.Exists (path)) {
         long size = new FileInfo (path).Length;
         Console.WriteLine ($"{path}\t{FormatSize (size, unit)} {unit}");
      } else if (Directory.Exists (path)) {
         var folders = Directory.GetDirectories (path, "*", SearchOption.AllDirectories)
                                .Prepend (path)
                                .OrderBy (p => p)
                                .ToList ();

         long total = 0;
         foreach (var folder in folders) {
            long folderSize = GetDirectorySize (folder, fileFormats, excludePatterns);
            if (folderSize != 0) {
               total += folderSize;
               if (isVerbose) Console.WriteLine ($"{folder}\t{FormatSize (folderSize, unit)} {unit}");
            }
         }

         Console.WriteLine ($"Total: {FormatSize (total, unit)} {unit}");
      } else {
         Console.WriteLine ("Invalid path.");
      }
   }

   static long GetDirectorySize (string folderPath, string[] filePatterns, string[] excludePatterns) {
      var files = Directory.EnumerateFiles (folderPath, "*", SearchOption.TopDirectoryOnly)
          .Where (file => {
             // File format inclusions
             if (!filePatterns.Any (ext => file.EndsWith (ext, StringComparison.OrdinalIgnoreCase))) return false;
             // Directory exclusions
             string normalizedPath = file.Replace (Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
             foreach (var pattern in excludePatterns) {
                if (normalizedPath
                   .Split (Path.DirectorySeparatorChar)
                   .Any (segment => segment.Equals (pattern, StringComparison.OrdinalIgnoreCase))) return false;
             }
             return true;
          });

      long total = 0;
      foreach (var file in files) {
         try { total += new FileInfo (file).Length; } catch { }
      }
      return total;
   }

   static string FormatSize (long bytes, string unit) {
      return unit switch {
         "MB" => (bytes / (1024.0 * 1024)).ToString ("F2"),
         "GB" => (bytes / (1024.0 * 1024 * 1024)).ToString ("F2"),
         _ => (bytes / 1024.0).ToString ("F2") // Default unit is KB
      };
   }
}
