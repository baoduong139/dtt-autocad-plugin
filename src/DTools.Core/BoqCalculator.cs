using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTools.Core
{
    public class BoqItem
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Unit { get; set; } = "Cái";
        public double Quantity { get; set; } = 1.0;
        public double UnitPrice { get; set; } = 0.0;
        public string Specification { get; set; } = "";

        public double TotalPrice => Quantity * UnitPrice;
    }

    public class BoqSummary
    {
        public string ProjectName { get; set; } = "DTools Pro Project";
        public List<BoqItem> Items { get; } = new List<BoqItem>();

        public void AddOrAggregate(BoqItem item)
        {
            if (item == null) return;
            var existing = Items.FirstOrDefault(i => i.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase) &&
                                                    i.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Quantity += item.Quantity;
                if (existing.UnitPrice <= 0 && item.UnitPrice > 0) existing.UnitPrice = item.UnitPrice;
            }
            else
            {
                Items.Add(item);
            }
        }

        public double TotalAmount => Items.Sum(i => i.TotalPrice);
        public double TotalQuantity => Items.Sum(i => i.Quantity);

        public string ExportToCsv(string separator = ",")
        {
            var sb = new StringBuilder();
            sb.AppendLine($"STT{separator}Mã Hiệu{separator}Tên Thiết Bị / Cấu Kiện{separator}Quy Cách{separator}Đơn Vị{separator}Số Lượng{separator}Đơn Giá (VNĐ){separator}Thành Tiền (VNĐ)");

            int index = 1;
            foreach (var item in Items)
            {
                string cleanName = item.Name.Replace("\"", "\"\"");
                string cleanSpec = item.Specification.Replace("\"", "\"\"");
                sb.AppendLine($"{index}{separator}\"{item.Code}\"{separator}\"{cleanName}\"{separator}\"{cleanSpec}\"{separator}{item.Unit}{separator}{item.Quantity:F1}{separator}{item.UnitPrice:F0}{separator}{item.TotalPrice:F0}");
                index++;
            }

            sb.AppendLine($"{separator}{separator}TỔNG CỘNG{separator}{separator}{separator}{TotalQuantity:F1}{separator}{separator}{TotalAmount:F0}");
            return sb.ToString();
        }
    }
}
