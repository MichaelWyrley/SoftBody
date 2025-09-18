using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

public class SoftBodyHandler : MonoBehaviour
{
    public int solverIterations = 3;
    public int gridSize = 4;
    // public float springStiffness = 1f;
    [Range(0.00000001f, 1f)]
    public float compliance = 0.001f;
    public float damping = 0.9f;
    public float friction = 0.1f;
    public float global_damping = 0.99f;



    public bool update = true;

    private const float EPSILON = 0.0001f;
    private const float POINT_SHRINK = 1.00001f;
    private float spring_dist = 0.1f;
    public float spring_point_neighbouhood = 2;

    ComputeBuffer particle_buffer = null;
    ComputeBuffer lambda_buffer = null;
    ComputeBuffer spring_buffer = null;
    ComputeBuffer predicted_pos_buffer = null;
    ComputeBuffer pos_delta_accumulator_buffer = null;

    ComputeBuffer ground_buffer = null;
    ComputeBuffer ground_mesh_buffer = null;
    [SerializeField] ComputeShader shader;

    private List<ParticleData> particles;
    private int mesh_particle;

    private List<SpringData> springs;
    private Mesh mesh;

    private List<GroundData> ground;
    private List<Triangles> ground_mesh;

    private List<Transform> previous_ground_transform;
    public bool update_ground = true;
    
    int spring_kernel;
    int particle_kernel;
    int spring_clear_kernel;
    int accumulator_kernel;
    int collisions_kernel;

