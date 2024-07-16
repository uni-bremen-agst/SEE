import { Alert, Box, Button, Card, CardContent, Chip, Container, Grid, IconButton, List, ListItem, ListItemText, Modal, Snackbar, Stack, Typography } from "@mui/material";
import Header from "../components/Header";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowLeft, faDownload, faEye, faPlay, faStop, faClipboard, faTrash } from "@fortawesome/free-solid-svg-icons";
import { grey } from "@mui/material/colors";
import Avatar from "../components/Avatar";
import { useContext, useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router";
import Server from "../types/Server";
import { AuthContext } from "../contexts/AuthContext";
import SeeFile from "../types/SeeFile";
import { FileTypeUtils } from "../types/FileType";

function getServerStatus(serverStatusType: string) {
  switch (serverStatusType) {
    case "ONLINE":
      return <Chip color="success" label="Online" />;
    case "OFFLINE":
      return <Chip color="error" label="Offline" />;
    case "STARTING":
      return <Chip color="warning" label="Starting" />;
    case "STOPPING":
      return <Chip color="warning" label="Stopping" />;
  }
}

const modalStyle = {
  position: 'absolute',
  top: '50%',
  left: '50%',
  transform: 'translate(-50%, -50%)',
  maxWidth: "1200px",
  minWidth: "400px",
  bgcolor: 'background.paper',
  borderRadius: "25px",
  boxShadow: 24,
  p: 4,
};

function ServerView() {
  const { axiosInstance } = useContext(AuthContext);

  const location = useLocation();
  const navigate = useNavigate();

  const [server, setServer] = useState<Server | undefined>(undefined);
  const [files, setFiles] = useState<SeeFile[] | undefined>(undefined);
  const [showDeleteServerModal, setShowDeleteServerModal] = useState(false);
  const [showLinkCopiedMessage, setShowLinkCopiedMessage] = useState(false);
  const [showPasswordCopiedMessage, setShowPasswordCopiedMessage] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  async function startServer() {
    if (server) {
      await axiosInstance.put("/server/startServer", {}, { params: { id: server.id } })
      axiosInstance.get(`/server/`, { params: { id: server.id } }).then(
        (response) => setServer(response.data)
      ).catch(() => {
        // TODO display error
      });
    }
  }

  async function stopServer() {
    if (server) {
      await axiosInstance.put("/server/stopServer", {}, { params: { id: server?.id } })
      axiosInstance.get(`/server/`, { params: { id: server.id } }).then(
        (response) => setServer(response.data)
      ).catch(() => {
        // TODO display error
      });
    }
  }

  useEffect(() => {
    let isApiSubscribed = true;
    const serverID = location.hash.slice(1);
    const fetchServer = setInterval(() => {
      if (serverID) {
        axiosInstance.get(`/server/`, { params: { id: serverID } }).then(
          (response) => setServer(response.data)
        )
      }
    }, 30000);

    if (isApiSubscribed && !server && serverID) {
      if (serverID) {
        axiosInstance.get(`/server/`, { params: { id: serverID } }).then(
          (response) => setServer(response.data)
        )
      }
    }
    if (isApiSubscribed && serverID) {
      axiosInstance.get(`/server/files`, { params: { id: serverID } }).then(
        (response) => setFiles(response.data)
      )
    }

    return () => {
      clearInterval(fetchServer);
      isApiSubscribed = false;
    }
  }, [location.state]);

  if (!server) {
    return <Typography>Invalid server ID.</Typography>
  }
  return (
    <Container sx={{ padding: "3em" }}>
      <Snackbar open={showLinkCopiedMessage} autoHideDuration={5000} onClose={() => setShowLinkCopiedMessage(false)}>
        <Alert onClose={() => setShowLinkCopiedMessage(false)} severity="info" sx={{ width: "100%", borderRadius: "25px" }}>
          Copied address to clipboard.
        </Alert>
      </Snackbar>
      <Snackbar open={showPasswordCopiedMessage} autoHideDuration={5000} onClose={() => setShowPasswordCopiedMessage(false)}>
        <Alert onClose={() => setShowPasswordCopiedMessage(false)} severity="info" sx={{ width: "100%", borderRadius: "25px" }}>
          Copied password to clipboard.
        </Alert>
      </Snackbar>
      <Modal
        open={showDeleteServerModal}
        onClose={() => setShowDeleteServerModal(false)}
        aria-labelledby="delete-server-modal-title"
        aria-describedby="delete-server-modal-description">
        <Box sx={modalStyle}>
          <Typography id="delete-server-modal-title" variant="h6">
            Delete Server
          </Typography>
          <Typography id="delete-server-modal-description" sx={{ marginTop: "2em" }}>
            Are you sure you want to delete server <b>{server ? server.name : ""}</b>?
          </Typography>
          <Stack justifyContent="end" direction="row" spacing={2} sx={{ marginTop: "2em" }}>
            <Button variant="contained" color="secondary" sx={{ borderRadius: "25px" }} onClick={() => setShowDeleteServerModal(false)}>
              Cancel
            </Button>
            <Button variant="contained" color="error" sx={{ borderRadius: "25px" }} onClick={() =>
              axiosInstance.delete("/server/delete", { params: { id: server.id } }).then(() => navigate("/", { replace: true }))}>
              Delete
            </Button>
          </Stack>
        </Box>
      </Modal>
      <Header />
      <Typography variant="h4">
        <Box display={"inline"} sx={{ "&:hover": { cursor: "pointer" } }}>
          <FontAwesomeIcon icon={faArrowLeft} onClick={() => navigate(-1)} />&nbsp;
        </Box>
        Server Details
      </Typography>
      <Card sx={{ marginTop: "2em", borderRadius: "25px", height: "calc(100% - 100px)", overflow: "auto" }}>
        <CardContent sx={{ height: "calc(100% - 3em)" }}>
          <Stack direction="column" spacing={2} height={"100%"}>
            <Stack direction="row" spacing={2}>
              <Box width={140} height={140}>
                <Card sx={{ width: "100%", height: "100%" }}>
                  <Avatar width={140} height={140} avatarSeed={server.avatarSeed} avatarColor={server.avatarColor} />
                </Card>
              </Box>
              <Stack direction="column" spacing={1}>
                <Typography variant="h6">{server.name}</Typography>
                {server.serverStatusType == "ONLINE"
                  ? <Typography>Online since: {new Date(server.startTime * 1000).toLocaleDateString()} {new Date(server.startTime * 1000).toLocaleTimeString()}</Typography>
                  : <Typography>Offline since:
                    {
                      server.stopTime
                        ? ` ${new Date(server.stopTime * 1000).toLocaleDateString()} ${new Date(server.stopTime * 1000).toLocaleTimeString()}`
                        : ` ${new Date(server.creationTime * 1000).toLocaleDateString()} ${new Date(server.creationTime * 1000).toLocaleTimeString()}`
                    }
                  </Typography>
                }
                <Stack direction="row">
                  <Typography>Address:&nbsp;</Typography>
                  <Typography sx={{ fontFamily: "monospace" }}>{server.containerAddress}:{server.containerPort}</Typography>
                  <IconButton
                    size="small"
                    onClick={(e) => {
                      e.stopPropagation();
                      e.preventDefault();
                      navigator.clipboard.writeText(`${server.containerAddress}:${server.containerPort}`);
                      setShowLinkCopiedMessage(true);
                    }}>
                    <FontAwesomeIcon icon={faClipboard} />
                  </IconButton>
                </Stack>
                {server.serverPassword &&
                  <Stack direction="row">
                    <Typography>Password:&nbsp;</Typography>
                    <Typography sx={{ fontFamily: "monospace" }}>{showPassword ? server.serverPassword : server.serverPassword.replace(/./g, "\u25CF")}</Typography>
                    <IconButton size="small" onClick={() => setShowPassword(!showPassword)}>
                      <FontAwesomeIcon icon={faEye} />
                    </IconButton>
                    <IconButton
                      size="small"
                      onClick={(e) => {
                        e.stopPropagation();
                        e.preventDefault();
                        navigator.clipboard.writeText(`${server.serverPassword}`);
                        setShowPasswordCopiedMessage(true);
                      }}>
                      <FontAwesomeIcon icon={faClipboard} />
                    </IconButton>
                  </Stack>
                }
              </Stack>
              <Stack direction="column">
                {getServerStatus(server.serverStatusType)}
                <Stack direction="row">
                  {server.serverStatusType !== "ONLINE" &&
                    <IconButton
                      aria-label="Start"
                      onMouseDown={(e) => { e.stopPropagation() }}
                      onClick={(e) => {
                        e.stopPropagation();
                        e.preventDefault();
                        startServer();
                      }}>
                      <FontAwesomeIcon icon={faPlay} />
                    </IconButton>
                  }
                  {server.serverStatusType === "ONLINE" &&
                    <IconButton
                      aria-label="Stop"
                      onMouseDown={(e) => { e.stopPropagation() }}
                      onClick={(e) => {
                        e.stopPropagation();
                        e.preventDefault();
                        stopServer();
                      }}>
                      <FontAwesomeIcon icon={faStop} />
                    </IconButton>
                  }
                  <IconButton
                    aria-label="Delete"
                    onMouseDown={(e) => { e.stopPropagation() }}
                    onClick={(e) => {
                      e.stopPropagation();
                      e.preventDefault();
                      setShowDeleteServerModal(true);
                    }}>
                    <FontAwesomeIcon icon={faTrash} />
                  </IconButton>
                </Stack>
              </Stack>
            </Stack>
            {files && files.length > 0 &&
              <div>
                <Typography variant="h6" sx={{ marginBottom: "6pt", marginTop: "6pt" }}>Project Files</Typography>
                <Card sx={{ borderRadius: "25px", backgroundColor: grey[200], flexGrow: 1, overflow: "auto", minHeight: "100px" }}>
                  <CardContent>
                    <List>
                      {
                        files?.map((projectFile) =>
                          <ListItem sx={{ backgroundColor: "white", borderRadius: "25px", marginBottom: "1em" }} key={projectFile.id}>
                            <Grid container>
                              <Grid item xs={5}>
                                <ListItemText>
                                  <Typography sx={{ fontWeight: "bold", marginLeft: "2pt" }}>{projectFile.name}</Typography>
                                </ListItemText>
                              </Grid>
                              <Grid item xs={4}>
                                <ListItemText>
                                  <Typography sx={{ fontStyle: "italic" }}>{FileTypeUtils.getLabel(projectFile.fileType)}</Typography>
                                </ListItemText>
                              </Grid>
                              <Grid item xs={2} sx={{ textAlign: "right" }}>
                                <ListItemText>
                                  <Typography>{Number(projectFile.size / 1024 / 1024).toFixed(2)} MiB</Typography>
                                </ListItemText>
                              </Grid>
                              <Grid item xs={1} sx={{ textAlign: "right" }}>
                                <ListItemText>
                                  <a href={axiosInstance.getUri() + "file/download?id=" + projectFile.id} download={projectFile.name} rel="noopener noreferrer" target="_blank" style={{ textDecoration: 'none' }}>
                                    <IconButton
                                      size="small">
                                      <FontAwesomeIcon icon={faDownload} />
                                    </IconButton>
                                  </a>
                                </ListItemText>
                              </Grid>
                            </Grid>
                          </ListItem>
                        )
                      }
                    </List>
                  </CardContent>
                </Card>
              </div>
            }
          </Stack>
        </CardContent>
      </Card>
    </Container>
  )
}

export default ServerView;
