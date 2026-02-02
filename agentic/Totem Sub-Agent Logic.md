Totem is designed to allow each piece of the flow on a timeline to be broken down to individual sub-tasks.

An orchestrator agent can read the C# code and explain the sub-task at a high level to a sub-agent who then carries it out and returns the result. 

As an example, if a new Command is created and ultimately will flow to a Topic which creates an Event which then flows to a Workflow and then another Command and so on... 
- The creation of the command is a simple DTO fill-in the blanks.
- The flow into a Topic is a simple Routing check based on an id.
- The actual logic of what happens When the event hits the Topic depends on the routing above and then has its own logic.
- A new event being kicked off is another simple DTO fill-in-the blanks.
- The flow of an Event into a Workflow is just like a Topic.

When working in this manner, the Orchestrator no longer needs to know when the flow is complete. They'll know because there's nothing more to process. Once no new Event or Command is kicked out, and all routes have been completed, then the work is done.

