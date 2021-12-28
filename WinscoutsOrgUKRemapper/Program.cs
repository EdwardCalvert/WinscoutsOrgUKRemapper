using HtmlAgilityPack;
using System.Text;

namespace test
{

    class Program
    {
        static void Main(string[] args)
        {
            string rootDirectory = @"C:\Users\ed\OneDrive\Documents\DofE\Scouts Web Development\winscoutsHTML";
            Console.Title = "Winscouts.org.uk Url Remapper";
            Console.Write("Please enter the root url to remap: ");
            string alternativeDirectory = Console.ReadLine();
            if (alternativeDirectory != null && alternativeDirectory.Length > 1)
            {
                rootDirectory = alternativeDirectory;
            }
            IUrlRemapper urlRemapper = new URLRemapper(new WebPageFileManager());
            urlRemapper.FindAndReplaceHyperlinks(rootDirectory);
        }
    }

    class WebPageFileManager : IWebPageFileManager
    {
        public List<IFileInformation> GetFileInformationInFolder(string directory, string fileExtension)
        {
            if (Directory.Exists(directory))
            {
                return RecursiveDirectoryTraversal(directory, fileExtension);
            }
            else
            {
                throw new Exception("The directory provided to the WebPageFileManager Doesn't exist.");
            }
        }

        public List<IFileInformation> RecursiveDirectoryTraversal(string directoryPath, string fileExtension)
        {
            List<IFileInformation> paths = new();
            foreach (string thing in Directory.GetFiles(directoryPath))
            {
                if (thing.EndsWith(fileExtension))
                {
                    paths.Add(new FileInformation(thing));
                }
            }
            foreach (string folder in Directory.GetDirectories(directoryPath))
            {
                if (!folder.Contains(".git"))
                {


                    List<IFileInformation> results = RecursiveDirectoryTraversal(folder, fileExtension);
                    if (results.Count > 0)
                    {
                        paths.AddRange(results);

                    }
                }
            }
            return paths;
        }


    }

    interface IWebPageFileManager
    {
        public List<IFileInformation> GetFileInformationInFolder(string directoryPath, string fileExtension);
    }

    interface IUrlRemapper
    {
        public void FindAndReplaceHyperlinks(string directory);
    }

    interface IFileInformation
    {
        string Path { get; set; }
    }

    public class FileInformation : IFileInformation
    {
        public string Path { get; set; }
        public FileInformation(string path)
        {
            Path = path;
        }

        public FileInformation()
        {
        }
    }

    class URLRemapper : IUrlRemapper
    {
        //Href, src,

        private IWebPageFileManager _fileManager;
        //private string thing;

        public URLRemapper(IWebPageFileManager fileManager)
        {
            _fileManager = fileManager;
        }

        public async void FindAndReplaceHyperlinks(string directory)
        {
            List<IFileInformation> files = _fileManager.GetFileInformationInFolder(directory, ".html");
            Console.WriteLine(files.Count);
            foreach (IFileInformation fileInformation in files)
            {
                Console.WriteLine(fileInformation.Path);
                HtmlDocument doc = new HtmlDocument();
                doc.Load(fileInformation.Path);
                doc = ExtractHref(doc, directory, fileInformation.Path, "a", "href");
                doc = ExtractHref(doc, directory, fileInformation.Path, "iframe", "src");
                doc = ExtractHref(doc, directory, fileInformation.Path, "script", "src");
                doc = ExtractHref(doc, directory, fileInformation.Path, "link", "href");
                doc = ExtractHref(doc, directory, fileInformation.Path, "img", "src");
                doc.Save(fileInformation.Path);
                //Console.WriteLine(doc.DocumentNode.OuterHtml);
            }
        }

        public string RelativePath(string absPath, string relTo)
        {
            string[] absDirs = absPath.Split('\\');
            string[] relDirs = relTo.Split('\\');

            // Get the shortest of the two paths
            int len = absDirs.Length < relDirs.Length ? absDirs.Length :
            relDirs.Length;

            // Use to determine where in the loop we exited
            int lastCommonRoot = -1;
            int index;

            // Find common root
            for (index = 0; index < len; index++)
            {
                if (absDirs[index] == relDirs[index]) lastCommonRoot = index;
                else break;
            }

            // If we didn't find a common prefix then throw
            if (lastCommonRoot == -1)
            {
                throw new ArgumentException("Paths do not have a common base");
            }

            // Build up the relative path
            StringBuilder relativePath = new StringBuilder();

            // Add on the ..
            for (index = lastCommonRoot + 1; index < absDirs.Length; index++)
            {
                if (absDirs[index].Length > 0) relativePath.Append("..\\");
            }

            // Add on the folders
            for (index = lastCommonRoot + 1; index < relDirs.Length - 1; index++)
            {
                relativePath.Append(relDirs[index] + "\\");
            }
            relativePath.Append(relDirs[relDirs.Length - 1]);
            //relativePath.Append(absDirs[absDirs.Length - 1]);

            return relativePath.ToString();
        }

        string PathHome(string baseDirectory, string currentDocumentPath)
        {
            return RelativePath(currentDocumentPath, baseDirectory);
        }

        string RemapUrl(string href, string pathHome)
        {
            if (href.StartsWith("//"))
            {
                href = "https:" + href;
            }
            else if (href.StartsWith("/"))
            {
                if (href.EndsWith("/"))
                {
                    href = pathHome + href + "index.html";
                }
                else if (href.EndsWith("\""))
                {
                    href = pathHome + href.Remove(href.Length - 1);
                }
                else
                {
                    href = pathHome + href;
                }
            }
            else if (href.StartsWith("./") && href.EndsWith("\""))
            {
                href = href.Remove(href.Length - 1);

            }
            else if (href.StartsWith("http://www.winscouts.org.uk"))
            {
                href = href.Replace("http://www.winscouts.org.uk", pathHome) + "index.html";
            }
            return href;
        }
        HtmlDocument ExtractHref(HtmlDocument doc, string baseDirectory, string doccumentPath, string tag, string property)
        {

            HtmlNodeCollection htmlNodes = doc.DocumentNode.SelectNodes($"//{tag}[@{property}]");
            if (htmlNodes != null)
            {
                string pathHome = PathHome(baseDirectory, doccumentPath).Replace("\\", "/");
                foreach (HtmlNode node in htmlNodes.ToList())
                {
                    node.SetAttributeValue(property, RemapUrl(node.Attributes[property].Value, pathHome));
                    Console.WriteLine(node.OuterHtml);
                }
            }
            return doc;
        }
    }
}
