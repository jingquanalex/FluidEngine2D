#version 330

in vec2 passTexcoord;
out vec4 outColor;

uniform sampler2D texture;

void main(void)
{
	vec4 color = texture2D(texture, passTexcoord);
	if (color.a < 0.08) discard;

	outColor = vec4(color.rgb, 1.0);
}