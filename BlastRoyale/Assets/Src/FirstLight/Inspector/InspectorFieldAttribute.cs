using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstLight.Inspector
{
	/// <summary>
	/// Concrete field behavior rendered by the game's native Unity property drawer.
	/// Member names refer to methods/fields on the field's declaring instance; expressions are unsupported.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class InspectorFieldAttribute : PropertyAttribute
	{
		public InspectorFieldAttribute() => order = -1000;
		public bool Required { get; set; }
		public bool ReadOnly { get; set; }
		public string Options { get; set; }
		public string OnChanged { get; set; }
		public string Validate { get; set; }
		public string ValidationMessage { get; set; }
		public string VisibleWhen { get; set; }
		public bool InvertVisibility { get; set; }
		public bool ChildObjectsOnly { get; set; }
	}

	/// <summary>Exposes a parameterless action, or an action with one editable bool argument, in the native inspector.</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class InspectorActionAttribute : Attribute
	{
		public InspectorActionAttribute(string label = null) => Label = label;
		public string Label { get; }
		public bool OnlyInPlayMode { get; set; }
	}

	/// <summary>A named value supplied by an actual inspector options method.</summary>
	public readonly struct InspectorOption
	{
		public InspectorOption(string label, object value)
		{
			Label = label;
			Value = value;
		}
		public string Label { get; }
		public object Value { get; }
	}

	public enum InspectorIssueSeverity
	{
		Info,
		Warning,
		Error
	}

	public readonly struct InspectorIssue
	{
		public InspectorIssue(string message, InspectorIssueSeverity severity)
		{
			Message = message;
			Severity = severity;
		}
		public string Message { get; }
		public InspectorIssueSeverity Severity { get; }
	}

	/// <summary>Provides concrete authoring diagnostics to the game's native inspector.</summary>
	public interface IInspectorValidator
	{
		IEnumerable<InspectorIssue> GetInspectorIssues();
	}
}
