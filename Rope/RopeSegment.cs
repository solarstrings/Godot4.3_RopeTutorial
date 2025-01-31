using Godot;
using System;

public partial class RopeSegment : RigidBody2D
{
	public int IndexInArray;                // Which index in the rope segment array the segment has
	public Godot.GodotObject Rope = null;   // The rope (which rope the segment is part of)	
}
