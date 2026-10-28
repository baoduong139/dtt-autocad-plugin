using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DTT_AutoCAD_Plugin.UI
{
    /// <summary>
    /// BỘ SINH MÔ HÌNH HỌC 3D (3D MESH GENERATOR) NATIVE WPF
    /// Tạo các đối tượng 3D MeshGeometry3D siêu nhẹ, mượt mà và trực quan
    /// </summary>
    public static class MeshBuilder3D
    {
        #region 1. Tạo Khối Hộp (Box / Cuboid)

        /// <summary>
        /// Tạo một khối hộp chữ nhật 3D với tâm chỉ định
        /// </summary>
        public static GeometryModel3D CreateBox(Point3D center, double sizeX, double sizeY, double sizeZ, Color color, double opacity = 1.0)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            double hx = sizeX / 2.0;
            double hy = sizeY / 2.0;
            double hz = sizeZ / 2.0;

            // 8 đỉnh của hình hộp
            Point3D[] p = new Point3D[]
            {
                new Point3D(center.X - hx, center.Y - hy, center.Z - hz), // 0
                new Point3D(center.X + hx, center.Y - hy, center.Z - hz), // 1
                new Point3D(center.X + hx, center.Y + hy, center.Z - hz), // 2
                new Point3D(center.X - hx, center.Y + hy, center.Z - hz), // 3
                new Point3D(center.X - hx, center.Y - hy, center.Z + hz), // 4
                new Point3D(center.X + hx, center.Y - hy, center.Z + hz), // 5
                new Point3D(center.X + hx, center.Y + hy, center.Z + hz), // 6
                new Point3D(center.X - hx, center.Y + hy, center.Z + hz)  // 7
            };

            // 6 mặt (mỗi mặt gồm 2 tam giác)
            AddQuad(mesh, p[0], p[1], p[2], p[3]); // Mặt sau (-Z)
            AddQuad(mesh, p[5], p[4], p[7], p[6]); // Mặt trước (+Z)
            AddQuad(mesh, p[4], p[0], p[3], p[7]); // Mặt trái (-X)
            AddQuad(mesh, p[1], p[5], p[6], p[2]); // Mặt phải (+X)
            AddQuad(mesh, p[3], p[2], p[6], p[7]); // Mặt trên (+Y)
            AddQuad(mesh, p[4], p[5], p[1], p[0]); // Mặt dưới (-Y)

            SolidColorBrush brush = new SolidColorBrush(color) { Opacity = opacity };
            MaterialGroup matGroup = new MaterialGroup();
            matGroup.Children.Add(new DiffuseMaterial(brush));
            matGroup.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), 25.0));

            return new GeometryModel3D(mesh, matGroup);
        }

        private static void AddQuad(MeshGeometry3D mesh, Point3D p0, Point3D p1, Point3D p2, Point3D p3)
        {
            int baseIdx = mesh.Positions.Count;
            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);

            // Vector pháp tuyến
            Vector3D normal = Vector3D.CrossProduct(p1 - p0, p2 - p0);
            normal.Normalize();
            for (int i = 0; i < 4; i++) mesh.Normals.Add(normal);

            // 2 tam giác
            mesh.TriangleIndices.Add(baseIdx);
            mesh.TriangleIndices.Add(baseIdx + 1);
            mesh.TriangleIndices.Add(baseIdx + 2);

            mesh.TriangleIndices.Add(baseIdx);
            mesh.TriangleIndices.Add(baseIdx + 2);
            mesh.TriangleIndices.Add(baseIdx + 3);
        }

        #endregion

        #region 2. Tạo Hình Trụ (Cylinder)

        /// <summary>
        /// Tạo một hình trụ đứng theo trục Y
        /// </summary>
        public static GeometryModel3D CreateCylinder(Point3D baseCenter, double radius, double height, Color color, int slices = 20)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            double y0 = baseCenter.Y;
            double y1 = baseCenter.Y + height;

            for (int i = 0; i <= slices; i++)
            {
                double angle = (2.0 * Math.PI * i) / slices;
                double x = radius * Math.Cos(angle) + baseCenter.X;
                double z = radius * Math.Sin(angle) + baseCenter.Z;

                mesh.Positions.Add(new Point3D(x, y0, z)); // Đáy
                mesh.Positions.Add(new Point3D(x, y1, z)); // Đỉnh

                Vector3D normal = new Vector3D(Math.Cos(angle), 0, Math.Sin(angle));
                mesh.Normals.Add(normal);
                mesh.Normals.Add(normal);
            }

            for (int i = 0; i < slices; i++)
            {
                int b0 = i * 2;
                int t0 = b0 + 1;
                int b1 = (i + 1) * 2;
                int t1 = b1 + 1;

                mesh.TriangleIndices.Add(b0);
                mesh.TriangleIndices.Add(t0);
                mesh.TriangleIndices.Add(t1);

                mesh.TriangleIndices.Add(b0);
                mesh.TriangleIndices.Add(t1);
                mesh.TriangleIndices.Add(b1);
            }

            // Nắp trên và nắp dưới
            int topCenterIdx = mesh.Positions.Count;
            mesh.Positions.Add(new Point3D(baseCenter.X, y1, baseCenter.Z));
            mesh.Normals.Add(new Vector3D(0, 1, 0));

            for (int i = 0; i < slices; i++)
            {
                int t0 = i * 2 + 1;
                int t1 = (i + 1) * 2 + 1;
                mesh.TriangleIndices.Add(topCenterIdx);
                mesh.TriangleIndices.Add(t0);
                mesh.TriangleIndices.Add(t1);
            }

            SolidColorBrush brush = new SolidColorBrush(color);
            MaterialGroup matGroup = new MaterialGroup();
            matGroup.Children.Add(new DiffuseMaterial(brush));
            matGroup.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 20.0));

            return new GeometryModel3D(mesh, matGroup);
        }

        #endregion

        #region 3. Tạo Mô Hình 3D Cửa Đi Động (3D Door Model)

        public static Model3DGroup BuildDoor3D(double widthMm = 900, double heightMm = 2200, double openAngleDeg = 45, bool flip = false)
        {
            Model3DGroup group = new Model3DGroup();

            double w = widthMm;
            double h = heightMm;
            double wallThick = 110;
            double frameW = 50;
            double doorThick = 40;

            // Màu sắc thiết kế kiến trúc cao cấp
            Color frameColor = Color.FromRgb(71, 85, 105);   // Slate 600
            Color leafColor = Color.FromRgb(217, 119, 6);    // Gỗ sồi vàng ấm
            Color glassColor = Color.FromRgb(186, 230, 253);  // Kính xanh nhạt
            Color handleColor = Color.FromRgb(234, 179, 8);   // Đồng vàng

            // 1. Khuôn cửa trái (Left Jamb)
            group.Children.Add(CreateBox(new Point3D(-w / 2 - frameW / 2, h / 2, 0), frameW, h, wallThick, frameColor));

            // 2. Khuôn cửa phải (Right Jamb)
            group.Children.Add(CreateBox(new Point3D(w / 2 + frameW / 2, h / 2, 0), frameW, h, wallThick, frameColor));

            // 3. Khung ngang trên (Header)
            group.Children.Add(CreateBox(new Point3D(0, h + frameW / 2, 0), w + frameW * 2, frameW, wallThick, frameColor));

            // 4. Ngưỡng cửa / ray dưới
            group.Children.Add(CreateBox(new Point3D(0, -10, 0), w + frameW * 2, 20, wallThick + 20, Color.FromRgb(148, 163, 184)));

            // 5. Cánh cửa (Door Leaf) quay theo góc mở openAngleDeg
            double rad = openAngleDeg * Math.PI / 180.0;
            double hingeX = -w / 2;
            if (flip) hingeX = w / 2;

            double leafCenterX = hingeX + (flip ? -w / 2 * Math.Cos(rad) : w / 2 * Math.Cos(rad));
            double leafCenterZ = flip ? -w / 2 * Math.Sin(rad) : w / 2 * Math.Sin(rad);

            GeometryModel3D leaf = CreateBox(new Point3D(0, (h - 20) / 2, 0), w - 10, h - 20, doorThick, leafColor);

            // Áp dụng phép xoay quanh bản lề
            Transform3DGroup xform = new Transform3DGroup();
            xform.Children.Add(new TranslateTransform3D(-w / 2, 0, 0));
            xform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), flip ? -openAngleDeg : openAngleDeg)));
            xform.Children.Add(new TranslateTransform3D(hingeX, 0, 0));
            leaf.Transform = xform;
            group.Children.Add(leaf);

            // 6. Ô kính trang trí giữa cánh cửa
            GeometryModel3D glassPanel = CreateBox(new Point3D(0, h * 0.6, 0), w * 0.45, h * 0.4, doorThick + 4, glassColor, 0.75);
            glassPanel.Transform = xform;
            group.Children.Add(glassPanel);

            // 7. Tay nắm cửa (Handle)
            GeometryModel3D handle = CreateBox(new Point3D(w * 0.35, 1000, doorThick / 2 + 25), 35, 140, 50, handleColor);
            handle.Transform = xform;
            group.Children.Add(handle);

            return group;
        }

        #endregion

        #region 4. Tạo Mô Hình 3D Cột Kiến Trúc (3D Column Model)

        public static Model3DGroup BuildColumn3D(double widthMm = 300, double heightMm = 3000, bool isRound = false)
        {
            Model3DGroup group = new Model3DGroup();

            Color concreteColor = Color.FromRgb(148, 163, 184); // Bê tông ghi sáng
            Color baseColor = Color.FromRgb(100, 116, 139);     // Đế cột ghi đậm
            Color accentColor = Color.FromRgb(2, 132, 199);     // Chỉ phào Cyan

            double w = widthMm;
            double h = heightMm;

            // 1. Chân đế móng cột (Footing Base)
            group.Children.Add(CreateBox(new Point3D(0, 75, 0), w * 1.5, 150, w * 1.5, baseColor));
            group.Children.Add(CreateBox(new Point3D(0, 180, 0), w * 1.25, 60, w * 1.25, accentColor));

            // 2. Thân cột chính (Shaft)
            if (isRound)
            {
                group.Children.Add(CreateCylinder(new Point3D(0, 210, 0), w / 2, h - 420, concreteColor, 28));
            }
            else
            {
                group.Children.Add(CreateBox(new Point3D(0, h / 2, 0), w, h - 420, w, concreteColor));

                // Gờ chỉ vát góc trang trí 4 cạnh
                group.Children.Add(CreateBox(new Point3D(0, h / 2, 0), w * 1.04, h * 0.7, w * 0.9, Color.FromRgb(203, 213, 225)));
            }

            // 3. Đầu mũ cột (Capital)
            group.Children.Add(CreateBox(new Point3D(0, h - 180, 0), w * 1.25, 60, w * 1.25, accentColor));
            group.Children.Add(CreateBox(new Point3D(0, h - 75, 0), w * 1.5, 150, w * 1.5, baseColor));

            return group;
        }

        #endregion

        #region 5. Tạo Mô Hình 3D Ký Hiệu Cao Độ (3D Elevation Marker)

        public static Model3DGroup BuildElevation3D(double poleHeightMm = 1200, string label = "+0.000")
        {
            Model3DGroup group = new Model3DGroup();

            Color poleColor = Color.FromRgb(2, 132, 199);    // Xanh Cyan
            Color flagColor = Color.FromRgb(239, 68, 68);    // Đỏ mốc
            Color datumColor = Color.FromRgb(16, 185, 129);  // Xanh lục mặt sàn

            // 1. Mặt đĩa mốc cốt 0.000 dưới đáy
            group.Children.Add(CreateCylinder(new Point3D(0, 0, 0), 200, 25, datumColor, 24));
            group.Children.Add(CreateBox(new Point3D(0, 12, 0), 500, 5, 20, datumColor));
            group.Children.Add(CreateBox(new Point3D(0, 12, 0), 20, 5, 500, datumColor));

            // 2. Trụ mốc đứng (Vertical Datum Pole)
            group.Children.Add(CreateCylinder(new Point3D(0, 25, 0), 22, poleHeightMm, poleColor, 16));

            // 3. Mũi tên tam giác chỉ cao độ trên đỉnh
            double topY = poleHeightMm + 25;
            group.Children.Add(CreateBox(new Point3D(75, topY, 0), 150, 20, 20, flagColor));
            group.Children.Add(CreateBox(new Point3D(150, topY + 40, 0), 200, 60, 15, Color.FromRgb(30, 41, 59)));

            // 4. Cánh mũi tên định hướng
            group.Children.Add(CreateBox(new Point3D(40, topY + 30, 0), 60, 60, 10, flagColor));

            return group;
        }

        #endregion

        #region 6. Tạo Mô Hình 3D Tủ Điện Vạn Năng (3D Electrical Cabinet)

        public static Model3DGroup BuildCabinet3D(double widthMm = 800, double heightMm = 1800, double depthMm = 400)
        {
            Model3DGroup group = new Model3DGroup();

            Color bodyColor = Color.FromRgb(226, 232, 240); // RAL 7035 Sơn tĩnh điện ghi sáng
            Color doorFrameColor = Color.FromRgb(71, 85, 105);
            Color basePlinthColor = Color.FromRgb(30, 41, 59); // Chân đế đen
            Color windowColor = Color.FromRgb(186, 230, 253);  // Kính quan sát
            Color lockColor = Color.FromRgb(245, 158, 11);     // Khóa mạ crom vàng
            Color lampRed = Color.FromRgb(239, 68, 68);        // Đèn R
            Color lampYellow = Color.FromRgb(234, 179, 8);     // Đèn S
            Color lampGreen = Color.FromRgb(34, 197, 94);      // Đèn T

            double w = widthMm;
            double h = heightMm;
            double d = depthMm;

            // 1. Chân đế tủ (Plinth)
            group.Children.Add(CreateBox(new Point3D(0, 50, 0), w + 20, 100, d + 20, basePlinthColor));

            // 2. Thùng vỏ tủ chính (Main Enclosure)
            group.Children.Add(CreateBox(new Point3D(0, h / 2 + 100, 0), w, h, d, bodyColor));

            // 3. Mái che dốc chống nước trên nóc (Canopy Roof)
            group.Children.Add(CreateBox(new Point3D(0, h + 115, 10), w + 40, 30, d + 40, doorFrameColor));

            // 4. Cửa tủ phía trước (Front Door)
            double doorZ = d / 2 + 10;
            group.Children.Add(CreateBox(new Point3D(0, h / 2 + 100, doorZ), w - 30, h - 50, 20, Color.FromRgb(241, 245, 249)));

            // 5. Ô kính quan sát thiết bị (Inspection Window)
            group.Children.Add(CreateBox(new Point3D(0, h * 0.72 + 100, doorZ + 5), w * 0.65, h * 0.35, 10, windowColor, 0.75));

            // 6. Cụm 3 đèn báo pha (Phase Indicators R - S - T)
            double lampY = h * 0.94 + 100;
            group.Children.Add(CreateCylinder(new Point3D(-w * 0.25, lampY, doorZ + 12), 15, 20, lampRed, 12));
            group.Children.Add(CreateCylinder(new Point3D(0, lampY, doorZ + 12), 15, 20, lampYellow, 12));
            group.Children.Add(CreateCylinder(new Point3D(w * 0.25, lampY, doorZ + 12), 15, 20, lampGreen, 12));

            // 7. Khóa tủ tay quay Master Lock
            group.Children.Add(CreateBox(new Point3D(w / 2 - 60, h * 0.5 + 100, doorZ + 15), 25, 120, 25, lockColor));

            return group;
        }

        #endregion

        #region 7. Tạo Mô Hình 3D Đường Ống MEP (3D MEP Pipe)

        public static Model3DGroup BuildPipe3D(double diameterMm = 110, double lengthMm = 1200, Color? pipeColor = null)
        {
            Model3DGroup group = new Model3DGroup();
            Color col = pipeColor ?? Color.FromRgb(14, 165, 233); // Xanh cấp nước
            Color flangeCol = Color.FromRgb(71, 85, 105);

            double r = diameterMm / 2.0;

            // Ống chính nằm ngang
            GeometryModel3D mainPipe = CreateCylinder(new Point3D(0, 0, 0), r, lengthMm, col, 24);
            Transform3DGroup rot = new Transform3DGroup();
            rot.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
            rot.Children.Add(new TranslateTransform3D(-lengthMm / 2, 0, 0));
            mainPipe.Transform = rot;
            group.Children.Add(mainPipe);

            // 2 Mặt bích (Flanges) ở 2 đầu
            GeometryModel3D flange1 = CreateCylinder(new Point3D(0, 0, 0), r * 1.5, 40, flangeCol, 24);
            flange1.Transform = rot;
            group.Children.Add(flange1);

            GeometryModel3D flange2 = CreateCylinder(new Point3D(0, lengthMm - 40, 0), r * 1.5, 40, flangeCol, 24);
            flange2.Transform = rot;
            group.Children.Add(flange2);

            // Van khóa ở giữa (Valve Body & Handwheel)
            group.Children.Add(CreateBox(new Point3D(0, 0, 0), r * 2.2, r * 2.2, r * 2.2, Color.FromRgb(220, 38, 38)));
            group.Children.Add(CreateCylinder(new Point3D(0, r, 0), r * 0.3, r * 1.6, flangeCol, 12));
            group.Children.Add(CreateCylinder(new Point3D(0, r * 2.6, 0), r * 1.2, 20, Color.FromRgb(239, 68, 68), 20));

            return group;
        }

        #endregion

        #region 8. Tạo Mô Hình 3D Tường Gạch (3D Brick Wall)

        public static Model3DGroup BuildWall3D(double lengthMm = 1500, double thickMm = 220, double heightMm = 1200)
        {
            Model3DGroup group = new Model3DGroup();

            Color brickColor = Color.FromRgb(180, 83, 9);   // Màu gạch nung đỏ gạch
            Color mortarColor = Color.FromRgb(203, 213, 225); // Màu vữa xi măng
            Color copingColor = Color.FromRgb(100, 116, 139); // Giằng tường bê tông

            // Thân tường gạch
            group.Children.Add(CreateBox(new Point3D(0, heightMm / 2, 0), lengthMm, heightMm, thickMm, brickColor));

            // Giằng móng đáy
            group.Children.Add(CreateBox(new Point3D(0, 40, 0), lengthMm + 40, 80, thickMm + 40, copingColor));

            // Giằng mũ đỉnh tường
            group.Children.Add(CreateBox(new Point3D(0, heightMm + 40, 0), lengthMm + 40, 80, thickMm + 40, copingColor));

            // Các đường roong chỉ vữa trang trí
            group.Children.Add(CreateBox(new Point3D(0, heightMm * 0.33, thickMm / 2 + 2), lengthMm, 8, 4, mortarColor));
            group.Children.Add(CreateBox(new Point3D(0, heightMm * 0.66, thickMm / 2 + 2), lengthMm, 8, 4, mortarColor));

            return group;
        }

        #endregion
    }
}
