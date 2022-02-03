using UnityEngine;

public class Stage : MonoBehaviour
{
	[SerializeField]
	Transform particlePrefab;

    [SerializeField, Range(0, 100)]
	int numberOfParticles = 3;

	[SerializeField, Range(1, 10)]
	int numberOfTypes = 2;

	[SerializeField]
	Bounds simulationBounds = new Bounds(Vector3.zero, new Vector3(16f, 9f));

	Particle[] particles;
	ParticleType[] particleTypes;

	float attractionRanges;
	
	Color[] colors;


	void Awake () {
		OnValidate();
	}

	void OnValidate () {
		if (particles != null && enabled) {
			OnDisable();
			OnEnable();
		}
	}

	void OnEnable () {
		GenerateColors();
		GenerateParticleTypes();
		GenerateParticles();
	}

	void OnDisable () {
		colors = null;
		for (int i = 0; i < particles.Length; i++) {
			Destroy(particles[i].transform.gameObject);
		}
		particles = null;
	}

	void GenerateColors () {
		colors = new Color[numberOfTypes];
		for (int c = 0; c < colors.Length; c++) {
			colors[c] = Random.ColorHSV();
		}
	}

	void GenerateParticles () {
		particles = new Particle[numberOfParticles];

		for (int i = 0; i < particles.Length; i++) {
			Particle particle = new Particle();
			
			particle.transform = Instantiate(
				particlePrefab, 
				new Vector3(
					Random.Range(simulationBounds.min.x, simulationBounds.max.x),
					Random.Range(simulationBounds.min.y, simulationBounds.max.y)
				),
				Quaternion.identity
			);

			particle.transform.SetParent(transform);
			particle.type = particleTypes[Random.Range(0, particleTypes.Length)];
			particle.transform.GetComponent<SpriteRenderer>().color = colors[particle.type.index];
			particle.radius = particle.transform.localScale.x / 2;
			particles[i] = particle;

		}
	}

	void Update () {
		for (int z = 0; z < particles.Length; z++) {
			particles[z].velocity = Vector2.zero;
			particles[z].forcesExperienced = 0;
		}
		
		//Vector2 screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
    	//Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
		
		//particles[0].transform.position = (Vector3)worldPosition;
		
		for (int i = 0; i < particles.Length; i++) {
			
			// Current particle setup
			Particle p = particles[i];
			Transform pt = p.transform;
			Vector2 pp = (Vector2)pt.position;

			void DoStuff (int j) {
				
				if (j == i) {
					return;
				}

				// Other particle setup
				Particle q = particles[j];
				Vector2 qp = (Vector2)q.transform.position;
				Vector2 qv = q.velocity;

				// Calculate deltas for distance
				float dx = pp.x - qp.x;
				float dy = pp.y - qp.y;
				Vector2 directionVector = (qp - pp).normalized;
				float distance = (qp - pp).magnitude;

				directionVector *= CalculateForces(p.type, q.type, p.radius + q.radius, distance) * Time.deltaTime;

				qv += directionVector;

				q.velocity = qv;
				q.forcesExperienced += directionVector.magnitude > 0f ? 1 : 0; 
				particles[j] = q;

				//Debug.Log(dx);
			}

			for (int j = 0; j < particles.Length; j++) {
				DoStuff(j);
			}
		}

		for (int k = 0; k < particles.Length; k++) {
			particles[k].transform.position += Vector3.ClampMagnitude((Vector3)Dampen(particles[k].velocity), 10f * Time.deltaTime);
		}

		ConstrainPositions();
	}

	Vector2 Dampen (Vector2 velocity) => velocity.normalized * (velocity.magnitude * 0.9f);

	void ConstrainPositions () {
		for (int i = 0; i < particles.Length; i++) {
			Particle particle = particles[i];
			particle.transform.position = new Vector3(
				Mathf.Clamp(particle.transform.position.x, simulationBounds.min.x, simulationBounds.max.x),
				Mathf.Clamp(particle.transform.position.y, simulationBounds.min.y, simulationBounds.max.y)
			);
			particles[i] = particle;
		}
	}

	void OnDrawGizmos () {
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(simulationBounds.center, simulationBounds.extents * 2);
	}

	struct Particle {
		public Transform transform;
		public Vector2 velocity;
		public int forcesExperienced;

		public float radius;
		public ParticleType type;
	}

	struct ParticleType {
		public int index;

		public float[] forceDistance;
		public float[] forceMagnitude;

		public float repelDistance;
		public float repelCurve;
	}

	void GenerateParticleTypes () {
		particleTypes = new ParticleType[numberOfTypes];

		for (int i = 0; i < particleTypes.Length; i++) {
			ParticleType particleType = new ParticleType();
			particleType.index = i;
			particleType.repelDistance = Random.Range(0.25f, 1f);
			particleType.repelCurve = 0.5f / particleType.repelDistance;
			
			particleType.forceDistance = new float[particleTypes.Length];
			particleType.forceMagnitude = new float[particleTypes.Length];

			for (int j = 0; j < particleTypes.Length; j++) {
				particleType.forceMagnitude[j] = Random.Range(1f, 10f) * (Random.Range(-1f, 1f) > 0 ? -1f : 1f);
				particleType.forceDistance[j] = Random.Range(2f, 4f);
			}
			particleTypes[i] = particleType;
		}
	}

	float CalculateForces (ParticleType p, ParticleType q, float r, float d) {

		float strength = 0f;

		if (d - r > p.forceDistance[q.index]) {
			return 0f;
		}
		
		if (d - r < p.forceDistance[q.index]) { // Apply normal force
			strength = -1f * (p.forceMagnitude[q.index] / (p.forceDistance[q.index] / 2f)) * Mathf.Abs(d - r - (p.forceDistance[q.index] / 2f)) + p.forceMagnitude[q.index];
			Debug.Log("Normal force with " + strength + " strength");
		}

		if (d - r <= p.repelDistance) { // If q is too close
			if (d - r > 0.0001f) {
				strength = -1f * Mathf.Log((1f / p.repelDistance) * (d - r)) * p.repelCurve;
			}
			else {
				strength = 10f;
			}
		}

		if (strength < 0.001f) {
			return 0f;
		}

		//Debug.Log("Strength: " + strength);

		return strength;
	}
}
