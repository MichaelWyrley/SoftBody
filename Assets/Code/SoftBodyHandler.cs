using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

public class SoftBodyHandler : MonoBehaviour
{
    public int solverIterations = 3;
    public int gridSize = 4;
    public float springStiffness = 50f;
    public float damping = 0.9f;
    public float friction = 0.1f;
    public float global_damping = 0.99f;



    public bool update = true;

    private const float EPSILON = 0.0001f;
    private const float POINT_SHRINK = 1.00001f;
    private float spring_dist = 0.1f;
    public float spring_point_neighbouhood = 2;

    ComputeBuffer particel_buffer = null;
    ComputeBuffer spring_buffer = null;
    ComputeBuffer ground_buffer = null;
    ComputeBuffer ground_mesh_buffer = null;
    [SerializeField] ComputeShader shader;

    private List<ParticelData> particels;
    private int mesh_particel;

    private List<SpringData> springs;
    private Mesh mesh;

    private List<GroundData> ground;
    private List<Triangles> ground_mesh;

    private List<Transform> previous_ground_transform;
    public bool update_ground = true;
    
    int kernel;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        kernel = shader.FindKernel("CSMain");
        mesh = gameObject.GetComponent<MeshFilter>().mesh;
        InitialiseParticels();
        InitialiseSprings();
        SendToShader();


    }

    // Update is called once per frame
    void Update()
    {
        kernel = shader.FindKernel("CSMain");
        if (update){
            if(update_ground) SendGround();

            float dt = Time.deltaTime;

            

        } 
    }

    // Update mesh
    void LateUpdate() {
        Vector3[] newVertices = new Vector3[mesh_particel];

        for (int i = 0; i < mesh_particel; i++)
        {
            Vector3 worldPos = particels[i].position;
            newVertices[i] = transform.InverseTransformPoint(worldPos);
        }

        mesh.vertices = newVertices;
        mesh.RecalculateNormals();
    }

    void OnDestroy()
    {
        if (particel_buffer != null) particel_buffer.Dispose();
        if (spring_buffer != null) spring_buffer.Dispose();
        if (ground_buffer != null) ground_buffer.Dispose();
        if (ground_mesh_buffer != null) ground_mesh_buffer.Dispose();
    }

    void OnDisable()
    {
        if (particel_buffer != null) particel_buffer.Dispose();
        if (spring_buffer != null) spring_buffer.Dispose();
        if (ground_buffer != null) ground_buffer.Dispose();
        if (ground_mesh_buffer != null) ground_mesh_buffer.Dispose();
    }

    void OnEnable()
    {
        kernel = shader.FindKernel("CSMain");
        mesh = gameObject.GetComponent<MeshFilter>().mesh;
        InitialiseParticels();
        InitialiseSprings();
        SendToShader();
    }



    private void SendToShader() {
        SendGround();
        SendSoftBodyInfo();

        shader.SetInt("solverIterations", solverIterations);
        shader.SetInt("gridSize", gridSize);
        shader.SetFloat("springStiffness", springStiffness);
        shader.SetFloat("damping", damping);
        shader.SetFloat("friction", friction);
        shader.SetFloat("global_damping", global_damping);
    }

    private void SendSoftBodyInfo() {

        if (particels.Count > 0 && springs.Count > 0) {

            if (particel_buffer != null) particel_buffer.Dispose();
            if (spring_buffer != null) spring_buffer.Dispose();

            particel_buffer = new ComputeBuffer (particels.Count, ParticelData.GetSize ());
            spring_buffer = new ComputeBuffer(springs.Count, SpringData.GetSize());
            particel_buffer.SetData (particels);
            spring_buffer.SetData(springs);

            shader.SetBuffer (kernel, "particels", particel_buffer);
            shader.SetBuffer (kernel, "springs", spring_buffer);

        }

    }

    private void InitialiseParticels() {

        var T = gameObject.transform;
        var localToWorldMatrix = T.localToWorldMatrix;

        var dist = (1 / gridSize + EPSILON) * spring_point_neighbouhood;
        spring_dist = (localToWorldMatrix.MultiplyPoint3x4(new Vector3(dist,dist,dist))).magnitude;
        
        var bounds = gameObject.GetComponent<Renderer>().bounds;
        var b_min = T.InverseTransformPoint(bounds.min);
        var b_max = T.InverseTransformPoint(bounds.max);
        var size = b_max - b_min;

        // Create a particle for each vertex of the mesh
        particels = new List<ParticelData>();
        mesh_particel = mesh.vertices.Length;

        for (int i = 0; i < mesh.vertices.Length; i++) {
            var m = mesh.vertices[i];
            var pos = localToWorldMatrix.MultiplyPoint3x4(m);
            ParticelData p = new ParticelData(pos);
            particels.Add(p);
        }
        
        // For the inner grid check if each particle in the grid is within the mesh then only keep the ones that are
        for (int x = 0; x <= gridSize; x++)
        {
            for (int y = 0; y <= gridSize; y++)
            {
                for (int z = 0; z <= gridSize; z++)
                {
                    Vector3 samplePoint = new Vector3(
                        ((((float) x / (float) gridSize)) + (b_min.x)) * (POINT_SHRINK),
                        ((((float) y / (float) gridSize)) + (b_min.y)) * (POINT_SHRINK),
                        ((((float) z / (float) gridSize)) + (b_min.z)) * (POINT_SHRINK)
                    );

                    Vector3 worldPoint = T.TransformPoint(samplePoint);

                    if (IsPointInsideMesh(worldPoint, mesh, T))
                    {
                        ParticelData p = new ParticelData(worldPoint);
                        particels.Add(p);
                    }
                }
            }
        }

        
    }

    private void InitialiseSprings() {
        // Generate all springs (top, bottom, left, right, back, front, diagonal in all dirs, )

        springs = new List<SpringData>();

        for (int i = 0; i < particels.Count; i++){
            var pos_i = particels[i].position;
            for (int j = i+1; j < particels.Count; j++){

                var pos_j = particels[j].position;

                if (Vector3.Distance(pos_i, pos_j) <= spring_dist){
                    springs.Add(new SpringData(particels[i],particels[j]) );
                }

            }
        }
    }

    private void SendGround() {
        GameObject[] groundMeshes = GameObject.FindGameObjectsWithTag("Ground");


        ground ??= new List<GroundData>();
        ground_mesh ??= new List<Triangles>();
        ground_mesh.Clear();

        int triangle_begining = 0;
        int triangle_end = 0;
        for (int i = 0; i < groundMeshes.Length; i++) {
            var s = groundMeshes[i];
            Mesh m = s.gameObject.GetComponent<MeshFilter>().sharedMesh;

            triangle_begining = triangle_end;
            triangle_end += m.triangles.Length /3;
            var bounds = s.GetComponent<Renderer>().bounds;

            var T = s.gameObject.transform;
            var M = T.localToWorldMatrix;

            var sd  = new GroundData () {
                triangle_begin = triangle_begining,
                triangle_count = m.triangles.Length /3,
                bounds_min = bounds.min,
                bounds_max = bounds.max,
            };
            ground.Add(sd);

            for (int j = 0; j < m.triangles.Length; j+=3){
                var a = M.MultiplyPoint3x4(m.vertices[m.triangles[j]]);
                var b = M.MultiplyPoint3x4(m.vertices[m.triangles[j+1]]);
                var c = M.MultiplyPoint3x4(m.vertices[m.triangles[j+2]]);

                var na = T.TransformDirection(m.normals[m.triangles[j]]).normalized;
                var nb = T.TransformDirection(m.normals[m.triangles[j+1]]).normalized;
                var nc = T.TransformDirection(m.normals[m.triangles[j+2]]).normalized;

                Triangles tri = new Triangles () {
                    posA = a,
                    posB = b,
                    posC = c,
                    normalA = na,
                    normalB = nb,
                    normalC = nc
                };

                ground_mesh.Add(tri);
            }

        }
        

        int numTriangles = ground_mesh.Count;
        if (numTriangles > 0) {


            if (ground_buffer != null) ground_buffer.Dispose();
            if (ground_mesh_buffer != null) ground_mesh_buffer.Dispose();

            ground_buffer = new ComputeBuffer (ground.Count, GroundData.GetSize ());
            ground_mesh_buffer = new ComputeBuffer(ground_mesh.Count, Triangles.GetSize());
            ground_buffer.SetData (ground);
            ground_mesh_buffer.SetData(ground_mesh);

            shader.SetBuffer (kernel, "ground", ground_buffer);
            shader.SetBuffer (kernel, "ground_mesh", ground_mesh_buffer);

        }
        
    }


    private bool IsPointInsideMesh(Vector3 point, Mesh mesh, Transform meshTransform)
    {
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        Matrix4x4 localToWorld = meshTransform.localToWorldMatrix;

        Vector3 rayDirection = Vector3.right; // arbitrary, just pick a direction
        int hitCount = 0;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Get triangle vertices in world space
            Vector3 v0 = localToWorld.MultiplyPoint3x4(vertices[triangles[i]]);
            Vector3 v1 = localToWorld.MultiplyPoint3x4(vertices[triangles[i + 1]]);
            Vector3 v2 = localToWorld.MultiplyPoint3x4(vertices[triangles[i + 2]]);

            if (RayIntersectsTriangle(point, rayDirection, v0, v1, v2))
                hitCount++;
        }

        // Odd number of hits = inside
        return (hitCount % 2) == 1;
    }

    // Möller–Trumbore triangle-ray intersection test
    private bool RayIntersectsTriangle(Vector3 origin, Vector3 dir, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        origin += dir * EPSILON;
        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;

        Vector3 h = Vector3.Cross(dir, edge2);
        float a = Vector3.Dot(edge1, h);
        if (a > -EPSILON && a < EPSILON)
            return false; // Ray is parallel to triangle

        float f = 1.0f / a;
        Vector3 s = origin - v0;
        float u = f * Vector3.Dot(s, h);
        if (u < 0.0f || u > 1.0f)
            return false;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(dir, q);
        if (v < 0.0f || (u + v) >= 1.0f)
            return false;

        float t = f * Vector3.Dot(edge2, q);
        return t > EPSILON; // Intersection ahead of origin
    }


}
