#version 330 core
out vec4 FragColor;

uniform sampler2D shadowMap;
uniform int type;
uniform float pos_power;
uniform float neg_power;

in vec4 FragPosLightSpace;

void main()
{             
    int range =5;
    float t=2.5f;
    float weight[7];
    float weightAll=0.0f;
    for(int i=-range;i<=range;i++){
        weight[i+range]=exp(-i*i/(2*t*t))/t;
        weightAll+=weight[i+range];
    }
    //gl_FragDepth = gl_FragCoord.z;
    float mean=0;
    float variance=0;
    float mean_second=0;
    float variance_second=0;
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    // perform perspective divide
    vec3 projCoords = FragPosLightSpace.xyz / FragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;
    if(type==1)//x_direction
    {
        for(int i=-range;i<=range;i++)
        {
            float depth=texture(shadowMap,projCoords.xy+i*vec2(1,0)*texelSize).x;
            depth=depth*2.0f-1.0f;
            float x=exp(depth*pos_power);
            float y=exp(-depth*neg_power);
            mean+=x*weight[i+range];
            variance+=x*x*weight[i+range];
            mean_second+=y*weight[i+range];
            variance_second+=y*y*weight[i+range];
        }
    }
    else//y_direction
    {
        for(int i=-range;i<=range;i++)
        {
            float depth=texture(shadowMap,projCoords.xy+i*vec2(0,1)*texelSize).x;
            depth=depth*2.0f-1.0f;
            float x=exp(depth*pos_power);
            float y=exp(-depth*neg_power);
            mean+=x*weight[i+range];
            variance+=x*x*weight[i+range];
            mean_second+=y*weight[i+range];
            variance_second+=y*y*weight[i+range];
        }
    }
    mean/=weightAll;
    variance/=weightAll;
    mean_second/=weightAll;
    variance_second/=weightAll;
    mean=log(mean)/pos_power*0.5f+0.5f;
    variance=log(variance)/pos_power*0.25f+0.5f;
    mean_second=log(mean_second)/-neg_power*0.5f+0.5f;
    variance_second=log(variance_second)/-neg_power*0.25f+0.5f;
    FragColor = vec4(mean,variance,mean_second,variance_second);
}