using Godot;
using System;
using System.Collections.Generic;

public partial class Rope : Node2D
{

	[Export]
	public bool StaticRopeEnd = false;                                  // If the end of the rope is attached to a wall

	[Export]
	public float intervalScaleFactor = 0.03f;                           // How much to scale the spacing between the pinjoints depending on the rope length	
	private PackedScene RopeSegmentPackedScene;                         // Variable to hold the rope segment packed scene	
	private RopeSegment RopeStart;                                      // The rope start segment
	private RopeSegment RopeEnd;                                        // The rope end segment	
	private PinJoint2D RopeStartPinJoint;                               // The pinjoint for the rope start segment
	private PinJoint2D RopeEndPinJoint;                                 // The pinjoint for the rope end segment
	private List<Vector2> RopePointsLine2D;                             // The Line2D points for drawing the rope			
	private Line2D Line2DNode;                                          // The Line2D node	
	public List<RopeSegment> RopeSegments = new List<RopeSegment>();    // List with all the rope segments

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		RopePointsLine2D = new List<Vector2>();                                         // Create a new list for the Line2D points	
		RopeSegmentPackedScene = GD.Load<PackedScene>("res://Rope/rope_segment.tscn");  // Load the rope segment scene		
		Line2DNode = GetNode<Line2D>("Line2D");                                         // Get the Line2D node
		RopeStart = GetNode<RopeSegment>("RopeStart");                                  // Get the rope start node
		RopeEnd = GetNode<RopeSegment>("RopeEnd");                                      // Get the rope end node
		RopeStart.Rope = this;                                                          // Set the parent node for the start node to this node
		RopeEnd.Rope = this;                                                            // Set the parent node for the end node to this node
		RopeStartPinJoint = GetNode<PinJoint2D>("RopeStart/PinJoint2D");                // Get the rope start pinjoint node
		RopeEndPinJoint = GetNode<PinJoint2D>("RopeEnd/PinJoint2D");                    // Get the rope end pinjoint node		

		SpawnRope();                                                                    // Spawn a new rope between the start and end nodes
	}

	public void SpawnRope()
	{
		Vector2 ropeStartPos = RopeStartPinJoint.GlobalPosition;        // Start position from the pin joint
		Vector2 ropeEndPos = RopeEndPinJoint.GlobalPosition;            // End position from the pin joint
		var dist = ropeStartPos.DistanceTo(ropeEndPos);                 // Get the total distance between start and end points
																		// Dynamically calculate the interval based on the rope length
		float baseInterval = 10.0f;                                     // Minimum distance between pinjoints for short ropes		
		float interval = baseInterval + (dist * intervalScaleFactor);   // Dynamic interval proportional to rope length		
		Vector2 direction = (ropeEndPos - ropeStartPos).Normalized();   // Calculate the direction vector from start to end and normalize it	
		var numSegments = Mathf.CeilToInt(dist / interval);             // Calculate the total number of segments based on the dynamic interval		
		var rotationAngle = direction.Angle() - Mathf.Pi / 2;           // Calculate the rotation angle of the rope			
		RopeStart.IndexInArray = 0;                                     // Set the start node index in the array
		Vector2 currentPos = ropeStartPos;                              // Initialize the position for the first joint		
		RopeSegment latestSegment = RopeStart;                          // Set the latest segment to the rope start segment

		RopeSegments.Clear();                                           // Clear the rope segments list		
		RopeSegments.Add(latestSegment);                                // Add the ropestart segment as start of the rope		

		// Loop through all the segments
		for (int i = 0; i < numSegments; i++)
		{
			currentPos += direction * interval;                                             // Move to the next position along the direction vector	
			latestSegment = AddRopeSegment(latestSegment, i + 1, rotationAngle, currentPos);// Add a new rope segment at the calculated position		
			RopeSegments.Add(latestSegment);                                                // Add the rope segment to the list of rope segments	

			var jointPos = latestSegment.GetNode<PinJoint2D>("PinJoint2D").GlobalPosition;  // Get the Pinjoint2D position from the latest segment	

			// If the distance between the joint position and the rope end is less than the length of one interval
			if (jointPos.DistanceTo(ropeEndPos) < interval)
			{
				// Break out of the for loop, the rope has been built, no need to add more segments
				break;
			}
		}
		ConnectRopeParts(RopeEnd, latestSegment);   // Connect the rope end pinjoint with the latest segment
		RopeEnd.Rotation = rotationAngle;           // Set the rope end rotation	
		RopeSegments.Add(RopeEnd);                  // Add the rope end to the rope segments list		

		// If the end is static
		if (StaticRopeEnd)
		{
			RopeEnd.Freeze = true;  // Freeze it, so the rope is attached in both ends
		}
		RopeEnd.IndexInArray = numSegments;         // Set the rope end index to the actual number of rope segments		
	}

	private void ConnectRopeParts(RopeSegment a, RopeSegment b)
	{
		PinJoint2D pinJoint = a.GetNode("PinJoint2D") as PinJoint2D;    // Get the pinjoint from the first passed in rope segment
		pinJoint.NodeA = a.GetPath();                                   // Connect the pinjoints first body to itself
		pinJoint.NodeB = b.GetPath();                                   // Connect the second body to the other rope part (b) 
	}

	public RopeSegment AddRopeSegment(Node previousSegment, int id, float rotationAngle, Vector2 position)
	{
		// Get the pinJoint of the previous rope segment
		PinJoint2D pinJoint = previousSegment.GetNode("PinJoint2D") as PinJoint2D;

		var segment = RopeSegmentPackedScene.Instantiate() as RopeSegment;  // Instance a new rope segment	
		segment.GlobalPosition = position;                                  // Set the start of it to the joint position
		segment.Rotation = rotationAngle;                                   // Set the rotation angle		
		segment.Rope = this;                                                // Set the rope the segment is part of		
		segment.IndexInArray = id;                                          // Set which index in the array the segment has		

		AddChild(segment);                                                  // Add the segment as a childnode to the rope node		
		pinJoint.NodeA = previousSegment.GetPath();                         // Connect the pin joint node A to the parent		
		pinJoint.NodeB = segment.GetPath();                                 // Connect the pin joint node B to the new segment
		pinJoint.Bias = 0.99f;                                              // Set the pin joint bias to 0.99		
		pinJoint.Softness = 0.003f;                                         // Set the rope softness to 0.003			
		return segment;                                                     // Return the new rope segment		
	}

	private void UpdateLine2DRope()
	{
		RopePointsLine2D.Clear();                               // Clear the Line2D points
		RopePointsLine2D.Add(RopeStartPinJoint.GlobalPosition); // Add the rope start position		

		// Loop through the rope segments
		foreach (var segment in RopeSegments)
		{
			RopePointsLine2D.Add(segment.GlobalPosition);       // Add the segment position to the rope line points list			
		}
		RopePointsLine2D.Add(RopeEndPinJoint.GlobalPosition);   // add the rope end position		
		Line2DNode.Points = RopePointsLine2D.ToArray();         // Set the Line2D points		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdateLine2DRope();      // Update the Line2D rope
	}
}
