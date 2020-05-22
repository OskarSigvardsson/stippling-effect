using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace GK {
	public class InstancingTest : MonoBehaviour {
		public int Count;
		
		public Mesh Mesh;
		public Material Material;

		CommandBuffer cmds;
		ComputeBuffer pos;
		ComputeBuffer args;

		void OnEnable() {
			// var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
			// var mesh = go.GetComponent<MeshFilter>().mesh;

			// AssetDatabase.CreateAsset(mesh, "Assets/quad.asset");
			
			var matrices = new Matrix4x4[Count];
			var positions = new float2[Count];

			pos = new ComputeBuffer(Count, 2 * sizeof(float), ComputeBufferType.Default);
			args = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
			
			for (int i = 0; i < Count; i++) {
				matrices[i] = Matrix4x4.Translate(new Vector3(i, 0, 0));
				positions[i] = float2(i, 0);
			}

			args.SetData(new uint[] {
					(uint)Mesh.GetIndexCount(0),
					(uint)Count,
					(uint)Mesh.GetIndexStart(0),
					(uint)Mesh.GetBaseVertex(0),
					(uint)0
				});

			pos.SetData(positions);

			cmds = new CommandBuffer();
			cmds.ClearRenderTarget(true, true, Color.white);
			cmds.SetGlobalBuffer("_Particles", pos);
			//cmds.DrawMeshInstanced(Mesh, 0, Material, -1, matrices, Count);
			cmds.DrawMeshInstancedIndirect(Mesh, 0, Material, -1, args);
			//cmds.DrawMesh(Mesh, Matrix4x4.identity, Material, 0, -1);

			Camera.main.AddCommandBuffer(CameraEvent.AfterSkybox, cmds);
		}

		void OnDisable() {
			pos.Release();
		}
			

		// void Update() {
		// 	Graphics.ExecuteCommandBuffer(cmds);
		// }
	}
}
