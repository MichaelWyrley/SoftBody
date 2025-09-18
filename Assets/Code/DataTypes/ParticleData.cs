using UnityEngine;

[System.Serializable]
public struct ParticleData 
{

    public Vector3 position;
    public float pad0;
    public Vector3 velocity;
    public float pad1;
    public Vector3 force;
    public float num_springs;
    public float mass;
    public float inv_mass;
    public float radius;
    public float collisionDamping;

    public ParticleData(Vector3 position, float mass, float radius, float collisionDamping){
        this.position = position;
        this.velocity = Vector3.zero;
        this.force = Vector3.zero;
        this.mass = mass;
        this.inv_mass = 1/mass;
        this.radius = radius;
        this.collisionDamping = collisionDamping;
        this.pad0 = 0f;
        this.pad1 = 0f;
        this.num_springs = 0f;


    }
    public ParticleData(Vector3 position){
        this.position = position;
        this.velocity = Vector3.zero;
        this.force = Vector3.zero;

        this.mass = 0.001f;
        this.inv_mass = 1/this.mass;
        this.radius = 0.1f; 
        this.collisionDamping = 1.1f;
        this.pad0 = 0f;
        this.pad1 = 0f;
        this.num_springs = 0f;
    }

    public static int GetSize () {
        return sizeof(float) * 16; // 16 floats = 64 bytes
    }


    
}
