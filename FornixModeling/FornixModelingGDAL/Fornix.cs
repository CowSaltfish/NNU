using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OSGeo.GDAL;

namespace FornixModelingGDAL
{
    class Fornix
    {
        public string name { get; set; }
        public UpFace upFace { get; private set; }
        public DownFace downFace { get; private set; }
        public OutSide outSide { get; private set; }
        public InSide inSide { get; private set; }
        public double dip { get; set; }
        //private IPointCollection area;

        public Fornix()
        {
            //this.area = new PolygonClass();
            this.upFace = new UpFace();
            this.downFace = new DownFace();
            this.outSide = new OutSide();
            this.inSide = new InSide();
            this.dip = Math.PI / 4;
        }

        public Fornix(string name)
        {
            this.name = name;
            //this.area = new PolygonClass();
            this.upFace = new UpFace();
            this.downFace = new DownFace();
            this.outSide = new OutSide();
            this.inSide = new InSide();
            this.dip = Math.PI / 4;
        }

        //地层存入侧面上顶点
        public void createOutSideUpvers(VertexCollection vc)
        {
            this.outSide.addUpvers(vc);
        }
        //地层存入侧面下顶点
        public void createOutSideLowvers(VertexCollection vc)
        {
            this.outSide.addLowvers(vc);
        }
        //地层侧面生成
        public int createOutSide()
        {
            Triangle tri0, tri1;
            Edge edge;
            int verPairNum = this.outSide.countUpVers();
            for (int i = 0; i < verPairNum - 1; ++i)
            {
                tri0 = new Triangle();
                tri1 = new Triangle();
                edge = new Edge(this.outSide.getDownver(i), this.outSide.getDownver(i + 1));
                tri0.addPoint(this.outSide.getUpver(i + 1));
                tri0.addPoint(this.outSide.getDownver(i));
                tri0.addPoint(this.outSide.getUpver(i));
                tri1.addPoint(this.outSide.getDownver(i + 1));
                tri1.addPoint(this.outSide.getDownver(i));
                tri1.addPoint(this.outSide.getUpver(i + 1));
                edge.adjTriName[0] = tri0.name;
                edge.adjTriName[1] = tri1.name;
                this.outSide.addTri(tri0);
                this.outSide.addTri(tri1);
                this.outSide.addEdge(edge);
            }
            tri0 = new Triangle();
            tri1 = new Triangle();
            edge = new Edge(this.outSide.getDownver(verPairNum - 1), this.outSide.getDownver(0));
            tri0.addPoint(this.outSide.getUpver(0));
            tri0.addPoint(this.outSide.getDownver(verPairNum - 1));
            tri0.addPoint(this.outSide.getUpver(verPairNum - 1));
            tri1.addPoint(this.outSide.getDownver(0));
            tri1.addPoint(this.outSide.getDownver(verPairNum - 1));
            tri1.addPoint(this.outSide.getUpver(0));
            edge.adjTriName[0] = tri0.name;
            edge.adjTriName[1] = tri1.name;
            this.outSide.addTri(tri0);
            this.outSide.addTri(tri1);
            this.outSide.addEdge(edge);

            //return 0;
            //检查底边线段有无相交，相交则处理
            return removeInsectedFace();
        }
    
        /// <summary>
        /// 解决突变面问题
        /// </summary>
        /// <param name="tri0"></param>
        /// <param name="tri1"></param>
        private void cancelMutateFace(Triangle tri0, Triangle tri1)
        {

        }

