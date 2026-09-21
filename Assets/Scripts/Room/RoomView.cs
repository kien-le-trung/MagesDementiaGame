using UnityEngine;

namespace MagesDementiaGame
{
    public enum RoomPerspective { Caregiver, Recipient, Resolution }
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
        [Header("Resolution")]
        [SerializeField] private ResolutionInspectable resolutionTelevision;
        [SerializeField] private ResolutionInspectable resolutionPhotograph;
        [SerializeField] private ResolutionInspectable resolutionMinh;
        private CompanionFollowController minhFollower;
        [SerializeField] private GameObject sofaWithMinh;
        [SerializeField] private Transform resolutionLanSpawn;
        [SerializeField] private Transform resolutionMinhSeat;
        [SerializeField] private Transform resolutionExit;
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
        public bool IsLanAtResolutionExit => resolutionExit != null && Vector2.Distance(lan.transform.position, resolutionExit.position) < 1.25f;
        public bool IsMinhAtResolutionExit => resolutionExit != null && Vector2.Distance(minh.transform.position, resolutionExit.position) < 2.25f;

        public bool Configure(RoomPerspective perspective, IInteractionHost interactionHost)
        {
            minhFollower = minh != null ? minh.GetComponent<CompanionFollowController>() : null;
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
            var recipient = perspective == RoomPerspective.Recipient;
            var resolution = perspective == RoomPerspective.Resolution;
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
            recipientTelevision.enabled = recipient;
            recipientPhoto.enabled = recipient;
            approachMarker.enabled = caregiver;
            resolutionTelevision.enabled = resolution;
            resolutionPhotograph.enabled = resolution;
            resolutionMinh.enabled = resolution;
            ConfigureCharacter(lan, lanPlayerController, lanInteractionController, caregiver || resolution,
                resolution ? 2.2f : 3f);
            ConfigureCharacter(minh, minhPlayerController, minhInteractionController, recipient, 1.6f);
            PlayerController = caregiver || resolution ? lanPlayerController : minhPlayerController;
            PlayerInteractionController = caregiver || resolution ? lanInteractionController : minhInteractionController;
            PlayerInteractionController.Initialize(interactionHost);
            lan.transform.position = resolution ? resolutionLanSpawn.position : caregiver ? caregiverSpawn.position : lanDoorway.position;
            minh.transform.position = resolution ? resolutionMinhSeat.position : recipientSpawn.position;
            lan.SetActive(caregiver || resolution);
            minh.SetActive(true);
            if (sofaWithMinh != null)
            {
                var combinedRenderer = sofaWithMinh.GetComponent<SpriteRenderer>();
                var hasCombinedArt = combinedRenderer != null && combinedRenderer.sprite != null;
                sofaWithMinh.SetActive(resolution && hasCombinedArt);
                if (resolution && hasCombinedArt) minh.SetActive(false);
            }
            return true;
        }

        public void InitializeResolutionInteractions(ResolutionSceneController controller)
        {
            resolutionTelevision.Initialize(controller);
            resolutionPhotograph.Initialize(controller);
            resolutionMinh.Initialize(controller);
        }

        public void SetResolutionInspectionAvailable(ResolutionInspectionTarget target, bool available)
        {
            var inspectable = target switch
            {
                ResolutionInspectionTarget.Television => resolutionTelevision,
                ResolutionInspectionTarget.Photograph => resolutionPhotograph,
                _ => resolutionMinh
            };
            inspectable.SetInspected(!available);
        }

        public void BeginResolutionEscort()
        {
            sofaWithMinh?.SetActive(false);
            minh.SetActive(true);
            minh.transform.position = resolutionMinhSeat.position;
            var body = minh.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            minhFollower.BeginFollowing(lan.transform);
        }

        public void StopResolutionEscort() => minhFollower.StopFollowing();

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
            // Preserve the actual television-stand artwork in every state. A cool
            // screen tint distinguishes an active TV without blacking out the cabinet.
            renderer.color = state switch
            {
                TelevisionState.Lowered => new Color(0.82f, 0.9f, 0.94f, televisionOnColor.a),
                TelevisionState.Off => Color.white,
                _ => new Color(0.72f, 0.9f, 1f, televisionOnColor.a)
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
                lanApproachWaypoints != null && lanApproachWaypoints.Length > 0 &&
                resolutionTelevision != null && resolutionPhotograph != null && resolutionMinh != null &&
                minhFollower != null && sofaWithMinh != null && resolutionLanSpawn != null &&
                resolutionMinhSeat != null && resolutionExit != null) return true;

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
