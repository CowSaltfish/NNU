using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using OSGeo.OGR;
using OSGeo.OSR;
using OSGeo.GDAL;

namespace FornixModelingGDAL
{
    class FornixModeling
    {
        private static DataSource _ds;
        private static List<Fornix> _fornixs = new List<Fornix>();//穹窿地层集合
        private static int _verNum = 1;
        private static List<Layer> layers = new List<Layer>();
        private static VertexCollection _RasterPoints;

        /// <summary>
        /// 生成穹窿模型
        /// </summary>
        public static void GenModel()
        {
            Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "NO");
            Gdal.SetConfigOption("SHAPE_ENCODING", "");
            Ogr.RegisterAll();// 注册所有的驱动
            //由带值点要素生成穹窿模型
            createFornixModelByPoint();
            MessageBox.Show("穹窿生成成功！");
        }

        /// <summary>
        /// 由带值点要素生成穹窿模型
        /// </summary>
        private static void createFornixModelByPoint()
        {
            string path = @"E:\Users\LiuXianyu\Documents\ExperimentData\myProject\FornixModelingGDAL\Data\LingYanShan\Export\fornix1.obj";
            string imgpath = @"E:\Users\LiuXianyu\Documents\ExperimentData\myProject\FornixModelingGDAL\Data\LingYanShan\lingyan.img";
            _RasterPoints = DEMServer.getVersFromDEM(imgpath);

            //_RasterPoints = new VertexCollection();
            //Vertex tempVer = new Vertex();
            //tempVer.X(139741.0);
            //tempVer.Y(175806.0);
            //tempVer.Z(300.0);
            //tempVer.innerPoint = true;
            //_RasterPoints.addVer(tempVer);

            Fornix preFornix = null;

            ReadShp(@"E:\Users\LiuXianyu\Documents\ExperimentData\myProject\FornixModelingGDAL\Data\LingYanShan\codedata\vertex1\lyspv1.shp");
            ReadShp(@"E:\Users\LiuXianyu\Documents\ExperimentData\myProject\FornixModelingGDAL\Data\LingYanShan\codedata\vertex1\lyspv2.shp");
            ReadShp(@"E:\Users\LiuXianyu\Documents\ExperimentData\myProject\FornixModelingGDAL\Data\LingYanShan\codedata\vertex1\lyspv3.shp");

            foreach (Layer PointLayer in layers)
            {
                //读入一个地层
                Fornix fornix = new Fornix(PointLayer.GetName());
                VertexCollection vcUp = new VertexCollection();
                fornix.dip = PointLayer.GetFeature(0).GetFieldAsDouble("dip");
                for (int i = 0; i < (int)PointLayer.GetFeatureCount(0); ++i)
                {
                    Vertex vertex = new Vertex();
                    vertex.ID = _verNum++;
                    vertex.X(PointLayer.GetFeature(i).GetFieldAsDouble("POINT_X"));
                    vertex.Y(PointLayer.GetFeature(i).GetFieldAsDouble("POINT_Y"));
                    //vertex.Z(PointLayer.GetFeature(i).GetFieldAsDouble("RASTERVALU"));
                    vertex.Z(DEMServer.GetElevation(vertex.X(), vertex.Y()));
                    vcUp.addVer(vertex);
                }
                if (vcUp.Count < 3)
                    MessageBox.Show("地层产状点不足！");
                //生成侧面上顶点
                fornix.createOutSideUpvers(vcUp);
                //侧面生成
                createSide(fornix, preFornix, fornix.dip);

                //将顶面边界点产状信息插回shp中
                //SetShp(PointLayer, fornix);

                //组合
                _fornixs.Add(fornix);
                preFornix = fornix;
            }
            foreach (Fornix fornix in _fornixs)
            {
                //基于相邻顶边，生成顶面
                fornix.createUpFace(_RasterPoints);
                //基于相邻底边，生成底面
                fornix.createDownFace();
            }
            //打印obj
            ObjWriter.writeFornixObj(_fornixs, path, _verNum);
        }

