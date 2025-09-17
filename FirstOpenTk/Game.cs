using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace FirstOpenTk
{
    public class Game : GameWindow
    {
        private int vertexBufferHandle;
        private int shaderProgramHandle;
        private int vertexArrayHandle;
        
        private float rotationAngle, scaleFactor;
        private bool scalingUp;
        
        private int modelLoc, viewLoc, projLoc;
        
        Vector3 a = new Vector3(1, 2, 3);
        Vector3 b = new Vector3(4, 5, 6);
        
        public Game() : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.CenterWindow(new Vector2i(1280, 768));

            Console.WriteLine($"Vector A: {a}");
            Console.WriteLine($"Vector B: {b}");
            
            // Addition
            Console.WriteLine($"Addition: {a + b}");
            
            // Subtraction
            Console.WriteLine($"Subtraction: {a - b}");
            
            // Dot
            Console.WriteLine($"Dot Product: {Vector3.Dot(a, b)}");

            // Cross
            Console.WriteLine($"Cross Product:  {Vector3.Cross(a, b)}");
            
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            // Update the OpenGL viewport to match the new window dimensions
            GL.Viewport(0, 0, e.Width, e.Height);
            base.OnResize(e);
        }
        
        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(new Color4(0.318f, 0.592f,0.804f,1.0f));

            float[] vertices = new float[]
            {
                // First triangle
                0.0f,  0.5f, 0.0f, // top-left
                -0.5f, -0.5f, 0.0f, // bottom-left
                0.5f,  -0.5f, 0.0f, // bottom-right
            };
            
            vertexBufferHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float),vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            vertexArrayHandle = GL.GenVertexArray();
            GL.BindVertexArray(vertexArrayHandle);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexArrayHandle);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            // Vertex shader with model, view, projection matrices
            string vertexShaderCode = @"
                #version 330 core
                layout(location = 0) in vec3 aPosition;

                uniform mat4 uModel;
                uniform mat4 uView;
                uniform mat4 uProj;

                void main()
                {
                    gl_Position = uProj * uView * uModel * vec4(aPosition, 1.0);
                }
            ";

            string fragmentShaderCode = @"
                #version 330 core
                out vec4 FragColor;

                void main()
                {
                    FragColor = vec4(0.6, 0.2, 0.8, 1.0);
                }
            ";
            
            // Compile shaders
            int vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShaderHandle, vertexShaderCode);
            GL.CompileShader(vertexShaderHandle);
            CheckShaderCompile(vertexShaderHandle, "Vertex Shader");

            int fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShaderHandle, fragmentShaderCode);
            GL.CompileShader(fragmentShaderHandle);
            CheckShaderCompile(fragmentShaderHandle, "Fragment Shader");

            // Create shader program and link shaders
            shaderProgramHandle = GL.CreateProgram();
            GL.AttachShader(shaderProgramHandle, vertexShaderHandle);
            GL.AttachShader(shaderProgramHandle, fragmentShaderHandle);
            GL.LinkProgram(shaderProgramHandle);

            // Cleanup shaders after linking (no longer needed individually)
            GL.DetachShader(shaderProgramHandle, vertexShaderHandle);
            GL.DetachShader(shaderProgramHandle, fragmentShaderHandle);
            GL.DeleteShader(vertexShaderHandle);
            GL.DeleteShader(fragmentShaderHandle);
            
            // Get uniform locations
            modelLoc = GL.GetUniformLocation(shaderProgramHandle, "uModel");
            viewLoc = GL.GetUniformLocation(shaderProgramHandle, "uView");
            projLoc = GL.GetUniformLocation(shaderProgramHandle, "uProj");
            
            // Initialize transformation array
            rotationAngle =  0.5f; // stagger initial rotations
            scaleFactor = 1f;
            scalingUp = true;
            
        }

        protected override void OnUnload()
        {
            // Unbind and delete buffers and shader program
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(vertexBufferHandle);

            GL.BindVertexArray(0);
            GL.DeleteVertexArray(vertexArrayHandle);

            GL.UseProgram(0);
            GL.DeleteProgram(shaderProgramHandle);

            base.OnUnload();
        }


        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            
            // Rotate continuously
            rotationAngle+= (float)args.Time; // different speed for each triangle

            // Oscillating scale between 0.5 and 1.5
            if (scalingUp)
            {
                scaleFactor += (float)args.Time;
                if (scaleFactor >= 1.5f) scalingUp = false;
            }
            else
            {
                scaleFactor -= (float)args.Time;
                if (scaleFactor <= 0.5f) scalingUp = true;
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            // Clear the screen with background color
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // Use our shader program
            GL.UseProgram(shaderProgramHandle);

            // View matrix (camera looking at origin)
            Matrix4 view = Matrix4.LookAt(
                new Vector3(0, 0, 5),
                Vector3.Zero,
                Vector3.UnitY);

            // Projection matrix (perspective)
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(60f),
                (float)Size.X / Size.Y,
                0.1f,
                100f
            );

            // Send view and projection to shader (same for all triangles)
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref projection);
            
            
            // Bind the VAO
            GL.BindVertexArray(vertexArrayHandle);
            
            // Rotation quaternion for this triangle
            Quaternion rotation = Quaternion.FromAxisAngle(Vector3.UnitY, rotationAngle);
            Matrix4 rotationMatrix = Matrix4.CreateFromQuaternion(rotation);

            // Scaling
            Matrix4 scaleMatrix = Matrix4.CreateScale(scaleFactor);

            // Translation: spread triangles along X axis
            Matrix4 translationMatrix = Matrix4.CreateTranslation(-2f + 1 * 2f, 0f, 0f);

            // Combine transformations: Model = Translation * Rotation * Scale
            Matrix4 model = scaleMatrix * rotationMatrix * translationMatrix;

            // Send model matrix to shader
            GL.UniformMatrix4(modelLoc, false, ref model);
            
            // draw the triangle
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            GL.BindVertexArray(0);

            // Display the rendered frame
            SwapBuffers();
        }
        
        // Helper function to check for shader compilation errors
        private void CheckShaderCompile(int shaderHandle, string shaderName)
        {
            GL.GetShader(shaderHandle, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shaderHandle);
                Console.WriteLine($"Error compiling {shaderName}: {infoLog}");
            }
        }
    }
}