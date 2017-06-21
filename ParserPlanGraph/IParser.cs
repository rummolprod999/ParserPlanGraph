using System.Data;

namespace ParserPlanGraph
{
    public interface IParser
    {
        void Parsing();
        DataTable GetRegions();
        void GetListFileArch(string Arch, string PathParse, string region, int region_id);
        string GetArch44(string Arch, string PathParse);
    }
}