# 🌡️ IoT Environmental Monitoring Dashboard

Real-time environmental monitoring system using STM32 B-L475E-IOT01A with MQTT protocol, Node.js REST API, MongoDB database, and a modern C# dashboard for data visualization.

## 🎯 Overview

This project implements a complete end-to-end IoT solution for monitoring environmental parameters (temperature, humidity, pressure) in real-time. The system features a distributed hybrid architecture combining MQTT publish/subscribe for sensor data streaming with REST API for historical data access and interactive visualization.

## ✨ Key Features

### Real-Time Monitoring
- **Multi-sensor acquisition**: HTS221 (temperature/humidity) and LPS22HB (pressure) sensors
- **3-second refresh rate**: Continuous environmental data collection
- **Live dashboard updates**: Automatic UI refresh every 3 seconds
- **Visual feedback**: LED indicators for connection status

### Modern Dashboard
- **Color-coded statistics cards**: Display average, min, max, and peak values for each sensor
- **Animated speedometer gauges**: Real-time analog visualization with animated needles
- **Multi-line historical chart**: Simultaneous visualization of temperature, humidity, and pressure trends
- **Time range selector**: Interactive timeline analysis (last hour, 6 hours, 12 hours, 24 hours, 7 days)
- **Console logging**: Real-time system event monitoring

### Robust Architecture
- **MQTT messaging**: Efficient IoT protocol for sensor data publishing
- **REST API**: Structured HTTP endpoints for data retrieval
- **MongoDB storage**: Persistent historical data with timestamp indexing
- **WiFi connectivity**: Wireless data transmission using ISM43362-M3G-L44 module

## 🛠️ Technologies Used

### Hardware
- **Development Board**: STM32 B-L475E-IOT01A Discovery Kit
- **Microcontroller**: STM32L475VG (ARM Cortex-M4, 80 MHz)
- **Sensors**: 
  - HTS221: Capacitive digital temperature and humidity sensor
  - LPS22HB: MEMS pressure sensor (260-1260 hPa)
- **WiFi Module**: Inventek ISM43362-M3G-L44 (802.11 b/g/n)
- **Communication**: I2C (sensors), SPI (WiFi), USART (debug)

### Software Stack

**Embedded Firmware:**
- **Arduino STM32** framework
- **WiFiST** library for WiFi connectivity
- **PubSubClient** for MQTT protocol
- **HTS221Sensor** & **LPS22HBSensor** drivers

**Backend:**
- **Node.js** - Runtime environment
- **Express.js** - REST API framework
- **MQTT.js** - MQTT client library
- **Mongoose** - MongoDB object modeling

**Database:**
- **MongoDB** - NoSQL document database
- **Time-series optimization** - Indexed timestamp queries

**Frontend:**
- **C#** - Desktop application development
- **Windows Forms** - GUI framework
- **LiveCharts** - Interactive charting library
- **RestSharp** - HTTP client for API calls

### Protocols & APIs
- **MQTT** (Message Queuing Telemetry Transport) - IoT messaging
- **HTTP/REST** - Stateless API communication
- **JSON** - Data interchange format

## 📊 System Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    STM32 B-L475E-IOT01A                      │
│  ┌─────────────┐  ┌─────────────┐  ┌──────────────┐         │
│  │   HTS221    │  │  LPS22HB    │  │ ISM43362     │         │
│  │ Temp/Humid  │  │  Pressure   │  │ WiFi Module  │         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬───────┘         │
│         │ I2C            │ I2C            │ SPI             │
│         └────────────────┴────────────────┘                 │
│                    STM32L475VG MCU                           │
└────────────────────────┬─────────────────────────────────────┘
                         │ WiFi + MQTT Publish
                         ▼
┌──────────────────────────────────────────────────────────────┐
│              Mosquitto MQTT Broker (Port 1883)               │
│     Topics: sensors/temperature, /humidity, /pressure        │
└────────────────────────┬─────────────────────────────────────┘
                         │ MQTT Subscribe
                         ▼
┌──────────────────────────────────────────────────────────────┐
│                    Node.js Backend Server                    │
│  ┌────────────────┐              ┌──────────────────┐        │
│  │ MQTT Subscriber│─────────────►│  MongoDB Client  │        │
│  │  (Real-time)   │   Insert     │   (Mongoose)     │        │
│  └────────────────┘              └──────────────────┘        │
│                                                               │
│  ┌────────────────────────────────────────────────┐          │
│  │          REST API Endpoints (Express)          │          │
│  │  GET /api/latest  |  GET /api/data/:range      │          │
│  └────────────────────────────────────────────────┘          │
└────────────────────────┬─────────────────────────────────────┘
                         │ HTTP REST API
                         ▼