        /// <summary>
        /// 通过检查侧边底边线段有无相交（形成意外圈），来消除相交三角面，转而用其最外交点为顶点，生成新的三角面代替
        /// </summary>
        private int removeInsectedFace()
        {
            int removeLowVersNum = 0;//移去侧面底边顶点个数
            int i, j, k = 0, idxEdge;//当前线段与其后第idxEdge个线段最后相交
            Triangle insertedTri;
            Edge curEdge, testEdge, crossEdge = null;
            Vertex insertedVer;
            VertexCollection vcDown1 = new VertexCollection();
            for (i = 0; i < this.outSide.edges.Count; ++i)
            {
                curEdge = this.outSide.edges[i];
                idxEdge = 0;
                for (j = i + 2; (i > 0 && j < this.outSide.edges.Count) || (i == 0 && j < this.outSide.edges.Count - 1); ++j)//一个线段的下两个线段才可能与其“相交”，所以从i+2开始
                {
                    testEdge = this.outSide.edges[j];
                    if (curEdge.InsectionJudge(testEdge))
                    {
                        crossEdge = testEdge;
                        idxEdge = j - i;
                    }
                }
                if (0 == idxEdge)
                    continue;
                if (idxEdge > this.outSide.edges.Count / 2)
                    MessageBox.Show("底边起始点可能处于‘意外圈’中！");//TODO:这种情况还没解决
                removeLowVersNum += (idxEdge - 1);
                //两个线段交点
                insertedVer = curEdge.getCrossPoint(crossEdge);
                //删除意外圈原有三角面，并插入意外圈所有新上三角面
                for (j = i; j <= i + idxEdge; ++j)
                {
                    this.outSide.tris.Remove(this.outSide.edges[j].adjTriName[0]);
                    this.outSide.tris.Remove(this.outSide.edges[j].adjTriName[1]);
                    insertedTri = new Triangle();
                    insertedTri.addPoint(this.outSide.upvers.getVer(j));
                    insertedTri.addPoint(this.outSide.upvers.getVer((j + 1 == this.outSide.edges.Count) ? 0 : j + 1));
                    insertedTri.addPoint(insertedVer);
                    this.outSide.tris.Add(insertedTri.name,insertedTri);
                }
                //插入“意外圈”前端新下三角面
                insertedTri = new Triangle();
                insertedTri.addPoint(this.outSide.upvers.getVer(i));
                insertedTri.addPoint(insertedVer);
                insertedTri.addPoint(this.outSide.lowvers.getVer(i));
                this.outSide.tris.Add(insertedTri.name, insertedTri);
                //插入“意外圈”后端新下三角面
                insertedTri = new Triangle();
                insertedTri.addPoint(this.outSide.upvers.getVer((i + idxEdge + 1 == this.outSide.edges.Count) ? 0 : i + idxEdge + 1));
                insertedTri.addPoint(this.outSide.lowvers.getVer((i + idxEdge + 1 == this.outSide.edges.Count) ? 0 : i + idxEdge + 1));
                insertedTri.addPoint(insertedVer);
                this.outSide.tris.Add(insertedTri.name, insertedTri);
                //当前顶点后的所有顶点ID前移
                for (; j < this.outSide.lowvers.Count; ++j)
                    this.outSide.lowvers.getVer(j).ID -= (idxEdge - 1);
                //往新底面顶点集合中插入已确定的顶点
                for (j = k; j <= i; ++j)
                    vcDown1.addVer(this.outSide.lowvers.getVer(j));
                vcDown1.addVer(insertedVer);
                k = i + idxEdge + 1;
                //向后推进到与当前侧面底边线段无交点的第一个线段
                i += idxEdge;
            }
            //插入剩余的顶点
            for (j = k; j < i; ++j)
                vcDown1.addVer(this.outSide.lowvers.getVer(j));
            this.outSide.lowvers.clear();
            this.outSide.lowvers.addVerCollection(vcDown1);
            return removeLowVersNum;
        }

        public void createInSide(Fornix foxnix)
        {
            this.inSide.createInByOut(foxnix);
        }

        public void createUpFace(VertexCollection rasterPoints)
        {
            this.upFace.createBySide(this.outSide.upvers, this.inSide.upvers, rasterPoints);
        }

        public void createDownFace()
        {
            this.downFace.createBySide(this.outSide.lowvers, this.inSide.lowvers);
        }

    }

    class UpFace
    {
        public SortedList<string, Triangle> tris { get; private set; }
        public VertexCollection InnerPoints { get; private set; }

        public UpFace()
        {
            this.tris = new SortedList<string, Triangle>();
        }
        public void createBySide(VertexCollection outSideLowvers, VertexCollection inSideLowvers, VertexCollection pointsOfDEM)
        {
            this.InnerPoints = Delaunay.genTriMesh(tris, outSideLowvers, inSideLowvers, pointsOfDEM);
        }
    }

    class DownFace
    {
        public SortedList<string, Triangle> tris { get; private set; }

        public DownFace()
        {
            this.tris = new SortedList<string, Triangle>();
        }

