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
    float t=float(range)/2;
    float weight[11];
    float weightAll=0.0f;
    for(int i=-range;i<=range;i++){
        weight[i+range]=exp(-i*i/(2*t*t))/t;
        weightAll+=weight[i+range];
    }
    float mean=0;
    float variance=0;
    float mean_second=0;
    float variance_second=0;
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    // perform perspective divide
    vec3 projCoords = FragPosLightSpace.xyz / FragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;
    if(type==0)//x_direction
    {
        for(int i=-range;i<=range;i++)
        {
            float depth=texture(shadowMap,projCoords.xy+i*vec2(1,0)*texelSize).x;
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
            float depthx=texture(shadowMap,projCoords.xy+i*vec2(0,1)*texelSize).x;
            float depthy=texture(shadowMap,projCoords.xy+i*vec2(0,1)*texelSize).y;
            float vx = texture(shadowMap,projCoords.xy+i*vec2(0,1)*texelSize).z;
            float vy = texture(shadowMap,projCoords.xy+i*vec2(0,1)*texelSize).w;
            float x=exp(depthx*pos_power);
            float y=exp(depthy*pos_power);
            float z = exp(-vx*neg_power);
            float w = exp(-vy*neg_power);
            mean+=x*weight[i+range];
            variance+=y*weight[i+range];
            mean_second+=z*weight[i+range];
            variance_second+=w*weight[i+range];
        }
    }
    mean/=weightAll;
    variance/=weightAll;
    mean_second/=weightAll;
    variance_second/=weightAll;
    mean=log(mean)/pos_power;
    variance=log(variance)/pos_power;
    mean_second=log(mean_second)/-neg_power;
    variance_second=log(variance_second)/-neg_power;
    FragColor = vec4(mean,variance,mean_second,variance_second);
}