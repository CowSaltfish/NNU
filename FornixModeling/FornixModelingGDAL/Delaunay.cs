using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FornixModelingGDAL
{
    class Delaunay
    {
        private static int idxInnerPoint = 0;
        private static bool UpOrDown = false;

        /// <summary>
        /// 根据多边形内外环、内部点生成Delauney三角网
        /// </summary>
        /// <param name="tris"></param>
        /// <param name="outRing"></param>
        /// <param name="inRing"></param>
        /// <param name="inner"></param>
        /// <returns>多边形内部点点集</returns>
        public static VertexCollection genTriMesh(SortedList<string, Triangle> tris, VertexCollection outRing, VertexCollection inRing, VertexCollection inner)
        {
            SortedList<string, Triangle> cavity = new SortedList<string, Triangle>();
            List<Edge> edges = new List<Edge>();
            VertexCollection vcInner = null;
            if (null != inner) UpOrDown = true;
            else UpOrDown = false;

            //建立矩形包围点集
            Vertex[] rectangle = createSuperRectangle(outRing);
            //将多边形外环顶点插入三角网中
            insertVC2Tris(ref tris, outRing, rectangle, cavity, edges);
            //将多边形内环顶点插入三角网中
            insertVC2Tris(ref tris, inRing, rectangle, cavity, edges);
            //边界恢复
            
            //删去多余三角形，调整法向
            postTreatment(ref tris, outRing, inRing);
            //将多边形内部顶点插入三角网中
            vcInner = insertVC2Tris(ref tris, inner, rectangle, cavity, edges);
            return vcInner;
        }

        /// <summary>
        /// 建立矩形包围点集
        /// </summary>
        /// <param name="outRing"></param>
        /// <returns></returns>
        private static Vertex[] createSuperRectangle(VertexCollection outRing)
        {
            Vertex[] rectangle = new Vertex[4];//左上，右上，左下，右下
            rectangle[0] = new Vertex();
            rectangle[1] = new Vertex();
            rectangle[2] = new Vertex();
            rectangle[3] = new Vertex();
            double minX = 100000000, maxX = 0.0, minY = 100000000, maxY = 0.0;
            for (int i = 0; i < outRing.Count; ++i)
            {
                Vertex ver = outRing.getVer(i);
                if (ver.X() < minX) minX = ver.X();
                if (ver.X() > maxX) maxX = ver.X();
                if (ver.Y() < minY) minY = ver.Y();
                if (ver.Y() > maxY) maxY = ver.Y();
            }
            rectangle[0].X(minX - (maxX - minX) / 10); rectangle[0].Y(maxY + (maxY - minY) / 10); rectangle[0].ID = 10000;
            rectangle[1].X(maxX + (maxX - minX) / 10); rectangle[1].Y(maxY + (maxY - minY) / 10); rectangle[1].ID = 10001;
            rectangle[2].X(minX - (maxX - minX) / 10); rectangle[2].Y(minY - (maxY - minY) / 10); rectangle[2].ID = 10002;
            rectangle[3].X(maxX + (maxX - minX) / 10); rectangle[3].Y(minY - (maxY - minY) / 10); rectangle[3].ID = 10003;
            return rectangle;
        }

        /// <summary>
        /// 由多边形顶点生成三角网
        /// </summary>
        /// <param name="tris"></param>
        /// <param name="vc"></param>
        /// <param name="rectangle"></param>
        /// <param name="cavity"></param>
        /// <param name="edges"></param>
        private static VertexCollection insertVC2Tris(ref SortedList<string, Triangle> tris, VertexCollection vc, Vertex[] rectangle, SortedList<string, Triangle> cavity, List<Edge> edges)
        {
            if (null == vc)
                return null;
            Triangle tri;
            int i = 0, j;
            VertexCollection vcInner = new VertexCollection();
            if (0 == tris.Count)
            {
                //第一个点连接矩形顶点
                Vertex ver0 = vc.getVer(0);
                Triangle[] triOfRectangle = new Triangle[4];
                triOfRectangle[0] = new Triangle();
                triOfRectangle[1] = new Triangle();
                triOfRectangle[2] = new Triangle();
                triOfRectangle[3] = new Triangle();
                triOfRectangle[0].addPoint(ver0);
                triOfRectangle[0].addPoint(rectangle[1]);
                triOfRectangle[0].addPoint(rectangle[0]);
                tris.Add(triOfRectangle[0].name, triOfRectangle[0]);
                triOfRectangle[1].addPoint(ver0);
                triOfRectangle[1].addPoint(rectangle[3]);
                triOfRectangle[1].addPoint(rectangle[1]);
                tris.Add(triOfRectangle[1].name, triOfRectangle[1]);
                triOfRectangle[2].addPoint(ver0);
                triOfRectangle[2].addPoint(rectangle[2]);
                triOfRectangle[2].addPoint(rectangle[3]);
                tris.Add(triOfRectangle[2].name, triOfRectangle[2]);
                triOfRectangle[3].addPoint(ver0);
                triOfRectangle[3].addPoint(rectangle[0]);
                triOfRectangle[3].addPoint(rectangle[2]);
                tris.Add(triOfRectangle[3].name, triOfRectangle[3]);
                ++i;
            }
            //将地层顶点插入三角网中
            for (; i < vc.Count; ++i)
            {
                Vertex verInserted = vc.getVer(i);
                if (!verInserted.innerPoint)
                {
                    for (j = 0; j < tris.Count; ++j)
                    {
                        tri = tris.Values[j];
                        Vertex circumcenter = tri.calCircumCenter();
                        //点在三角形外接圆中
                        if (verInserted.calDistance(circumcenter) <= tri.points.getVer(0).calDistance(circumcenter))
                        {
                            //广度遍历得到空腔
                            findCavity(verInserted, tri, tris, cavity, edges);
                            //由空腔生成新三角形
                            createTriByCavity(verInserted,ref tris, cavity, edges);
                            break;
                        }
                    }
                }
                else
                {
                    for (j = 0; j < tris.Count; ++j)
                    {
                        tri = tris.Values[j];
                        //判断点在三角形中
                        if (verInserted.inside(tri))
                        {
                            verInserted.ID = ++idxInnerPoint;
                            vcInner.addVer(verInserted);
                            //广度遍历得到空腔
                            findCavity(verInserted, tri, tris, cavity, edges);
                            //由空腔生成新三角形
                            createTriByCavity(verInserted,ref tris, cavity, edges);
                            break;
                        }
                    }
                }
                
            }
            //插入内部点后调整三角面法向
            if (0 != vc.Count && vc.getVer(0).innerPoint)
            {
                for (i = 0; i < tris.Count; ++i)
                {
                    tri = tris.Values[i];
                    if (tri.calVector() < 0.0)
                        tri.reverse();
                }
            }
            return vcInner;
        }

        /// <summary>
        /// 后处理，删去多余三角形，调整法向
        /// </summary>
        /// <param name="tris"></param>
        /// <param name="outRing"></param>
        /// <param name="inRing"></param>
        private static void postTreatment(ref SortedList<string, Triangle> tris, VertexCollection outRing, VertexCollection inRing)
        {
            int minOutRingID = outRing.getVer(0).ID;
            int maxOutRingID = outRing.getVer(outRing.Count - 1).ID;
            int minInRingID = 0 != inRing.Count ? inRing.getVer(0).ID : 0;
            int maxInRingID = 0 != inRing.Count ? inRing.getVer(inRing.Count - 1).ID : 0;
            Triangle tri;
            int i;
            List<String> nameOFTriRemoved = new List<string>();

            for (i = 0; i < tris.Count; ++i)
            {
                tri = tris.Values[i];
                //删去与超级矩形顶点有关的三角形、内环内的三角形、外环外的三角形
                if (tri.hasVer(10000) || tri.hasVer(10001) || tri.hasVer(10002) || tri.hasVer(10003)
                    || (0 != inRing.Count && tri.pointsOnRing(minInRingID, maxInRingID) && tri.calVector() < 0.0)
                    || (tri.pointsOnRing(minOutRingID, maxOutRingID) && tri.calVector() > 0.0))
                    nameOFTriRemoved.Add(tri.name);
                //调整三角形法向
                else if (!UpOrDown && tri.calVector() > 0.0)
                    tri.reverse();
                else if (UpOrDown && tri.calVector() < 0.0)
                    tri.reverse();
            }
            //执行删除
            for (i = 0; i < nameOFTriRemoved.Count; ++i)
                tris.Remove(nameOFTriRemoved[i]);
        }

        /// <summary>
        /// 寻找空腔
        /// </summary>
        /// <param name="verInserted"></param>
        /// <param name="tri"></param>
        /// <param name="tris"></param>
        /// <param name="cavity"></param>
        /// <param name="edges"></param>
        private static void findCavity(Vertex verInserted, Triangle tri,SortedList<string, Triangle> tris, SortedList<string, Triangle> cavity, List<Edge> edges)
        {
            Queue<Triangle> triQueue = new Queue<Triangle>();
            cavity.Clear();
            edges.Clear();
            triQueue.Clear();
            triQueue.Enqueue(tri);

            //广度遍历生成空腔
            while (0 != triQueue.Count)
            {
                Triangle curTri = triQueue.Dequeue();
                if (!curTri.isCavity)
                    addTri2Cavity(curTri, cavity, ref edges);
                foreach (Triangle adjtri in curTri.getAdjTri(tris))
                {
                    if (adjtri.isCavity)
                        continue;
                    Vertex circumcenter = adjtri.calCircumCenter();
                    if (verInserted.calDistance(circumcenter) <= adjtri.points.getVer(0).calDistance(circumcenter))
                        triQueue.Enqueue(adjtri);
                }
            }
        }

        /// <summary>
        /// 将三角形加入空腔
        /// </summary>
        /// <param name="curTri"></param>
        /// <param name="cavity"></param>
        /// <param name="edges"></param>
        private static void addTri2Cavity(Triangle curTri, SortedList<string, Triangle> cavity, ref List<Edge> edges)
        {
            Edge edge, newEdge0, newEdge1, newEdge2;
            newEdge0 = new Edge(curTri.points.getVer(0), curTri.points.getVer(1));
            newEdge1 = new Edge(curTri.points.getVer(1), curTri.points.getVer(2));
            newEdge2 = new Edge(curTri.points.getVer(0), curTri.points.getVer(2));
            List<Edge> edges1 = new List<Edge>();
            bool flag;

            for (int i = 0; i < edges.Count; ++i)
            {
                edge = edges[i];
                flag = false;
                if (edge.Equals(curTri.points.getVer(0), curTri.points.getVer(1)))
                {
                    flag = true;
                    newEdge0 = null;
                }
                else if (edge.Equals(curTri.points.getVer(1), curTri.points.getVer(2)))
                {
                    flag = true;
                    newEdge1 = null;
                }
                else if (edge.Equals(curTri.points.getVer(2), curTri.points.getVer(0)))
                {
                    flag = true;
                    newEdge2 = null;
                }
                if (!flag)
                    edges1.Add(edge);
            }
            if (null != newEdge0)
                edges1.Add(newEdge0);
            if (null != newEdge1)
                edges1.Add(newEdge1);
            if (null != newEdge2)
                edges1.Add(newEdge2);
            cavity.Add(curTri.name, curTri);
            curTri.isCavity = true;
            edges.Clear();
            edges.AddRange(edges1);
        }

        /// <summary>
        /// 在空腔中新建三角面
        /// </summary>
        /// <param name="verInserted"></param>
        /// <param name="tris"></param>
        /// <param name="cavity"></param>
        /// <param name="edges"></param>
        private static void createTriByCavity(Vertex verInserted,ref SortedList<string, Triangle> tris, SortedList<string, Triangle> cavity, List<Edge> edges)
        {
            Triangle  newTri;
            int i;
            string nameR;
            //删去空腔中三角形
            for (i = 0; i < cavity.Count; ++i)
            {
                if (tris.ContainsKey(cavity.Keys[i]))
                    tris.Remove(cavity.Keys[i]);
                else
                {
                    nameR = cavity.Values[i].reverseName();
                    tris.Remove(nameR);
                }
            }
            //添加新三角形
            for (i = 0; i < edges.Count; ++i)
            {
                newTri = new Triangle();
                newTri.addPoint(edges[i].v0);
                newTri.addPoint(edges[i].v1);
                newTri.addPoint(verInserted);
                tris.Add(newTri.name, newTri);
            }
        }
    }
}