        public void createBySide(VertexCollection outSideLowvers, VertexCollection inSideLowvers)
        {

            Delaunay.genTriMesh(tris, outSideLowvers, inSideLowvers, null);
        }
    }

    class OutSide
    {
        public SortedList<string, Triangle> tris { get; private set; }
        public List<Edge> edges { get; private set; }
        public VertexCollection upvers { get; private set; }
        public VertexCollection lowvers { get; private set; }
        public OutSide()
        {
            this.tris = new SortedList<string, Triangle>();
            this.edges = new List<Edge>();
            this.upvers = new VertexCollection();
            this.lowvers = new VertexCollection();
        }
        public void addUpvers(VertexCollection upvers)
        {
            this.upvers.addVerCollection(upvers);
        }
        public void addLowvers(VertexCollection lowvers)
        {
            this.lowvers.addVerCollection(lowvers);
        }
        public Vertex getUpver(int index)
        {
            return this.upvers.getVer(index);
        }
        public Vertex getDownver(int index)
        {
            return this.lowvers.getVer(index);
        }
        public int countUpVers()
        {
            return this.upvers.Count;
        }
        public void addTri(Triangle tri)
        {
            this.tris.Add(tri.name, tri);
        }
        public void addEdge(Edge edge)
        {
            this.edges.Add(edge);
        }
    }
    class InSide
    {
        private SortedList<string, Triangle> tris;
        public VertexCollection upvers { get; private set; }
        public VertexCollection lowvers { get; private set; }

        public InSide()
        {
            this.tris = new SortedList<string, Triangle>();
            this.upvers = new VertexCollection();
            this.lowvers = new VertexCollection();
        }

        public void createInByOut(Fornix fornix)
        {
            this.upvers.clear();
            this.lowvers.clear();
            this.upvers.addVerCollection(fornix.outSide.upvers);
            this.lowvers.addVerCollection(fornix.outSide.lowvers);

        }
    }

    class Triangle
    {
        public string name { get; set; }
        public VertexCollection points { get; private set; }  //三角面点集
        public bool isCavity { get; set; }

        public Triangle()
        {
            this.points = new VertexCollection();
            this.points.clear();
            this.isCavity = false;
        }

        public void addPoint(Vertex ver)
        {
            this.points.addVer(ver);
            if (this.points.Count == 3)
            {
                string point0 = this.points.getVer(0).ID.ToString(),
                    point1 = this.points.getVer(1).ID.ToString(),
                    point2 = this.points.getVer(2).ID.ToString();
                if (this.points.getVer(0).innerPoint)
                    point0 = "in" + point0;
                if (this.points.getVer(1).innerPoint)
                    point1 = "in" + point1;
                if (this.points.getVer(2).innerPoint)
                    point2 = "in" + point2;
                this.name = point0 + "_" + point1 + "_" + point2;
            }
        }

        public Vertex calCircumCenter()
        {
            if (this.points.Count != 3)
                MessageBox.Show("顶点不为3，无法求外心！");
            return this.points.getVer(0).calCircumCenter(this.points.getVer(0), this.points.getVer(1), this.points.getVer(2));
        }

        public List<Triangle> getAdjTri(SortedList<string, Triangle> tris)
        {
            Vertex ver0 = this.points.getVer(0);
            Vertex ver1 = this.points.getVer(1);
            Vertex ver2 = this.points.getVer(2);
            List<Triangle> adjTris = new List<Triangle>();
            adjTris.Clear();
            for (int i = 0; i < tris.Count; ++i)
            {
                Triangle tri = tris.Values[i];
                if ((tri.hasVer(ver0) && tri.hasVer(ver1) && !tri.hasVer(ver2))
                    || (tri.hasVer(ver0) && !tri.hasVer(ver1) && tri.hasVer(ver2))
                    || (!tri.hasVer(ver0) && tri.hasVer(ver1) && tri.hasVer(ver2)))
                    adjTris.Add(tri);
            }
            return adjTris;
        }

        public bool hasVer(Vertex ver)
        {
            for (int i = 0; i < 3; ++i)
                if (ver.ID == this.points.getVer(i).ID && !(ver.innerPoint ^ this.points.getVer(i).innerPoint))
                    return true;
            return false;
        }

