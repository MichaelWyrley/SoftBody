using UnityEngine;

public class Particel : MonoBehaviour
{
    public Vector3 velocity;
    public float mass = 1f;
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public Vector3 force;
    public float radius = 0.005f; 
    public float friction;
    public float global_damping;
    public float collisionDamping = 0.2f;


    private Vector3 accumulatedForce;

    void Start() {
        accumulatedForce = Vector3.zero;
    }

    public void Integrate(float dt)
    {
        

        // Apply gravity as force
        ApplyForce(gravity * mass);

        // Integrate motion
        velocity += (accumulatedForce / mass) * dt;

        // velocity *= global_damping;
        if (Mathf.Abs(velocity.x) < 0.01f && Mathf.Abs(velocity.y) < 0.01f && Mathf.Abs(velocity.y) < 0.01f){
            velocity = Vector3.zero;
        }

        if (velocity.y == 0){
            gameObject.GetComponent<Renderer>().material.color = Color.white;
        } else if (velocity.y < 0) {
            gameObject.GetComponent<Renderer>().material.color = Color.red;
        } else if (velocity.y > 0) {
            gameObject.GetComponent<Renderer>().material.color = Color.green;
        } 


        transform.position += velocity * dt;

        // Reset forces for next frame
        accumulatedForce = Vector3.zero;

        // Handle collisions after integration
        ResolveCollisions();
    }

    public void ApplyForce(Vector3 force) {
        
        accumulatedForce += force;

    }

    private void ResolveCollisions()
    {
        GameObject[] groundMeshes = GameObject.FindGameObjectsWithTag("Ground");

        foreach (var obj in groundMeshes)
        {
            Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
            Bounds bounds = mesh.bounds;
            Matrix4x4 localToWorld = obj.transform.localToWorldMatrix;

            // Convert AABB to world space
            Bounds worldBounds = TransformBounds(bounds, localToWorld);

            // Quick bounding-box check
            if (!worldBounds.Contains(transform.position))
                continue;

            // If inside bounds, check all triangles
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = localToWorld.MultiplyPoint3x4(vertices[triangles[i]]);
                Vector3 v1 = localToWorld.MultiplyPoint3x4(vertices[triangles[i + 1]]);
                Vector3 v2 = localToWorld.MultiplyPoint3x4(vertices[triangles[i + 2]]);

                Vector3 closest = ClosestPointOnTriangle(transform.position, v0, v1, v2);
                Vector3 diff = transform.position - closest;
                float dist = diff.magnitude;

                if (dist < radius)
                {
                    Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                    float penetrationDepth = radius - dist;

                    // Push particle out of triangle
                    transform.position += normal * (penetrationDepth * 1.1f);

                    // Separate velocity into normal/tangent
                    float vDot = Vector3.Dot(velocity, normal);
                    Vector3 vNormal = vDot * normal;
                    Vector3 vTangent = velocity - vNormal;

                    if (vDot < 0f) // moving into surface
                    {
                        if (Mathf.Abs(vDot) < 0.05f)
                        {
                            // Kill micro-bounces completely
                            velocity = vTangent;
                        }
                        else
                        {
                            // Apply collision damping on normal velocity
                            Vector3 newNormalVel = -vNormal * (1f - collisionDamping);
                            // Apply surface friction to tangential velocity
                            vTangent *= (1f - friction);
                            velocity = newNormalVel + vTangent;

                        }
                        float postDot = Vector3.Dot(velocity, normal);
                        if (postDot < 0f)
                            velocity -= postDot * normal; 
                    }
                }
            }
        }
    }

    // Transform mesh bounds to world space
    private Bounds TransformBounds(Bounds localBounds, Matrix4x4 localToWorld)
    {
        // Get 8 corners of the local bounding box
        Vector3 center = localBounds.center;
        Vector3 extents = localBounds.extents;

        Vector3[] corners = new Vector3[8];
        corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
        corners[1] = center + new Vector3(-extents.x, -extents.y, extents.z);
        corners[2] = center + new Vector3(-extents.x, extents.y, -extents.z);
        corners[3] = center + new Vector3(-extents.x, extents.y, extents.z);
        corners[4] = center + new Vector3(extents.x, -extents.y, -extents.z);
        corners[5] = center + new Vector3(extents.x, -extents.y, extents.z);
        corners[6] = center + new Vector3(extents.x, extents.y, -extents.z);
        corners[7] = center + new Vector3(extents.x, extents.y, extents.z);

        // Transform all corners and compute new bounds
        Bounds worldBounds = new Bounds(localToWorld.MultiplyPoint3x4(corners[0]), Vector3.zero);
        for (int i = 1; i < 8; i++)
            worldBounds.Encapsulate(localToWorld.MultiplyPoint3x4(corners[i]));

        return worldBounds;
    }

    // Helper: Closest point on triangle
    private Vector3 ClosestPointOnTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
    {
        // Compute triangle edges
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ap = point - a;

        float d1 = Vector3.Dot(ab, ap);
        float d2 = Vector3.Dot(ac, ap);

        // Check if P is in vertex region outside A
        if (d1 <= 0f && d2 <= 0f) return a;

        // Check if P is in vertex region outside B
        Vector3 bp = point - b;
        float d3 = Vector3.Dot(ab, bp);
        float d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3) return b;

        // Check if P is in vertex region outside C
        Vector3 cp = point - c;
        float d5 = Vector3.Dot(ab, cp);
        float d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6) return c;

        // Check if P is in edge region of AB, if so return projection on AB
        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f)
        {
            float v = d1 / (d1 - d3);
            return a + v * ab;
        }

        // Check if P is in edge region of AC, if so return projection on AC
        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f)
        {
            float w = d2 / (d2 - d6);
            return a + w * ac;
        }

        // Check if P is in edge region of BC, if so return projection on BC
        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
        {
            float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return b + w * (c - b);
        }

        // P inside face region
        float denom = 1f / (va + vb + vc);
        float v2 = vb * denom;
        float w2 = vc * denom;
        return a + ab * v2 + ac * w2;
    }
}
