using UnityEngine;

namespace MagesDementiaGame
{
    public enum RoomPerspective { Caregiver, Recipient }
    public enum TelevisionState { On, Lowered, Off }

    /// <summary>Configures the authored SharedRoom prefab without creating scene objects.</summary>
    public sealed class RoomView : MonoBehaviour
    {
        [Header("Art")]
        [SerializeField] private RoomArtSet artSet;
        [Header("Environment")]
        [SerializeField] private GameObject television;
        [SerializeField] private GameObject table;
        [SerializeField] private GameObject sofa;
        [SerializeField] private GameObject photograph;
        [SerializeField] private GameObject photographSpot;
        [SerializeField] private GameObject approachStagingPoint;
        [Header("Characters")]
        [SerializeField] private GameObject lan;
        [SerializeField] private GameObject minh;
        [SerializeField] private TopDownPlayerController lanPlayerController;
        [SerializeField] private TopDownPlayerController minhPlayerController;
        [SerializeField] private InteractionController lanInteractionController;
        [SerializeField] private InteractionController minhInteractionController;
        [SerializeField] private WaypointCharacterMover lanMover;
        [SerializeField] private WaypointCharacterMover minhMover;
        [Header("Authored positions")]
        [SerializeField] private Transform lanDoorway;
        [SerializeField] private Transform caregiverSpawn;
        [SerializeField] private Transform recipientSpawn;
        [SerializeField] private Transform approachStaging;
        [SerializeField] private Transform lanCloseApproach;
        [SerializeField] private Transform lanIntroduction;
        [SerializeField] private Transform[] lanEntranceWaypoints;
        [SerializeField] private Transform[] lanApproachWaypoints;

        private Color televisionOnColor = Color.white;

        public Camera RoomCamera { get; private set; }
        public GameObject Television => television;
        public GameObject Table => table;
        public GameObject Sofa => sofa;
        public GameObject Photograph => photograph;
        public GameObject PhotographSpot => photographSpot;
        public GameObject ApproachStagingPoint => approachStagingPoint;
        public GameObject Lan => lan;
        public GameObject Minh => minh;
        public TopDownPlayerController PlayerController { get; private set; }
        public InteractionController PlayerInteractionController { get; private set; }
        public WaypointCharacterMover LanMover => lanMover;
        public Vector3 LanDoorwayPosition => lanDoorway.position;
        public Vector3 ApproachStagingPosition => approachStaging.position;
        public Vector3 LanCloseApproachPosition => lanCloseApproach.position;
        public Vector3 LanIntroductionPosition => lanIntroduction.position;
        public Vector3[] LanEntranceWaypoints => Positions(lanEntranceWaypoints);

        public bool Configure(RoomPerspective perspective, IInteractionHost interactionHost)
        {
            if (!ValidateReferences()) { enabled = false; return false; }
            RoomCamera = Camera.main;
            if (RoomCamera == null)
            {
                Debug.LogError($"{name}: The authored room requires a scene camera tagged MainCamera.", this);
                enabled = false;
                return false;
            }

            var renderer = television.GetComponent<SpriteRenderer>();
            if (renderer != null) televisionOnColor = renderer.color;
            lan.GetComponentInChildren<CharacterVisualController>(true)?.Initialize(artSet.Lan);
            minh.GetComponentInChildren<CharacterVisualController>(true)?.Initialize(artSet.Minh);

            var caregiver = perspective == RoomPerspective.Caregiver;
            var caregiverTelevision = television.GetComponent<TelevisionInteractable>();
            var recipientTelevision = television.GetComponent<RecipientTelevisionInteractable>();
            var recipientPhoto = photographSpot.GetComponent<RecipientPhotoInteractable>();
            var approachMarker = approachStagingPoint.GetComponent<ApproachMarkerInteractable>();
            if (caregiverTelevision == null || recipientTelevision == null || recipientPhoto == null || approachMarker == null)
            {
                Debug.LogError($"{name}: SharedRoom has a missing interaction component. " +
                               "Reimport the prefab and verify that no component says Missing Script.", this);
                enabled = false;
                return false;
            }

            caregiverTelevision.enabled = caregiver;
            recipientTelevision.enabled = !caregiver;
            recipientPhoto.enabled = !caregiver;
            approachMarker.enabled = caregiver;
            ConfigureCharacter(lan, lanPlayerController, lanInteractionController, caregiver, 3f);
            ConfigureCharacter(minh, minhPlayerController, minhInteractionController, !caregiver, 1.6f);
            PlayerController = caregiver ? lanPlayerController : minhPlayerController;
            PlayerInteractionController = caregiver ? lanInteractionController : minhInteractionController;
            PlayerInteractionController.Initialize(interactionHost);
            lan.transform.position = caregiver ? caregiverSpawn.position : lanDoorway.position;
            minh.transform.position = recipientSpawn.position;
            lan.SetActive(caregiver);
            return true;
        }

        public Vector3[] BuildLanApproachRoute(Vector3 destination)
        {
            var route = new Vector3[lanApproachWaypoints.Length + 1];
            for (var i = 0; i < lanApproachWaypoints.Length; i++) route[i] = lanApproachWaypoints[i].position;
            route[route.Length - 1] = destination;
            return route;
        }

        public void SetLanVisible(bool visible) => lan.SetActive(visible);
        public void SetPhotoVisible(bool visible) => photograph.SetActive(visible);

        public void SetTelevisionState(TelevisionState state)
        {
            var renderer = television.GetComponent<SpriteRenderer>();
            if (renderer == null) return;
            renderer.color = state switch
            {
                TelevisionState.Lowered => Color.Lerp(televisionOnColor, Color.black, 0.4f),
                TelevisionState.Off => Color.Lerp(televisionOnColor, Color.black, 0.75f),
                _ => televisionOnColor
            };
        }

        private static void ConfigureCharacter(GameObject character, TopDownPlayerController player,
            InteractionController interaction, bool controlled, float speed)
        {
            player.Configure(speed);
            player.enabled = controlled;
            interaction.enabled = controlled;
            var body = character.GetComponent<Rigidbody2D>();
            body.bodyType = controlled ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            body.linearVelocity = Vector2.zero;
        }

        private bool ValidateReferences()
        {
            if (artSet != null && television != null && table != null && sofa != null && photograph != null &&
                photographSpot != null && approachStagingPoint != null && lan != null && minh != null &&
                lanPlayerController != null && minhPlayerController != null &&
                lanInteractionController != null && minhInteractionController != null &&
                lanMover != null && minhMover != null && lanDoorway != null && caregiverSpawn != null &&
                recipientSpawn != null && approachStaging != null && lanCloseApproach != null &&
                lanIntroduction != null && lanEntranceWaypoints != null && lanEntranceWaypoints.Length > 0 &&
                lanApproachWaypoints != null && lanApproachWaypoints.Length > 0) return true;

            Debug.LogError($"{name}: SharedRoom prefab is missing required RoomView references. Repair its serialized assignments.", this);
            return false;
        }

        private static Vector3[] Positions(Transform[] source)
        {
            var result = new Vector3[source.Length];
            for (var i = 0; i < source.Length; i++) result[i] = source[i].position;
            return result;
        }
    }
}
