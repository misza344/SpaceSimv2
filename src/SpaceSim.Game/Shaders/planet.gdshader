shader_type spatial;
varying vec3 local_direction;
float hash3(vec3 p) { p=fract(p*.3183099+vec3(.1,.2,.3)); p*=17.0; return fract(p.x*p.y*p.z*(p.x+p.y+p.z)); }
float noise3(vec3 p) {
 vec3 i=floor(p), f=fract(p); f=f*f*(3.0-2.0*f);
 return mix(mix(mix(hash3(i),hash3(i+vec3(1,0,0)),f.x),mix(hash3(i+vec3(0,1,0)),hash3(i+vec3(1,1,0)),f.x),f.y),mix(mix(hash3(i+vec3(0,0,1)),hash3(i+vec3(1,0,1)),f.x),mix(hash3(i+vec3(0,1,1)),hash3(i+vec3(1,1,1)),f.x),f.y),f.z);
}
float fbm(vec3 p) { return noise3(p)*.52+noise3(p*2.03)*.26+noise3(p*4.09)*.13+noise3(p*8.2)*.065; }
void vertex() { local_direction=normalize(VERTEX); }
void fragment() {
 vec3 p=normalize(local_direction);
 float terrain=fbm(p*5.0+vec3(8.0,3.0,4.0));
 float land=smoothstep(.48,.52,terrain);
 vec3 ocean=mix(vec3(.004,.026,.065),vec3(.024,.10,.14),smoothstep(.38,.5,terrain));
 vec3 continent=mix(vec3(.025,.068,.048),vec3(.19,.16,.10),smoothstep(.52,.7,terrain));
 float clouds=smoothstep(.50,.67,fbm(p*12.0+vec3(2.0,7.0,4.0)));
 float polar=smoothstep(.83,.98,abs(p.y)+noise3(p*28.0)*.035);
 ALBEDO=mix(mix(mix(ocean,continent,land),vec3(.55,.64,.7),polar),vec3(.65,.72,.78),clouds*.8);
 ROUGHNESS=mix(.4,.95,land);
}
