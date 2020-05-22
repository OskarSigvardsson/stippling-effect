using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
//using Unity.Mathematics;

//using static Unity.Mathematics.math;

namespace GK {
	[RequireComponent(typeof(Camera))]
	public class Stippling : MonoBehaviour {

		public TextAsset VoronoiSet;
		//public Shader Shader;
		public ComputeShader VoronoiCompute;
		public Material Material;
		public Texture DebugTexture;
		
		//[Range(0.0f, 2.0f/1024.0f)]
		[Range(0.0f, 0.02f)]
		public float NibRadius;

		[Range(0, 1024*1024)]
		public int MaxStipples = 1024*1024;
		
		//Material material;
		ComputeBuffer points;
		ComputeBuffer particles;
		ComputeBuffer args;
		CommandBuffer commands;

		Mesh quad;

		int stippleCount;
		
		void Start() {
		}

		// void Update() {
		// 	SetShaderParams();
		// }

		void SetShaderParams() {
			Material.SetFloat("_NibRadius", NibRadius);
			VoronoiCompute.SetInt("_MaxStipples", (int)Mathf.Min(stippleCount, MaxStipples));
			VoronoiCompute.SetFloat("_NibSize", Mathf.PI * NibRadius * NibRadius);
			Material.SetColor("_Color", Color.black);
			particles.SetCounterValue(0);
		}

		void OnDisable() {
			if (points != null) {
				//Destroy(material);
				Destroy(quad);

				points.Release();
				particles.Release();
				args.Release();

				//GetComponent<Camera>().RemoveCommandBuffer(CameraEvent.AfterEverything, commands);

				points = null;
				particles = null;
				args = null;
				commands = null;
			}
		}

		Vector2[] GetPoints() {
			return VoronoiSet.text
				.Split('\n')
				.Where(str => str.IndexOf(',') != -1)
				.Select(str => str
					.Split(',')
					.Select(s => float.Parse(s))
					.ToArray())
				.Select(p => new Vector2(p[0], p[1]))
				.ToArray();
		}

		void OnEnable() {
			// material = new Material(Shader);
			
			// material.enableInstancing = true;
			
			var data = GetPoints();
			stippleCount = data.Length;
			
			Debug.Assert(stippleCount == (1024 * 1024));

			points    = new ComputeBuffer(stippleCount, 2 * sizeof(float), ComputeBufferType.Default);
			particles = new ComputeBuffer(stippleCount, 3 * sizeof(float), ComputeBufferType.Append);
			args      = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);

			points.SetData(data);

			quad = new Mesh();

			quad.name = "Quad";
			quad.vertices = new Vector3[] {
				new Vector3(-1.0f, -1.0f, 0.0f),
				new Vector3( 1.0f, -1.0f, 0.0f),
				new Vector3(-1.0f,  1.0f, 0.0f),
				new Vector3( 1.0f,  1.0f, 0.0f),
			};

			quad.uv = new Vector2[] {
				new Vector2(0.0f, 0.0f),
				new Vector2(1.0f, 0.0f),
				new Vector2(0.0f, 1.0f),
				new Vector2(1.0f, 1.0f),
			};

			quad.triangles = new int[] {
				0, 1, 2,
				1, 3, 2,
			};

			args.SetData(new uint[] {
					(uint)quad.GetIndexCount(0),
					(uint)0,
					(uint)quad.GetIndexStart(0),
					(uint)quad.GetBaseVertex(0),
					(uint)0,
				});

			SetShaderParams();

			Material.SetBuffer("_Particles", particles);
			uint gx, gy, gz;
			var kernel = VoronoiCompute.FindKernel("StreamStipples");
			VoronoiCompute.GetKernelThreadGroupSizes(kernel, out gx, out gy, out gz);

			var dispatch = 1 + (((uint)stippleCount-1)/gx);

			// VoronoiCompute.SetBuffer(kernel, "_Points", points);
			// VoronoiCompute.SetBuffer(kernel, "_Particles", particles);

			// VoronoiCompute.Dispatch(kernel, (int)dispatch, 1, 1);

			// ComputeBuffer.CopyCount(particles, args, 4);

			// var buf = new uint[5];

			// args.GetData(buf);

			// foreach (var v in buf) {
			// 	Debug.Log(v);
			// }
			commands = new CommandBuffer();

			commands.name = "TestBuf";

			// var texId = Shader.PropertyToID("_ScreenTexture");
			// commands.GetTemporaryRT(texId, -1, -1, 0);
			// commands.Blit(BuiltinRenderTextureType.CurrentActive, texId);
			
			// commands.SetComputeTextureParam(
			// 	VoronoiCompute, kernel, "_",
			// 	BuiltinRenderTextureType.CurrentActive);
			// commands.SetComputeTextureParam(VoronoiCompute, kernel, "_ScreenTexture", texId);