        /// <summary>
        /// 生成穹窿模型侧面
        /// </summary>
        /// <param name="fornix"></param>
        /// <param name="preFornix"></param>
        private static void createSide(Fornix fornix, Fornix preFornix, double FornixDip)
        {
            VertexCollection vcDown = new VertexCollection();//底面顶点集合
            Vertex vertexUp, vertexDown;
            double dip = FornixDip;

            //根据产状生成底面顶点
            for (int i = 1; i < fornix.outSide.countUpVers() - 1; ++i)
            {
                vertexUp = fornix.outSide.getUpver(i);
                vertexUp.calOccuurence(fornix.outSide.getUpver(i - 1), fornix.outSide.getUpver(i + 1), dip);
                vertexDown = new Vertex();
                vertexDown.createDownVer(vertexUp);
                vertexDown.ID = _verNum++;
                vcDown.addVer(vertexDown);
            }

            vertexUp = fornix.outSide.getUpver(fornix.outSide.countUpVers() - 1);
            vertexUp.calOccuurence(fornix.outSide.getUpver(fornix.outSide.countUpVers() - 2), fornix.outSide.getUpver(0), dip);
            vertexDown = new Vertex();
            vertexDown.createDownVer(vertexUp);
            vertexDown.ID = _verNum++;
            vcDown.addVer(vertexDown);

            vertexUp = fornix.outSide.getUpver(0);
            vertexUp.calOccuurence(fornix.outSide.getUpver(fornix.outSide.countUpVers() - 1), fornix.outSide.getUpver(1), dip);
            vertexDown = new Vertex();
            vertexDown.createDownVer(vertexUp);
            vertexDown.ID = _verNum++;
            vcDown.addVer(vertexDown);

            //生成地层外侧面
            fornix.createOutSideLowvers(vcDown);
            _verNum -= fornix.createOutSide();

            //生成前一地层的内侧面
            if (preFornix != null)
                preFornix.createInSide(fornix);
        }

        /// <summary>
        /// 读入SHP文件
        /// </summary>
        /// <param name="shpFilePath"></param>
        private static void ReadShp(string shpFilePath)
        {
            _ds = Ogr.Open(shpFilePath, 1);//0表示只读，1表示可修改  
            if (_ds == null) { MessageBox.Show("打开文件【{0}】失败！", shpFilePath); return; }
            // 获取第一个图层
            Layer curLayer = _ds.GetLayerByIndex(0);
            if (curLayer == null) { MessageBox.Show("获取第{0}个图层失败！ n", "0"); return; }
            layers.Add(curLayer);
        }


        private static void SetShp(Layer PointLayer, Fornix fornix)
        {
            if (-1 == PointLayer.FindFieldIndex("trend", 0))
            {
                FieldDefn oFieldName0 = new FieldDefn("trend", FieldType.OFTReal);
                oFieldName0.SetWidth(50);
                oFieldName0.SetPrecision(7);
                PointLayer.CreateField(oFieldName0, 1);
            }
            if (-1 == PointLayer.FindFieldIndex("incli", 0))
            {
                FieldDefn oFieldName1 = new FieldDefn("incli", FieldType.OFTReal);
                oFieldName1.SetWidth(50);
                oFieldName1.SetPrecision(7);
                PointLayer.CreateField(oFieldName1, 1);
            }
            if (-1 == PointLayer.FindFieldIndex("dip", 0))
            {
                FieldDefn oFieldName2 = new FieldDefn("dip", FieldType.OFTReal);
                oFieldName2.SetWidth(50);
                oFieldName2.SetPrecision(7);
                PointLayer.CreateField(oFieldName2, 1);
            }

            for (int i = 0; i < (int)PointLayer.GetFeatureCount(0); ++i)
            {
                Vertex vertex = fornix.outSide.upvers.getVer(i);
                Feature pointFeature = PointLayer.GetFeature(i);
                pointFeature.SetField("trend", vertex.occurrence.trend);
                pointFeature.SetField("incli", vertex.occurrence.inclination);
                pointFeature.SetField("dip", vertex.occurrence.dip);
                PointLayer.SetFeature(pointFeature);//更改其值
                pointFeature.Dispose();//释放对象
            }
        }
    }
}
