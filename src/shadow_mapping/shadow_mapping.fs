#version 330 core
out vec4 FragColor;

in VS_OUT {
    vec3 FragPos;
    vec3 Normal;
    vec2 TexCoords;
    vec4 FragPosLightSpace;
} fs_in;

uniform sampler2D diffuseTexture;
uniform sampler2D shadowMap;

uniform vec3 lightPos;
uniform vec3 viewPos;

uniform float pos_power;
uniform float neg_power;

vec2 warpDepth(float depth)
{
    depth = 2.0f * depth - 1.0f;
    float pos = exp(pos_power * depth);
    float neg = -exp(-neg_power * depth);
    vec2 wDepth = vec2(pos, neg);
    return wDepth;
}

float Chebyshev(vec2 moments, float mean, float minVariance)
{
    float shadow = 1.0f;
    if(mean <= moments.x)
    {
        shadow = 1.0f;
        return shadow;
    }
    else
    {
        float variance = moments.y - (moments.x * moments.x);
        variance = max(variance, minVariance);
        float d = mean - moments.x;
        shadow = variance / (variance + (d * d));
        return shadow;
    }
}

float ShadowCalculation(vec4 fragPosLightSpace)
{

    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;
    // get closest depth value from light's perspective (using [0,1] range fragPosLight as coords)
    float closestDepth = texture(shadowMap, projCoords.xy).r; 
    // get depth of current fragment from light's perspective
    float currentDepth = projCoords.z;
    // calculate bias (based on depth map resolution and slope)
    vec3 normal = normalize(fs_in.Normal);
    vec3 lightDir = normalize(lightPos - fs_in.FragPos);
    float bias = max(0.005 * (1.0 - dot(normal, lightDir)), 0.005);
    
    //ESM
    float Mapdepth=texture(shadowMap, projCoords.xy).x;
    Mapdepth=2.0f*Mapdepth-1.0f;
    Mapdepth=exp(pos_power*Mapdepth);
    currentDepth=2.0f*currentDepth-1.0f;
    currentDepth=exp(-pos_power*currentDepth);
    float shadow=0.0;

    shadow=Mapdepth*currentDepth;
    if (shadow>1.0f)
        shadow=1.0f;
    /*
    vec2 wDepth = warpDepth(currentDepth);
    //EVSM  
    vec4 moments = texture(shadowMap, projCoords.xy).xyzw;
    moments=2.0f*moments-1.0f;
    vec2 posMoments = vec2(exp(pos_power*moments.x), exp(pos_power*2*moments.y));
    vec2 negMoments = vec2(-exp(-neg_power*moments.z), exp(-neg_power*2*moments.w));
    vec2 depthScale = 0.00001f * vec2(pos_power, neg_power) * wDepth;
    vec2 minVariance = depthScale * depthScale;
    float posResult = Chebyshev(posMoments, wDepth.x, minVariance.x);
    float negResult = Chebyshev(negMoments, wDepth.y, minVariance.y);
    float shadow = min(posResult, negResult);
    */


    /*
    //reflect to [Minshadow,1]
    const float Minshadow=0.1;
    if (shadow<Minshadow)
        return 0;
    shadow=Minshadow+(1-Minshadow)*shadow;
    */

    //PCF
    /*
    //float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    for(int x = -1; x <= 1; ++x)
    {
        for(int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(shadowMap, projCoords.xy + vec2(x, y) * texelSize).r; 
            shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0;        
        }    
    }

    shadow /= 9.0;
    */

    // keep the shadow at 0.0 when outside the far_plane region of the light's frustum.
    if(projCoords.z > 1.0)
        shadow = 1.0;
        
    return 1.0-shadow;
}

void main()
{           
    vec3 color = texture(diffuseTexture, fs_in.TexCoords).rgb;
    vec3 normal = normalize(fs_in.Normal);
    vec3 lightColor = vec3(1);
    // ambient
    vec3 ambient = 0.3 * lightColor;
    // diffuse
    vec3 lightDir = normalize(lightPos - fs_in.FragPos);
    float diff = max(dot(lightDir, normal), 0.0);
    vec3 diffuse = diff * lightColor;
    // specular
    vec3 viewDir = normalize(viewPos - fs_in.FragPos);
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = 0.0;
    vec3 halfwayDir = normalize(lightDir + viewDir);  
    spec = pow(max(dot(normal, halfwayDir), 0.0), 64.0);
    vec3 specular = spec * lightColor;    
    // calculate shadow
    float shadow = ShadowCalculation(fs_in.FragPosLightSpace);                      
    vec3 lighting = (ambient + (1.0 - shadow) * (diffuse + specular)) * color;    
    
    FragColor = vec4(lighting, 1.0);
    //FragColor = vec4(shadow);
}