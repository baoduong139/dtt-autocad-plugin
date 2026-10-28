using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DTT_AutoCAD_Plugin.Common;

namespace DTT_AutoCAD_Plugin.Modules
{
    // =========================================================================
    // DATA TRANSFER OBJECTS (DTOs) CHO AI VISION & CAD VECTORIZER
    // =========================================================================

    public class AiCadPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class AiCadWall
    {
        public string Id { get; set; }
        public AiCadPoint Start { get; set; } = new AiCadPoint();
        public AiCadPoint End { get; set; } = new AiCadPoint();
        public double Thickness { get; set; } = 220.0;
    }

    public class AiCadDoor
    {
        public AiCadPoint InsertPoint { get; set; } = new AiCadPoint();
        public double Width { get; set; } = 900.0;
        public double AngleDegrees { get; set; } = 0.0;
        public string SwingDirection { get; set; } = "inward_right";
        public string DoorType { get; set; } = "single_swing";
    }

    public class AiCadWindow
    {
        public AiCadPoint Start { get; set; } = new AiCadPoint();
        public AiCadPoint End { get; set; } = new AiCadPoint();
        public double Width { get; set; } = 1500.0;
    }

    public class AiCadText
    {
        public string Content { get; set; } = "";
        public AiCadPoint Position { get; set; } = new AiCadPoint();
        public string Category { get; set; } = "dimension";
        public double Height { get; set; } = 200.0;
    }

    public class AiCadReconstructionData
    {
        public double ScaleFactor { get; set; } = 10.0; // mm per pixel or normalized unit
        public List<AiCadWall> Walls { get; set; } = new List<AiCadWall>();
        public List<AiCadDoor> Doors { get; set; } = new List<AiCadDoor>();
        public List<AiCadWindow> Windows { get; set; } = new List<AiCadWindow>();
        public List<AiCadText> Texts { get; set; } = new List<AiCadText>();
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
    }

    // =========================================================================
    // CORE ENGINE: RASTER TO CAD & AI VISION RECONSTRUCTION
    // =========================================================================

    public static class AiRasterToCadEngine
    {
        /// <summary>
        /// Prompt chuẩn hóa để gửi vào Ollama Vision (llama3.2-vision, qwen2.5-vl)
        /// </summary>
        public const string OllamaVisionSystemPrompt = 
@"You are an expert Architectural CAD Drafter and Computer Vision AI.
Analyze the provided floor plan or schematic drawing and reconstruct all elements into precise 2D CAD vectors.
All coordinates MUST be normalized in range [0, 1000] where:
- X=0, Y=0 is the BOTTOM-LEFT corner of the drawing (standard Cartesian).
- X=1000, Y=1000 is the TOP-RIGHT corner.

Output ONLY valid JSON matching this exact structure:
{
  ""metadata"": { ""estimated_scale_ratio"": 15.0, ""drawing_type"": ""floor_plan"" },
  ""walls"": [
    { ""id"": ""W1"", ""start"": { ""x"": 100, ""y"": 100 }, ""end"": { ""x"": 900, ""y"": 100 }, ""thickness"": 220 }
  ],
  ""doors"": [
    { ""insert_point"": { ""x"": 250, ""y"": 100 }, ""width"": 900, ""angle_degrees"": 0, ""door_type"": ""single_swing"" }
  ],
  ""windows"": [
    { ""start"": { ""x"": 650, ""y"": 100 }, ""end"": { ""x"": 800, ""y"": 100 }, ""width"": 1500 }
  ],
  ""texts"": [
    { ""content"": ""PHÒNG KHÁCH"", ""position"": { ""x"": 350, ""y"": 400 }, ""category"": ""room_name"" }
  ]
}";

        /// <summary>
        /// Thuật toán Vector hóa ảnh bản vẽ thuần C# GDI+ (Fast Native Image Edge/Runlength Vectorizer)
        /// Không cần phụ thuộc DLL ngoài, chạy mượt mà ngay trên mọi máy tính.
        /// </summary>
        public static AiCadReconstructionData VectorizeImageBitmap(string imagePath, double scaleMmPerPx = 10.0, int threshold = 160, int minLengthPx = 25, bool orthoSnap = true)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("Không tìm thấy file ảnh: " + imagePath);