        public bool hasVer(int ID)
        {
            for (int i = 0; i < 3; ++i)
                if (ID == this.points.getVer(i).ID)
                    return true;
            return false;
        }

        /// <summary>
        /// 判断每个顶点都在环上
        /// </summary>
        /// <param name="minID"></param>
        /// <param name="maxID"></param>
        /// <returns></returns>
        public bool pointsOnRing(int minID, int maxID)
        {
            if ((this.points.getVer(0).ID >= minID && this.points.getVer(0).ID <= maxID)
                && (this.points.getVer(1).ID >= minID && this.points.getVer(1).ID <= maxID)
                && (this.points.getVer(2).ID >= minID && this.points.getVer(2).ID <= maxID))
                return true;
            return false;
        }

        public double calVector()
        {
            return this.points.getVer(1).calVector(this.points.getVer(0), this.points.getVer(2));
        }

        /// <summary>
        /// 按照三角形中点ID从小到大求
        /// </summary>
        /// <param name="featureLayer"></param>
        /// <param name="fornixs"></param>
        /// <returns></returns>
        public double calOrderVector()
        {
            return this.points.getVer(1).calVector(this.points.getVer(0), this.points.getVer(2));
        }

        public Triangle reverse()
        {
            List<Vertex> revs = new List<Vertex>();

            revs.Add(this.points.getVer(2));
            revs.Add(this.points.getVer(1));
            revs.Add(this.points.getVer(0));

            this.points.remove(2);
            this.points.remove(1);
            this.points.remove(0);

            this.addPoint(revs[0]);
            this.addPoint(revs[1]);
            this.addPoint(revs[2]);

            return this;

        }

        public string reverseName()
        {
            string point0 = this.points.getVer(0).ID.ToString(),
                   point1 = this.points.getVer(1).ID.ToString(),
                   point2 = this.points.getVer(2).ID.ToString();
            if (this.points.getVer(0).innerPoint)
                point0 = "in" + point0;
            if (this.points.getVer(1).innerPoint)
                point1 = "in" + point1;
            if (this.points.getVer(2).innerPoint)
                point2 = "in" + point2;
            return point2 + "_" + point1 + "_" + point0;
        }

    }

    class Edge
    {
        public Vertex v0 { get; set; }
        public Vertex v1 { get; set; }
        public String name;
        public String[] adjTriName { get; set; }//删除底边相交点时，用于找到应当删除的三角面

        public Edge(Vertex v0, Vertex v1)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.adjTriName = new string[2];
            name = v0.ID.ToString() + "_" + v1.ID.ToString();
        }

        public bool Equals(Edge edge)
        {
            if (this.v0.ID == edge.v0.ID && this.v1.ID == edge.v1.ID
                || this.v0.ID == edge.v1.ID && this.v1.ID == edge.v0.ID)
                return true;
            return false;
        }

        public bool Equals(Vertex v0, Vertex v1)
        {
            if ((this.v0.ID == v0.ID && this.v1.ID == v1.ID && !(this.v0.innerPoint ^ v0.innerPoint) && !(this.v1.innerPoint ^ v1.innerPoint))
                || (this.v0.ID == v1.ID && this.v1.ID == v0.ID && !(this.v0.innerPoint ^ v1.innerPoint) && !(this.v1.innerPoint ^ v0.innerPoint)))
                return true;
            return false;
        }

        public Vertex getVer(int idx)
        {
            if (0 == idx)
                return this.v0;
            else
                return this.v1;
        }

