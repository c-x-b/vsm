#version 330 core
out vec4 FragColor;
uniform float pos_power;
uniform float neg_power;

void main()
{             
    //gl_FragDepth = gl_FragCoord.z;
    float depth = gl_FragCoord.z;
    FragColor = vec4(depth);
}