            var data = new AiCadReconstructionData { ScaleFactor = scaleMmPerPx };

            using (Bitmap origBmp = new Bitmap(imagePath))
            {
                data.ImageWidth = origBmp.Width;
                data.ImageHeight = origBmp.Height;

                int w = origBmp.Width;
                int h = origBmp.Height;

                // 1. Nhị phân hóa ảnh bằng byte array tốc độ cao
                bool[,] binary = new bool[w, h]; // true = nét vẽ đen, false = nền trắng

                BitmapData bData = origBmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    int stride = bData.Stride;
                    int bytes = Math.Abs(stride) * h;
                    byte[] rgbValues = new byte[bytes];
                    System.Runtime.InteropServices.Marshal.Copy(bData.Scan0, rgbValues, 0, bytes);

                    for (int y = 0; y < h; y++)
                    {
                        int rowOffset = y * stride;
                        for (int x = 0; x < w; x++)
                        {
                            int idx = rowOffset + x * 3;
                            int b = rgbValues[idx];
                            int g = rgbValues[idx + 1];
                            int r = rgbValues[idx + 2];
                            int gray = (r * 30 + g * 59 + b * 11) / 100;

                            binary[x, y] = (gray < threshold);
                        }
                    }
                }
                finally
                {
                    origBmp.UnlockBits(bData);
                }

                // 2. Quét các đoạn thẳng nằm ngang (Horizontal Runs)
                List<AiCadWall> horizLines = new List<AiCadWall>();
                for (int y = 0; y < h; y += 2) // Bước nhảy 2px tăng tốc
                {
                    int startX = -1;
                    for (int x = 0; x < w; x++)
                    {
                        if (binary[x, y])
                        {
                            if (startX < 0) startX = x;
                        }
                        else
                        {
                            if (startX >= 0)
                            {
                                int len = x - startX;
                                if (len >= minLengthPx)
                                {
                                    horizLines.Add(new AiCadWall
                                    {
                                        Id = $"H_{horizLines.Count + 1}",
                                        Start = new AiCadPoint { X = startX * scaleMmPerPx, Y = (h - y) * scaleMmPerPx },
                                        End = new AiCadPoint { X = x * scaleMmPerPx, Y = (h - y) * scaleMmPerPx },
                                        Thickness = 220.0
                                    });
                                }
                                startX = -1;
                            }
                        }
                    }
                    if (startX >= 0 && (w - startX) >= minLengthPx)
                    {
                        horizLines.Add(new AiCadWall
                        {
                            Id = $"H_{horizLines.Count + 1}",
                            Start = new AiCadPoint { X = startX * scaleMmPerPx, Y = (h - y) * scaleMmPerPx },
                            End = new AiCadPoint { X = w * scaleMmPerPx, Y = (h - y) * scaleMmPerPx },
                            Thickness = 220.0
                        });
                    }
                }

                // 3. Quét các đoạn thẳng thẳng đứng (Vertical Runs)
                List<AiCadWall> vertLines = new List<AiCadWall>();
                for (int x = 0; x < w; x += 2)
                {
                    int startY = -1;
                    for (int y = 0; y < h; y++)
                    {
                        if (binary[x, y])
                        {
                            if (startY < 0) startY = y;
                        }
                        else
                        {
                            if (startY >= 0)
                            {
                                int len = y - startY;
                                if (len >= minLengthPx)
                                {
                                    vertLines.Add(new AiCadWall
                                    {
                                        Id = $"V_{vertLines.Count + 1}",
                                        Start = new AiCadPoint { X = x * scaleMmPerPx, Y = (h - startY) * scaleMmPerPx },
                                        End = new AiCadPoint { X = x * scaleMmPerPx, Y = (h - y) * scaleMmPerPx },
                                        Thickness = 220.0
                                    });
                                }
                                startY = -1;
                            }
                        }
                    }
                    if (startY >= 0 && (h - startY) >= minLengthPx)
                    {
                        vertLines.Add(new AiCadWall
                        {
                            Id = $"V_{vertLines.Count + 1}",
                            Start = new AiCadPoint { X = x * scaleMmPerPx, Y = (h - startY) * scaleMmPerPx },
                            End = new AiCadPoint { X = x * scaleMmPerPx, Y = 0 },
                            Thickness = 220.0
                        });
                    }
                }

