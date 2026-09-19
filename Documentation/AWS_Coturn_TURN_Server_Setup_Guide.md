# AWS Self-Hosted TURN Server (coturn) Setup & Integration Guide

> Comprehensive step-by-step guide to installing, configuring, securing, and integrating an open-source **coturn** TURN/STUN server on **AWS EC2 (Ubuntu)** for **M4 Nikah Real-Time WebRTC Audio & Video Calling**.

---

## 1. Overview & Architecture

In WebRTC:
* **~80% of calls** connect directly **Peer-to-Peer (Device-to-Device)** using Google's public STUN server.
* **~20% of calls** (behind strict 4G/5G mobile carriers, NATs, and restrictive corporate Wi-Fi firewalls) cannot connect directly.
* The **AWS TURN server (`coturn`)** acts as a secure, high-speed encrypted relay for audio/video media packets when direct P2P is blocked.

```
┌─────────────┐                                            ┌─────────────┐
│   Caller    │                                            │  Receiver   │
│  (Mobile)   │                                            │  (Mobile)   │
└──────┬──────┘                                            └──────┬──────┘
       │                                                          │
       │ 1. Direct P2P connection blocked by 4G carrier/firewall  │
       │                                                          │
       │ 2. Relays Encrypted Audio/Video Stream                   │
       ├───────────────────────►┌────────────────┐                │
       │                        │ AWS TURN Server│◄───────────────┤
       │                        │   (coturn)     │                │
       │                        └────────────────┘                │
       │                      100% Free & Self-Hosted             │
```

---

## 2. Step 1: Launch AWS EC2 Instance & Allocate Elastic IP

1. **Log in to AWS Console** → Navigate to **EC2** → Click **Launch Instance**.
2. **Instance Configuration**:
   * **Name**: `M4Nikah-Coturn-Server`
   * **OS Image**: **Ubuntu Server 22.04 LTS** (or 24.04 LTS, 64-bit x86)
   * **Instance Type**: `t3.micro` (AWS Free Tier eligible) or `t3.small` (recommended for production multi-call traffic)
   * **Key Pair**: Select an existing SSH key pair (`.pem`) or create a new one.
   * **Storage**: Default 8 GB - 20 GB gp3 is sufficient.
3. **Allocate and Associate an Elastic IP (Static Public IP)**:
   * Go to **EC2 Dashboard > Network & Security > Elastic IPs**.
   * Click **Allocate Elastic IP** → Click **Allocate**.
   * Select the newly allocated Elastic IP → Click **Actions > Associate Elastic IP address**.
   * Select your `M4Nikah-Coturn-Server` instance and bind it.
   * *(Note down your Elastic IP — this IP will never change even if the server reboots).*

---

## 3. Step 2: Configure AWS Security Group (Firewall Ports)

Navigate to **EC2 > Security Groups** for your instance and add the following **Inbound Rules**:

| Type | Protocol | Port Range | Source | Description |
| :--- | :--- | :--- | :--- | :--- |
| **SSH** | TCP | `22` | `My IP` (or `0.0.0.0/0`) | SSH Administration Access |
| **Custom TCP** | TCP | `3478` | `0.0.0.0/0` | STUN / TURN Standard TCP |
| **Custom UDP** | UDP | `3478` | `0.0.0.0/0` | STUN / TURN Standard UDP (Primary) |
| **Custom TCP** | TCP | `5349` | `0.0.0.0/0` | TURNS (TLS Encrypted TCP) |
| **Custom UDP** | UDP | `5349` | `0.0.0.0/0` | TURNS (TLS Encrypted UDP) |
| **Custom UDP** | UDP | `49152 - 65535` | `0.0.0.0/0` | WebRTC Media Relay UDP Port Range |

> [!IMPORTANT]
> The port range `49152-65535` (UDP) is essential. When direct peer connection fails, coturn allocates a port in this range to relay audio and video packets between the caller and receiver.

---

## 4. Step 3: Install & Configure `coturn` on Ubuntu

Connect to your EC2 instance via SSH terminal:
```bash
ssh -i your-key.pem ubuntu@YOUR_AWS_ELASTIC_IP
```

### 1. Update Packages & Install Coturn:
```bash
sudo apt update && sudo apt upgrade -y
sudo apt install coturn -y
```

### 2. Enable Coturn Daemon:
```bash
sudo nano /etc/default/coturn
```
Ensure this line is uncommented:
```ini
TURNSERVER_ENABLED=1
```
*(Save and exit with `Ctrl + O`, `Enter`, `Ctrl + X`)*.

---

### 3. Generate a High-Entropy Static Secret:
Run this command on the server to generate a secure random 32-character hexadecimal secret:
```bash
openssl rand -hex 16
```
*Example output:* `a8e4f1c9d234567890abcdef12345678` *(Keep this secret safe!)*

---