┌──────────────────────────────────────────────────────────────┐
│                MongoDB Database (Port 27017)                 │
│   Collection: sensor_data                                    │
│   Documents: { type, value, timestamp, _id }                 │
└──────────────────────────────────────────────────────────────┘
                         ▲
                         │ HTTP GET Requests
                         │
┌──────────────────────────────────────────────────────────────┐
│                   C# Desktop Dashboard                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │ Stats Cards  │  │ Speedometers │  │  Line Chart  │       │
│  │  (3 Cards)   │  │  (3 Gauges)  │  │  (History)   │       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
│         Auto-refresh every 3 seconds via REST API            │
└──────────────────────────────────────────────────────────────┘
```

## 🚀 Installation & Setup

### Prerequisites

**Hardware:**
- STM32 B-L475E-IOT01A Discovery board
- USB cable (Micro-B)
- WiFi network access

**Software:**
- **Arduino IDE** 1.8+ with STM32 board support
- **Node.js** 14+ and npm
- **MongoDB** 4.4+ (local or cloud instance)
- **Visual Studio** 2019+ (for C# dashboard)
- **Mosquitto broker** (or use public broker)

### 1. Embedded Firmware Setup

```bash
# Install Arduino STM32 Board Support
# In Arduino IDE: File → Preferences → Additional Board Manager URLs
https://github.com/stm32duino/BoardManagerFiles/raw/main/package_stmicroelectronics_index.json

# Install required libraries via Library Manager:
- WiFiST
- PubSubClient
- HTS221
- LPS22HB

# Configure WiFi credentials in firmware code:
const char* ssid = "YOUR_WIFI_SSID";
const char* password = "YOUR_WIFI_PASSWORD";

# Upload to STM32 board
# Tools → Board → STM32 Boards → B-L475E-IOT01A
# Tools → Port → Select your board's COM port
# Sketch → Upload
```

### 2. Backend Server Setup

```bash
# Clone repository
git clone https://github.com/YOUR-USERNAME/iot-environmental-dashboard.git
cd iot-environmental-dashboard/backend

# Install dependencies
npm install

# Configure environment variables
# Create .env file:
MONGODB_URI=mongodb://localhost:27017/iot_sensors
MQTT_BROKER=mqtt://test.mosquitto.org
PORT=3000

# Start the server
npm start

# Server runs on http://localhost:3000
```

### 3. MongoDB Setup

```bash
# Install MongoDB Community Edition
# https://www.mongodb.com/try/download/community

# Start MongoDB service
# Windows:
net start MongoDB

# Linux/Mac:
sudo systemctl start mongod

# Verify connection
mongo --version
```

### 4. C# Dashboard Setup

```bash
# Open Visual Studio
# File → Open → Project/Solution
# Select: dashboard/IoTDashboard.sln

# Install NuGet packages:
- RestSharp
- Newtonsoft.Json
- LiveCharts.WinForms

# Update API endpoint in code if needed:
private const string API_URL = "http://localhost:3000/api";

# Build and run:
# Debug → Start Debugging (F5)
```

## 💡 Usage

### 1. System Startup Sequence

1. **Start MongoDB**:
   ```bash
   # Ensure MongoDB is running
   sudo systemctl status mongod
   ```

2. **Launch Backend Server**:
   ```bash
   cd backend
   npm start
   # Wait for: "Server running on port 3000"
   ```

3. **Power On STM32 Board**:
   - Connect via USB or external power
   - Watch LED PB14 for connection status:
     - Slow blink = Connected to WiFi and MQTT
     - Fast blink = Connection error

4. **Open Serial Monitor** (optional):
   ```bash
   # Arduino IDE: Tools → Serial Monitor (115200 baud)
   # View real-time sensor readings
   ```

5. **Launch C# Dashboard**:
   - Run from Visual Studio or executable
   - Dashboard auto-connects to backend API

### 2. Dashboard Features

**Statistics Cards:**
- Green: Temperature (°C)
- Blue: Humidity (%)
- Orange: Pressure (hPa)
- Shows: Current, Average, Min, Max, Peak

**Speedometer Gauges:**
- Animated needle indicators
- Color-coded by sensor type
- Real-time value display

**Historical Chart:**
- Multi-line graph (temp, humidity, pressure)
- Interactive zoom and pan
- Tooltip on hover

**Time Range Selector:**
- Last hour, 6h, 12h, 24h, 7 days
- Updates chart dynamically

**Console Log:**
- Connection status
- Data refresh events
- Error messages

### 3. API Endpoints

```bash
# Get latest sensor readings
GET http://localhost:3000/api/latest
Response: {
  "temperature": 23.5,
  "humidity": 45.2,
  "pressure": 1013.25,
  "timestamp": "2026-01-20T10:30:00.000Z"
}