                // 4. Lọc gộp các đoạn thẳng gần nhau (Collinear Filter)
                FilterAndMergeLines(horizLines, isHorizontal: true, maxGap: minLengthPx * scaleMmPerPx);
                FilterAndMergeLines(vertLines, isHorizontal: false, maxGap: minLengthPx * scaleMmPerPx);

                data.Walls.AddRange(horizLines);
                data.Walls.AddRange(vertLines);
            }

            return data;
        }

        private static void FilterAndMergeLines(List<AiCadWall> list, bool isHorizontal, double maxGap)
        {
            if (list == null || list.Count < 2) return;

            for (int i = list.Count - 1; i >= 1; i--)
            {
                var cur = list[i];
                var prev = list[i - 1];

                if (isHorizontal)
                {
                    if (Math.Abs(cur.Start.Y - prev.Start.Y) <= 15.0 && Math.Abs(cur.Start.X - prev.End.X) <= maxGap)
                    {
                        prev.End.X = Math.Max(prev.End.X, cur.End.X);
                        list.RemoveAt(i);
                    }
                }
                else
                {
                    if (Math.Abs(cur.Start.X - prev.Start.X) <= 15.0 && Math.Abs(cur.Start.Y - prev.End.Y) <= maxGap)
                    {
                        prev.End.Y = Math.Min(prev.End.Y, cur.End.Y);
                        list.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Phân tích JSON từ Ollama Vision (llama3.2-vision) và chuyển đổi thành DTO
        /// </summary>
        public static AiCadReconstructionData ParseOllamaVisionJson(string jsonContent, double customScale = 0.0)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
                throw new ArgumentException("Nội dung JSON từ AI Vision trống!");

            string cleanJson = jsonContent;
            int startIdx = cleanJson.IndexOf("{");
            int endIdx = cleanJson.LastIndexOf("}");
            if (startIdx >= 0 && endIdx > startIdx)
            {
                cleanJson = cleanJson.Substring(startIdx, endIdx - startIdx + 1);
            }

            var result = new AiCadReconstructionData();
            double scale = customScale > 0 ? customScale : 15.0;

            try
            {
                var mScale = Regex.Match(cleanJson, @"""estimated_scale_ratio""\s*:\s*([0-9\.]+)");
                if (mScale.Success && double.TryParse(mScale.Groups[1].Value, out double parsedScale) && customScale <= 0)
                {
                    scale = parsedScale;
                }
                result.ScaleFactor = scale;

                // 1. Bóc tách Walls
                var wallMatches = Regex.Matches(cleanJson, @"\{\s*""id""\s*:\s*""([^""]*)""\s*,\s*""start""\s*:\s*\{\s*""x""\s*:\s*([0-9\.\-]+)\s*,\s*""y""\s*:\s*([0-9\.\-]+)\s*\}\s*,\s*""end""\s*:\s*\{\s*""x""\s*:\s*([0-9\.\-]+)\s*,\s*""y""\s*:\s*([0-9\.\-]+)\s*\}\s*(?:,\s*""thickness""\s*:\s*([0-9\.\-]+))?\s*\}");
                foreach (Match m in wallMatches)
                {
                    string id = m.Groups[1].Value;
                    double x1 = double.Parse(m.Groups[2].Value) * scale;
                    double y1 = double.Parse(m.Groups[3].Value) * scale;
                    double x2 = double.Parse(m.Groups[4].Value) * scale;
                    double y2 = double.Parse(m.Groups[5].Value) * scale;
                    double thick = 220.0;
                    if (m.Groups[6].Success && double.TryParse(m.Groups[6].Value, out double t)) thick = t;

                    result.Walls.Add(new AiCadWall
                    {
                        Id = id,
                        Start = new AiCadPoint { X = x1, Y = y1 },
                        End = new AiCadPoint { X = x2, Y = y2 },
                        Thickness = thick
                    });
                }

                // 2. Bóc tách Doors
                var doorMatches = Regex.Matches(cleanJson, @"\{\s*""insert_point""\s*:\s*\{\s*""x""\s*:\s*([0-9\.\-]+)\s*,\s*""y""\s*:\s*([0-9\.\-]+)\s*\}\s*,\s*""width""\s*:\s*([0-9\.\-]+)(?:.*?\""angle_degrees\""\s*:\s*([0-9\.\-]+))?(?:.*?\""door_type\""\s*:\s*""([^""]*)"")?");
                foreach (Match m in doorMatches)
                {
                    double x = double.Parse(m.Groups[1].Value) * scale;
                    double y = double.Parse(m.Groups[2].Value) * scale;
                    double w = double.Parse(m.Groups[3].Value);
                    double ang = 0.0;
                    if (m.Groups[4].Success && double.TryParse(m.Groups[4].Value, out double a)) ang = a;
                    string dtype = m.Groups[5].Success ? m.Groups[5].Value : "single_swing";

                    result.Doors.Add(new AiCadDoor
                    {
                        InsertPoint = new AiCadPoint { X = x, Y = y },
                        Width = w > 0 ? w : 900.0,
                        AngleDegrees = ang,
                        DoorType = dtype
                    });
                }

                // 3. Bóc tách Windows
                var winMatches = Regex.Matches(cleanJson, @"\{\s*""start""\s*:\s*\{\s*""x""\s*:\s*([0-9\.\-]+)\s*,\s*""y""\s*:\s*([0-9\.\-]+)\s*\}\s*,\s*""end""\s*:\s*\{\s*""x""\s*:\s*([0-9\.\-]+)\s*,\s*""y""\s*:\s*([0-9\.\-]+)\s*\}\s*,\s*""width""\s*:\s*([0-9\.\-]+)\s*\}");
                foreach (Match m in winMatches)
                {
                    double x1 = double.Parse(m.Groups[1].Value) * scale;
                    double y1 = double.Parse(m.Groups[2].Value) * scale;
                    double x2 = double.Parse(m.Groups[4].Value) * scale;
                    double y2 = double.Parse(m.Groups[5].Value) * scale;
                    double w = double.Parse(m.Groups[6].Value);

                    result.Windows.Add(new AiCadWindow
                    {
                        Start = new AiCadPoint { X = x1, Y = y1 },
                        End = new AiCadPoint { X = x2, Y = y2 },
                        Width = w > 0 ? w : 1500.0
                    });
                }

                // 4. Bóc tách Texts
                var textMatches = Regex.Matches(cleanJson, @"\{\s*""content""\s*:\s*""([^""]*)""\s*,\s*""position""\s*:\s*\{\s*""x""\s*:\s*([0-9\.\-]+)\s*,\s*""y""\s*:\s*([0-9\.\-]+)\s*\}\s*(?:,\s*""category""\s*:\s*""([^""]*)"")?\s*\}");
                foreach (Match m in textMatches)
                {
                    string content = m.Groups[1].Value;
                    double x = double.Parse(m.Groups[2].Value) * scale;
                    double y = double.Parse(m.Groups[3].Value) * scale;
                    string cat = m.Groups[4].Success ? m.Groups[4].Value : "room_name";

                    result.Texts.Add(new AiCadText
                    {
                        Content = content,
                        Position = new AiCadPoint { X = x, Y = y },
                        Category = cat,
                        Height = (cat == "room_name") ? 250.0 : 180.0
                    });
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi phân tích cú pháp JSON AI Vision: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Lấy danh sách mô hình đã được tải về trong Ollama cục bộ
        /// </summary>
        public static List<string> GetInstalledOllamaModels(string endpoint = "http://localhost:11434")
        {
            List<string> models = new List<string>();
            try
            {
                string tagsUrl = endpoint.TrimEnd('/') + "/api/tags";
                using (WebClient client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    string resp = client.DownloadString(tagsUrl);
                    var matches = Regex.Matches(resp, @"""name""\s*:\s*""([^""]+)""");
                    foreach (Match m in matches)
                    {
                        string name = m.Groups[1].Value;
                        if (!models.Contains(name)) models.Add(name);
                    }
                }
            }
            catch { }
            return models;
        }

        /// <summary>
        /// Gọi trực tiếp API Ollama Vision (llama3.2-vision) trên máy cục bộ với chẩn đoán lỗi chi tiết
        /// </summary>
        public static string CallLocalOllamaVision(string imagePath, string endpoint = "http://localhost:11434/api/generate", string model = "llama3.2-vision")
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("Không tìm thấy ảnh: " + imagePath);

            byte[] imgBytes = File.ReadAllBytes(imagePath);
            string base64Img = Convert.ToBase64String(imgBytes);

            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                client.Headers[HttpRequestHeader.ContentType] = "application/json";

                string payload = $@"{{
  ""model"": ""{model}"",
  ""prompt"": ""Analyze this floor plan and extract CAD vectors JSON."",
  ""system"": ""{OllamaVisionSystemPrompt.Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n")}"",
  ""images"": [""{base64Img}""],
  ""stream"": false
}}";

                try
                {
                    string respText = client.UploadString(endpoint, "POST", payload);

                    var matchResp = Regex.Match(respText, @"""response""\s*:\s*""(.*?)""\s*,\s*""done""", RegexOptions.Singleline);
                    if (matchResp.Success)
                    {
                        string raw = matchResp.Groups[1].Value;
                        return Regex.Unescape(raw);
                    }

                    return respText;
                }
                catch (WebException wex)
                {
                    string errBody = "";
                    try
                    {
                        if (wex.Response != null)
                        {
                            using (var r = new StreamReader(wex.Response.GetResponseStream()))
                            {
                                errBody = r.ReadToEnd();
                            }
                        }
                    }
                    catch { }

                    if (errBody.Contains("mllama") || errBody.Contains("unknown model architecture"))
                    {
                        throw new InvalidOperationException(
                            $"Mô hình '{model}' (7.8 GB) sử dụng kiến trúc 'mllama' đòi hỏi Ollama hỗ trợ GPU và VRAM lớn, hiện không tải được trên CPU của máy bạn.\n\n" +
                            $"💡 GIẢI PHÁP:\n" +
                            $"• Dùng ngay (Khuyên dùng): Chọn '⚡ Computer Vision Vectorizer' ở trên để quét nét bản vẽ trực tiếp từ ảnh siêu nhanh mà không cần Ollama!\n" +
                            $"• Hoặc tải mô hình AI nhẹ chạy tốt trên CPU: Mở CMD gõ:\n" +
                            $"    ollama pull moondream\n" +
                            $"  (Mô hình chỉ 1.6 GB, hỗ trợ nhận diện hình ảnh rất tốt trên máy CPU)");
                    }
                    else if (errBody.Contains("not found") || wex.Message.Contains("404"))
                    {
                        throw new InvalidOperationException(
                            $"Mô hình '{model}' chưa được tìm thấy trong Ollama trên máy tính của bạn.\n\n" +
                            $"💡 HƯỚNG DẪN XỬ LÝ NHANH:\n" +
                            $"• Cách 1 (Dùng Ngay): Chọn chế độ '⚡ Computer Vision Vectorizer' ở trên để quét nét ảnh trực tiếp ngay lập tức không cần cài thêm gì!\n" +
                            $"• Cách 2 (Tải mô hình Vision nhẹ): Mở CMD gõ:  ollama pull moondream");
                    }
                    else if (wex.Status == WebExceptionStatus.ConnectFailure)
                    {
                        throw new InvalidOperationException(
                            $"Không thể kết nối đến Ollama tại địa chỉ: {endpoint}\n\n" +
                            $"💡 Hãy chắc chắn bạn đã bật ứng dụng Ollama trên máy tính, hoặc chuyển sang chế độ '⚡ Computer Vision Vectorizer' để quét nét ảnh trực tiếp.");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Lỗi từ Ollama: {wex.Message}\n{errBody}");
                    }
                }
            }
        }

        /// <summary>
        /// Dựng toàn bộ dữ liệu Vectorization vào bản vẽ AutoCAD DWG hiện hành
        /// </summary>
        public static bool InjectIntoActiveDwg(Database db, Point3d insertBasePoint, AiCadReconstructionData data)
        {
            if (db == null || data == null) return false;
            Editor ed = Application.DocumentManager.MdiActiveDocument?.Editor;

            try
            {
                ObjectId wallLayer = CadUtils.GetOrCreateLayer(db, "A-WALL", 7, LineWeight.LineWeight050);
                ObjectId doorLayer = CadUtils.GetOrCreateLayer(db, "A-DOOR", 2, LineWeight.LineWeight025);
                ObjectId winLayer = CadUtils.GetOrCreateLayer(db, "A-GLAZ", 4, LineWeight.LineWeight025);
                ObjectId textLayer = CadUtils.GetOrCreateLayer(db, "A-TEXT", 3, LineWeight.LineWeight018);

                ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    double bx = insertBasePoint.X;
                    double by = insertBasePoint.Y;

                    // 1. Vẽ Tường (Walls)
                    foreach (var w in data.Walls)
                    {
                        Line line = new Line(
                            new Point3d(bx + w.Start.X, by + w.Start.Y, 0),
                            new Point3d(bx + w.End.X, by + w.End.Y, 0)
                        )
                        {
                            LayerId = wallLayer
                        };
                        btr.AppendEntity(line);
                        tr.AddNewlyCreatedDBObject(line, true);
                    }

                    // 2. Vẽ Cửa Đi (Doors)
                    foreach (var d in data.Doors)
                    {
                        double dx = bx + d.InsertPoint.X;
                        double dy = by + d.InsertPoint.Y;
                        double dw = d.Width > 0 ? d.Width : 900.0;

                        Line leaf = new Line(new Point3d(dx, dy, 0), new Point3d(dx, dy + dw, 0)) { LayerId = doorLayer };
                        Arc swing = new Arc(new Point3d(dx, dy, 0), dw, 0, Math.PI / 2.0) { LayerId = doorLayer };
                        btr.AppendEntity(leaf);
                        tr.AddNewlyCreatedDBObject(leaf, true);
                        btr.AppendEntity(swing);
                        tr.AddNewlyCreatedDBObject(swing, true);
                    }

                    // 3. Vẽ Cửa Sổ (Windows)
                    foreach (var win in data.Windows)
                    {
                        double wx1 = bx + win.Start.X;
                        double wy1 = by + win.Start.Y;
                        double wx2 = bx + win.End.X;
                        double wy2 = by + win.End.Y;

                        Line wLine = new Line(new Point3d(wx1, wy1, 0), new Point3d(wx2, wy2, 0)) { LayerId = winLayer };
                        btr.AppendEntity(wLine);
                        tr.AddNewlyCreatedDBObject(wLine, true);
                    }

                    // 4. Vẽ Văn Bản & Kích Thước (Texts & Dimensions)
                    foreach (var txt in data.Texts)
                    {
                        MText mt = new MText
                        {
                            Contents = txt.Content,
                            Location = new Point3d(bx + txt.Position.X, by + txt.Position.Y, 0),
                            TextHeight = Math.Max(120.0, txt.Height),
                            Attachment = AttachmentPoint.MiddleCenter,
                            LayerId = textLayer,
                            TextStyleId = textStyle
                        };
                        btr.AppendEntity(mt);
                        tr.AddNewlyCreatedDBObject(mt, true);
                    }

                    tr.Commit();
                }

                ed?.WriteMessage($"\n✅ [DTOOLS AI VISION] Đã dựng thành công {data.Walls.Count} nét tường, {data.Doors.Count} cửa, {data.Windows.Count} cửa sổ, {data.Texts.Count} chữ vào CAD!\n");
                return true;
            }
            catch (Exception ex)
            {
                ed?.WriteMessage($"\n❌ [DTOOLS LỖI] Dựng bản vẽ thất bại: {ex.Message}\n");
                return false;
            }
        }
    }
}
