# Snippet of introduction from the thesis
The core objective of this project is to design a compliance monitoring system that can easily adapt to changes in regulations while maintaining transparency and ease of use for the end-user. By integrating HiveMQ with DCR graphs, the aim is to create a system that not only ensures compliance with Hvidvaskloven but also improves the overall effectiveness, transparency and user-friendliness of compliance processes.

The developed system will include the following components:

Detector/rules graph: This graph is based on Hvidvaskloven and outlines the conditions under which KYC checks must be performed. It includes various transaction types and thresholds that trigger KYC checks if violated.

Customer behaviour graph: This graph models customer events and behaviour patterns. It helps simulate different scenarios, including both "usual" and fraudulent/suspicious activities, to test the compliance system.

HiveMQ incorporation: HiveMQ will aid in real-time communication between different parts of the compliance system, namely the producer and subscriber, ensuring that data and events are processed efficiently.

DCR graphs: DCR graphs will be used to model the compliance processes using the online tool, making it easier to update and understand regulatory requirements.

By combining these components, the system will offer a comprehensive solution for monitoring compliance with Hvidvaskloven. It will provide a real-time overview of customer transactions, automatically trigger KYC checks when certain conditions are met, and offer a user-friendly and transparent interface for monitoring and managing compliance activities, as well as modelling the regulations.

## How to run the program
The first step to run the program navigate to the appsetting.json file in both the producer and subscriber part of the program. Here the correct server and port should be entered. Then authentication is set in the environment variables. Four variables are needed, DCR\_\_Username, DCR\_\_Password, HiveMQ\_\_Username and HiveMQ\_\_Username.

Navigate to the folder BachelorProject and run the command "dotnet run --project Bachelor.MQTT.Subscriber" followed by "dotnet run --project Bachelor.MQTT.Producer"

The first command will launch the producer and have it awaiting incoming messages. The second command launches the producer. Upon startup, the producer will ask the user to input a detector graph id. The default is "BSc Albert Detector" and is chosen by simply pressing enter. After the detector graph id has been provided, the user is asked to choose a customer behavoiur graph. The prompt allows the user to choose either "BSc Albert Sus Customer", "BSc Albert Good Customer" or enter their own. Subsequently, the console is cleared and the program begins running.  