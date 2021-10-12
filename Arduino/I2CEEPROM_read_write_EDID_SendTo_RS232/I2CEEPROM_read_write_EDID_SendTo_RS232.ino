/*
 * EEPROM Read
 *
 * Reads the value of each byte of the EEPROM and prints it
 * to the computer.
 * This example code is in the public domain.
 */
#include <Wire.h>
#define ADDR_Ax 0b000 //A2, A1, A0
#define ADDR (0b1010 << 3) + ADDR_Ax
const int buttonPin = 2;     // the number of the pushbutton pin
byte data2read = 0;
int address = 0;
byte value[16][16]={255};
bool recive_EDID = false;
int i,j =0;
int data_len = 256;
int buttonState = 0;  
int DATA=0 ;
int recive_data;
void setup() {
  // initialize serial and wait for port to open:
  Serial.begin(9600);
    // initialize the pushbutton pin as an input:
  pinMode(buttonPin, INPUT);
  Wire.begin();  
  while (!Serial) {
    ; // wait for serial port to connect. Needed for native USB port only
  }
}

void loop() {
  if (Serial.available()) 
  {
    delay(128);
    if (recive_EDID == true)
    {
      recive_data = Serial.available();
        if ( recive_data == 32)
        {
          for (i=0; i<32; i++)
          {
            value[address/16][address % 16] = Serial.read();
            address++;
          }
          if (address < 256)
          {
            Serial.write(87);
          }
          else
          {
            recive_EDID = false;
            writeI2CByte();
            Serial.write(70);
            Serial.flush();
            address = 0;
            
          }
        }
        else
        {
          Serial.write(DATA);
          Serial.write(recive_data);
          recive_EDID = false;
        }
      DATA = 0;
    }
    DATA = Serial.read();
    if (DATA == 82) //hex = 52; Ascii = "R"
    {
      for (i=0;i<256;i++) {
        Serial.write(readI2CByte(i));
      }
      DATA = 0;
    }
     if (DATA == 87) //hex = 57; Ascii = "W"
    {
      Serial.flush();
      Serial.write(87);
      recive_EDID = true;
    }
  delay(10);  
  }
}
byte readI2CByte(byte data_addr){
  byte data = NULL;
  Wire.beginTransmission(ADDR);
  Wire.write(data_addr);
  Wire.endTransmission();
  Wire.requestFrom(ADDR, 1); //retrieve 1 returned byte
  delay(1);
  if(Wire.available())
  {
    data = Wire.read();
  }
  return data;
}

byte writeI2CByte(){
  //Wire.beginTransmission(0x50);
  for (i=0; i<16; i++) 
  {
    Wire.beginTransmission(0x50);
    Wire.write(i * 16);
    for (j=0; j<16; j++)
    {
      Wire.write(value[i][j]);
    }
    Wire.endTransmission();
    delay(4);
  }
  delay(1);
}
