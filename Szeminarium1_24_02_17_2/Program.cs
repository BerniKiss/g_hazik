using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System.Numerics;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();
        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;
        private static GL Gl;
        private static uint program;
        private static ImGuiController imGuiController;

        private static GlCube[,,] rubikCubies = new GlCube[3, 3, 3];

        private const float CubieSize = 1.0f;
        private const float Gap = 0.05f;
        private const float Step = CubieSize + Gap;

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string SliceRotationMatrixVariableName = "uSliceRotation";
        private const string NormalMatrixVariableName = "uNormal";
        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";
        private const string ShinenessVariableName = "uShininess";

        // Phong paraméterek
        private static Vector3 ambientStrength = new Vector3(0.3f, 0.3f, 0.3f);
        private static Vector3 diffuseStrength = new Vector3(0.7f, 0.7f, 0.7f);
        private static Vector3 specularStrength = new Vector3(0.5f, 0.5f, 0.5f);
        private static Vector3 lightColor = new Vector3(1f, 1f, 1f);
        private static Vector3 lightPosition = new Vector3(3f, 5f, 3f);
        private static float shininess = 32f;

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNormal;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform mat4 uSliceRotation;
        uniform mat3 uNormal;

        out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPosition;

        void main()
        {
            outCol = vCol;
            outNormal = uNormal * vNormal;
            vec4 worldPos = uSliceRotation * uModel * vec4(vPos, 1.0);
            outWorldPosition = vec3(worldPos);
            gl_Position = uProjection * uView * worldPos;
        }
        ";

        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;

        in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;

        uniform vec3 uLightColor;
        uniform vec3 uLightPos;
        uniform vec3 uViewPos;
        uniform float uShininess;
        uniform vec3 ambientStrength;
        uniform vec3 diffuseStrength;
        uniform vec3 specularStrength;

        void main()
        {
            vec3 ambient = ambientStrength * uLightColor;

            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(uLightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * uLightColor * diffuseStrength;

            vec3 viewDir = normalize(uViewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);
            vec3 specular = spec * uLightColor * specularStrength;

            vec3 result = (ambient + diffuse + specular) * outCol.rgb;
            FragColor = vec4(result, outCol.w);
        }
        ";

        private static readonly float[] ColTop = [1.00f, 0.08f, 0.58f, 1.0f];
        private static readonly float[] ColBottom = [0.56f, 0.00f, 1.00f, 1.0f];
        private static readonly float[] ColFront = [0.93f, 0.51f, 0.93f, 1.0f];
        private static readonly float[] ColBack = [0.29f, 0.00f, 0.51f, 1.0f];
        private static readonly float[] ColLeft = [1.00f, 0.41f, 0.71f, 1.0f];
        private static readonly float[] ColRight = [0.80f, 0.00f, 0.80f, 1.0f];
        private static readonly float[] ColInner = [0.15f, 0.15f, 0.15f, 1.0f];

        static void Main(string[] args)
        {
            var windowOptions = WindowOptions.Default;
            windowOptions.Title = "2 szeminárium";
            windowOptions.Size = new Vector2D<int>(600, 600);
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
            IInputContext inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
                keyboard.KeyDown += Keyboard_KeyDown;

            Gl = window.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.White);

            imGuiController = new ImGuiController(Gl, window, inputContext);

            SetUpObjects();
            LinkProgram();

            Gl.Enable(EnableCap.CullFace);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            window.FramebufferResize += s =>
            {
                Gl.Viewport(s);
            };
        }

        private static void SetUpObjects()
        {
            for (int xi = 0; xi < 3; xi++)
                for (int yi = 0; yi < 3; yi++)
                    for (int zi = 0; zi < 3; zi++)
                    {
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
                throw new Exception("Vertex shader failed: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed: " + Gl.GetShaderInfoLog(fshader));

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static void Keyboard_KeyDown(IKeyboard kb, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Up: cameraDescriptor.MoveForward(); break;
                case Key.Down: cameraDescriptor.MoveBackward(); break;
                case Key.Left: cameraDescriptor.StrafeLeft(); break;
                case Key.Right: cameraDescriptor.StrafeRight(); break;
                case Key.A: cameraDescriptor.TurnLeft(); break;
                case Key.D: cameraDescriptor.TurnRight(); break;
                case Key.W: cameraDescriptor.TiltUp(); break;
                case Key.S: cameraDescriptor.TiltDown(); break;
                case Key.Q: cameraDescriptor.MoveUp(); break;
                case Key.E: cameraDescriptor.MoveDown(); break;
                case Key.Space: cubeArrangementModel.StartRotationPositive(); break;
                case Key.Backspace: cubeArrangementModel.StartRotationNegative(); break;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            cubeArrangementModel.AdvanceTime(deltaTime);
            imGuiController.Update((float)deltaTime);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.UseProgram(program);

            // Phong uniformok
            SetUniform3("ambientStrength", ambientStrength);
            SetUniform3("diffuseStrength", diffuseStrength);
            SetUniform3("specularStrength", specularStrength);
            SetUniform3(LightColorVariableName, lightColor);
            SetUniform3(LightPositionVariableName, lightPosition);
            SetUniform1(ShinenessVariableName, shininess);

            var camPos = cameraDescriptor.Position;
            SetUniform3(ViewPositionVariableName, new Vector3(camPos.X, camPos.Y, camPos.Z));

            SetViewMatrix();
            SetProjectionMatrix();
            DrawRubikCube();

            ImGuiNET.ImGui.SetNextWindowPos(
            new System.Numerics.Vector2(10, 20),
            ImGuiNET.ImGuiCond.Once);

            // ImGui UI
            ImGuiNET.ImGui.Begin("Lighting Controls",
                ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize | ImGuiNET.ImGuiWindowFlags.NoCollapse);

            ImGuiNET.ImGui.SeparatorText("Megvilagitas szine");
            ImGuiNET.ImGui.SliderFloat("Light R", ref lightColor.X, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat("Light G", ref lightColor.Y, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat("Light B", ref lightColor.Z, 0f, 1f);

            ImGuiNET.ImGui.SeparatorText("Fenyforras poz");
            ImGuiNET.ImGui.InputFloat("Pos X", ref lightPosition.X, 0.1f);
            ImGuiNET.ImGui.InputFloat("Pos Y", ref lightPosition.Y, 0.1f);
            ImGuiNET.ImGui.InputFloat("Pos Z", ref lightPosition.Z, 0.1f);

            ImGuiNET.ImGui.SeparatorText("Kamera forgatas");
            if (ImGuiNET.ImGui.Button("Bal")) cameraDescriptor.TurnLeft();
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Jobb")) cameraDescriptor.TurnRight();
            if (ImGuiNET.ImGui.Button("Fel")) cameraDescriptor.TiltUp();
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Le")) cameraDescriptor.TiltDown();

            ImGuiNET.ImGui.End();

            imGuiController.Render();
        }

        private static unsafe void DrawRubikCube()
        {
            Matrix4X4<float> topSliceRot = cubeArrangementModel.TopSliceRotationMatrix;
            Matrix4X4<float> identity = Matrix4X4<float>.Identity;

            for (int xi = 0; xi < 3; xi++)
                for (int yi = 0; yi < 3; yi++)
                    for (int zi = 0; zi < 3; zi++)
                    {
                        float tx = (xi - 1) * Step;
                        float ty = (yi - 1) * Step;
                        float tz = (zi - 1) * Step;

                        var model = Matrix4X4.CreateTranslation(tx, ty, tz);
                        SetModelMatrix(model);

                        var sliceRot = cubeArrangementModel.IsOnTopSlice(xi, yi, zi)
                            ? topSliceRot : identity;
                        SetSliceRotationMatrix(sliceRot);

                        var cubie = rubikCubies[xi, yi, zi];
                        Gl.BindVertexArray(cubie.Vao);
                        Gl.DrawElements(GLEnum.Triangles, cubie.IndexArrayLength, GLEnum.UnsignedInt, null);
                        Gl.BindVertexArray(0);
                    }
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> m)
        {
            int loc = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (loc == -1) throw new Exception($"{ModelMatrixVariableName} uniform not found.");
            Gl.UniformMatrix4(loc, 1, false, (float*)&m);

            // norm matrix
            int nLoc = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (nLoc != -1)
            {
                Matrix4X4.Invert(m, out var inv);
                var normalMat = new Matrix3X3<float>(Matrix4X4.Transpose(inv));
                Gl.UniformMatrix3(nLoc, 1, false, (float*)&normalMat);
            }
            CheckError();
        }

        private static unsafe void SetSliceRotationMatrix(Matrix4X4<float> m)
        {
            int loc = Gl.GetUniformLocation(program, SliceRotationMatrixVariableName);
            if (loc == -1) throw new Exception($"{SliceRotationMatrixVariableName} uniform not found.");
            Gl.UniformMatrix4(loc, 1, false, (float*)&m);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var view = Matrix4X4.CreateLookAt(
                cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            int loc = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            if (loc == -1) throw new Exception($"{ViewMatrixVariableName} uniform not found.");
            Gl.UniformMatrix4(loc, 1, false, (float*)&view);
            CheckError();
        }

        private static unsafe void SetProjectionMatrix()
        {



            float aspectRatio = (float)window.Size.X / (float)window.Size.Y;

            var proj = Matrix4X4.CreatePerspectiveFieldOfView<float>(
                (float)Math.PI / 4f, aspectRatio, 0.1f, 100f);
            int loc = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);
            if (loc == -1) throw new Exception($"{ProjectionMatrixVariableName} uniform not found.");
            Gl.UniformMatrix4(loc, 1, false, (float*)&proj);
            CheckError();
        }

        private static unsafe void SetUniform1(string name, float val)
        {
            int loc = Gl.GetUniformLocation(program, name);
            if (loc == -1) throw new Exception($"{name} uniform not found.");
            Gl.Uniform1(loc, val);
            CheckError();
        }

        private static unsafe void SetUniform3(string name, Vector3 val)
        {
            int loc = Gl.GetUniformLocation(program, name);
            if (loc == -1) throw new Exception($"{name} uniform not found.");
            Gl.Uniform3(loc, val);
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