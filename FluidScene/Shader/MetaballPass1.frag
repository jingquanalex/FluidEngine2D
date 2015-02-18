#version 330

in vec2 passTexcoord;
in vec4 passColor;
out vec4 outColor;

void main(void)
{
	float dist = distance(passTexcoord, vec2(0.5, 0.5));
	float i = (1.0 - smoothstep(0.0, 0.5, dist)) * 0.3;
	vec4 color = vec4(passColor.rgb * passColor.a, i);

	outColor = color;
}