			// commands.SetComputeBufferParam(VoronoiCompute, kernel, "_Points", points);
			// commands.SetComputeBufferParam(VoronoiCompute, kernel, "_Particles", particles);
			//commands.DispatchCompute(VoronoiCompute, kernel, (int)dispatch, 1, 1);
			// commands.ReleaseTemporaryRT(texId);
			commands.ClearRenderTarget(true, true, Color.white);
			commands.CopyCounterValue(particles, args, 4);
			commands.DrawMeshInstancedIndirect(quad, 0, Material, -1, args, 0);
			// commands.DrawMeshInstanced(quad, 0, Material, -1, new Matrix4x4[] { Matrix4x4.identity, Matrix4x4.identity }, 2);
			// // commands.DrawMeshInstancedProcedural(quad, 0, Material, -1, 10);
			// //commands.DrawMesh(quad, Matrix4x4.identity, material, 0);

			// GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterEverything, commands);
		}
			
		void FlushGPU() {
			GL.Flush();
			var bufIn = new ComputeBuffer(1024, sizeof(int), ComputeBufferType.Default);
			var bufOut = new ComputeBuffer(1024, sizeof(int), ComputeBufferType.Default);

			bufIn.SetData(Enumerable.Range(0, 1024).ToArray());
			var kernel = VoronoiCompute.FindKernel("DebugFlush");

			VoronoiCompute.SetBuffer(kernel, Shader.PropertyToID("_DebugIn"), bufIn);
			VoronoiCompute.SetBuffer(kernel, Shader.PropertyToID("_DebugOut"), bufOut);

			VoronoiCompute.Dispatch(kernel, 1024/64, 1, 1);

			bufOut.GetData(new int[1024]);
			bufIn.Release();
			bufOut.Release();

			GL.Flush();
		}

		void OnRenderImage(RenderTexture src, RenderTexture dst) {
			SetShaderParams();

			uint gx, gy, gz;
			var kernel = VoronoiCompute.FindKernel("StreamStipples");
			VoronoiCompute.GetKernelThreadGroupSizes(kernel, out gx, out gy, out gz);

			var dispatch = 1 + (((uint)stippleCount-1)/gx);
			var rt = new RenderTexture(src);

			VoronoiCompute.SetVector("_TexSize", new Vector2(src.width, src.height));
			VoronoiCompute.SetTexture(kernel, "_ScreenTexture", src);
			VoronoiCompute.SetBuffer(kernel, "_Points", points);
			VoronoiCompute.SetBuffer(kernel, "_Particles", particles);
			VoronoiCompute.Dispatch(kernel, (int)dispatch, 1, 1);
			
			//var oldRt = RenderTexture.active;

			RenderTexture.active = dst;

			Graphics.ExecuteCommandBuffer(commands);

			//GL.Clear(true, true, Color.white);
			// ComputeBuffer.CopyCount(particles, args, 4);
			// Material.SetBuffer("_Particles", particles);

			// args.SetData(new uint[] {
			// 		(uint)quad.GetIndexCount(0),
			// 		(uint)17,
			// 		(uint)quad.GetIndexStart(0),
			// 		(uint)quad.GetBaseVertex(0),
			// 		(uint)0,
			// 	});

			// Graphics.DrawMeshInstancedIndirect(
			// 	quad,
			// 	0,
			// 	Material,
			// 	new Bounds(Vector3.zero, 100000 * Vector3.one),
			// 	args);
			// 	// 0,
			// 	// null,
			// 	// ShadowCastingMode.Off,
			// 	// false,
			// 	// 0,
			// 	// GetComponent<Camera>(),
			// 	// LightProbeUsage.Off,
			// 	// null);

			// Graphics.DrawMeshInstanced(
			// 	quad,
			// 	0,
			// 	Material,
			// 	new Matrix4x4[] { Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity });
				

			//RenderTexture.active = oldRt;

			//Graphics.Blit(rt, dst);
		}

		// void OnRenderImage(RenderTexture src, RenderTexture dst) {
		// 	try {
		// 		if (points == null) {
		// 			Initialize();
		// 		}

		// 		particles.SetCounterValue(0);

		// 		var kernel = VoronoiCompute.FindKernel("StreamStipples");

		// 		VoronoiCompute.SetTexture(kernel, Shader.PropertyToID("_MainTex"), src);
		// 		VoronoiCompute.SetBuffer(kernel, Shader.PropertyToID("_Points"), points);
		// 		VoronoiCompute.SetBuffer(kernel, Shader.PropertyToID("_Particles"), particles);
				
		// 		VoronoiCompute.Dispatch(kernel, (1024*1024)/256, 1, 1);

		// 		var oldRt = RenderTexture.active;
				
		// 		Graphics.SetRenderTarget(dst);
				
		// 		ComputeBuffer.CopyCount(particles, count, 0);

		// 		material.SetBuffer("_Particles", particles);

		// 		GL.Clear(true, true, Color.white);
		// 		Graphics.DrawMeshInstancedIndirect(
		// 			quad,
		// 			0,
		// 			material,
		// 			new Bounds(Vector3.zero, 1000000000*Vector3.one),
		// 			count);

		// 		var countCPU = new int[1];

		// 		count.GetData(countCPU);

		// 		Debug.Log(countCPU[0]);
		// 	} catch {
		// 		Graphics.Blit(src, dst);
		// 	}
		// }
	}
}
