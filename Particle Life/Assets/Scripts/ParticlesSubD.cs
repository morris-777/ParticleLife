using UnityEngine;
using System.Collections.Generic;

struct Particle {
	public int type; // 4 bytes
	public int divisionIndex;
	public int particleIndex;
	public float x, y; // 2 * 4 bytes
	public float vx, vy; // 2 * 4 bytes
	public Transform transform;
	public LineRenderer lineRenderer;
}

public class ParticleTypes {

	public ParticleTypes (int size) {
		mCol = new Color[size];
		mAttract = new float[size * size];
		mMinR = new float[size * size];
		mMaxR = new float[size * size];
	}

	public int Size () => mCol.Length;

	public Color GetColor (int i) => mCol[i];
	public void SetColor (int i, Color color) { mCol[i] = color; }
	
	public float GetAttaract (int i, int j) => mAttract[i * mCol.Length + j];
	public void SetAttaract (int i, int j, float v) { mAttract[i * mCol.Length + j] = v; }
	
	public float GetMinR (int i, int j) => mMinR[i * mCol.Length + j];
	public void SetMinR (int i, int j, float v) { mMinR[i * mCol.Length + j] = v; }
	
	public float GetMaxR (int i, int j) => mMaxR[i * mCol.Length + j];
	public void SetMaxR (int i, int j, float v) { mMaxR[i * mCol.Length + j] = v; }

	private Color[] mCol;
	private float[] mAttract;
	private float[] mMinR;
	private float[] mMaxR;
}
