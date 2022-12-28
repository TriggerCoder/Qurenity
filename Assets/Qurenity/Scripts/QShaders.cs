using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
public class QShaderParser
{
	public static readonly string[] ignoreParams = { "//", "surfaceparms", "q3map_", "qer_", "tessSize", "cull" };

	public static string Parse(byte[] shaderBytes)
	{
		// Create a list to store the parsed shader stages
		List<QShaderStage> shaderStages = new List<QShaderStage>();

		// Create a memory stream and binary reader to read the byte array
		using (MemoryStream ms = new MemoryStream(shaderBytes))
		using (BinaryReader reader = new BinaryReader(ms))
		{
			// Read the shader name
			string shaderName = ReadNullTerminatedString(reader);

			// Read the number of stages
			int numStages = reader.ReadInt32();

			// Loop through each stage
			for (int i = 0; i < numStages; i++)
			{
				// Read the stage name
				string stageName = ReadNullTerminatedString(reader);

				// Create a new QShaderStage object and add it to the list
				QShaderStage stage = new QShaderStage(stageName);
				shaderStages.Add(stage);

				// Read the number of stage commands
				int numCommands = reader.ReadInt32();

				// Loop through each stage command
				for (int j = 0; j < numCommands; j++)
				{
					// Read the command name
					string commandName = ReadNullTerminatedString(reader);

					// Read the number of command parameters
					int numParams = reader.ReadInt32();

					// Create a list to store the command parameters
					List<string> commandParams = new List<string>();

					// Loop through each command parameter
					for (int k = 0; k < numParams; k++)
					{
						string param = ReadNullTerminatedString(reader);
						commandParams.Add(param);
					}

					// Add the stage command to the current QShaderStage object
					stage.AddCommand(commandName, commandParams);
				}
			}
		}
		return JsonUtility.ToJson(shaderStages);
	}

	private static string ReadNullTerminatedString(BinaryReader reader)
	{
		// Create a StringBuilder to store the string
		StringBuilder sb = new StringBuilder();

		// Read each character from the BinaryReader until a null character is found
		char ch;
		while ((ch = reader.ReadChar()) != '\0')
		{
			sb.Append(ch);
		}

		// Return the string
		return sb.ToString();
	}

}

// Class representing a stage in a Quake 3 shader
public class QShaderStage
{
	public string Name { get; }
	public List<QShaderCommand> Commands { get; }

	public QShaderStage(string name)
	{
		Name = name;
		Commands = new List<QShaderCommand>();
	}

	public void AddCommand(string name, List<string> parameters)
	{
		Commands.Add(new QShaderCommand(name, parameters));
	}

}

// Class representing a command in a Quake 3 shader stage
public class QShaderCommand
{
	public string Name { get; }
	public List<string> Parameters { get; }

	public QShaderCommand(string name, List<string> parameters)
	{
		Name = name;
		Parameters = parameters;
	}
}