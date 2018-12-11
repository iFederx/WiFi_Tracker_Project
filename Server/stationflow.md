EVENT: Application startup
DO:
	-Read the file for saved rooms, together with their names and dimensions, and load them all using Context.createRoom()
EVENT: Station connects to passive socket and pairing completes with success.
DO:
	-Call Context.TryAddstation(Station name or station MAC) -> To implement: look if there is a saved configuration for the station. The configuration contains station name, the name of the room in which it is, the position of the station in the room, the interpolators data (they are serializable objects!).
	THERE IS A SAVED CONFIGURATION:
		-Load it into a Station object. Pass this object, along the Room name in which the station has been put, to addstation()
	THERE IS NO SAVED CONFIGURATION:
		-Open GUI asking for which room (allow to create)
