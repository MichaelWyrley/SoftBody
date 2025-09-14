using UnityEngine;

public class Spring
{
    private Particel p1, p2;
    private float restLength;
    private float stiffness;
    private float damping;

    public Spring(Particel p1, Particel p2, float stiffness, float damping) {
        this.p1 = p1;
        this.p2 = p2;
        this.restLength = Vector3.Distance(p1.transform.position, p2.transform.position);
        // this.restLength = 0;
        this.stiffness = stiffness;
        this.damping = damping;
    }

    public void UpdateSpring() {

        Vector3 delta = p2.transform.position - p1.transform.position;
        float currentLength = delta.magnitude;

        if (currentLength == 0) return;

        Vector3 direction = delta / currentLength;
        Vector3 relativeVelocity = p2.velocity - p1.velocity;

        float stretch = currentLength - restLength;
        float dampingForce = Vector3.Dot(relativeVelocity, direction);

        float forceMag = (stiffness * stretch) + (damping * dampingForce);
        Vector3 force = forceMag * direction;

        if (Mathf.Abs(forceMag) < 0.01f){
            return;
        }

        p1.ApplyForce(force);
        p2.ApplyForce(-force);
        
    }
}
