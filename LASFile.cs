using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using LibLAS;

namespace LasView {

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct LPoint {
        public float X;
        public float Y;
        public float Z;
        public float Intensity;
    }

    public class LASFile {
        public double XOffset { get; private set; }
        public double YOffset { get; private set; }
        public double ZOffset { get; private set; }
        public double XExtent { get; private set; }
        public double YExtent { get; private set; }
        public double ZExtent { get; private set; }

        public long NumPoints { get; private set; }
        public long StoredPoints { get; private set; }
        public string Filename { get; private set; }
        public string ProjectID { get; private set; }
        public string Signature { get; private set; }
        public bool IsLoaded { get; private set; }

        private List<LASChunk> chunks = null;

        public LASFile(string filename, int pointsToStore) {
            IsLoaded = false;

            using (LASReader reader = new LASReader(filename))
            using (LASHeader header = reader.GetHeader()) {
                ProjectID = header.ProjectId;
                Signature = header.FileSignature;
                NumPoints = header.PointRecordsCount;
                XOffset = header.GetMinX();
                YOffset = header.GetMinY();
                ZOffset = header.GetMinZ();
                XExtent = header.MaxX() - XOffset;
                YExtent = header.GetMaxY() - YOffset;
                ZExtent = header.GetMaxZ() - ZOffset;
                chunks = new List<LASChunk>((int) (NumPoints / LASChunk.ChunkSize + 1));

                LASChunk curChunk = new LASChunk(0);
                long curOffset = 0, stored = 0;
                long pointSkip = NumPoints / pointsToStore;
                if (pointSkip <= 0) {
                    pointSkip = 1;
                }
                
                while (reader.GetNextPoint()) {
                    curOffset++;

                    if (curOffset % pointSkip == 0) {
                        LASPoint point = reader.GetPoint();
                        if (point.ReturnNumber > 1) {
                            curOffset--;
                            continue;
                        }

                        curChunk.Points.Add(
                            new LPoint() {
                                X = (float)(point.X - XOffset),
                                Y = (float)(point.Y - YOffset),
                                Z = (float)(point.Z - ZOffset),
                                Intensity = (float)point.Intensity
                            }
                        );
                        stored++;

                        if (curChunk.Points.Count >= LASChunk.ChunkSize) {
                            chunks.Add(curChunk);
                            curChunk = new LASChunk(curOffset);
                        }
                    }
                }

                if (curChunk.Points.Count > 0) {
                    chunks.Add(curChunk);
                }

                StoredPoints = stored;
            }

            Filename = filename;
            IsLoaded = true;
        }

        public IEnumerable<LPoint> Points {
            get {
                foreach (LASChunk chunk in chunks) {
                    foreach (LPoint point in chunk.Points) {
                        yield return point;
                    }
                }
            }
        }

        public LPoint this[long index] {
            get {
                LASChunk chunk = chunks[(int)(index / LASChunk.ChunkSize)];
                return chunk.Points[(int) (index % LASChunk.ChunkSize)];
            }
        }

        private class LASChunk {
            public static int ChunkSize = 4096;

            public long Offset { get; private set; }
            public List<LPoint> Points {get; private set;}

            public LASChunk(long offset) {
                Points = new List<LPoint>(ChunkSize);
                Offset = offset;
            }
        }
    }
}
