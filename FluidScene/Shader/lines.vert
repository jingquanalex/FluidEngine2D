#version 330

layout(location = 0) in vec2 inPosition;
layout(location = 1) in vec4 inColor;
out vec4 passColor;

uniform mat4 matProjection;
uniform mat4 matView;

void main(void)
{
	gl_Position = matProjection * matView * vec4(inPosition, 0.0, 1.0);
	passColor = inColor;
}