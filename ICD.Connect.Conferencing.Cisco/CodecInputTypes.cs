﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Conferencing.Cisco
{
	public sealed class CodecInputTypes
	{
		private readonly Dictionary<int, eCodecInputType> m_InputTypes;
		private readonly SafeCriticalSection m_InputTypesSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public CodecInputTypes()
		{
			m_InputTypes = new Dictionary<int, eCodecInputType>();
			m_InputTypesSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Sets the input type at the given input address.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		public void SetInputType(int input, eCodecInputType type)
		{
			m_InputTypesSection.Execute(() => m_InputTypes[input] = type);
		}

		/// <summary>
		/// Gets the input type for the given input address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public eCodecInputType GetInputType(int input)
		{
			return m_InputTypesSection.Execute(() => m_InputTypes.GetDefault(input, eCodecInputType.None));
		}

		/// <summary>
		/// Gets the inputs of the given type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<int> GetInputs(eCodecInputType type)
		{
			m_InputTypesSection.Enter();

			try
			{
				return m_InputTypes.Where(kvp => kvp.Value == type)
				                   .Select(kvp => kvp.Key)
				                   .Order()
				                   .ToArray();
			}
			finally
			{
				m_InputTypesSection.Leave();
			}
		}

		#endregion

		#region Settings

		/// <summary>
		/// Clears the configured input types.
		/// </summary>
		public void ClearSettings()
		{
			m_InputTypesSection.Execute(() => m_InputTypes.Clear());
		}

		/// <summary>
		/// Updates the input types from the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		public void ApplySettings(CiscoCodecSettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException("settings");

			ClearSettings();

			SetInputType(1, settings.Input1CodecInputType);
			SetInputType(2, settings.Input2CodecInputType);
			SetInputType(3, settings.Input3CodecInputType);
			SetInputType(4, settings.Input4CodecInputType);
		}

		/// <summary>
		/// Copies the input types onto the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		public void CopySettings(CiscoCodecSettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException("settings");

			settings.Input1CodecInputType = GetInputType(1);
			settings.Input2CodecInputType = GetInputType(2);
			settings.Input3CodecInputType = GetInputType(3);
			settings.Input4CodecInputType = GetInputType(4);
		}

		#endregion
	}
}
