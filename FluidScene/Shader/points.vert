#version 330

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec2 inTexcoord;
layout(location = 2) in mat4 inModelMatrix;
layout(location = 6) in vec4 inColor;
out vec2 passTexcoord;
out vec4 passColor;

uniform mat4 matProjection;
uniform mat4 matView;

void main(void)
{
	gl_Position = matProjection * matView * inModelMatrix * vec4(inPosition, 1.0);
	passTexcoord = inTexcoord;
	passColor = inColor;
}