        /// <summary>
        /// 判断俩直线是否相交
        /// </summary>
        /// <param name="edge">第二条线段</param>
        /// <returns>是否相交</returns>
        public bool InsectionJudge(Edge edge)
        {
            Vertex a = this.v0;
            Vertex b = this.v1;
            Vertex c = edge.v0;
            Vertex d = edge.v1;
            /*
            快速排斥：
            两个线段为对角线组成的矩形，如果这两个矩形没有重叠的部分，那么两条线段是不可能出现重叠的
            */
            if (!(Math.Min(a.X(), b.X()) <= Math.Max(c.X(), d.X()) &&
                Math.Min(c.Y(), d.Y()) <= Math.Max(a.Y(), b.Y()) &&
                Math.Min(c.X(), d.X()) <= Math.Max(a.X(), b.X()) &&
                Math.Min(a.Y(), b.Y()) <= Math.Max(c.Y(), d.Y())))//这一步是判定两矩形是否相交
            {
                return false;
            }
            /*
            跨立实验：
            如果两条线段相交，那么必须跨立，就是以一条线段为标准，另一条线段的两端点一定在这条线段的两段
            也就是说a b两点在线段cd的两端，c d两点在线段ab的两端
            */
            double u, v, w, z;//分别记录两个向量
            u = (c.X() - a.X()) * (b.Y() - a.Y()) - (b.X() - a.X()) * (c.Y() - a.Y());
            v = (d.X() - a.X()) * (b.Y() - a.Y()) - (b.X() - a.X()) * (d.Y() - a.Y());
            w = (a.X() - c.X()) * (d.Y() - c.Y()) - (d.X() - c.X()) * (a.Y() - c.Y());
            z = (b.X() - c.X()) * (d.Y() - c.Y()) - (d.X() - c.X()) * (b.Y() - c.Y());
            return (u * v <= 0.00000001 && w * z <= 0.00000001);
        }

