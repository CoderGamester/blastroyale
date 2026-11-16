using Unity.Cinemachine;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EditorOnly
{
	public class FilmOrbit : MonoBehaviour
	{
		public float speed = 30f;

		private CinemachineOrbitalFollow _orbitalFollow;

		void Start()
		{
			_orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
		}

		void Update()
		{
			if (_orbitalFollow != null)
				_orbitalFollow.HorizontalAxis.Value += Time.deltaTime * speed;
		}
	}
}
