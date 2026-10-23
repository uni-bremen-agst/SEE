# **Setup Guide**

## **1. Open the Root Repository Folder**

Navigate to the root directory of the repository named **SEE**.

---

## **2. Create a Python 3.12 Virtual Environment**

> **Important (Windows users with multiple Python versions installed):**
> If you have more than one Python version on your system, make sure to create the virtual environment using Python 3.12 explicitly by running:
>
> ```bash
> py -3.12 -m venv venv
> ```
>
> This ensures the correct Python version is used.

If you only have Python 3.12 installed, you may also use:

```bash
python -m venv venv
```

---

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

## **4. Install Required Dependencies**

With the virtual environment active, install the required Python packages:

### **4.1 Install MediaPipe**

```bash
pip install mediapipe
```

### **4.2 Install OpenCV**

```bash
pip install opencv-python
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
