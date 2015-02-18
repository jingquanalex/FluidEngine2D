#version 330

in vec4 passColor;
out vec4 outColor;

void main(void)
{
	vec4 baseColor = vec4(1.0, 0.0, 0.0, 1.0);

	outColor = passColor;
}