using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace OctahedralImposter
{
	/// <summary>
	/// Preview Grid
	/// </summary>
	static public class OctahedralGrid
	{
		static public Mesh CreatePrimitive(float scale, int length, int strength, Color normalColor, Color strengthColor)
		{
			if (length <= 1)
			{
				Debug.LogWarning("OctahedralMeshGenerator length is must 1 over.");
				length = 2;
			}

			Vector2 add = Vector2.one * (1.0f / length);
			Vector2 offset = new Vector2(-scale * 0.5f, -scale * 0.5f);

			int points = (length + 1) * 4;
			Vector3[] vertices = new Vector3[points];
			Color[] colors = new Color[points];
			for (int line = 0, v = 0, c = 0; line <= length; line++)
			{
				vertices[v++] = new Vector3(offset.x,         0f, offset.y + line / (float)length * scale);
				vertices[v++] = new Vector3(offset.x + scale, 0f, offset.y + line / (float)length * scale);
				vertices[v++] = new Vector3(offset.x + line / (float)length * scale, 0f, offset.y        );
				vertices[v++] = new Vector3(offset.x + line / (float)length * scale, 0f, offset.y + scale);

				Color color = (line % strength == 0) ? strengthColor : normalColor;
				colors[c++] = color;
				colors[c++] = color;
				colors[c++] = color;
				colors[c++] = color;
			}

			int[] indecies = new int[points];
			for (int i = 0; i < points; i++)
				indecies[i] = i;

			Mesh mesh = new Mesh();
			mesh.vertices = vertices;
			mesh.colors = colors;
			mesh.SetIndices(indecies, MeshTopology.Lines, 0);
			return mesh;
		}
	}

	/// <summary>
	/// Octahedral Hemi-Sphere
	/// </summary>
	static public class OctahedralHemiSphere
	{
		static public Mesh CreatePrimitive(float radius, int length, float ratio = 1.0f)
		{
			if (length <= 1)
			{
				Debug.LogWarning("OctahedralMeshGenerator length is must 1 over.");
				length = 2;
			}

			length++;
			Vector2 add    = Vector2.one * (1.0f / (length - 1));
			Vector2 offset = new Vector2(-0.5f, -0.5f);

			Vector3[] vertices = new Vector3[length * length];
			Vector3[] normals = new Vector3[length * length];
			for (int y = 0; y < length; y++)
			{
				for (int x = 0; x < length; x++)
				{
					var vertex = new Vector3(add.x * x + offset.x, 0f, -add.y * y - offset.y) * 2.0f;
					float angle = Mathf.Atan2(vertex.z, vertex.x);

					float oneFrameRatio = 1.0f / Mathf.Max(Mathf.Abs(vertex.x), Mathf.Abs(vertex.z), 1e-5f);
					float maxMagnitude = Mathf.Max((vertex * oneFrameRatio).magnitude, 1.0f);

					Vector3 deformedVertex;
					deformedVertex = vertex / maxMagnitude;
					deformedVertex.y = Mathf.Cos(vertex.magnitude / maxMagnitude * Mathf.PI * 0.5f);
					deformedVertex.Normalize();

					vertices[y * length + x] = Vector3.Lerp(vertex, deformedVertex, ratio) * radius;
					normals[y * length + x] = Vector3.up;
				}
			}

			int lineSquares = length - 1;
			int lineSquaresHalf = Mathf.RoundToInt(lineSquares * 0.5f);
			int[] triangles = new int[lineSquares * lineSquares * 2 * 3];
			for (int y = 0, i = 0, square = 0; y < lineSquares; y++)
			{
				for (int x = 0; x < lineSquares; x++)
				{
					if (x < lineSquaresHalf ^ y < lineSquaresHalf)
					{
						triangles[i++] = square;
						triangles[i++] = square + 1;
						triangles[i++] = square + length;
						triangles[i++] = square + 1;
						triangles[i++] = square + length + 1;
						triangles[i++] = square + length;
					}
					else
					{
						triangles[i++] = square;
						triangles[i++] = square + 1;
						triangles[i++] = square + length + 1;
						triangles[i++] = square;
						triangles[i++] = square + length + 1;
						triangles[i++] = square + length;
					}
					square++;
				}
				square++;
			}


			Mesh mesh = new Mesh();
			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.triangles = triangles;
			return mesh;
		}
	}

	/// <summary>
	/// OctahedralEditor
	/// </summary>
	public class OctahedralTextureEditor : EditorWindow
	{
		[System.Serializable]
		private struct MouseAction
		{
			public Vector3 rotateEuler;
			public float zoom;
		};

		private PreviewRenderUtility renderer;
		[SerializeField]
		private MouseAction mouseAction;
		[SerializeField]
		private float modelRatio = 1.0f;
		[SerializeField]
		private int modelMeshes = 5;
		[SerializeField]
		private float modelScale = 1.0f;
		[SerializeField]
		private float cameraFov = 15.0f;
		[SerializeField]
		private int captureResolution = 10;
		[SerializeField]
		private Vector3 captureLookatOffset;
		[SerializeField]
		private GameObject targetObject = null;


		/// <summary>
		/// CreateWindow
		/// </summary>
		[MenuItem("Window/OctahedralTextureEditor")]
		public static void CreateWindow()
		{
			var window = EditorWindow.CreateWindow<OctahedralTextureEditor>();
			window.Show();
			window.mouseAction.zoom = 10.0f;
			window.mouseAction.rotateEuler = Quaternion.LookRotation(new Vector3(0f, -0.5f, 0.5f).normalized).eulerAngles;
		}

		/// <summary>
		/// Unity Event OnDestroy
		/// </summary>
		private void OnDestroy()
		{
			if (this.renderer != null)
				this.renderer.Cleanup();
		}

		/// <summary>
		/// Unity Event OnGUI
		/// </summary>
		private void OnGUI()
		{
			Setup();

			DrawScene();
			DrawUI();

			DoEvent();
			if (GUI.changed) { Repaint(); }
		}


		private void Setup()
		{
			if (this.renderer == null)
			{
				this.renderer = new PreviewRenderUtility();
				this.renderer.ambientColor = RenderSettings.ambientLight;
				this.renderer.m_Light = FindObjectsOfType<Light>();
			}
		}

		private void DrawUI()
		{
			modelRatio = EditorGUILayout.Slider("Texture -> Model", modelRatio, 0.0f, 1.0f);
			modelMeshes = EditorGUILayout.IntSlider("Model Meshes", modelMeshes, 3, 64);
			modelScale = EditorGUILayout.Slider("Model Scale", modelScale, 0.5f, 20.0f);
			cameraFov = EditorGUILayout.Slider("Camera FieldOfView", cameraFov, 0.5f, 60f);
			captureResolution = EditorGUILayout.IntSlider("Capture Resolution", captureResolution, 8, 13);
			EditorGUILayout.FloatField("Capture Resolution", Mathf.Pow(2, captureResolution));
			captureLookatOffset = EditorGUILayout.Vector3Field("Capture LookAt Offset", captureLookatOffset);

			if (GUILayout.Button("Capture"))
				Capture();
		}

		private void DrawScene()
		{
			var rect = new Rect(0, 0, this.position.size.x, this.position.size.y);
			this.renderer.camera.nearClipPlane = 0.1f;
			this.renderer.camera.farClipPlane = 10000.0f;
			this.renderer.camera.fieldOfView = cameraFov;
			this.renderer.camera.transform.position = Quaternion.Euler(mouseAction.rotateEuler) * new Vector3(0, 0, -this.mouseAction.zoom);
			this.renderer.camera.transform.rotation = Quaternion.Euler(mouseAction.rotateEuler);
			this.renderer.camera.clearFlags = CameraClearFlags.SolidColor;

			var mesh = OctahedralHemiSphere.CreatePrimitive(modelScale, modelMeshes, modelRatio);
			var grid = OctahedralGrid.CreatePrimitive(10.0f, 100, 10, Color.white * 0.125f, Color.white * 0.25f);
			var gridMaterial = new Material(Shader.Find("OctahedralImposter/Grid"));
			var material = new Material(Shader.Find("OctahedralImposter/Wireframe"));

			renderer.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0);
			renderer.DrawMesh(grid, Vector3.zero, Quaternion.identity, gridMaterial, 0);

			this.renderer.BeginPreview(rect, GUIStyle.none);
			this.renderer.camera.Render();
			var tex = this.renderer.EndPreview();

			GameObject.DestroyImmediate(mesh);
			GameObject.DestroyImmediate(grid);

			GUI.DrawTexture(rect, tex);
		}

		private void DoEvent()
		{
			switch (Event.current.type)
			{
				case EventType.DragUpdated:
				case EventType.DragPerform:
					OnDragAndDrop();
					break;

				case EventType.MouseDrag:
					OnMouseDrag();
					break;

				case EventType.ScrollWheel:
					OnMouseScrollWheel();
					break;
			}
		}

		void OnDragAndDrop()
		{
			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

			if (Event.current.type == EventType.DragPerform)
			{
				DragAndDrop.AcceptDrag();

				foreach (var path in DragAndDrop.paths)
				{
					if (!LoadModel(path)) { continue; }
				}

				DragAndDrop.activeControlID = 0;
			}

			Event.current.Use();
		}

		private void OnMouseDrag()
		{
			if (Event.current.type == EventType.MouseDrag)
			{
				var rect = new Rect(0, 0, this.position.size.x, this.position.size.y);
				Vector3 delta = Event.current.delta;
				Vector2 ratio = new Vector2(rect.width / this.renderer.camera.pixelWidth, rect.height / this.renderer.camera.pixelHeight);
				delta.x /= ratio.x;
				delta.y /= ratio.y;

				mouseAction.rotateEuler += new Vector3(360f * delta.y / rect.height, 360f * delta.x / rect.width);
				Repaint();
			}
		}

		private void OnMouseScrollWheel()
		{
			this.mouseAction.zoom += Event.current.delta.y * this.mouseAction.zoom * 1e-2f;
			Repaint();
		}

		private bool LoadModel(string path)
		{
			if (string.IsNullOrEmpty(path)) { return false; }

			var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (null == asset) { return false; }

			if (null != this.targetObject)
				DestroyImmediate(this.targetObject);

			this.targetObject = Instantiate<GameObject>(asset);
			this.renderer.AddSingleGO(this.targetObject);
			return true;
		}

		private void Capture()
		{
			var mesh = OctahedralHemiSphere.CreatePrimitive(modelScale, modelMeshes, modelRatio);
			var meshVertices = mesh.vertices;
			var rect = new Rect(0, 0, this.position.size.x, this.position.size.y);
			var captures = modelMeshes + 1;
			var oneSize = 1.0f / captures;

			int textureResolution = Mathf.FloorToInt(Mathf.Pow(2, this.captureResolution));
			var bgColor = this.renderer.camera.backgroundColor;
			this.renderer.BeginPreview(new Rect(0f, 0f, textureResolution, textureResolution), GUIStyle.none);

			for (int i = 0; i < meshVertices.Length; ++i)
			{
				var capturePoint = meshVertices[i];
				var captureLookAt = Quaternion.LookRotation(Vector3.Normalize(this.targetObject.transform.localPosition + captureLookatOffset - capturePoint));

				this.renderer.camera.transform.localPosition = capturePoint;
				this.renderer.camera.transform.localRotation = captureLookAt;

				this.renderer.camera.rect = new Rect(oneSize * (i % captures), oneSize * (i / captures), oneSize, oneSize);
				this.renderer.camera.backgroundColor = Color.clear;
				this.renderer.camera.Render();
			}

			var tex = this.renderer.EndPreview();
			var tex2D = ToTexture2D(tex);
			System.IO.File.WriteAllBytes(Application.dataPath + "/OctahedralImposter/capture.png", tex2D.EncodeToPNG());

			this.renderer.camera.backgroundColor = bgColor;
			this.renderer.camera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

			Debug.Log(Application.dataPath);
			DestroyImmediate(mesh);
			AssetDatabase.Refresh();
		}

		// http://baba-s.hatenablog.com/entry/2018/02/26/210100
		public static Texture2D ToTexture2D(Texture self)
		{
			var sw = self.width;
			var sh = self.height;
			var format = TextureFormat.RGBA32;
			var result = new Texture2D(sw, sh, format, false);
			var currentRT = RenderTexture.active;
			var rt = new RenderTexture(sw, sh, 32);
			Graphics.Blit(self, rt);
			RenderTexture.active = rt;
			var source = new Rect(0, 0, rt.width, rt.height);
			result.ReadPixels(source, 0, 0);
			result.Apply();
			RenderTexture.active = currentRT;
			return result;
		}
	}
}
