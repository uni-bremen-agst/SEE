# **Setup Guide**

## **1. Open the Root Repository Folder**

Navigate to the root directory of the repository named **SEE**.

---

## **2. Create a Python 3.12 Virtual Environment**

```bash
python3.12 -m venv venv
```

### **2.1 If Python 3.12 Is Not Installed**

Download and install it from the official Python website:

🔗 [https://www.python.org/downloads/](https://www.python.org/downloads/)

After installation, run the virtual environment command again.

---

## **3. Activate the Virtual Environment**

### **Windows (PowerShell)**

```powershell
venv\Scripts\Activate
```

### **3.1 If You Get a Permission Error**

Some systems block script execution. Fix it with:

```powershell
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
```

Then activate the virtual environment again:

```powershell
venv\Scripts\Activate
```

---

## **4. Install MediaPipe**

With the venv active:

```bash
pip install mediapipe
```

---

## **5. Navigate to the Client Folder**

Move into the folder containing `client.py`:

```bash
cd Assets/SEE/Tools/EchoFace/Client
```

---

## **6. Run the Client**

```bash
python client.py
```
