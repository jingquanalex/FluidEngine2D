#version 330

in vec2 passTexcoord;
out vec4 outColor;

uniform sampler2D texText;

void main(void)
{
	outColor = texture2D(texText, passTexcoord);
}