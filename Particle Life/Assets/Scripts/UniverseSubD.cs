using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class UniverseSubD : MonoBehaviour {

	private static float RandomNormal(float mean, float sigma) {
		float u, v, S;

		do {
			u = 2f * Random.value - 1f;
			v = 2f * Random.value - 1f;
			S = u * u + v * v;
		}
		while (S >= 1f);

		float fac = Mathf.Sqrt(-2f * Mathf.Log(S) / S);
		float val = u * fac;
		return val * sigma + mean;
	}

	[SerializeField, Range(0.5f, 10f)]
	float radius = 50f;
	
	float diameter;
	float r_smooth = 2f;

	[SerializeField, Range(1, 10)]
	int numberOfTypes = 3;

	[SerializeField, Range(1, 3000)]
	int numberOfParticles = 10;

	[SerializeField]
	Bounds bounds = new Bounds(Vector3.zero, Vector3.one);

	[SerializeField]
	bool mWrapMode = true;

	[SerializeField, Range(1, 16)]
	int subDivisionsX, subDivisionsY;

	[SerializeField, Range(0.1f, 10f)]
	float divUpdateDelay = 1f;

	List<int>[] subDivisions;

	Vector3 screenBounds;
	float P2W, W2P;

	private Particle[] mParticles;
	private ParticleTypes mTypes;
	
	private float mWidth;
	private float mHeight;

	private float mCenterX;
	private float mCenterY;

	private float mAttractMean;
	private float mAttractStd;
	private float mMinRLower;
	private float mMinRUpper;
	private float mMaxRLower;
	private float mMaxRUpper;
	private float mFriction;
	private bool mFlatForce;

	

    void Awake () {
		//OnEnable();
    }

	void OnValidate () {
		if (enabled && mParticles != null) {
			OnDisable();
			OnEnable();
		}
	}

	void OnEnable () {
		diameter = 2f * radius;

		MakeSubdivisions();

		mWidth = Camera.main.WorldToScreenPoint(bounds.max).x;
		mHeight = Camera.main.WorldToScreenPoint(bounds.max).y;
		
		P2W = (bounds.extents.x * 2f) / mWidth;
		W2P = mWidth / (bounds.extents.x * 2f);
		screenBounds = bounds.extents * W2P;
        
		BigBang(numberOfTypes, numberOfParticles, (int)screenBounds.x * 2, (int)screenBounds.y * 2);
		ReSeed(-0.02f, 0.06f, 0.0f, 20.0f, 20.0f, 70.0f, 0.05f, false);
		StartCoroutine(UpdateParticleSubDivisions());
	}

	void MakeSubdivisions () {
		subDivisions = new List<int>[subDivisionsX * subDivisionsY];
		for (int i = 0; i < subDivisions.Length; i++) {
			subDivisions[i] = new List<int>();
		}
	}

	void OnDisable () {
		mTypes = null;
		for (int i = 0; i < mParticles.Length; i++) {
			Destroy(mParticles[i].transform.gameObject);
		}
		mParticles = null;

		for (int i = 0; i < subDivisions.Length; i++) {
			subDivisions[i].Clear();
		}

		subDivisions = null;
	}

	public void BigBang (int num_types, int num_particles, int width, int height) {

		SetPopulation(num_types, num_particles);
		mCenterX = mWidth * 0.5f;
		mCenterY = mHeight * 0.5f;

		mAttractMean = 0f;
		mAttractStd = 0f;
		mMinRLower = 0f;
		mMinRUpper = 0f;
		mMaxRLower = 0f;
		mMaxRUpper = 0f;
		mFriction = 0f;
		mFlatForce = false;
	}

	private void ReSeed (float attractMean, float attractStd, float minRLower, float minRUpper, float maxRLower, float maxRUpper, float friction, bool flatForce) {
		mAttractMean = attractMean;
		mAttractStd = attractStd;
		mMinRLower = minRLower;
		mMinRUpper = minRUpper;
		mMaxRLower = maxRLower;
		mMaxRUpper = maxRUpper;
		mFriction = friction;
		mFlatForce = flatForce;
		SetRandomTypes();
		SetRandomParticles();
	}

	private void SetPopulation (int numTypes, int numParticles) {
		mTypes = new ParticleTypes(numTypes);
		mParticles = new Particle[numParticles];
	}

	private void SetRandomTypes () {
		for (int i = 0; i < mTypes.Size(); i++) {

			mTypes.SetColor(i, Color.HSVToRGB((float)i / mTypes.Size(), 1f, (float)(i % 2) * 0.5f + 0.5f));
			
			for (int j = 0; j < mTypes.Size(); j++) {
				if (i == j) {
					mTypes.SetAttaract(i, j, 
						-Mathf.Abs(RandomNormal(mAttractMean, mAttractStd)));
					mTypes.SetMinR(i, j, diameter);
				}
				else {
					mTypes.SetAttaract(i, j, RandomNormal(mAttractMean, mAttractStd));
					mTypes.SetMinR(i, j, 
						Mathf.Max(Random.Range(mMinRLower, mMinRUpper), diameter));
				}

				mTypes.SetMaxR(i, j, 
					Mathf.Max(Random.Range(mMaxRLower, mMaxRUpper), mTypes.GetMinR(i, j)));

				mTypes.SetMaxR(j, i, mTypes.GetMaxR(i, j));
				mTypes.SetMinR(j, i, mTypes.GetMinR(i, j));
			}
		}
	}

	void CreateParticle (int i) {
		Particle p = mParticles[i];
		p.type = Random.Range(0, mTypes.Size());
		
		p.particleIndex = i;
		p.divisionIndex = 0;
		subDivisions[0].Add(i);

		p.x = (Random.value) * mWidth;
		p.y = (Random.value) * mHeight;
		p.vx = RandomNormal(0f, 1f) * 0.2f;
		p.vy = RandomNormal(0f, 1f) * 0.2f;
		
		GameObject pG = new GameObject("Particle");
		pG.transform.SetParent(transform, false);
		pG.transform.localScale = new Vector3(radius * P2W, radius * P2W, 1f) * 0.5f;

		pG.AddComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/Circle");
		pG.GetComponent<SpriteRenderer>().color = mTypes.GetColor(p.type);

		/*pG.AddComponent<LineRenderer>();
		pG.GetComponent<LineRenderer>().startWidth = radius * P2W;
		pG.GetComponent<LineRenderer>().endWidth = radius * P2W;
		pG.GetComponent<LineRenderer>().startColor = mTypes.GetColor(p.type);
		pG.GetComponent<LineRenderer>().endColor = mTypes.GetColor(p.type);*/

		p.transform = pG.transform;
		//p.lineRenderer = pG.GetComponent<LineRenderer>();

		mParticles[i] = p; // IMPORTANT!!!! DO THIS ANY TIME YOU MODIFY A PARTICLE!!!!
	}
	

	private void SetRandomParticles () {
		for (int i = 0; i < mParticles.Length; i++) {
			CreateParticle(i);
			//print(t);
		}
	}

	IEnumerator UpdateParticleSubDivisions () {
		for (;;) {
			for (int i = 0; i < mParticles.Length; i++) {
				Particle p = mParticles[i];
				
				int xDiv = (int)Mathf.Floor((p.x / mWidth) / (1f / subDivisionsX));
				int yDiv = (int)Mathf.Floor((p.y / mHeight) / (1f / subDivisionsY));

				int newIndex = xDiv * subDivisionsY + yDiv;
				int oldIndex = p.divisionIndex;
				int particleIndex = p.particleIndex;

				//Debug.Log(newIndex);

				if (newIndex != oldIndex) {
					subDivisions[oldIndex].Remove(particleIndex);
					subDivisions[newIndex].Add(particleIndex);
					//Debug.Log(subDivisions[newIndex].Count);
					p.divisionIndex = newIndex;
					mParticles[i] = p;
				}
			}
			yield return new WaitForSeconds(divUpdateDelay);
		}
	}

	void FixedUpdate () {
		Forces();
		Draws();
	}

	private void Draws () {
		for (int i = 0; i < mParticles.Length; i++) {
			Particle p = mParticles[i];
			Vector3 worldPos = new Vector3(p.x - (mWidth / 2f), p.y - (mHeight / 2f), 0f) * P2W;

			//Debug.Log(p.x);

			p.transform.position = worldPos;

			mParticles[i] = p;
		}
	}

	private void Forces () {
		int loops = 0;
		for (int i = 0; i < mParticles.Length; i++) {
			
			// Current particle
			Particle p = mParticles[i];

			int[] divsToCheck = {
				p.divisionIndex,   					// Zero
				p.divisionIndex - subDivisionsY,	// Left
				p.divisionIndex + subDivisionsY,	// Right
				p.divisionIndex - 1,				// Up
				p.divisionIndex + 1,				// Down
				p.divisionIndex + 1 - subDivisionsY, 	// Down left
				p.divisionIndex + 1 + subDivisionsY,	// Down right
				p.divisionIndex - 1 - subDivisionsY,	// Up left
				p.divisionIndex - 1 + subDivisionsY,	// Up right

			};

			//List<Vector3> linePositions = new List<Vector3>();

			for (int d = 0; d < 9; d++) {
				//Debug.Log(divsToCheck[0]);
				//Debug.Log(subDivisions);
				if (divsToCheck[d] <= subDivisions.Length - 1 && divsToCheck[d] >= 0 && Mathf.Floor(divsToCheck[d] / subDivisionsY) == Mathf.Floor(divsToCheck[0] / subDivisionsY)) {
					//Debug.Log("checking div: " + divsToCheck[d]);
					//Debug.Log(subDivisions[divsToCheck[d]].Count);

					List<int> div = subDivisions[divsToCheck[d]];
					// Interactions
					for (int j = 0; j < div.Count; j++) {
						// Other particle
						Particle q = mParticles[div[j]];

						// Get deltas
						float dx = q.x - p.x;
						float dy = q.y - p.y;
						
						// Get distance squared
						float r2 = dx * dx + dy * dy;
						float minR = mTypes.GetMinR(p.type, q.type);
						float maxR = mTypes.GetMaxR(p.type, q.type);

						if (r2 > maxR * maxR || r2 < 0.01f) {
							continue;
						}

						// Normalize displacement
						float r = Mathf.Sqrt(r2);
						dx /= r;
						dy /= r;

						// Calculate force
						float f = 0f;
						if (r > minR) {
							float numer = 2f * Mathf.Abs(r - 0.5f * (maxR - minR));
							float denom = maxR - minR;
							f = mTypes.GetAttaract(p.type, q.type) * (1f - numer / denom);
						}
						else {
							f = r_smooth * minR * (1f / (minR + r_smooth) - 1f / (r + r_smooth));
						}

						//f *= Time.fixedDeltaTime;

						// Apply force
						p.vx += f * dx;
						p.vy += f * dy;

						/*linePositions.Add(new Vector3((p.x - mWidth / 2f) * P2W, (p.y - mHeight / 2f) * P2W, 0f));
						linePositions.Add(new Vector3((q.x - mWidth / 2f) * P2W, (q.y - mHeight / 2f) * P2W, 0f));
						linePositions.Add(new Vector3((p.x - mWidth / 2f) * P2W, (p.y - mHeight / 2f) * P2W, 0f));*/


						loops += 1;
					}
				}
			}

			/*p.lineRenderer.SetPositions(linePositions.ToArray());
			p.lineRenderer.useWorldSpace = true;*/

			mParticles[i] = p;
		}

		// Update position
		for (int i = 0; i < mParticles.Length; i++) {
			// Current particle
			Particle p = mParticles[i];

			// Update position and velocity
			p.x += p.vx;
			p.y += p.vy;
			p.vx *= (1f - mFriction);
			p.vy *= (1f - mFriction);

			// Check for wall collisions
			if (mWrapMode) {
				if (p.x < 0f) {
					p.x += mWidth;
				}
				else if (p.x >= mWidth) {
					p.x -= mWidth;
				}

				if (p.y < 0f) {
					p.y += mHeight;
				}
				else if (p.y >= mHeight) {
					p.y -= mHeight;
				}
			}
			else {
				if (p.x <= diameter) {
					p.vx = -p.vx;
					p.x = diameter;
				}
				else if (p.x >= mWidth - diameter) {
					p.vx = -p.vx;
					p.x = mWidth - diameter;
				}

				if (p.y <= diameter) {
					p.vy = -p.vy;
					p.y = diameter;
				}
				else if (p.y >= mHeight - diameter) {
					p.vy = -p.vy;
					p.y = mHeight - diameter;
				}
			}

			mParticles[i] = p;
		}

		Debug.Log(loops);
	}

	private int GetIndex (int x, int y) {
		return 0;
	}

	private int GetParticleX (int index) {
		return 0;
	}

	private int GetParticleY (int index) {
		return 0;
	}

	void OnDrawGizmos () {
		Gizmos.color = Color.red;
		Gizmos.DrawLine(new Vector3(bounds.min.x, bounds.min.y), new Vector3(bounds.min.x, bounds.max.y));
		Gizmos.DrawLine(new Vector3(bounds.min.x, bounds.max.y), new Vector3(bounds.max.x, bounds.max.y));
		Gizmos.DrawLine(new Vector3(bounds.max.x, bounds.max.y), new Vector3(bounds.max.x, bounds.min.y));
		Gizmos.DrawLine(new Vector3(bounds.max.x, bounds.min.y), new Vector3(bounds.min.x, bounds.min.y));

		Gizmos.color = Color.yellow;
		//Gizmos.DrawLine(new Vector3((mWidth - mWidth / 2) * P2W, (mHeight - mHeight / 2) * P2W, 0f), new Vector3(0f, 0f));
		//Gizmos.DrawLine(new Vector3(Camera.main.WorldToScreenPoint(bounds.max).x * P2W, Camera.main.WorldToScreenPoint(bounds.max).y * P2W), Vector3.zero);

		/*Gizmos.DrawLine(new Vector3(-mWidth / 2f * P2W, mHeight / 2f * P2W), new Vector3(mWidth / 2f * P2W, mHeight / 2f * P2W));
		Gizmos.DrawLine(new Vector3(-mWidth / 2f * P2W, -mHeight / 2f * P2W), new Vector3(mWidth / 2f * P2W, -mHeight / 2f * P2W));

		for (int i = 1; i < subDivisions.Length - 1; i++) {
			float xCoord = (-mWidth / 2f) + (mWidth / subDivisionsX) * (i);
			Gizmos.DrawLine(new Vector3(xCoord * P2W, mHeight / 2f * P2W), new Vector3(xCoord * P2W, -mHeight / 2f * P2W));
		}

		Gizmos.DrawLine(new Vector3(mWidth / 2f * P2W, -mHeight / 2f * P2W), new Vector3(mWidth / 2f * P2W, mHeight / 2f * P2W));
		Gizmos.DrawLine(new Vector3(-mWidth / 2f * P2W, -mHeight / 2f * P2W), new Vector3(-mWidth / 2f * P2W, mHeight / 2f * P2W));

		for (int i = 1; i < subDivisions[0].Count - 1; i++) {
			float yCoord = (-mHeight / 2f) + (mHeight / subDivisionsY) * (i);
			Gizmos.DrawLine(new Vector3(mWidth / 2f * P2W, yCoord * P2W), new Vector3(-mWidth / 2f * P2W, yCoord * P2W));
		}*/
	}
}

