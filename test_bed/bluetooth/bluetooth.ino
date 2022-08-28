#include "Adafruit_Thermal_Galileo.h"
#include "SoftwareSerial.h"

#define LED_PIN  13        // white wire
#define BLUETHT_TX_PIN  3  // Arduino transmit  BLUE   WIRE   labeled RX  on bluetooth
#define BLUETHT_RX_PIN  2  // Arduino receive   PURPLE WIRE   labeled TX  on bluetooth
#define monochrome_width  384
//#define monochrome_height 544
#define monochrome_height 4

//SoftwareSerial BluetoothSerial(BLUETHT_RX_PIN, BLUETHT_TX_PIN); // Declare SoftwareSerial obj first

byte imageBuffer[384*4]; // monochrome_width * monochrome_height
int bufferIndex = 0;
int bufferHeight = 0;
const int duration = 2000;

void setup() {
  pinMode(LED_PIN, OUTPUT);

  //BluetoothSerial.begin(9600);
  Serial.begin(9600); // bluetooth serial

  for (int i=0; i<monochrome_width * monochrome_height; i++)
    imageBuffer[i] = 0;
}

void loop() {
  byte d;
  while (Serial.available()) {
    d = Serial.read();
    imageBuffer[bufferIndex] = d;
    bufferIndex += 1;

    if (bufferIndex == monochrome_width/8 * monochrome_height) {
      // print
      bufferIndex = 0;
      Serial.write(5); // let the program know we're ready      

      digitalWrite(LED_PIN, HIGH);   // turn the LED on (HIGH is the voltage level)
      delay(duration);           // wait for a second
      digitalWrite(LED_PIN, LOW);    // turn the LED off by making the voltage LOW
      delay(duration);           // wait for a second
    }
  }
}
