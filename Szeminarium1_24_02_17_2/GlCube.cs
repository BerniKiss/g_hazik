using Silk.NET.OpenGL;

namespace Szeminarium1_24_02_17_2
{
    internal class GlCube
    {
        private static uint sharedVertices = 0;
        private static uint sharedIndices = 0;
        private static bool sharedInitialised = false;

        public uint Vao { get; }
        public uint Colors { get; }
        public uint IndexArrayLength { get; }

        private GL Gl;

        private GlCube(uint vao, uint colors, uint indexArrayLength, GL gl)
        {
            this.Vao = vao;
            this.Colors = colors;
            this.IndexArrayLength = indexArrayLength;
            this.Gl = gl;
        }

        public static unsafe GlCube CreateCubeWithFaceColors(GL Gl,
            float[] face1Color, float[] face2Color, float[] face3Color,
            float[] face4Color, float[] face5Color, float[] face6Color)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            // poz + normalvekor (x,y,z, nx,ny,nz)
            float[] vertexArray = new float[] {
                // top (+Y)
                -0.5f, 0.5f, 0.5f,   0,1,0,
                 0.5f, 0.5f, 0.5f,   0,1,0,
                 0.5f, 0.5f,-0.5f,   0,1,0,
                -0.5f, 0.5f,-0.5f,   0,1,0,
                // front (+Z)
                -0.5f, 0.5f, 0.5f,   0,0,1,
                -0.5f,-0.5f, 0.5f,   0,0,1,
                 0.5f,-0.5f, 0.5f,   0,0,1,
                 0.5f, 0.5f, 0.5f,   0,0,1,
                // left (-X)
                -0.5f, 0.5f, 0.5f,  -1,0,0,
                -0.5f, 0.5f,-0.5f,  -1,0,0,
                -0.5f,-0.5f,-0.5f,  -1,0,0,
                -0.5f,-0.5f, 0.5f,  -1,0,0,
                // bottom (-Y)
                -0.5f,-0.5f, 0.5f,   0,-1,0,
                 0.5f,-0.5f, 0.5f,   0,-1,0,
                 0.5f,-0.5f,-0.5f,   0,-1,0,
                -0.5f,-0.5f,-0.5f,   0,-1,0,
                // back (-Z)
                 0.5f, 0.5f,-0.5f,   0,0,-1,
                -0.5f, 0.5f,-0.5f,   0,0,-1,
                -0.5f,-0.5f,-0.5f,   0,0,-1,
                 0.5f,-0.5f,-0.5f,   0,0,-1,
                // right (+X)
                 0.5f, 0.5f, 0.5f,   1,0,0,
                 0.5f, 0.5f,-0.5f,   1,0,0,
                 0.5f,-0.5f,-0.5f,   1,0,0,
                 0.5f,-0.5f, 0.5f,   1,0,0,
            };

            List<float> colorsList = new List<float>();
            colorsList.AddRange(face1Color); colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color); colorsList.AddRange(face1Color);

            colorsList.AddRange(face2Color); colorsList.AddRange(face2Color);
            colorsList.AddRange(face2Color); colorsList.AddRange(face2Color);

            colorsList.AddRange(face3Color); colorsList.AddRange(face3Color);
            colorsList.AddRange(face3Color); colorsList.AddRange(face3Color);

            colorsList.AddRange(face4Color); colorsList.AddRange(face4Color);
            colorsList.AddRange(face4Color); colorsList.AddRange(face4Color);

            colorsList.AddRange(face5Color); colorsList.AddRange(face5Color);
            colorsList.AddRange(face5Color); colorsList.AddRange(face5Color);

            colorsList.AddRange(face6Color); colorsList.AddRange(face6Color);
            colorsList.AddRange(face6Color); colorsList.AddRange(face6Color);

            float[] colorArray = colorsList.ToArray();

            uint[] indexArray = new uint[] {
                0, 1, 2,   0, 2, 3,
                4, 5, 6,   4, 6, 7,
                8, 9, 10,  10, 11, 8,
                12, 14, 13, 12, 15, 14,
                17, 16, 19, 17, 19, 18,
                20, 22, 21, 20, 23, 22
            };

            if (!sharedInitialised)
            {
                sharedVertices = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ArrayBuffer, sharedVertices);
                Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);

                sharedIndices = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ElementArrayBuffer, sharedIndices);
                Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);

                sharedInitialised = true;
            }

            // location=0: pozíció, location=2: normálvektor
            uint stride = (3 + 3) * sizeof(float);
            Gl.BindBuffer(GLEnum.ArrayBuffer, sharedVertices);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
            Gl.EnableVertexAttribArray(2);

            // location=1: szín
            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            Gl.BindBuffer(GLEnum.ElementArrayBuffer, sharedIndices);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            Gl.BindVertexArray(0);

            return new GlCube(vao, colors, (uint)indexArray.Length, Gl);
        }

        internal void ReleaseGlCube()
        {
            Gl.DeleteBuffer(Colors);
            Gl.DeleteVertexArray(Vao);
        }

        internal static void ReleaseSharedGeometry(GL Gl)
        {
            if (!sharedInitialised) return;
            Gl.DeleteBuffer(sharedVertices);
            Gl.DeleteBuffer(sharedIndices);
            sharedInitialised = false;
        }
    }
}