### 4. Create `/etc/turnserver.conf`:
Backup the default configuration and create a clean configuration file:
```bash
sudo mv /etc/turnserver.conf /etc/turnserver.conf.bak
sudo nano /etc/turnserver.conf
```

Paste the following production configuration (replace `YOUR_AWS_ELASTIC_IP` and `YOUR_STATIC_SECRET` with your values):

```ini
# ==========================================
# M4Nikah coturn Configuration
# ==========================================

# Standard Listening Ports
listening-port=3478
tls-listening-port=5349

# Listening IP addresses (listen on all local interfaces)
listening-ip=0.0.0.0

# External Public Elastic IP of AWS EC2 Instance
external-ip=YOUR_AWS_ELASTIC_IP

# Realm (Domain or Elastic IP)
realm=turn.m4nikah.com

# Long-term Credential Mechanism & Shared Secret (REST API Authentication)
lt-cred-mech
use-auth-secret
static-auth-secret=a8e4f1c9d234567890abcdef12345678

# WebRTC Media Relay UDP Port Range
min-port=49152
max-port=65535

# Security & Fingerprinting
fingerprint
no-cli
no-loopback-peers
no-multicast-peers

# Performance & Logging
log-file=/var/log/turnserver.log
verbose
```

---

## 5. Step 4: Start and Enable the Coturn Service

Restart and enable coturn so it automatically runs on boot:
```bash
sudo systemctl restart coturn
sudo systemctl enable coturn
```

Verify the service status:
```bash
sudo systemctl status coturn
```

Check listening ports:
```bash
sudo netstat -tulpn | grep turnserver
```
*(You should see coturn listening on port 3478 UDP/TCP).*

---

## 6. Step 5: Configure `.NET` Backend `appsettings.json`

Add the `TurnServerSettings` section to your `URMARRY/appsettings.json`:

```json
{
  "TurnServerSettings": {
    "StunUrl": "stun:stun.l.google.com:19302",
    "TurnUdpUrl": "turn:YOUR_AWS_ELASTIC_IP:3478?transport=udp",
    "TurnTcpUrl": "turn:YOUR_AWS_ELASTIC_IP:3478?transport=tcp",
    "Secret": "a8e4f1c9d234567890abcdef12345678",
    "Realm": "turn.m4nikah.com",
    "TtlHours": 2
  }
}
```

---

## 7. Step 6: Dynamic Ephemeral Credentials Generation in .NET

To protect your TURN server from unauthorized bandwidth abuse, the .NET backend dynamically generates **time-limited HMAC-SHA1 tokens** for authorized users when they enter a call.

### Flow:
1. Client calls `GET /api/call/ice-servers` before starting/joining a call.
2. The .NET backend computes:
   ```csharp
   long expiryTimestamp = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds();
   string username = $"{expiryTimestamp}:{userId}";
   string credential = ComputeHmacSha1(secret, username);
   ```
3. The API returns:
   ```json
   {
     "iceServers": [
       {
         "urls": "stun:stun.l.google.com:19302"
       },
       {
         "urls": [
           "turn:YOUR_AWS_ELASTIC_IP:3478?transport=udp",
           "turn:YOUR_AWS_ELASTIC_IP:3478?transport=tcp"
         ],
         "username": "1726760000:12345",
         "credential": "generated-hmac-sha1-base64-token"
       }
     ]
   }
   ```
4. Coturn validates the HMAC signature and timestamp automatically. Expired tokens are rejected.

---

## 8. Step 7: Testing with Official WebRTC Trickle ICE Tool

Before connecting the mobile app or web app, test your TURN server in your browser:

1. Go to the [WebRTC Official Trickle ICE Test Tool](https://webrtc.github.io/samples/src/content/peerconnection/trickle-ice/).
2. Under **STUN or TURN URI**, enter:
   ```
   turn:YOUR_AWS_ELASTIC_IP:3478?transport=udp
   ```
3. Enter your temporary username and password (or test credentials).
4. Click **Add Server** → Click **Gather candidates**.
5. **Expected Output**:
   * You should see candidates with Component `1` and Type `relay`.
   * Seeing **`relay`** candidates confirms coturn is relaying WebRTC packets through your AWS instance.

---

## 9. Troubleshooting & FAQ

| Issue | Cause | Solution |
| :--- | :--- | :--- |
| **No `relay` candidates in Trickle ICE** | AWS Security Group blocking UDP ports | Ensure UDP ports `3478` and `49152-65535` are open in AWS Inbound Rules. |
| **401 Unauthorized from TURN** | Secret mismatch | Ensure `static-auth-secret` in `/etc/turnserver.conf` matches `"Secret"` in `appsettings.json`. |
| **Coturn failed to start** | IP binding conflict | Ensure `listening-ip=0.0.0.0` and `external-ip` has your Elastic IP. |
| **Performance / High Call Volume** | Server resources | A single `t3.small` instance comfortably handles 200–500 concurrent relayed streams. Scale horizontally or vertically as active calls grow. |
