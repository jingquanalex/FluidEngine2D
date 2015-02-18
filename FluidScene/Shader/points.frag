#version 330

in vec2 passTexcoord;
in vec4 passColor;
out vec4 outColor;

void main(void)
{
	float dist = distance(passTexcoord, vec2(0.5, 0.5));
	if (dist > 0.5) discard;

	vec3 color = vec3(1.0 - dist) * passColor.rgb;

	outColor = vec4(color, 1.0);
}