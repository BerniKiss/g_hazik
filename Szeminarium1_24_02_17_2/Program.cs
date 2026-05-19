using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();
        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;
        private static GL Gl;
        private static uint program;

        // 27 kicsi kocka
        private static GlCube[,,] rubikCubies = new GlCube[3, 3, 3];

        private const float CubieSize = 1.0f;
        private const float Gap = 0.05f;
        private const float Step = CubieSize + Gap;

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string SliceRotationMatrixVariableName = "uSliceRotation";

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec4 vCol;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform mat4 uSliceRotation;

        out vec4 outCol;

        void main()
        {
            outCol = vCol;
            gl_Position = uProjection * uView * uSliceRotation * uModel * vec4(vPos, 1.0);
        }
        ";

        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
        in  vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";

        // a szinek
        private static readonly float[] ColTop = [1.00f, 0.08f, 0.58f, 1.0f]; // sotetebb lila    
        private static readonly float[] ColBottom = [0.56f, 0.00f, 1.00f, 1.0f]; // sotetebb violet
        private static readonly float[] ColFront = [0.93f, 0.51f, 0.93f, 1.0f]; // violet     
        private static readonly float[] ColBack = [0.29f, 0.00f, 0.51f, 1.0f]; // sotet lila (-Z)
        private static readonly float[] ColLeft = [1.00f, 0.41f, 0.71f, 1.0f]; // rozsaszin
        private static readonly float[] ColRight = [0.80f, 0.00f, 0.80f, 1.0f]; // magenta  
        // belso / rejtett lapok
        private static readonly float[] ColInner = [0.15f, 0.15f, 0.15f, 1.0f]; // sotet szurke

        static void Main(string[] args)
        {
            var windowOptions = WindowOptions.Default;
            windowOptions.Title = "2 szeminárium";
            windowOptions.Size = new Vector2D<int>(600, 600);

            // on some systems there is no depth buffer by default, so we need to make sure one is created
            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;
            window.Run();
        }

        private static void Window_Load()
        {
            // set up input handling
            IInputContext inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.White);

            SetUpObjects();

            LinkProgram();

            Gl.Enable(EnableCap.CullFace);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        // mind a 27 kis kocka es hozza szinek
        private static void SetUpObjects()
        {
            for (int xi = 0; xi < 3; xi++)
                for (int yi = 0; yi < 3; yi++)
                    for (int zi = 0; zi < 3; zi++)
                    {
                        // sorrend: top(+Y) front(+Z) left(-X) bottom(-Y) back(-Z) right(+X)
                        float[] fTop = (yi == 2) ? ColTop : ColInner;
                        float[] fFront = (zi == 2) ? ColFront : ColInner;
                        float[] fLeft = (xi == 0) ? ColLeft : ColInner;
                        float[] fBottom = (yi == 0) ? ColBottom : ColInner;
                        float[] fBack = (zi == 0) ? ColBack : ColInner;
                        float[] fRight = (xi == 2) ? ColRight : ColInner;

                        rubikCubies[xi, yi, zi] = GlCube.CreateCubeWithFaceColors(
                            Gl, fTop, fFront, fLeft, fBottom, fBack, fRight);
                    }
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static void Keyboard_KeyDown(IKeyboard kb, Key key, int arg3)
        {
            switch (key)
            {
                // kamera mozgas - nyilak
                case Key.Up:
                    cameraDescriptor.MoveForward();
                    break;
                case Key.Down:
                    cameraDescriptor.MoveBackward();
                    break;
                case Key.Left:
                    cameraDescriptor.StrafeLeft();
                    break;
                case Key.Right:
                    cameraDescriptor.StrafeRight();
                    break;

                // kamera fordulas - WASD
                case Key.A:
                    cameraDescriptor.TurnLeft();
                    break;
                case Key.D:
                    cameraDescriptor.TurnRight();
                    break;
                case Key.W:
                    cameraDescriptor.TiltUp();
                    break;
                case Key.S:
                    cameraDescriptor.TiltDown();
                    break;

                // fel-le mozgas - Q es E
                case Key.Q:
                    cameraDescriptor.MoveUp();
                    break;
                case Key.E:
                    cameraDescriptor.MoveDown();
                    break;

                // lap forgatas - Space es Backspace
                case Key.Space:
                    cubeArrangementModel.StartRotationPositive();
                    Console.WriteLine("SPACE: " + cubeArrangementModel.TopSliceAngle);
                    break;
                case Key.Backspace:
                    cubeArrangementModel.StartRotationNegative();
                    break;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            cubeArrangementModel.AdvanceTime(deltaTime);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            DrawRubikCube();
        }

        private static unsafe void DrawRubikCube()
        {
            // a felso lap forgatasat egyszerre szamoljuk ki
            Matrix4X4<float> topSliceRot = cubeArrangementModel.TopSliceRotationMatrix;
            Matrix4X4<float> identity = Matrix4X4<float>.Identity;

            for (int xi = 0; xi < 3; xi++)
                for (int yi = 0; yi < 3; yi++)
                    for (int zi = 0; zi < 3; zi++)
                    {
                        float tx = (xi - 1) * Step;
                        float ty = (yi - 1) * Step;
                        float tz = (zi - 1) * Step;

                        // saját model mátrix: a kocka helyere tolja
                        var model = Matrix4X4.CreateTranslation(tx, ty, tz);

                        SetModelMatrix(model);

                        // ha a felso lapon van, a lap forgatasat kapja, kulonben egysegmatrixot
                        if (cubeArrangementModel.IsOnTopSlice(xi, yi, zi))
                            SetSliceRotationMatrix(topSliceRot);
                        else
                            SetSliceRotationMatrix(identity);

                        var cubie = rubikCubies[xi, yi, zi];
                        Gl.BindVertexArray(cubie.Vao);
                        Gl.DrawElements(GLEnum.Triangles, cubie.IndexArrayLength, GLEnum.UnsignedInt, null);
                        Gl.BindVertexArray(0);
                    }
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> m)
        {
            int loc = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (loc == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found.");
            }

            Gl.UniformMatrix4(loc, 1, false, (float*)&m);
            CheckError();
        }

        private static unsafe void SetSliceRotationMatrix(Matrix4X4<float> m)
        {
            int loc = Gl.GetUniformLocation(program, SliceRotationMatrixVariableName);
            if (loc == -1)
            {
                throw new Exception($"{SliceRotationMatrixVariableName} uniform not found.");
            }

            Gl.UniformMatrix4(loc, 1, false, (float*)&m);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var view = Matrix4X4.CreateLookAt(
                cameraDescriptor.Position,
                cameraDescriptor.Target,
                cameraDescriptor.UpVector);
            int loc = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            if (loc == -1)
                throw new Exception($"{ViewMatrixVariableName} uniform not found.");
            Gl.UniformMatrix4(loc, 1, false, (float*)&view);
            CheckError();
        }

        private static unsafe void SetProjectionMatrix()
        {
            var proj = Matrix4X4.CreatePerspectiveFieldOfView<float>(
                (float)Math.PI / 4f, 1f, 0.1f, 100f);
            int loc = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);
            if (loc == -1)
                throw new Exception($"{ProjectionMatrixVariableName} uniform not found.");
            Gl.UniformMatrix4(loc, 1, false, (float*)&proj);
            CheckError();
        }

        private static void Window_Closing()
        {
            for (int xi = 0; xi < 3; xi++)
                for (int yi = 0; yi < 3; yi++)
                    for (int zi = 0; zi < 3; zi++)
                        rubikCubies[xi, yi, zi].ReleaseGlCube();

            GlCube.ReleaseSharedGeometry(Gl);
        }

        public static void CheckError()
        {
            var err = (ErrorCode)Gl.GetError();
            if (err != ErrorCode.NoError)
                throw new Exception("GL error: " + err);
        }
    }
}