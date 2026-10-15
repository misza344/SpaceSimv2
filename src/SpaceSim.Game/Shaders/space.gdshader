shader_type sky;
render_mode use_debanding;
uniform vec4 sector_tint : source_color = vec4(1.0);
float hash(vec3 p) { p = fract(p * 0.3183099 + vec3(.1,.2,.3)); p *= 17.0; return fract(p.x*p.y*p.z*(p.x+p.y+p.z)); }
float noise(vec3 p) {
 vec3 i=floor(p), f=fract(p); f=f*f*(3.0-2.0*f);
 return mix(mix(mix(hash(i),hash(i+vec3(1,0,0)),f.x),mix(hash(i+vec3(0,1,0)),hash(i+vec3(1,1,0)),f.x),f.y),mix(mix(hash(i+vec3(0,0,1)),hash(i+vec3(1,0,1)),f.x),mix(hash(i+vec3(0,1,1)),hash(i+vec3(1,1,1)),f.x),f.y),f.z);
}
void sky() {
 vec3 d=normalize(EYEDIR);
 float n=noise(d*5.0)*.5+noise(d*11.0)*.27+noise(d*23.0)*.13;
 float band=pow(max(0.0,1.0-abs(d.y*.9+d.x*.32+.15)),7.0);
 vec3 nebula=mix(vec3(.028,.055,.09),vec3(.15,.057,.125),noise(d*3.0+9.0));
 COLOR=vec3(.006,.01,.024)+nebula*sector_tint.rgb*pow(n,2.0)*band*6.0;
 vec2 grid=SKY_COORDS*vec2(1500.0,750.0);
 vec2 cell=floor(grid); vec2 f=fract(grid)-.5;
 float seed=hash(vec3(cell,1.0));
 float star=(1.0-smoothstep(.025,.095,length(f)))*step(.979,seed);
 float bright=hash(vec3(cell,3.0));
 float sun_dot=max(dot(d,normalize(vec3(-.48,.38,-.78))),0.0);
 COLOR+=vec3(1.0,.8,.52)*(pow(sun_dot,1500.0)*3.0+pow(sun_dot,90.0)*.07);
 COLOR+=mix(vec3(.55,.7,1.0),vec3(1.0,.84,.63),bright)*star*(.7+bright*2.0);
}
