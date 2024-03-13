namespace Bachelor.MQTT.Subscriber;
using System.Xml.Serialization;
// XmlSerializer serializer = new XmlSerializer(typeof(Events));
// using (StringReader reader = new StringReader(xml))
// {
//    var test = (Events)serializer.Deserialize(reader);
// }

[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[XmlRoot(ElementName="event", Namespace = "")]
public class Event { 

	// [XmlElement(ElementName="eventTypeData")] 
	// public object EventTypeData { get; set; } 

	[XmlAttribute(AttributeName="id")] 
	public string Id { get; set; } 

	[XmlAttribute(AttributeName="included")] 
	public bool Included { get; set; } 

	[XmlAttribute(AttributeName="enabled")] 
	public bool Enabled { get; set; } 

	[XmlAttribute(AttributeName="pending")] 
	public bool Pending { get; set; } 

	[XmlAttribute(AttributeName="EffectivelyPending")] 
	public bool EffectivelyPending { get; set; } 

	[XmlAttribute(AttributeName="EffectivelyIncluded")] 
	public bool EffectivelyIncluded { get; set; } 

	[XmlAttribute(AttributeName="executed")] 
	public bool Executed { get; set; } 

	[XmlAttribute(AttributeName="fullPath")] 
	public string FullPath { get; set; } 

	[XmlAttribute(AttributeName="roles")] 
	public string Roles { get; set; } 

	[XmlAttribute(AttributeName="groups")] 
	public string Groups { get; set; } 

	[XmlAttribute(AttributeName="description")] 
	public string Description { get; set; } 

	[XmlAttribute(AttributeName="label")] 
	public string Label { get; set; } 

	[XmlAttribute(AttributeName="eventType")] 
	public string EventType { get; set; } 

	[XmlAttribute(AttributeName="phases")] 
	public string Phases { get; set; } 

	[XmlAttribute(AttributeName="deadline")] 
	public string Deadline { get; set; } 

	[XmlAttribute(AttributeName="sequence")] 
	public int Sequence { get; set; } 

	[XmlAttribute(AttributeName="parent")] 
	public string Parent { get; set; } 

	[XmlAttribute(AttributeName="type")] 
	public string Type { get; set; } 

	[XmlAttribute(AttributeName="referId")] 
	public string ReferId { get; set; } 
}

[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(ElementName = "events", Namespace = "", IsNullable = false)]
// [XmlRoot(ElementName="events", Namespace = "")]
public class Events { 

	[XmlElement(ElementName="event")] 
	public List<Event> Event { get; set; } 

	[XmlAttribute(AttributeName="nextDelay")] 
	public string NextDelay { get; set; } 

	[XmlAttribute(AttributeName="nextDeadline")] 
	public string NextDeadline { get; set; } 

	[XmlAttribute(AttributeName="isAccepting")] 
	public string IsAccepting { get; set; } 

	[XmlAttribute(AttributeName="currentTime")] 
	public DateTime CurrentTime { get; set; } 

	[XmlAttribute(AttributeName="currentPhase")] 
	public string CurrentPhase { get; set; } 
}

