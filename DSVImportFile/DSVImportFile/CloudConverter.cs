using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSVImportFile
{
    public class CloudConverter
    {
        public string file { get; set; }
        public string apikey { get; set; }
        public string inputformat { get; set; }
        public string outputformat { get; set; }
        public string input { get; set; }
        public string filename { get; set; }
        public string timeout { get; set; }
        public string wait { get; set; }
        public string download { get; set; }
        public string save { get; set; }
    }
    public enum DirectoryFileType
    {
        InboundNormalFiles = 1,
        InboundInprogressFiles = 2,
        InboundFileWithPrefix = 3

    }

    public enum FolderType
    {
        onlyfolder = 1,
        folderwithprefix = 2,
        folderwithoutput = 3
    }

}
