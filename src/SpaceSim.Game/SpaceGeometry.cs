using Godot;
using SpaceSim.Core;

public static class SpaceGeometry
{
    private static Texture2D? _plating;
    private static Texture2D Plating()
    {
        if (_plating is not null) return _plating;
        using var image=Image.CreateEmpty(256,256,false,Image.Format.Rgb8);
        var random=new Random(703);
        for(int y=0;y<256;y++) for(int x=0;x<256;x++)
        {
            int px=x%128, py=y%64;
            float value=.83f+(float)random.NextDouble()*.09f;
            if(px<2||py<2) value=.36f;
            else if(px==3||py==3) value=.98f;
            if((px==8||px==119)&&(py==8||py==55)) value=.4f;
            image.SetPixel(x,y,new Color(value,value,value));
        }
        image.GenerateMipmaps();
        return _plating=ImageTexture.CreateFromImage(image);
    }

    public static StandardMaterial3D Material(string color, float metal = 0, float rough = .65f, float emission = 0)
    {
        var c = new Color(color);
        var material = new StandardMaterial3D { AlbedoColor = c, Metallic = metal, Roughness = rough, EmissionEnabled = emission > 0, Emission = c, EmissionEnergyMultiplier = emission };
        if (metal >= .3f && rough >= .35f && emission == 0)
        {
            material.AlbedoTexture=Plating(); material.Uv1Triplanar=true;
            material.Uv1Scale=Vector3.One*.26f; material.TextureFilter=BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps;
        }
        return material;
    }

