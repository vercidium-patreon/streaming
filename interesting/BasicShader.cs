namespace streaming;

public static class BasicShader
{
    public static ShaderProgram shader;
    public static bool Active => shader?.Active ?? false;

    public static void Initialise()
    {
        shader = new(VertexShader, FragmentShader);

        mvp = new(shader, "mvp");
        groundTexture = new(shader, "groundTexture");
    }


    public static void UseProgram()
    {
        if (shader == null)
            Initialise();

        shader.UseProgram();
    }

    public static ShaderValue mvp;
    public static ShaderValue groundTexture;

    public static string FragmentShader = @"
out vec4 gColor;

in vec2 vUV;

uniform sampler2D groundTexture;

void main()
{
    gColor = texture(groundTexture, vUV);
}
";

    public static string VertexShader = @"
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aUV;

out vec2 vUV;

uniform mat4 mvp;

void main()
{ 
    vUV = aUV;

    gl_Position = mvp * vec4(aPosition, 1.0);
}
";
}
