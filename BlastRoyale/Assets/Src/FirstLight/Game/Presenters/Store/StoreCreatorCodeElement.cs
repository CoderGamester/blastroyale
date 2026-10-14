using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters.Store
{

    [UxmlElement]
    public partial class StoreCreatorCodeElement : VisualElement
    {

        private VisualElement notSupportingCreatorContainer;
        private VisualElement supportingCreatorContainer;

        private Label creatorNameLabel;

        private LocalizedButton enterCodeButton;
        private LocalizedButton changeCodeButton;
        private LocalizedButton stopSupportingButton;

        private string currentCreatorSupporting;

        public Action OnEnterCodeClicked;
        public Action OnUpdateCodeClicked;
        public Action OnStopSupportingClicked;

        public StoreCreatorCodeElement()
        {
            var treeAsset = Resources.Load<VisualTreeAsset>("StoreCreatorCodeElement");
            treeAsset.CloneTree(this);

            notSupportingCreatorContainer = this.Q<VisualElement>("NotSupportingContainer");
            supportingCreatorContainer = this.Q<VisualElement>("SupportingContainer");

            creatorNameLabel = this.Q<Label>("SupportingCreatorNameText");

            enterCodeButton = this.Q<LocalizedButton>("EnterCodeButton");
            enterCodeButton.clicked += () => OnEnterCodeClicked?.Invoke();

            changeCodeButton = this.Q<LocalizedButton>("ChangeButton");
            changeCodeButton.clicked += () => OnUpdateCodeClicked?.Invoke();

            stopSupportingButton = this.Q<LocalizedButton>("StopButton");
            stopSupportingButton.clicked += () => OnStopSupportingClicked?.Invoke();
        }

        public void SetData(string creatorCode)
        {
            currentCreatorSupporting = creatorCode;

            if (string.IsNullOrEmpty(currentCreatorSupporting))
            {
                notSupportingCreatorContainer.SetDisplay(true);
                supportingCreatorContainer.SetDisplay(false);
                return;
            }

            notSupportingCreatorContainer.SetDisplay(false);
            supportingCreatorContainer.SetDisplay(true);

            creatorNameLabel.text = currentCreatorSupporting;
        }

        public void UpdateContentCreator(string previousValue, string newValue)
        {
            if (previousValue.Equals(newValue))
                return;

            SetData(newValue);
        }

    }
}
