shader_type spatial;
render_mode unshaded, cull_back, blend_add, depth_draw_never;
void fragment() {
 float rim=pow(1.0-max(dot(normalize(NORMAL),normalize(VIEW)),0.0),3.5);
 ALBEDO=vec3(.06,.28,.66);
 ALPHA=rim*.45;
}
