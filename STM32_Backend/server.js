// server.js - MQTT to MongoDB Bridge with REST API
const mqtt = require('mqtt');
const express = require('express');
const { MongoClient } = require('mongodb');
const cors = require('cors');

// Configuration
const MQTT_BROKER = 'mqtt://test.mosquitto.org';
const MONGODB_URI = 'mongodb://localhost:27017';
const DB_NAME = 'sensor_data';
const PORT = 3000;

// MQTT Topics
const topics = {
  temperature: 'sensors/temperature',
  humidity: 'sensors/humidity',
  pressure: 'sensors/pressure'
};

// Initialize Express
const app = express();
app.use(cors());
app.use(express.json());

let db;
let sensorsCollection;

// Connect to MongoDB
async function connectMongoDB() {
  try {
    const client = await MongoClient.connect(MONGODB_URI, {
      useUnifiedTopology: true
    });
    console.log('✓ Connected to MongoDB');
    
    db = client.db(DB_NAME);
    sensorsCollection = db.collection('readings');
    
    // Create indexes for efficient querying
    await sensorsCollection.createIndex({ timestamp: -1 });
    await sensorsCollection.createIndex({ sensor_type: 1, timestamp: -1 });
    
    console.log('✓ Database indexes created');
  } catch (error) {
    console.error('MongoDB connection error:', error);
    process.exit(1);
  }
}

// Connect to MQTT Broker
function connectMQTT() {
  const client = mqtt.connect(MQTT_BROKER);
  
  client.on('connect', () => {
    console.log('✓ Connected to MQTT Broker');
    
    // Subscribe to all sensor topics
    Object.values(topics).forEach(topic => {
      client.subscribe(topic, (err) => {
        if (!err) {
          console.log(`✓ Subscribed to ${topic}`);
        } else {
          console.error(`✗ Failed to subscribe to ${topic}:`, err);
        }
      });
    });
  });
  
  client.on('message', async (topic, message) => {
    try {
      const value = parseFloat(message.toString());
      
      // Determine sensor type from topic
      let sensorType;
      if (topic === topics.temperature) sensorType = 'temperature';
      else if (topic === topics.humidity) sensorType = 'humidity';
      else if (topic === topics.pressure) sensorType = 'pressure';
      
      // Store in MongoDB
      const reading = {
        sensor_type: sensorType,
        value: value,
        timestamp: new Date(),
        raw_topic: topic
      };
      
      await sensorsCollection.insertOne(reading);
      console.log(`Stored ${sensorType}: ${value}`);
      
    } catch (error) {
      console.error('Error processing message:', error);
    }
  });
  
  client.on('error', (error) => {
    console.error('MQTT Error:', error);
  });
}

// REST API Endpoints

// Get latest readings for all sensors
app.get('/api/latest', async (req, res) => {
  try {
    const latest = await Promise.all([
      sensorsCollection.findOne(
        { sensor_type: 'temperature' },
        { sort: { timestamp: -1 } }
      ),
      sensorsCollection.findOne(
        { sensor_type: 'humidity' },
        { sort: { timestamp: -1 } }
      ),
      sensorsCollection.findOne(
        { sensor_type: 'pressure' },
        { sort: { timestamp: -1 } }
      )
    ]);
    
    res.json({
      temperature: latest[0],
      humidity: latest[1],
      pressure: latest[2]
    });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// Get historical data for a specific sensor
app.get('/api/history/:sensorType', async (req, res) => {
  try {
    const { sensorType } = req.params;
    const { hours = 1, limit = 100 } = req.query;
    
    const startTime = new Date();
    startTime.setHours(startTime.getHours() - parseInt(hours));
    
    const readings = await sensorsCollection
      .find({
        sensor_type: sensorType,
        timestamp: { $gte: startTime }
      })
      .sort({ timestamp: 1 })
      .limit(parseInt(limit))
      .toArray();
    
    res.json(readings);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// Get all historical data (for all sensors)
app.get('/api/history', async (req, res) => {
  try {
    const { hours = 1 } = req.query;
    
    const startTime = new Date();
    startTime.setHours(startTime.getHours() - parseInt(hours));
    
    const [temperature, humidity, pressure] = await Promise.all([
      sensorsCollection
        .find({
          sensor_type: 'temperature',
          timestamp: { $gte: startTime }
        })
        .sort({ timestamp: 1 })
        .toArray(),
      sensorsCollection
        .find({
          sensor_type: 'humidity',
          timestamp: { $gte: startTime }
        })
        .sort({ timestamp: 1 })
        .toArray(),
      sensorsCollection
        .find({
          sensor_type: 'pressure',
          timestamp: { $gte: startTime }
        })
        .sort({ timestamp: 1 })
        .toArray()
    ]);
    
    res.json({ temperature, humidity, pressure });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// Get statistics for a sensor
app.get('/api/stats/:sensorType', async (req, res) => {
  try {
    const { sensorType } = req.params;
    const { hours = 24 } = req.query;
    
    const startTime = new Date();
    startTime.setHours(startTime.getHours() - parseInt(hours));
    
    const stats = await sensorsCollection.aggregate([
      {
        $match: {
          sensor_type: sensorType,
          timestamp: { $gte: startTime }
        }
      },
      {
        $group: {
          _id: null,
          avg: { $avg: '$value' },
          min: { $min: '$value' },
          max: { $max: '$value' },
          count: { $sum: 1 }
        }
      }
    ]).toArray();
    
    res.json(stats[0] || {});
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// Health check endpoint
app.get('/api/health', (req, res) => {
  res.json({ status: 'ok', timestamp: new Date() });
});

// Start the server
async function start() {
  await connectMongoDB();
  connectMQTT();
  
  app.listen(PORT, () => {
    console.log(`✓ REST API server running on http://localhost:${PORT}`);
    console.log('\nAvailable endpoints:');
    console.log(`  GET /api/latest - Latest readings`);
    console.log(`  GET /api/history/:sensorType?hours=1 - Historical data`);
    console.log(`  GET /api/history?hours=1 - All historical data`);
    console.log(`  GET /api/stats/:sensorType?hours=24 - Statistics`);
  });
}

start().catch(console.error);