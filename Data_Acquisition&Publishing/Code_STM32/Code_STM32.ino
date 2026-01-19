/*
 * WiFi + Environmental Sensors + MQTT
 * B-L475E-IOT01A
 */

#include <SPI.h>
#include <WiFiST.h>
#include <Wire.h>
#include <HTS221Sensor.h>
#include <LPS22HBSensor.h>
#include <PubSubClient.h>

//WiFi parameters
char ssid[] = "Galaxy A23 FA4C";
char pass[] = "elaaelaa";

//MQTT Broker settings
const char* mqtt_server = "test.mosquitto.org";  // Public test broker
const int mqtt_port = 1883;
const char* mqtt_client_id = "STM32_IoT_Board_478";


const char* topic_temperature = "sensors/temperature";
const char* topic_humidity = "sensors/humidity";
const char* topic_pressure = "sensors/pressure";

//USART1 
#define TX_PIN PB6
#define RX_PIN PB7


HardwareSerial SerialVCP(RX_PIN, TX_PIN);

SPIClass SPI_3(PC12, PC11, PC10);
WiFiClass WiFi(&SPI_3, PE_0, PE_1, PE_8, PB13);


TwoWire dev_i2c(PB11, PB10);  
HTS221Sensor *HumTemp;
LPS22HBSensor *PressTemp;


WiFiClient wifiClient;
PubSubClient mqttClient(wifiClient);

int status = WL_IDLE_STATUS;

void setup() {
  pinMode(LED_BUILTIN, OUTPUT);
  pinMode(PB14, OUTPUT);  
  
  //Initializing Serial
  SerialVCP.begin(115200);
  delay(2000);
  
  SerialVCP.println("========================================");
  SerialVCP.println("WiFi + Sensors + MQTT - B-L475E-IOT01A");
  SerialVCP.println("========================================");
  SerialVCP.println();
  SerialVCP.flush();
  
  //Fast blink to confirm startup
  for(int i = 0; i < 3; i++) {
    digitalWrite(PB14, HIGH);
    delay(100);
    digitalWrite(PB14, LOW);
    delay(100);
  }
  
  //Initialize I2C and sensors
  SerialVCP.println("Initializing I2C and sensors...");
  SerialVCP.flush();
  dev_i2c.begin();
  delay(100);
  
  //Scan I2C bus (to make sure the adresses are not wrong)
  SerialVCP.println("Scanning I2C bus...");
  SerialVCP.flush();
  byte error, address;
  int nDevices = 0;
  
  for(address = 1; address < 127; address++) {
    dev_i2c.beginTransmission(address);
    error = dev_i2c.endTransmission();
    
    if (error == 0) {
      SerialVCP.print("I2C device found at address 0x");
      if (address < 16) SerialVCP.print("0");
      SerialVCP.println(address, HEX);
      nDevices++;
    }
  }
  
  SerialVCP.print("Found ");
  SerialVCP.print(nDevices);
  SerialVCP.println(" device(s)");
  SerialVCP.println();
  SerialVCP.flush();
  
  //Initialize HTS221 (Temperature and Humidity)
  HumTemp = new HTS221Sensor(&dev_i2c);
  if (HumTemp->begin() == 0) {
    SerialVCP.println("✓ HTS221 initialized");
    HumTemp->Enable();
    delay(200);
  } else {
    SerialVCP.println("✗ HTS221 initialization failed!");
  }
  
  //Initialize LPS22HB (Pressure)
  PressTemp = new LPS22HBSensor(&dev_i2c);
  if (PressTemp->begin() == 0) {
    SerialVCP.println("✓ LPS22HB initialized");
    PressTemp->Enable();
    delay(200);
  } else {
    SerialVCP.println("✗ LPS22HB initialization failed!");
  }
  
  //Re-initialize Serial after sensors
  SerialVCP.end();
  delay(100);
  SerialVCP.begin(115200);
  delay(1000);
  
  SerialVCP.println("All sensors ready!");
  SerialVCP.println();
  SerialVCP.flush();
  
  //Initialize WiFi
  SerialVCP.println("Initializing WiFi module...");
  SerialVCP.flush();
  
  if (WiFi.status() == WL_NO_SHIELD) {
    SerialVCP.println("ERROR: WiFi module not found!");
    while (true) {
      digitalWrite(PB14, HIGH);
      delay(100);
      digitalWrite(PB14, LOW);
      delay(100);
    }
  }
  
  SerialVCP.println("✓ WiFi module detected!");
  SerialVCP.flush();
  
  //Connect to WiFi
  SerialVCP.print("Connecting to: ");
  SerialVCP.println(ssid);
  SerialVCP.flush();
  
  status = WiFi.begin(ssid, pass);
  
  int attempts = 0;
  while (status != WL_CONNECTED && attempts < 30) {
    delay(1000);
    status = WiFi.status();
    SerialVCP.print(".");
    SerialVCP.flush();
    attempts++;
    digitalWrite(PB14, !digitalRead(PB14));
  }
  
  SerialVCP.println();
  SerialVCP.flush();
  
  if (status == WL_CONNECTED) {
    SerialVCP.println("========================================");
    SerialVCP.println("✓ SUCCESS! Connected to WiFi");
    SerialVCP.println("========================================");
    SerialVCP.flush();
    printWiFiStatus();
    digitalWrite(PB14, HIGH);
    
    //Setup MQTT
    SerialVCP.println("Setting up MQTT...");
    SerialVCP.flush();
    mqttClient.setServer(mqtt_server, mqtt_port);
    
    //Connect to MQTT broker
    connectMQTT();
  } else {
    SerialVCP.println("✗ WiFi connection failed!");
    SerialVCP.flush();
  }
  
  SerialVCP.println("Starting sensor readings...");
  SerialVCP.println();
  SerialVCP.flush();
}