        /// <summary>
        /// 求线段与当前线段的交点
        /// </summary>
        /// <param name="edge">目标线段</param>
        /// <returns>交点</returns>
        public Vertex getCrossPoint(Edge edge)
        {
            if (null == edge)
                return null;
            Vertex crossPoint = new Vertex();
            Vertex a = this.v0;
            Vertex b = this.v1;
            Vertex c = edge.v0;
            Vertex d = edge.v1;
            double A = (b.X() - a.X()) / (b.Y() - a.Y());
            double B = (d.X() - c.X()) / (d.Y() - c.Y());
            double y = (c.X() - a.X() + A * a.Y() - B * c.Y()) / (A - B);
            double x = A * y - A * a.Y() + a.X();
            crossPoint.X(x);
            crossPoint.Y(y);
            crossPoint.Z((a.Z() + b.Z() + c.Z() + d.Z()) / 4);
            crossPoint.ID = b.ID;
            return crossPoint;
        }
    }

    class VertexCollection
    {
        private List<Vertex> vers;
        public int Count { get { return this.vers.Count; } }

        public VertexCollection()
        {
            this.vers = new List<Vertex>();
            this.clear();
        }
        public void addVer(Vertex ver)
        {
            this.vers.Add(ver);
        }
        public void addVerCollection(VertexCollection vers)
        {
            int num = vers.Count;
            for (int i = 0; i < num; ++i)
            {
                this.addVer(vers.getVer(i));
            }
        }
        public void clear()
        {
            this.vers.Clear();
        }
        public Vertex getVer(int index)
        {
            return this.vers[index];
        }
        public void remove(int index)
        {
            this.vers.Remove(this.vers[index]);
        }

        public void removeVers(int index, int count)
        {
            this.vers.RemoveRange(index, count);
        }

        public void insert(int idx, Vertex ver)
        {
            this.vers.Insert(idx, ver);
        }
    }

    class Vertex
    {
        public int ID { get; set; }
        private Coordinate position { get; set; }
        public Occurrence occurrence { get; set; }
        public bool innerPoint { get; set; }//是否是内部点
        public bool toRemove { get; set; }
        public Vertex()
        {
            this.position = new Coordinate();
            this.occurrence = new Occurrence();
            this.innerPoint = false;
            this.toRemove = false;
        }

        //计算顶点产状
        public void calOccuurence(Vertex prev, Vertex nextv, double dip)
        {
            Vertex circumCenter = calCircumCenter(this, prev, nextv);
            //外心指向当前点的向量
            double x = this.X() - circumCenter.X();
            double y = this.Y() - circumCenter.Y();
            if (x == 0.0 || y == 0.0)
                MessageBox.Show("方向角为九十度倍数!");
            if (x > 0 && y > 0)
            {
                this.occurrence.inclination = Math.Atan(Math.Abs(x) / Math.Abs(y));
            }
            else if (x > 0 && y < 0)
            {
                this.occurrence.inclination = Math.PI / 2 + Math.Atan(Math.Abs(y) / Math.Abs(x));
            }
            else if (x < 0 && y < 0)
            {
                this.occurrence.inclination = Math.PI + Math.Atan(Math.Abs(x) / Math.Abs(y));
            }
            else
            {
                this.occurrence.inclination = Math.PI * 3 / 2 + Math.Atan(Math.Abs(y) / Math.Abs(x));
            }
            if (0.0 < this.calVector(prev, nextv))
            {
                this.occurrence.inclination = (this.occurrence.inclination - Math.PI >= 0.0) ?
                    this.occurrence.inclination - Math.PI : this.occurrence.inclination + Math.PI;
            }
            this.occurrence.trend = (this.occurrence.inclination - Math.PI / 2 >= 0.0) ?
                this.occurrence.inclination - Math.PI / 2 : this.occurrence.inclination + Math.PI / 2;
            this.occurrence.dip = dip;
        }

        public Vertex calCircumCenter(Vertex curv, Vertex prev, Vertex nextv)
        {
            Vertex circumCenter = new Vertex();
            double A0 = prev.X() * prev.X() + prev.Y() * prev.Y();
            double A1 = this.X() * this.X() + this.Y() * this.Y();
            double A2 = nextv.X() * nextv.X() + nextv.Y() * nextv.Y();
            double Ax = A1 * nextv.Y() + A0 * this.Y() + A2 * prev.Y() - prev.Y() * A1 - A0 * nextv.Y() - this.Y() * A2;
            double Ay = this.X() * A2 + prev.X() * A1 + A0 * nextv.X() - A0 * this.X() - prev.X() * A2 - A1 * nextv.X();
            double B = this.X() * nextv.Y() + prev.X() * this.Y() + prev.Y() * nextv.X() - prev.Y() * this.X() - prev.X() * nextv.Y() - this.Y() * nextv.X();
            if (B == 0.0)
                MessageBox.Show("边界点可能重复！");
            circumCenter.X(Ax / (2 * B));
            circumCenter.Y(Ay / (2 * B));
            return circumCenter;
        }

        //创建顶面顶点的底面对应顶点
        public Vertex createDownVer(Vertex ver)
        {
            double height = 200.0;
            this.X(ver.X() + height * Math.Sin(ver.occurrence.inclination) / Math.Tan(ver.occurrence.dip));
            this.Y(ver.Y() + height * Math.Cos(ver.occurrence.inclination) / Math.Tan(ver.occurrence.dip));
            this.Z(ver.Z() - height);
            return this;
        }

        public double calAngle(Vertex prev, Vertex nextv)
        {
            double x0 = prev.X() - this.X();
            double y0 = prev.Y() - this.Y();
            double x1 = nextv.X() - this.X();
            double y1 = nextv.Y() - this.Y();
            return (x0 * x1 + y0 * y1) / (Math.Sqrt(x0 * x0 + y0 * y0) + Math.Sqrt(x1 * x1 + y1 * y1));
        }

        public double calVector(Vertex prev, Vertex nextv)
        {
            double x0 = prev.X() - this.X();
            double y0 = prev.Y() - this.Y();
            double x1 = this.X() - nextv.X();
            double y1 = this.Y() - nextv.Y();
            return x0 * y1 - x1 * y0;
        }

        public double calDistance(Vertex ver)
        {
            return Math.Sqrt(Math.Pow(this.X() - ver.X(), 2) + Math.Pow(this.Y() - ver.Y(), 2));
        }

        public bool inside(Triangle tri)
        {
            double CAd = tri.points.getVer(0).calVector(tri.points.getVer(2), this);
            double dAB = tri.points.getVer(0).calVector(this, tri.points.getVer(1));
            double ABd = tri.points.getVer(1).calVector(tri.points.getVer(0), this);
            double dBC = tri.points.getVer(1).calVector(this, tri.points.getVer(2));
            if (CAd * dAB > 0.0 && ABd * dBC > 0.0)
                return true;
            return false;
        }

        public double X() { return this.position.x; }
        public double Y() { return this.position.y; }
        public double Z() { return this.position.z; }
        public void X(double x) { this.position.x = x; }
        public void Y(double y) { this.position.y = y; }
        public void Z(double z) { this.position.z = z; }

    }

    class Occurrence
    {
        public double trend { get; set; }//走向
        public double inclination { get; set; }//倾向
        public double dip { get; set; }//倾角
    }

    class Coordinate
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
    }

}
