using Godot;
using SpaceSim.Core;

public static class SpaceGeometry
{
    public static StandardMaterial3D Material(string color, float metal = 0, float rough = .65f, float emission = 0)
    {
        var c = new Color(color);
        return new StandardMaterial3D { AlbedoColor = c, Metallic = metal, Roughness = rough, EmissionEnabled = emission > 0, Emission = c, EmissionEnergyMultiplier = emission };
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

    public static Node3D Ship(Node3D parent, bool cargo)
    {
        var root = new Node3D(); parent.AddChild(root);
        var hull = Material(cargo ? "8c7966" : "bdcbd0", .7f, .35f);
        var dark = Material("192632", .6f, .38f);
        var trim = Material(cargo ? "ffb869" : "61e5ea", .4f, .3f, .4f);
        var glass = Material("1b607f", .85f, .1f);
        var flame = Material(cargo ? "ffa04e" : "5ddfff", 0, .25f, 3);
        Box(root, new(3.4f, 1.5f, 8), Vector3.Zero, hull);
        Mesh(root, new CylinderMesh { TopRadius = 0, BottomRadius = 2.1f, Height = 5, RadialSegments = 4 }, hull, new(0, 0, -6), new(-90, 45, 0));
        Box(root, new(2.5f, .35f, 2.7f), new(0, 1, -1.6f), glass);
        foreach (int side in new[] { -1, 1 })
        {
            var wing = Box(root, new(5, .45f, 4.2f), new(side * 3.5f, -.2f, 1.1f), hull);
            wing.RotationDegrees = new(0, side * 18, side * 5);
            Box(root, new(.5f, .6f, 5.8f), new(side * 5.6f, 0, 2), dark);
            Box(root, new(.25f, .1f, 3.7f), new(side * 3.1f, .2f, 1.1f), trim);
            Mesh(root, new CylinderMesh { TopRadius = .9f, BottomRadius = 1, Height = 3, RadialSegments = 16 }, dark, new(side * 2.7f, -.3f, 4), new(90, 0, 0));
            var plume = Mesh(root, new CylinderMesh { TopRadius = .62f, BottomRadius = 0, Height = 4, RadialSegments = 12 }, flame, new(side * 2.7f, -.3f, 7), new(90, 0, 0));
            plume.Name = side == -1 ? "PlumeLeft" : "PlumeRight";
            Sphere(root, .22f, new(side * 6.2f, .1f, 2), trim, 12);
            if (cargo)
            {
                Box(root, new(2.5f, 2.7f, 6), new(side * 3.2f, 1, 0), Material("b57740", .3f));
                for (int i = 0; i < 3; i++) Box(root, new(2.7f, .13f, .22f), new(side * 3.2f, 2.4f, i * 2 - 2), dark);
            }
        }
        Box(root, new(.4f, 2.5f, 2.8f), new(0, 1.3f, 3), dark);
        return root;
    }

    public static Node3D Station(Node3D parent, Station station)
    {
        var root = new Node3D { Position = Convert(station.Position) }; parent.AddChild(root);
        var hull = Material("485963", .7f, .45f);
        var pale = Material("a0afb3", .55f, .4f);
        var dark = Material("17232d", .65f, .5f);
        var light = Material(station.IsMine ? "55e5db" : "ffb564", .1f, .3f, .65f);
        var panel = Material("102b4a", .5f, .2f);
        var grid = Material("28668a", .4f, .45f, .2f);
        Box(root, new(34, 32, 52), new(0, 0, 0), hull);
        Box(root, new(27, 25, 4), new(0, 0, 28), dark);
        Box(root, new(20, 2, 2), new(0, 12, 31), light);
        // Arms and solar panels provide readable scale from a distance.
        foreach (int side in new[] { -1, 1 })
        {
            Box(root, new(80, 5, 5), new(side * 52, 0, 0), pale);
            Box(root, new(38, 2, 52), new(side * 105, 0, 0), panel);
            for (int i = -3; i <= 3; i++) Box(root, new(.35f, .2f, 51), new(side * 105 + i * 5, 1.2f, 0), grid);
            for (int i = -4; i <= 4; i++) Box(root, new(37, .2f, .35f), new(side * 105, 1.2f, i * 6), grid);
            Sphere(root, 1.7f, new(side * 124, 1, 25), light, 12);
        }
        var orbit = new Node3D { Name = "HabitatRing" }; root.AddChild(orbit);
        Ring(orbit, station.IsMine ? 46 : 58, 3, Vector3.Zero, hull, new(90, 0, 0));
        Ring(orbit, station.IsMine ? 47 : 59, .45f, new(0, 0, 2), light, new(90, 0, 0));
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.Tau / 8;
            var pos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * (station.IsMine ? 46 : 58);
            var module = Box(orbit, new(12, 8, 15), pos, pale); module.Rotation = new(0, 0, angle);
            Box(orbit, new(4, 1, 1), pos + new Vector3(0, 0, 8), light);
        }
        if (!station.IsMine)
        {
            foreach (int side in new[] { -1, 1 })
            {
                Mesh(root, new CylinderMesh { TopRadius = 10, BottomRadius = 10, Height = 54, RadialSegments = 20 }, hull, new(side * 28, 0, -30));
                Ring(root, 10.2f, .6f, new(side * 28, 12, -30), light);
                Ring(root, 10.2f, .6f, new(side * 28, -12, -30), light);
            }
        }
        // The illuminated docking gate is offset from the solid hull.
        Box(root, new(31, 2, 63), new(0, -8, 70), dark);
        for (int i = 0; i < 6; i++)
        {
            Box(root, new(.6f, .25f, 4), new(-12, -6.8f, 45 + i * 10), light);
            Box(root, new(.6f, .25f, 4), new(12, -6.8f, 45 + i * 10), light);
        }
        Ring(root, 16, .65f, new(0, 0, 100), light, new(90, 0, 0));
        return root;
    }

    public static void Asteroids(Node3D parent, World world)
    {
        var material = Material("49433f", .02f, .98f);
        var random = new Random(811);
        foreach (var body in world.CollisionBodies.Where(b => b.IsAsteroid))
        {
            using var sphere = new SphereMesh { Radius = 1, Height = 2, RadialSegments = 12, Rings = 6 };
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