# Get historical data (last 24 hours)
GET http://localhost:3000/api/data/24h
Response: [
  {
    "type": "temperature",
    "value": 23.5,
    "timestamp": "2026-01-20T10:30:00.000Z"
  },
  ...
]

# Available time ranges: 1h, 6h, 12h, 24h, 7d
```

## 🔧 Project Structure

```
iot-environmental-dashboard/
├── firmware/
│   ├── stm32_mqtt_sensors.ino      # Main Arduino sketch
│   ├── config.h                     # WiFi/MQTT configuration
│   └── README.md                    # Firmware documentation
│
├── backend/
│   ├── server.js                    # Express + MQTT server
│   ├── models/
│   │   └── SensorData.js            # Mongoose schema
│   ├── routes/
│   │   └── api.js                   # REST endpoints
│   ├── package.json
│   └── .env.example
│
├── dashboard/
│   ├── IoTDashboard.sln             # Visual Studio solution
│   ├── MainForm.cs                  # Dashboard UI logic
│   ├── ApiClient.cs                 # REST API client
│   └── Models/
│       └── SensorReading.cs         # Data models
│
├── docs/
│   └── Rapport_de_projet.pdf        # Full technical report (French)
│
└── README.md
```

## 📈 Data Flow

1. **Acquisition (every 3s)**:
   ```
   HTS221/LPS22HB → I2C → STM32L475VG → Format JSON
   ```

2. **Transmission**:
   ```
   STM32 → WiFi (ISM43362) → MQTT Publish → Mosquitto Broker
   Topics: sensors/{temperature|humidity|pressure}
   ```

3. **Storage**:
   ```
   Backend subscribes to MQTT topics → Parse values → Insert to MongoDB
   Document: { type: "temperature", value: 23.5, timestamp: ISODate() }
   ```

4. **Retrieval**:
   ```
   Dashboard → HTTP GET /api/latest → Backend queries MongoDB → JSON response
   ```

5. **Visualization**:
   ```
   Dashboard deserializes JSON → Updates UI components → Refreshes every 3s
   ```

## 🎓 Key Learnings

This project provided hands-on experience with:
- ✅ End-to-end IoT system architecture design
- ✅ MQTT publish/subscribe messaging pattern
- ✅ RESTful API development with Node.js/Express
- ✅ NoSQL database design and time-series optimization
- ✅ Real-time data visualization with C#
- ✅ Embedded sensor integration (I2C protocol)
- ✅ WiFi connectivity on STM32 microcontrollers
- ✅ Hybrid architecture combining MQTT and REST

## 🔮 Future Improvements

- [ ] **Alert system**: Email/SMS notifications when thresholds are exceeded
- [ ] **User authentication**: Secure login system with role-based access
- [ ] **Mobile app**: React Native or Flutter dashboard
- [ ] **Dynamic configuration**: Edit settings without recompilation
- [ ] **Multi-device support**: Connect multiple STM32 boards simultaneously
- [ ] **Predictive analytics**: Machine learning for anomaly detection
- [ ] **Cloud deployment**: AWS IoT or Azure IoT Hub integration

## 📄 License

MIT License - Free to use and modify

## 👥 Authors

**Alaa DKHIL** & **Farah HENTATI**  
3rd Year Electrical Engineering Students, ENIT  
Academic Year: 2025/2026

**Supervisor:** M. Khaled Jelassi

## 📫 Contact

- LinkedIn: [Alaa Dkhil](https://www.linkedin.com/in/alaa-dkhil-00866b288/)
- Email: alaa.dkhil@etudiant-enit.utm.tn

---

⭐ If you found this project useful, give it a star! Contributions are welcome.
