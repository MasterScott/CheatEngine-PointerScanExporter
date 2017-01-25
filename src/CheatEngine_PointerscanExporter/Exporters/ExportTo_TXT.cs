using System.Text;

namespace CheatEngine_PointerscanExporter.Exporters
{
    public class ExportTo_TXT
    {
        public static string Convert(PointerscanresultReader reader)
        {
            StringBuilder SB = new StringBuilder();

            foreach (var record in reader.TableResults)
            {
                SB.Append(reader.Modules[record.modulenr] + "+" + record.moduleoffset.ToString("X"));
                SB.Append("\t");


                for (int i = record.offsets.Length - 1; i >= 0; i--)
                {
                    SB.Append(record.offsets[i].ToString("X"));
                    SB.Append("\t");
                }
                SB.Append("\r\n");
            }

            return SB.ToString();
        }
    }
}
