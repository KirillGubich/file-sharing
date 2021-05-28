## p2p file sharing

Each participant in the file exchange (node) is identified by an IP address and an arbitrary name that 
is specified by the user when starting the application. The uniqueness of the names is not required.
Each node uses UDP to form a list of active nodes (IP addresses and names):
- after startup, the node sends a broadcast packet containing its name to notify other nodes 
in the network about its connection;
- other nodes that received such a packet establish with the sender
TCP connection for file exchange and transmit their name over it for identification.

A new client can join at any time.
Files are exchanged using TCP in a logically shared space: each host maintains one TCP connection 
to each other and sends files to all hosts on the network. 
Disconnecting a node is correct processed by other nodes.
