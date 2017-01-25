using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML;
using ClosedXML.Excel;

namespace CheatEngine_PointerscanExporter.Exporters
{
    public class ExportTo_Excel
    {
        public static void SaveXml(PointerscanresultReader reader, string path)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Addresses");
                    
            for (int i = 1; i < reader.MaxOffsetCount + 2; i++)
            {
                if (i == 1)
                {
                    worksheet.Cell(1, i).Value = "Base Offset";
                    worksheet.Column(i).Width = 30; 
                }
                else
                {
                    worksheet.Cell(1, i).Value = "Offset " + (i - 1);
                    worksheet.Column(i).Width = 10;
                }
                worksheet.Column(i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            }


            int row = 2;
            foreach (var record in reader.TableResults)
            {
                var firstCell = worksheet.Cell(row, 1);
             
                firstCell.Value = reader.Modules[record.modulenr] + "+" + record.moduleoffset.ToString("X");
                firstCell.DataType = XLCellValues.Text;
         

                for (int i = record.offsets.Length - 1; i >= 0; i--)
                {
                    var cell = worksheet.Cell(row, i + 2);
              
                    cell.Value = record.offsets[(record.offsets.Length - 1) - i].ToString("X");
                    cell.DataType = XLCellValues.Text;
                }
                row++;
            }

            workbook.SaveAs(path);
        }
    }
}