    public static MeshInstance3D Mesh(Node3D parent, Mesh mesh, Material material, Vector3 position = default, Vector3 rotation = default)
    {
        var node = new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = position, RotationDegrees = rotation };
        parent.AddChild(node);
        return node;
    }
    public static MeshInstance3D Box(Node3D parent, Vector3 size, Vector3 at, Material mat) => Mesh(parent, new BoxMesh { Size = size }, mat, at);
    public static MeshInstance3D Sphere(Node3D parent, float radius, Vector3 at, Material mat, int segments = 24) => Mesh(parent, new SphereMesh { Radius = radius, Height = radius * 2, RadialSegments = segments, Rings = segments / 2 }, mat, at);
    public static MeshInstance3D Ring(Node3D parent, float radius, float width, Vector3 at, Material mat, Vector3 rotation = default) => Mesh(parent, new TorusMesh { InnerRadius = radius - width, OuterRadius = radius + width, Rings = 48, RingSegments = 8 }, mat, at, rotation);

    // Eight-sided cross sections make actual bevels, so silhouette and highlights survive at any scale.
    public static MeshInstance3D Hull(Node3D parent, Vector3 size, Vector3 at, Material mat, float nose = .72f)
    {
        using var surface = new SurfaceTool();
        surface.Begin(Godot.Mesh.PrimitiveType.Triangles);
        Vector2[] profile = [new(-.32f,.5f),new(.32f,.5f),new(.5f,.28f),new(.5f,-.28f),new(.32f,-.5f),new(-.32f,-.5f),new(-.5f,-.28f),new(-.5f,.28f)];
        var vertices = new Vector3[16];
        for (int ring = 0; ring < 2; ring++) for (int i = 0; i < 8; i++)
            vertices[ring*8+i] = new(profile[i].X*size.X*(ring==0?nose:1), profile[i].Y*size.Y*(ring==0?nose:1), (ring-.5f)*size.Z);
        void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            var normal = (c-a).Cross(b-a).Normalized();
            foreach (var v in new[] {a,b,c}) { surface.SetNormal(normal); surface.AddVertex(v); }
        }
        for(int i=0;i<8;i++)
        {
            int j=(i+1)%8;
            Tri(vertices[i],vertices[j],vertices[8+j]); Tri(vertices[i],vertices[8+j],vertices[8+i]);
            Tri(new(0,0,-size.Z/2),vertices[j],vertices[i]);
            Tri(new(0,0,size.Z/2),vertices[8+i],vertices[8+j]);
        }
        return Mesh(parent, surface.Commit(), mat, at);
    }

    public static void Beam(Node3D root, Vector3 a, Vector3 b, float width, Material mat)
    {
        var beam = Box(root, new(width,width,a.DistanceTo(b)), (a+b)/2, mat);
        var direction=(b-a).Normalized();
        beam.LookAtFromPosition((a+b)/2,b,Mathf.Abs(direction.Dot(Vector3.Up))>.98f?Vector3.Right:Vector3.Up);
    }

    private static void Sign(Node3D root, string text, Vector3 at, float pixelSize, Color color)
    {
        root.AddChild(new Label3D { Text=text, Position=at, PixelSize=pixelSize, FontSize=64,
            Modulate=color, OutlineSize=0, NoDepthTest=false, Billboard=BaseMaterial3D.BillboardModeEnum.Disabled });
    }

    public static Node3D Ship(Node3D parent, bool cargo)
    {
        var root = new Node3D { Name=cargo?"AtlasFreighter":"PionierCourier" }; parent.AddChild(root);
        var hull=Material("a9b5b4",.55f,.46f);
        var dark=Material("202c36",.65f,.4f);
        var trim=Material(cargo?"bf6934":"cb9b45",.45f,.42f);
        var glass=Material("193846",.7f,.16f);
        var light=Material("94e8ff",.1f,.22f,1.5f);
        var flame=Material("42bded",0,.3f,.7f);
        flame.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;
        flame.AlbedoColor=new Color(.15f,.65f,1,.55f);
        flame.ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded;
        Hull(root,new(4.2f,2.4f,11),new(0,0,0),hull,.42f);
        Hull(root,new(2.5f,1.25f,3.5f),new(0,1,-3.4f),dark,.5f);
        Hull(root,new(2.2f,.65f,2.8f),new(0,1.65f,-3.7f),glass,.55f);
        Box(root,new(.12f,.12f,2.8f),new(0,2,-3.6f),hull);
        Hull(root,new(3.6f,.5f,5),new(0,1.3f,.7f),trim);
        for(int i=0;i<7;i++) Box(root,new(2.4f,.16f,.16f),new(0,1.63f,-.7f+i*.46f),dark);
        foreach(int side in new[]{-1,1})
        {
            Hull(root,new(3.6f,.85f,6.8f),new(side*3.1f,-.35f,1.2f),dark,.4f);
            Hull(root,new(2.6f,.42f,5.3f),new(side*3.1f,.2f,.8f),hull,.45f);
            Hull(root,new(1.9f,2,5.4f),new(side*4.1f,.1f,3),hull,.85f);
            Hull(root,new(2,.6f,3.6f),new(side*4.1f,1.12f,2.6f),trim);
            for(int i=0;i<4;i++) Box(root,new(.1f,.16f,2.3f),new(side*4.1f+(i-1.5f)*.3f,1.47f,2.6f),dark);
            Mesh(root,new CylinderMesh {TopRadius=.77f,BottomRadius=.86f,Height=1.2f,RadialSegments=16},dark,new(side*4.1f,.1f,6),new(90,0,0));
            Ring(root,.67f,.12f,new(side*4.1f,.1f,6.65f),trim,new(90,0,0));
            Mesh(root,new CylinderMesh {TopRadius=.56f,BottomRadius=.56f,Height=.08f,RadialSegments=16},light,new(side*4.1f,.1f,6.68f),new(90,0,0));
            var plume=Mesh(root,new CylinderMesh {TopRadius=0,BottomRadius=.48f,Height=3.4f,RadialSegments=16},flame,new(side*4.1f,.1f,8.4f),new(90,0,0));
            plume.Name=side==-1?"PlumeLeft":"PlumeRight";
            Sphere(root,.12f,new(side*5.1f,.3f,1.3f),Material(side<0?"ed554d":"78ecc9",0,.4f,2),8);
            Box(root,new(.12f,.1f,2),new(side*2.5f,.68f,-.2f),light);
            // Recessed cargo pods and external restraint ribs distinguish the hauler.
            if(cargo) for(int bay=0;bay<2;bay++)
            {
                var pos=new Vector3(side*2.7f,1.2f,bay*3.6f-1.5f);
                Hull(root,new(2.1f,2.3f,3.3f),pos,trim,1);
                for(int rib=0;rib<4;rib++) Box(root,new(2.2f,.12f,.13f),pos+new Vector3(0,1.18f,rib*.8f-1.2f),dark);
                Box(root,new(.08f,1.5f,2.2f),pos+new Vector3(side*1.07f,0,0),hull);
            }
        }
        Hull(root,new(.28f,2.2f,2.8f),new(0,2.1f,4.3f),dark,.35f);
        Box(root,new(.1f,1.7f,.1f),new(1,2.1f,2),dark);
        Sign(root,cargo?"ATLAS  /  07":"PIONIER  /  01",new(0,.2f,5.56f),.013f,new Color("e0e4db"));
        return root;
    }

    public static Node3D Station(Node3D parent, Station station)
    {
        var root=new Node3D { Name=station.IsMine?"KeplerExtraction":"HeliosFoundry", Position=Convert(station.Position) }; parent.AddChild(root);
        var hull=Material("67767b",.6f,.55f); var pale=Material("bbc4bf",.5f,.5f);
        var dark=Material("202b32",.6f,.48f); var ochre=Material("b78640",.4f,.55f);
        var light=Material(station.IsMine?"78dce5":"ffc17a",.1f,.3f,1.2f);
        var panel=Material("172e47",.65f,.25f);
        foreach(var material in new[]{hull,pale,dark,ochre}) material.Uv1Scale=Vector3.One*.045f;
        Hull(root,new(33,29,45),Vector3.Zero,hull,.85f);
        for(int side=-1;side<=1;side+=2)
        {
            Hull(root,new(6,25,43),new(side*19,0,0),dark);
            for(int i=0;i<5;i++)
            {
                Hull(root,new(7,17,6),new(side*20,0,i*8-16),pale,1);
                Box(root,new(7.2f,1.2f,1),new(side*20,5,i*8-16),ochre);
            }
        }
        var orbit=new Node3D {Name="HabitatRing"};root.AddChild(orbit);
        if(station.IsMine)
        {
            // Excavation hub: open trusses, ore hoppers, photovoltaic wings.
            foreach(int side in new[]{-1,1})
            {
                for(int rail=-1;rail<=1;rail+=2) Beam(root,new(side*27,rail*4,0),new(side*104,rail*4,0),1.6f,dark);
                for(int i=0;i<6;i++) Beam(root,new(side*(28+i*12),-4,0),new(side*(40+i*12),4,0),1.2f,ochre);
                Hull(root,new(19,25,27),new(side*53,0,-14),ochre,.8f);
                for(int i=0;i<4;i++) Box(root,new(19.4f,1,1),new(side*53,i*6-9,0),dark);
                Box(root,new(37,1.6f,40),new(side*105,0,0),dark);
                for(int x=0;x<4;x++) for(int z=0;z<5;z++)
                {
                    Box(root,new(8.2f,.4f,6.8f),new(side*105+(x-1.5f)*9,1,(z-2)*7.6f),panel);
                    Box(root,new(.15f,.05f,6.8f),new(side*105+(x-1.5f)*9,1.23f,(z-2)*7.6f),pale);
                }
                Sphere(root,1,new(side*123,2,19),light,12);
            }
            Ring(orbit,42,2.6f,new(0,0,-13),dark,new(90,0,0));
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.Tau/12;
                var pos=new Vector3(Mathf.Cos(a)*42,Mathf.Sin(a)*42,-13);
                var module=Hull(orbit,new(10,7,12),pos,pale,1); module.Rotation=new(0,0,a);
                Box(orbit,new(3,.8f,.3f),pos+new Vector3(0,0,6.2f),light);
            }
        }
        else
        {
            // Foundry: a compact vertical industrial silhouette within its collision envelope.
            foreach(int side in new[]{-1,1})
            {
                Beam(root,new(0,-15,-12),new(side*34,-15,-12),4,dark);
                for(int z=0;z<2;z++)
                {
                    var at=new Vector3(side*32,0,z*27-25);
                    Mesh(root,new CylinderMesh {TopRadius=8,BottomRadius=9,Height=48,RadialSegments=16},hull,at);
                    Sphere(root,8,at+new Vector3(0,24,0),pale,20);
                    for(int y=-2;y<=2;y++) Ring(root,9,.6f,at+new Vector3(0,y*9,0),y==0?light:dark);
                    Box(root,new(3,34,4),at+new Vector3(side*10,0,0),ochre);
                }
                Hull(root,new(18,10,23),new(side*22,38,-10),pale);
                for(int i=0;i<5;i++) Box(root,new(18,.7f,1),new(side*22,43.4f,i*4-18),dark);
                Beam(root,new(side*12,10,-8),new(side*22,38,-8),3,dark);
            }
            Ring(orbit,22,1,new(0,31,-8),ochre);
            Hull(root,new(14,17,18),new(0,37,-8),dark);
            for(int i=0;i<7;i++) Box(root,new(12,.65f,.3f),new(0,32+i*1.8f,1.2f),light);
        }
        // Dock stays in the existing approach corridor, away from solid collision volumes.
        Hull(root,new(34,3,69),new(0,-10,66),dark,1);
        Box(root,new(24,.2f,55),new(0,-8.4f,68),hull);
        foreach(int side in new[]{-1,1})
        {
            Box(root,new(1,1,65),new(side*15,-7.6f,67),ochre);
            Beam(root,new(side*18,-6,30),new(side*18,15,30),3,pale);
            for(int i=0;i<7;i++)
            {
                Box(root,new(.45f,.2f,3),new(side*12,-8.1f,43+i*8),light);
                var stripe=Box(root,new(2,.15f,1.1f),new(side*14,-6.95f,44+i*7),dark);stripe.Rotation=new(0,.5f,0);
            }
            Box(root,new(2,1,1),new(side*10,14,32),light);
        }
        Hull(root,new(41,8,7),new(0,17,29),pale,1);
        Sign(root,station.IsMine?"KEPLER  /  ORE EXTRACTION":"HELIOS  /  METAL FOUNDRY",new(0,18,32.6f),.018f,new Color("15242c"));
        Sign(root,"01",new(0,7,24),.11f,new Color("81dce3"));
        // Four corner guides replace the oversized solid-looking neon ring.
        foreach(int side in new[]{-1,1}) foreach(int y in new[]{-1,1})
        {
            Box(root,new(3,.35f,.35f),new(side*14,y*7,100),light);
            Box(root,new(.35f,2,.35f),new(side*15.3f,y*6.2f,100),light);
        }
        return root;
    }

    public static Node3D CockpitFrame(Node3D camera)
    {
        var frame=new Node3D {Name="CockpitFrame"};camera.AddChild(frame);
        var dark=Material("18232b",.4f,.6f);
        var metal=Material("505d62",.5f,.5f);
        var screen=new ShaderMaterial { Shader=GD.Load<Shader>("res://Shaders/instrument.gdshader") };
        foreach(int side in new[]{-1,1})
        {
            Beam(frame,new(side*.62f,-.6f,-1.2f),new(side*1.35f,.9f,-2),.065f,metal);
            Hull(frame,new(.65f,.23f,.8f),new(side*.71f,-.53f,-1.15f),dark,1);
            var display=Box(frame,new(.34f,.01f,.25f),new(side*.67f,-.405f,-1.21f),screen);
            display.Rotation=new(.1f,0,0);
            for(int i=0;i<4;i++) Box(frame,new(.035f,.015f,.025f),new(side*.58f+i*.055f,-.4f,-.97f),metal);
        }
        Beam(frame,new(-1.35f,.9f,-2),new(1.35f,.9f,-2),.09f,dark);
        Hull(frame,new(.6f,.17f,.3f),new(0,-.61f,-1.1f),dark,1);
        return frame;
    }

    public static void Asteroids(Node3D parent, World world)
    {
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/rock.gdshader") };
        var random = new Random(811);
        foreach (var body in world.CollisionBodies.Where(b => b.IsAsteroid))
        {
            using var sphere = new SphereMesh { Radius = 1, Height = 2, RadialSegments = 24, Rings = 12 };
            var arrays = sphere.GetMeshArrays();
            var vertices = arrays[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array();
            // Deformation never exceeds the collision sphere.
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                float rough = .72f + .21f * Mathf.Sin(v.X * 8 + v.Z * 7 + body.Position.X) * Mathf.Cos(v.Y * 9 + body.Position.Z);
                vertices[i] = v * body.Radius * rough;
            }
            var indices = arrays[(int)Godot.Mesh.ArrayType.Index].AsInt32Array();
            using var surface = new SurfaceTool(); surface.Begin(Godot.Mesh.PrimitiveType.Triangles);
            surface.SetSmoothGroup(uint.MaxValue);
            foreach (int index in indices) surface.AddVertex(vertices[index]);
            surface.GenerateNormals();
            var mesh = surface.Commit();
            var asteroid = Mesh(parent, mesh, material, Convert(body.Position));
            asteroid.Rotation = new((float)random.NextDouble() * 5, (float)random.NextDouble() * 5, 0);
        }
    }

    public static Vector3 Convert(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);
}
