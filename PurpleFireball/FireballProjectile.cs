using UnityEngine;

namespace PurpleFireball
{
    public class FireballProjectile : MonoBehaviour
    {
        static readonly Color Purple = new Color(0.65f, 0.15f, 1f, 1f);
        static Material mat;

        Vector3 velocity;
        bool launched;
        float age;
        const float Radius = 0.09f;
        const float Gravity = 6f;      // gentle arc
        const float Lifetime = 6f;

        static Material GetMaterial()
        {
            if (mat != null) return mat;
            Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            mat = new Material(s) { color = Purple };
            return mat;
        }

        public static FireballProjectile Create(Vector3 pos)
        {
            var go = new GameObject("PurpleFireball");
            go.transform.position = pos;

            // Core orb (visual only, no collider so it can't push you)
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(orb.GetComponent<Collider>());
            orb.transform.SetParent(go.transform, false);
            orb.transform.localScale = Vector3.one * Radius * 2f;
            orb.GetComponent<Renderer>().material = GetMaterial();

            // Purple glow
            var light = go.AddComponent<Light>();
            light.color = Purple;
            light.range = 3f;
            light.intensity = 1.5f;

            // Flame trail
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.45f;
            main.startSpeed = 0.15f;
            main.startSize = 0.14f;
            main.startColor = new Color(0.8f, 0.3f, 1f, 0.9f);

            var emission = ps.emission;
            emission.rateOverTime = 70f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Radius * 0.6f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.6f, 1f), 0f),
                        new GradientColorKey(new Color(0.4f, 0f, 0.8f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            go.GetComponent<ParticleSystemRenderer>().material = GetMaterial();

            return go.AddComponent<FireballProjectile>();
        }

        public void Launch(Vector3 v)
        {
            velocity = v;
            launched = true;
        }

        void Update()
        {
            if (!launched) return;

            age += Time.deltaTime;
            if (age > Lifetime) { Destroy(gameObject); return; }

            velocity += Vector3.down * Gravity * Time.deltaTime;
            Vector3 step = velocity * Time.deltaTime;

            // Impact check (ignores triggers; skip the first moments so it won't hit your own hand/body)
            if (age > 0.12f &&
                Physics.SphereCast(transform.position, Radius, step.normalized, out RaycastHit hit,
                                   step.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                Explode(hit.point);
                return;
            }

            transform.position += step;
        }

        void Explode(Vector3 point)
        {
            var go = new GameObject("PurpleFireballBurst");
            go.transform.position = point;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.duration = 0.2f;
            main.startLifetime = 0.6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = 0.16f;
            main.startColor = new Color(0.75f, 0.25f, 1f, 1f);
            main.gravityModifier = 0.3f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 45) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            go.GetComponent<ParticleSystemRenderer>().material = GetMaterial();
            ps.Play();
            Destroy(go, 1.5f);
            Destroy(gameObject);
        }
    }
}
