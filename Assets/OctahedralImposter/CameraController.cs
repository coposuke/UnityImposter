using UnityEngine;


[ExecuteInEditMode]
public class CameraController : MonoBehaviour
{
	[SerializeField]
	private bool isAuto;

	[SerializeField]
	private bool isAutoClockwise;

	[SerializeField, Tooltip("距離")]
	private float distance;

	[SerializeField, Tooltip("高度")]
	private float altitude;

	[SerializeField, Tooltip("方位")]
	private float azimuth;

	[SerializeField, Tooltip("視線先")]
	private Vector3 lookAt;

	private Transform CachedTransform { get { return (cachedTransform != null) ? cachedTransform : (cachedTransform = transform); } }

	private Transform cachedTransform;

	/// <summary>
	/// Unity Override Update
	/// </summary>
	private void Update()
	{
		if (Application.isPlaying && isAuto)
		{
			azimuth += Time.deltaTime * 50.0f * (isAutoClockwise ? -1 : 1);
		}

		var altitudeAngle = Quaternion.Euler(altitude, 0.0f, 0.0f);
		var azimuthAngle = Quaternion.Euler(0.0f, azimuth, 0.0f);
		var position = Vector3.zero;

		position = altitudeAngle * new Vector3(0.0f, 0.0f, distance);
		position = azimuthAngle * position;

		CachedTransform.localPosition = position;
		CachedTransform.LookAt(lookAt);
	}
}
