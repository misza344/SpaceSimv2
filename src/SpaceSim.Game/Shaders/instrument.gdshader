shader_type spatial;
render_mode unshaded;
void fragment() {
 vec2 p=UV;
 float edge=step(.05,p.x)*step(p.x,.95)*step(.07,p.y)*step(p.y,.93);
 vec2 grid=abs(fract(p*vec2(12.0,8.0))-.5);
 float lines=(1.0-smoothstep(.015,.035,min(grid.x,grid.y)))*.13;
 float ring=1.0-smoothstep(.008,.018,abs(length((p-vec2(.32,.5))*vec2(1.0,1.35))-.19));
 float bars=step(.63,p.x)*step(p.x,.89)*step(.2,p.y)*step(p.y,.8)*step(.38,fract(p.y*12.0));
 float trace=1.0-smoothstep(.006,.014,abs(p.y-(.5+sin(p.x*19.0)*.06)));
 ALBEDO=(vec3(.008,.022,.03)+vec3(.06,.4,.48)*(lines+ring*.65+bars*.45+trace*.18))*edge;
}
