using System;
using Godot;

namespace GazaGame.Core
{
    /// <summary>
    /// Safely loads and instances 3D models at runtime without crashing scene parsing.
    /// </summary>
    public partial class PropLoader : Node3D
    {
        [Export] public string ModelPath { get; set; } = "";

        public override void _Ready()
        {
            if (!string.IsNullOrEmpty(ModelPath) && ResourceLoader.Exists(ModelPath))
            {
                try
                {
                    var scene = GD.Load<PackedScene>(ModelPath);
                    if (scene != null)
                    {
                        var instance = scene.Instantiate();
                        AddChild(instance);
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[PropLoader] Could not load model '{ModelPath}': {ex.Message}");
                }
            }
        }
    }
}
