using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
//using Unity.Mathematics;


//using static Unity.Mathematics.math;

namespace GK {
	[RequireComponent(typeof(Camera))]
	public class Stippling : MonoBehaviour {

		public IVSAsset IVS;
		//public Shader Shader;
		public ComputeShader VoronoiCompute;
		public Texture DebugTexture;
		
		//[Range(0.0f, 2.0f/1024.0f)]
		[Range(0.0f, 0.02f)]
		public float NibRadius;

		[Range(0, 1024*1024)]
		public int MaxStipples = 1024*1024;
		
		Material stipplingMat;
		Material blitMat;
		ComputeBuffer points;
		ComputeBuffer particles;
		ComputeBuffer args;
		CommandBuffer commands;

		Mesh quad;

		int stippleCount;

		Camera _cam;
		Camera cam {
			get {
				if (_cam == null) {
					_cam = GetComponent<Camera>();
				}

				return _cam;
			}
		}
		
		void Start() {
		}

		void Update() {
			SetShaderParams();
		}

		void SetShaderParams() {
			stipplingMat.SetFloat("_NibRadius", NibRadius);
			VoronoiCompute.SetInt("_MaxStipples", (int)Mathf.Min(stippleCount, MaxStipples));
			VoronoiCompute.SetFloat("_NibSize", Mathf.PI * NibRadius * NibRadius);
			VoronoiCompute.SetVector("_TexSize", new Vector2(cam.pixelWidth, cam.pixelHeight));
			stipplingMat.SetColor("_Color", Color.black);
			particles.SetCounterValue(0);

            var a = (float)cam.pixelWidth / (float)cam.pixelHeight;

            if (a > 1) {
                VoronoiCompute.SetFloats("_PixTrans",
                    1, 0,
                    0, 1/a
                );
            } else {
                VoronoiCompute.SetFloats("_PixTrans",
                    a, 0,
                    0, 1
                );
            }
        }

		void OnDisable() {
			if (points != null) {
				Destroy(blitMat);
				Destroy(stipplingMat);
				Destroy(quad);

				blitMat = null;
				stipplingMat = null;
				quad = null;

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

		void OnEnable() {
			if (!IVS) return;
			cam.forceIntoRenderTexture = true;
			
			var data = IVS.Points;
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

			blitMat = new Material(Shader.Find("Hidden/BlitFlip"));
			stipplingMat = new Material(Shader.Find("Hidden/Stippling"));

			SetShaderParams();

			stipplingMat.SetBuffer("_Particles", particles);
			uint gx, gy, gz;
			var kernel = VoronoiCompute.FindKernel("StreamStipples");
			VoronoiCompute.GetKernelThreadGroupSizes(kernel, out gx, out gy, out gz);

			var dispatch = 1 + (((uint)stippleCount-1)/gx);

			commands = new CommandBuffer();

			commands.name = "StipplingEffect";

			var texId = Shader.PropertyToID("_ScreenTexture");
			commands.GetTemporaryRT(texId, -1, -1, 0);
			commands.Blit(BuiltinRenderTextureType.CameraTarget, texId);

			commands.SetComputeTextureParam(VoronoiCompute, kernel, "_ScreenTexture", texId);
			commands.SetComputeBufferParam(VoronoiCompute, kernel, "_Points", points);
			commands.SetComputeBufferParam(VoronoiCompute, kernel, "_Particles", particles);

			commands.DispatchCompute(VoronoiCompute, kernel, (int)dispatch, 1, 1);
			commands.ReleaseTemporaryRT(texId);

			commands.ClearRenderTarget(true, true, Color.white);
			commands.CopyCounterValue(particles, args, 4);
			commands.DrawMeshInstancedIndirect(quad, 0, stipplingMat, -1, args, 0);

			cam.AddCommandBuffer(CameraEvent.AfterEverything, commands);
		}
	}
}