    int spring_thread_count;
    int particle_thread_count;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        particle_kernel = shader.FindKernel("CalculateParticles");
        spring_kernel = shader.FindKernel("CalculateSprings");
        spring_clear_kernel = shader.FindKernel("ClearSpring");
        accumulator_kernel = shader.FindKernel("Accumulator");
        collisions_kernel = shader.FindKernel("ProjectCollisions");
        mesh = gameObject.GetComponent<MeshFilter>().mesh;
        InitialiseParticles();
        InitialiseSprings();
        SendToShader();


    }

    // Update is called once per frame
    void Update()
    {

        if (update){
            if(update_ground) SendGround();

            float dt = Time.deltaTime;
            shader.SetFloat("dt", dt / solverIterations);

            for (int i = 0; i < solverIterations; i++){

                shader.Dispatch(collisions_kernel, particle_thread_count, 1, 1);
                shader.Dispatch(spring_kernel, spring_thread_count, 1, 1);
                shader.Dispatch(accumulator_kernel, particle_thread_count, 1, 1);
            }
            shader.SetFloat("dt", dt);
            shader.Dispatch(particle_kernel, particle_thread_count, 1, 1);
            shader.Dispatch(spring_clear_kernel, spring_thread_count, 1,1);
        } 
    }

    // Update mesh
    void LateUpdate() {

        ParticleData[] buff_data = new ParticleData[particles.Count];
        particle_buffer.GetData(buff_data);

        Vector3[] newVertices = new Vector3[mesh_particle];

        // print(buff_data[0].position);

        for (int i = 0; i < mesh_particle; i++)
        {
            Vector3 worldPos = buff_data[i].position;
            newVertices[i] = transform.InverseTransformPoint(worldPos);
        }

        mesh.vertices = newVertices;
        // mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    void OnDestroy()
    {
        if (particle_buffer != null) particle_buffer.Dispose();
        if (lambda_buffer != null) lambda_buffer.Dispose(); 
        if (spring_buffer != null) spring_buffer.Dispose();
        if (predicted_pos_buffer != null) predicted_pos_buffer.Dispose();
        if (pos_delta_accumulator_buffer != null) pos_delta_accumulator_buffer.Dispose();
        if (ground_buffer != null) ground_buffer.Dispose();
        if (ground_mesh_buffer != null) ground_mesh_buffer.Dispose();
    }

    void OnDisable()
    {
        if (particle_buffer != null) particle_buffer.Dispose();
        if (predicted_pos_buffer != null) predicted_pos_buffer.Dispose();
        if (pos_delta_accumulator_buffer != null) pos_delta_accumulator_buffer.Dispose();
        if (lambda_buffer != null) lambda_buffer.Dispose();
        if (spring_buffer != null) spring_buffer.Dispose();
        if (ground_buffer != null) ground_buffer.Dispose();
        if (ground_mesh_buffer != null) ground_mesh_buffer.Dispose();
    }

    void OnEnable()
    {
        particle_kernel = shader.FindKernel("CalculateParticles");
        spring_kernel = shader.FindKernel("CalculateSprings");
        spring_clear_kernel = shader.FindKernel("ClearSpring");
        accumulator_kernel = shader.FindKernel("Accumulator");
        collisions_kernel = shader.FindKernel("ProjectCollisions");
        mesh = gameObject.GetComponent<MeshFilter>().mesh;
        InitialiseParticles();
        InitialiseSprings();
        SendToShader();
    }



    private void SendToShader() {
        SendGround();
        SendSoftBodyInfo();

        shader.SetInt("solverIterations", solverIterations);
        shader.SetInt("gridSize", gridSize);
        // shader.SetFloat("springStiffness", springStiffness);
        shader.SetFloat("compliance", compliance);
        shader.SetFloat("damping", damping);
        shader.SetFloat("friction", friction);
        shader.SetFloat("global_damping", global_damping);
    }

    private void SendSoftBodyInfo() {

        if (particles.Count > 0 && springs.Count > 0) {

            if (particle_buffer != null) particle_buffer.Dispose();
            if (predicted_pos_buffer != null) predicted_pos_buffer.Dispose();
            if (pos_delta_accumulator_buffer != null) pos_delta_accumulator_buffer.Dispose();
            if (lambda_buffer != null) lambda_buffer.Dispose();
            if (spring_buffer != null) spring_buffer.Dispose();

            particle_buffer = new ComputeBuffer (particles.Count, ParticleData.GetSize ());
            predicted_pos_buffer = new ComputeBuffer(particles.Count, sizeof(float) * 3);
            pos_delta_accumulator_buffer = new ComputeBuffer(particles.Count, sizeof(int)*3);
            spring_buffer = new ComputeBuffer(springs.Count, SpringData.GetSize());
            lambda_buffer = new ComputeBuffer(springs.Count, sizeof(float));


            float[] lambdas = new float[springs.Count]; // initialized to 0
            Vector3[] initial_prediction = new Vector3[particles.Count];
            for (int i = 0; i < particles.Count; i++){
                initial_prediction[i] = particles[i].position;
            }

            int[,] initial_deltas = new int[particles.Count,3];
            for (int i = 0; i < particles.Count; i++){
                initial_deltas[i,0] = 0;
                initial_deltas[i,1] = 0;
                initial_deltas[i,2] = 0;

            }


            particle_buffer.SetData (particles);
            predicted_pos_buffer.SetData(initial_prediction);
            pos_delta_accumulator_buffer.SetData(initial_deltas);
            lambda_buffer.SetData (lambdas);
            spring_buffer.SetData(springs);

            shader.SetBuffer (spring_kernel, "particles", particle_buffer);
            shader.SetBuffer (spring_kernel, "predicted_pos", predicted_pos_buffer);
            shader.SetBuffer (spring_kernel, "pos_delta_accumulator", pos_delta_accumulator_buffer);
            shader.SetBuffer (spring_kernel, "spring_lambdas", lambda_buffer);
            shader.SetBuffer (spring_kernel, "springs", spring_buffer);

            shader.SetBuffer (spring_clear_kernel, "spring_lambdas", lambda_buffer);

            shader.SetBuffer (accumulator_kernel, "pos_delta_accumulator", pos_delta_accumulator_buffer);
            shader.SetBuffer (accumulator_kernel, "predicted_pos", predicted_pos_buffer);
            shader.SetBuffer (accumulator_kernel, "particles", particle_buffer);

            shader.SetBuffer (collisions_kernel, "particles", particle_buffer);
            shader.SetBuffer (collisions_kernel, "predicted_pos", predicted_pos_buffer);
            shader.SetBuffer (collisions_kernel, "ground",      ground_buffer);
            shader.SetBuffer (collisions_kernel, "ground_mesh", ground_mesh_buffer);

            shader.SetBuffer (particle_kernel, "particles", particle_buffer);
            shader.SetBuffer (particle_kernel, "predicted_pos", predicted_pos_buffer);
            shader.SetBuffer (particle_kernel, "spring_lambdas", lambda_buffer);
            shader.SetBuffer (particle_kernel, "springs", spring_buffer);

            spring_thread_count = Mathf.CeilToInt((float)springs.Count / 64);
            particle_thread_count = Mathf.CeilToInt((float)particles.Count / 64);


        }

    }

    private void InitialiseParticles() {

        var T = gameObject.transform;
        var localToWorldMatrix = T.localToWorldMatrix;

        var dist = (1 / gridSize + EPSILON) * spring_point_neighbouhood;
        spring_dist = (localToWorldMatrix.MultiplyPoint3x4(new Vector3(dist,dist,dist))).magnitude;
        
        var bounds = gameObject.GetComponent<Renderer>().bounds;
        var b_min = T.InverseTransformPoint(bounds.min);
        var b_max = T.InverseTransformPoint(bounds.max);
        var size = b_max - b_min;

        // Create a particle for each vertex of the mesh
        particles = new List<ParticleData>();
        mesh_particle = mesh.vertices.Length;

        for (int i = 0; i < mesh.vertices.Length; i++) {
            var m = mesh.vertices[i];
            var pos = localToWorldMatrix.MultiplyPoint3x4(m);
            ParticleData p = new ParticleData(pos);
            particles.Add(p);
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
                        ParticleData p = new ParticleData(worldPoint);
                        particles.Add(p);
                    }
                }
            }
        }

        
    }

    private void InitialiseSprings() {
        // Generate all springs (top, bottom, left, right, back, front, diagonal in all dirs, )

        springs = new List<SpringData>();

        for (uint i = 0; i < particles.Count; i++){
            var pos_i = particles[(int)i].position;
            for (uint j = i+1; j < particles.Count; j++){

                var pos_j = particles[(int)j].position;
                var dist = (Vector3.Distance(pos_i, pos_j));
                if (dist > 1e-5f && dist <= spring_dist){
                    springs.Add(new SpringData(i,j, dist) );
                    var p1 = particles[(int)i];
                    var p2 = particles[(int)j];
                    p1.num_springs += 1;
                    p2.num_springs += 1;
                    particles[(int)i] = p1;
                    particles[(int)j] = p2;
                }

            }
        }
    }

    private void SendGround() {
        GameObject[] groundMeshes = GameObject.FindGameObjectsWithTag("Ground");


        ground ??= new List<GroundData>();
        ground_mesh ??= new List<Triangles>();

        ground.Clear();
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
                Triangles tri = new Triangles () {
                    posA = a,
                    posB = b,
                    posC = c,
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

            shader.SetBuffer (particle_kernel, "ground", ground_buffer);
            shader.SetBuffer (particle_kernel, "ground_mesh", ground_mesh_buffer);

            shader.SetBuffer (spring_kernel, "ground", ground_buffer);
            shader.SetBuffer (spring_kernel, "ground_mesh", ground_mesh_buffer);

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