void loop() {
  static unsigned long lastReading = 0;
  unsigned long currentTime = millis();
  
  if (!mqttClient.connected()) {
    connectMQTT();
  }
  mqttClient.loop();
  
  //Blink LED slowly when connected
  if (currentTime % 1000 < 500) {
    digitalWrite(PB14, HIGH);
  } else {
    digitalWrite(PB14, LOW);
  }
  
  //Read and publish sensors every 3 seconds
  if (currentTime - lastReading >= 3000) {
    lastReading = currentTime;
    
    //Read sensors
    float temperature = 0;
    float humidity = 0;
    float pressure = 0;
    
    HumTemp->GetTemperature(&temperature);
    HumTemp->GetHumidity(&humidity);
    PressTemp->GetPressure(&pressure);
    
    //Print to serial
    SerialVCP.println("----------------------------------------");
    SerialVCP.print("Temperature: ");
    SerialVCP.print(temperature, 2);
    SerialVCP.println(" °C");
    
    SerialVCP.print("Humidity:    ");
    SerialVCP.print(humidity, 2);
    SerialVCP.println(" %");
    
    SerialVCP.print("Pressure:    ");
    SerialVCP.print(pressure, 2);
    SerialVCP.println(" hPa");
    SerialVCP.flush();
    
    //Publish to MQTT if connected
    if (mqttClient.connected()) {
      char tempStr[10];
      char humStr[10];
      char pressStr[10];
      
      dtostrf(temperature, 4, 2, tempStr);
      dtostrf(humidity, 4, 2, humStr);
      dtostrf(pressure, 6, 2, pressStr);

      
      
      mqttClient.publish(topic_temperature, tempStr);
      delay(100);  
      mqttClient.publish(topic_humidity, humStr);
      delay(100);
      mqttClient.publish(topic_pressure, pressStr);
      delay(100);

      
      SerialVCP.println("✓ Data published to MQTT");
    } else {
      SerialVCP.println("✗ MQTT disconnected");
    }
    
    SerialVCP.println("----------------------------------------");
    SerialVCP.println();
    SerialVCP.flush();
  }
  
  delay(100);
}

void connectMQTT() {
  while (!mqttClient.connected()) {
    SerialVCP.print("Connecting to MQTT broker at ");
    SerialVCP.print(mqtt_server);
    SerialVCP.print("...");
    SerialVCP.flush();
    
    if (mqttClient.connect(mqtt_client_id, "", "", 0, 0, 0, 0, 1)) {
      SerialVCP.println(" Connected!");
      SerialVCP.flush();
      
      //Flash LED to confirm MQTT connection
      for(int i = 0; i < 5; i++) {
        digitalWrite(PB14, HIGH);
        delay(50);
        digitalWrite(PB14, LOW);
        delay(50);
      }
      break;
    } else {
      SerialVCP.print(" Failed! RC=");
      SerialVCP.println(mqttClient.state());
      SerialVCP.flush();
      delay(5000);
    }
  }
}

void printWiFiStatus() {
  SerialVCP.print("SSID: ");
  SerialVCP.println(WiFi.SSID());
  
  IPAddress ip = WiFi.localIP();
  SerialVCP.print("IP Address: ");
  SerialVCP.println(ip);
  
  long rssi = WiFi.RSSI();
  SerialVCP.print("Signal strength: ");
  SerialVCP.print(rssi);
  SerialVCP.println(" dBm");
  SerialVCP.println();
  SerialVCP.flush();
}