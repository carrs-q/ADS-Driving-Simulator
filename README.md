# CARRSQ ADS Driving Simulator

![Main Menue](https://github.com/me89/VideoWall/blob/master/Doc/MainMenue.PNG "CARRSQ ADS Driving Simulator")

This Software is written at CARRSQ to run research studies for autonomous driving vehicles, 
with focus on AES Level 3. 

---


### Questions regarding the research to:
**Dr. Ronald Schroeter | QUT**    
r.schroeter@qut.edu.au  
internal phone: 84629  

**Michael A. Gerber | QUT**  
michaelandreas.gerber@hdr.qut.edu.au  
internal phone: 82860  

### Technical questions to
gerberm@qut.edu.au  
mail@gerbermichael.de  

---
### Licence
Current: No permition for everyone, 
except members of CARRS-Q and selected research partner

---
### Requirements
* Nvidia GTX1080 or better
* SSD (~ 10GB per scenario, if local 20GB )
* 8GB RAM
* Windows 10
* VR-Mode requires Oculus Rift
* [HDMI Video Capture Card](http://www.tnpproducts.com/product/hdmi-to-usb-3-0-capture-card-device-dongle-hdmi-full-hd-1080p-video-audio-to-usb-adapter-converter-compatible-with-windows-mac-linux/) (HDMI to USB3) we are using this one (noname)

### Setup
1. Compile with Unity
2. Install NodeJs with NPM
3. Run [CDN](https://github.com/me89/VideoWallCDN) (If not installed, install dependencies with ```npm install```)
4. Check config-file for Simulator in ```/ADS Simulator_Data/StreamingAssets/node-config.xml```
5. Run ADS Simulator
6. Choose Simulator-Type (top left)

### Config File (possible values)
If the config files contains  
node type: **master | slave** (Cluster Master, VR | Cluster Slave)  
node screen: **1 - 5** rendered Camera   
node debug: **0 | 1** (OFF|ON) for Cluster  
server ip: **IP.v4** (IP of Cluster Master)  
server port: **PORTNR** (Port of Cluster Master)  
cdn address: **HTTP ADDRESS of CDN** (with Port)  
hdmi video: **Name of Video Capture device** (Hardware name)  
hdmi audio: **Name of Audio Capture device** (Hardware name)  
