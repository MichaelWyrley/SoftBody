using UnityEngine;
using System.Collections.Generic;

public class SoftBody : MonoBehaviour
{

    public int gridSize = 4;
    public float springStiffness = 0.5f;
    public float damping = 0.9f;
    public GameObject particlePrefab;
    public float friction = 0.1f;
    public float global_damping = 0.95f;

    public bool particel_vis = true;

    private List<Particel> particels;
    private int mesh_particel;

    private List<Spring> springs;
    private Mesh mesh;

    public int solverIterations = 3;

    public bool update = true;

    private const float EPSILON = 0.0001f;
    private const float POINT_SHRINK = 1.00001f;
    private float spring_dist = 0.1f;
    public float spring_point_neighbouhood = 2;
    


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mesh = gameObject.GetComponent<MeshFilter>().mesh;
        InitialiseParticels();
        InitialiseSprings();

    }

    // Update is called once per frame
    void Update()
    {
        if (update){
            float dt = Time.deltaTime / solverIterations;

            for (int iter = 0; iter < solverIterations; iter++)
            {
                // Compute spring forces
                foreach (var s in springs)
                    s.UpdateSpring();

                // Integrate particles
                if (particels[0].GetComponent<MeshRenderer>().enabled != particel_vis){

                    foreach (var p in particels){
                        p.Integrate(dt);
                        p.GetComponent<MeshRenderer>().enabled = particel_vis;
                    }
                } else {
                    foreach (var p in particels)
                        p.Integrate(dt);

                }

                    
            }

        } 
    }

    // Update mesh
    void LateUpdate() {
        Vector3[] newVertices = new Vector3[mesh_particel];

        for (int i = 0; i < mesh_particel; i++)
        {
            Vector3 worldPos = particels[i].transform.position;
            newVertices[i] = transform.InverseTransformPoint(worldPos);
        }

        mesh.vertices = newVertices;
        mesh.RecalculateNormals();
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
        particels = new List<Particel>();
        mesh_particel = mesh.vertices.Length;

        for (int i = 0; i < mesh.vertices.Length; i++) {
            var m = mesh.vertices[i];
            var pos = localToWorldMatrix.MultiplyPoint3x4(m);

            GameObject particleObj = Instantiate(particlePrefab, pos , Quaternion.identity);
            // particleObj.transform.parent = gameObject.transform;

            Particel particle = particleObj.AddComponent<Particel>();
            particle.friction = friction;
            particle.global_damping = global_damping;

            particels.Add(particle);
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
                        GameObject particleObj = Instantiate(particlePrefab, worldPoint , Quaternion.identity);
                        // particleObj.transform.parent = gameObject.transform;

                        Particel particle = particleObj.AddComponent<Particel>();
                        particle.friction = friction;
                        particle.global_damping = global_damping;
                        particels.Add(particle);
                    }
                }
            }
        }

        
    }

    private void InitialiseSprings() {
        // Generate all springs (top, bottom, left, right, back, front, diagonal in all dirs, )

        springs = new List<Spring>();

        for (int i = 0; i < particels.Count; i++){
            var pos_i = particels[i].transform.localPosition;
            for (int j = i+1; j < particels.Count; j++){

                var pos_j = particels[j].transform.localPosition;

                if (Vector3.Distance(pos_i, pos_j) <= spring_dist){
                    springs.Add(new Spring(particels[i],particels[j], springStiffness, damping) );
                }

            }
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
