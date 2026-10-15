using Godot;

/// <summary>Walkable cabin in ship-local coordinates; bounds keep the pilot behind the window.</summary>
public static class Cabin
{
    public static readonly Vector3 Seat = new(0,.65f,-2.5f);
    public static Vector3 Clamp(Vector3 position) => new(Mathf.Clamp(position.X,-1.25f,1.25f),.65f,Mathf.Clamp(position.Z,-1.7f,2.5f));
    public static Node3D Build(Node3D parent)
    {
        var root=new Node3D {Name="WalkableCabin"};parent.AddChild(root);
        var hull=SpaceGeometry.Material("465462",.4f,.6f);
        var dark=SpaceGeometry.Material("15212b",.3f,.65f);
        var lights=SpaceGeometry.Material("8ddde6",0,.4f,1);
        SpaceGeometry.Box(root,new(3.2f,.15f,7),new(0,-1,0),dark);
        SpaceGeometry.Box(root,new(3.2f,.65f,.15f),new(0,-.65f,-3.5f),dark);
        SpaceGeometry.Box(root,new(3.2f,.14f,.15f),new(0,1.25f,-3.5f),hull);
        var glass=SpaceGeometry.Material("346575",.1f,.2f);
        glass.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;glass.AlbedoColor=new Color(.12f,.3f,.38f,.08f);
        SpaceGeometry.Box(root,new(3,1.55f,.02f),new(0,.45f,-3.5f),glass);
        var screen=new ShaderMaterial {Shader=GD.Load<Shader>("res://Shaders/instrument.gdshader")};
        foreach(int side in new[]{-1,1})
        {
            SpaceGeometry.Box(root,new(.7f,.6f,.6f),new(side*1,-.5f,-3),dark);
            var display=SpaceGeometry.Box(root,new(.6f,.025f,.4f),new(side*1,-.18f,-3),screen);
            display.RotationDegrees=new(18,0,0);
        }
        SpaceGeometry.Box(root,new(3.2f,.15f,7),new(0,1.4f,0),hull);
        SpaceGeometry.Box(root,new(3.2f,2.4f,.12f),new(0,.2f,3.5f),hull);
        foreach(int side in new[]{-1,1})
        {
            SpaceGeometry.Box(root,new(.12f,2.4f,7),new(side*1.6f,.2f,0),hull);
            SpaceGeometry.Box(root,new(.04f,.06f,6.3f),new(side*1.52f,1.2f,0),lights);
            SpaceGeometry.Box(root,new(.3f,.65f,1.3f),new(side*1.35f,-.55f,-2.5f),dark);
            SpaceGeometry.Box(root,new(.8f,1.5f,.35f),new(side*.85f,-.15f,3.15f),dark);
            for(int i=0;i<4;i++) SpaceGeometry.Box(root,new(.65f,.03f,.03f),new(side*.85f,-.65f+i*.3f,2.95f),lights);
        }
        SpaceGeometry.Box(root,new(.65f,.16f,.65f),new(0,-.45f,-2.5f),dark);
        SpaceGeometry.Box(root,new(.65f,.75f,.14f),new(0,-.05f,-2.1f),dark);
        SpaceGeometry.Box(root,new(.12f,.65f,.12f),new(0,-.7f,-2.5f),hull);
        root.AddChild(new Label3D {Text="PIONIER  /  KABINA\nF przy fotelu — pilotuj",Position=new(0,.8f,3.35f),RotationDegrees=new(0,180,0),PixelSize=.002f,FontSize=32,Modulate=new Color("8ddde6")});
        root.AddChild(new OmniLight3D {Position=new(0,.9f,0),LightColor=new Color("acd9e5"),LightEnergy=.8f,OmniRange=5,ShadowEnabled=false});
        return root;
    }
}
