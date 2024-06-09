# How to run the program
The first step to run the program navigate to the appsetting.json file in both the producer and subscriber part of the program. Here the correct server and port should be entered. Then authentication is set in the environment variables. Four variables are needed, DCR\_\_Username, DCR\_\_Password, HiveMQ\_\_Username and HiveMQ\_\_Username.

Navigate to the folder BachelorProject and run the command "dotnet run --project Bachelor.MQTT.Subscriber" followed by "dotnet run --project Bachelor.MQTT.Producer"

The first command will launch the producer and have it awaiting incoming messages. The second command launches the producer. Upon startup, the producer will ask the user to input a detector graph id. The default is "BSc Albert Detector" and is chosen by simply pressing enter. After the detector graph id has been provided, the user is asked to choose a customer behavoiur graph. The prompt allows the user to choose either "BSc Albert Sus Customer", "BSc Albert Good Customer" or enter their own. Subsequently, the console is cleared and the program begins running.  