#version 330

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec2 inTexcoord;
out vec2 passTexcoord;

uniform mat4 matProjection;
uniform mat4 matView;
uniform mat4 matModel;

void main(void)
{
	gl_Position = matProjection * matView * matModel * vec4(inPosition, 1.0);
	passTexcoord = inTexcoord;
}