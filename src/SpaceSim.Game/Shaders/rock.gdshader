shader_type spatial;
varying vec3 local_pos;
float hash(vec3 p) { return fract(sin(dot(p,vec3(127.1,311.7,74.7)))*43758.5453); }
float noise(vec3 p) {
 vec3 i=floor(p), f=fract(p); f=f*f*(3.0-2.0*f);
 return mix(mix(mix(hash(i),hash(i+vec3(1,0,0)),f.x),mix(hash(i+vec3(0,1,0)),hash(i+vec3(1,1,0)),f.x),f.y),mix(mix(hash(i+vec3(0,0,1)),hash(i+vec3(1,0,1)),f.x),mix(hash(i+vec3(0,1,1)),hash(i+vec3(1,1,1)),f.x),f.y),f.z);
}
void vertex() { local_pos=VERTEX; }
void fragment() {
 float n=noise(local_pos*.35)*.6+noise(local_pos*1.4)*.27+noise(local_pos*5.0)*.13;
 float vein=pow(1.0-abs(noise(local_pos*.18)*2.0-1.0),18.0);
 ALBEDO=mix(vec3(.095,.105,.12),vec3(.32,.28,.23),n)*mix(.7,1.15,noise(local_pos*6.0));
 ALBEDO=mix(ALBEDO,vec3(.22,.15,.075),vein*.5);
 ROUGHNESS=.96;
}
