using System.Data;

namespace ParserPlanGraph
{
    public interface IParser
    {
        void Parsing();
        DataTable GetRegions();
        void GetListFileArch(string arch, string pathParse, string region, int regionId);
        string GetArch44(string arch, string pathParse);
    }
}