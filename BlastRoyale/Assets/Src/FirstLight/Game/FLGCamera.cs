using FirstLight.Inspector;
using Unity.Cinemachine;
using FirstLight.Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstLight.Game
{
	/// <summary>
	/// The Main camera of the game
	/// </summary>
	public class FLGCamera : MonoSingleton<FLGCamera>
	{
		[SerializeField, InspectorField(Required = true)] private Camera _camera;
		[SerializeField, InspectorField(Required = true)] private CinemachineBrain _cinemachineBrain;
		[SerializeField, InspectorField(Required = true)] private PhysicsRaycaster _physicsRaycaster;

		public Camera MainCamera => _camera;

		public PhysicsRaycaster PhysicsRaycaster => _physicsRaycaster;

		public CinemachineBrain CinemachineBrain => _cinemachineBrain;

		protected override void _Awake()
		{
			DontDestroyOnLoad(gameObject);
		}
	}
}
