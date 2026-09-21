using UnityEngine;

namespace MagesDementiaGame
{
    public enum ResolutionInspectionTarget { Television, Photograph, Minh }

    public sealed class ResolutionInspectable : MonoBehaviour, IInteractable
    {
        [SerializeField] private ResolutionInspectionTarget target;
        private ResolutionSceneController controller;
        private bool inspected;

        public ResolutionInspectionTarget Target => target;
        public string Prompt => target == ResolutionInspectionTarget.Minh && inspected
            ? "E — Invite Minh to walk with you"
            : $"E — Reflect on {TargetName}";
        public bool CanInteract => controller != null &&
            ((!inspected && controller.FlowState == ResolutionFlowState.InspectConsequences) ||
             (target == ResolutionInspectionTarget.Minh && inspected && controller.FlowState == ResolutionFlowState.InviteMinh));

        private string TargetName => target switch
        {
            ResolutionInspectionTarget.Television => "the television",
            ResolutionInspectionTarget.Photograph => "the photograph",
            _ => "Minh"
        };

        public void Initialize(ResolutionSceneController host)
        {
            controller = host;
            inspected = false;
            enabled = true;
        }

        public void SetInspected(bool value)
        {
            inspected = value;
            if (target != ResolutionInspectionTarget.Minh && value) enabled = false;
        }

        public void Interact(InteractionController interactor)
        {
            if (!CanInteract) return;
            if (target == ResolutionInspectionTarget.Minh && inspected) controller.InviteMinh();
            else controller.Inspect(target);
        }
    }
}
