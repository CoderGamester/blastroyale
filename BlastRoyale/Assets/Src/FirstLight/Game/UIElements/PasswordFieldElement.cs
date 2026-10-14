using UnityEngine.UIElements;
namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays the password field
	/// </summary>
	[UxmlElement]
	public partial class PasswordFieldElement : LocalizedTextField
	{
		private const string UssViewHideButton = "view-hide-button";
		private const string UssViewHideButtonShow = UssViewHideButton + "--show";

		private readonly ImageButton _viewHideButton;

		public PasswordFieldElement()
		{
			isPasswordField = true;
			Add(_viewHideButton = new ImageButton(OnViewHideClicked) { name = "view-hide-button" });
			_viewHideButton.AddToClassList(UssViewHideButton);
		}

		private void OnViewHideClicked()
		{
			_viewHideButton.ToggleInClassList(UssViewHideButtonShow);
			isPasswordField = !_viewHideButton.ClassListContains(UssViewHideButtonShow);
		}
	}
}
