import { Alert, Box, Button, Card, CardContent, Chip, CircularProgress, Container, Grid, IconButton, List, ListItem, ListItemText, Modal, Snackbar, Stack, Typography } from "@mui/material";
import Header from "../components/Header";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowLeft, faDownload, faEye, faPlay, faShare, faStop, faTrash } from "@fortawesome/free-solid-svg-icons";
import { grey } from "@mui/material/colors";
import Avatar from "../components/Avatar";
import { useContext, useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router";
import Server from "../types/Server";
import { AuthContext } from "../contexts/AuthContext";
import DummyFile from "../types/File";
import { saveAs } from 'file-saver';
import { base64StringToBlob } from 'blob-util';

function getServerStatus(serverStatusType: string){
  if(serverStatusType == "ONLINE"){
    return <Chip color="success" label="Online"/>;
  }
  if(serverStatusType == "OFFLINE"){
    return <Chip color="error" label="Offline"/>;
  }
  if(serverStatusType == "STARTING"){
    return <Chip color="warning" label="Startet"/>;
  }
  if(serverStatusType == "STOPPING"){
    return <Chip color="warning" label="Stoppt"/>;    
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
    const {axiosInstance} = useContext(AuthContext);

    const location = useLocation();
    const navigate = useNavigate();

    const [server, setServer] = useState<Server|undefined>(undefined);
    const [files, setFiles] = useState<DummyFile[] | undefined>(undefined);
    const [showDeleteServerModal, setShowDeleteServerModal] = useState(false);
    const [showLinkCopiedMessage, setShowLinkCopiedMessage] = useState(false);
    const [showPassword, setShowPassword] = useState(false);

    async function startServer() {
      if(server){
        await axiosInstance.put("/server/startServer", {}, {params: {id: server.id}})
        axiosInstance.get(`/server/`, {params: {id: server.id}}).then(
          (response) => setServer(response.data)
        )  
      }
    }

    async function stopServer() {
      if(server){
        await axiosInstance.put("/server/stopServer", {}, {params: {id: server?.id}})
        axiosInstance.get(`/server/`, {params: {id: server.id}}).then(
          (response) => setServer(response.data)
        )  
      }
    }

    function downloadFiles () {
      if(files){
        for(const file of files){
          axiosInstance.get("/file/get", {params: {id: file.id}}).then(
            (response) => {
              saveAs(base64StringToBlob(response.data.content, response.data.contentType), response.data.originalFileName);
            }
          )
        }
      }
    }

    useEffect(() => {
      let isApiSubscribed = true;
      const fetchServer = setInterval(() => {
        const serverID = location.state.serverID; 
        if(serverID){
          axiosInstance.get(`/server/`, {params: {id: serverID}}).then(
            (response) => setServer(response.data)
          )  
        }
      }, 30000);

      if(isApiSubscribed && !server && location.state.serverID){
        const serverID = location.state.serverID; 
        if(serverID){
          axiosInstance.get(`/server/`, {params: {id: serverID}}).then(
            (response) => setServer(response.data)
          )  
        }
      }
      if(isApiSubscribed && location.state.serverID){
        axiosInstance.get(`/server/files`, {params: {id: location.state.serverID}}).then(
          (response) => setFiles(response.data)
        )  
      }

      return () => {
        clearInterval(fetchServer);
        isApiSubscribed = false;
      }
    }, [location.state]);
    
    if(!server){
      return <CircularProgress/>
    }
    return (
      <Container sx={{padding: "3em", height:"100vh"}}>
        <Snackbar open={showLinkCopiedMessage} autoHideDuration={5000} onClose={() => setShowLinkCopiedMessage(false)}>
          <Alert onClose={() => setShowLinkCopiedMessage(false)} severity="success" sx={{width: "100%", borderRadius: "25px"}}>
            Link in Zwischenablage kopiert.
          </Alert>
        </Snackbar>
        <Modal
          open={showDeleteServerModal}
          onClose={() => setShowDeleteServerModal(false)}
          aria-labelledby="delete-server-modal-title"
          aria-describedby="delete-server-modal-description">
            <Box sx={modalStyle}>
                <Typography id="delete-server-modal-title" variant="h6">
                  Server löschen
                </Typography>
                <Typography id="delete-server-modal-description" sx={{marginTop: "2em"}}>
                  Sind Sie sich sicher, dass Sie den Server <b>{server? server.name : ""}</b> löschen möchten?
                </Typography>
                <Stack justifyContent="end" direction="row" spacing={2} sx={{marginTop: "2em"}}>
                  <Button variant="contained" color="secondary" sx={{borderRadius:"25px"}} onClick={() => setShowDeleteServerModal(false)}>
                    Abbrechen
                  </Button>
                  <Button variant="contained" color="error" sx={{borderRadius:"25px"}} onClick={() => 
                  axiosInstance.delete("/server/delete", {params: {id: server.id}}).then(() => navigate("/", {replace: true}))}>
                    Löschen
                  </Button>
                </Stack>
            </Box>
        </Modal>
        <Header/>
        <Card sx={{marginTop: "2em", borderRadius: "25px", height: "calc(100% - 100px)", overflow: "auto"}}>
          <CardContent sx={{height: "calc(100% - 3em)"}}>
            <Stack direction="column" spacing={2} height={"100%"}>
              <Stack direction="row" sx={{justifyContent: 'space-between'}}>
                <Stack direction="row">
                  <Typography variant="h4"><Box display={"inline"} sx={{"&:hover" : {cursor: "pointer"}}}><FontAwesomeIcon icon={faArrowLeft} onClick={() => navigate(-1)}/></Box> {server.name}</Typography>
                </Stack>
                <Stack direction="row">
                  <IconButton 
                        aria-label="start"
                        disabled = {server.serverStatusType == "ONLINE"}
                        onMouseDown={(e) => {e.stopPropagation()}} 
                        onClick={(e) => {e.stopPropagation();
                                        e.preventDefault();
                                        startServer();
                    }}>
                      <FontAwesomeIcon icon={faPlay}/>
                  </IconButton>
                  <IconButton 
                        aria-label="stop"
                        disabled = {server.serverStatusType == "OFFLINE"}
                        onMouseDown={(e) => {e.stopPropagation()}} 
                        onClick={(e) => {e.stopPropagation();
                                        e.preventDefault();
                                        stopServer();
                    }}>
                      <FontAwesomeIcon icon={faStop}/>
                  </IconButton>
                  <IconButton
                        aria-label="delete"
                        onMouseDown={(e) => {e.stopPropagation()}} 
                        onClick={(e) => {e.stopPropagation();
                                        e.preventDefault();
                                        setShowDeleteServerModal(true);
                    }}>
                      <FontAwesomeIcon icon={faTrash}/>
                  </IconButton>
                  <Stack direction="column" sx={{justifyContent: "center", marginLeft: "1em"}}>
                    {getServerStatus(server.serverStatusType)}
                  </Stack>
                </Stack>
              </Stack>
              <Grid container spacing={2} sx={{paddingRight: "32px"}}>
                <Grid item md={2} xs={12}>
                  <Stack direction="column" spacing={1}>
                    <Box width={140} height={140}>
                      <Card sx={{width: "100%", height: "100%"}}>
                        <Avatar width={140} height={140} avatarSeed={server.avatarSeed} avatarColor={server.avatarColor}/>
                      </Card>
                    </Box>
                  </Stack>
                </Grid>
                <Grid item md={8} xs={12}>
                  <Stack direction="column" spacing={1}>
                    <Typography variant="h6">Status</Typography>
                    {server.serverStatusType == "ONLINE" ? 
                      <Typography>Online seit: {new Date(server.startTime*1000).toLocaleDateString()} {new Date(server.startTime*1000).toLocaleTimeString()}</Typography>
                      : <Typography>Offline seit:
                        {
                           server.stopTime?
                              ` ${new Date(server.stopTime*1000).toLocaleDateString()} ${new Date(server.stopTime*1000).toLocaleTimeString()}`
                            :
                              ` ${new Date(server.creationTime*1000).toLocaleDateString()} ${new Date(server.creationTime*1000).toLocaleTimeString()}`
                        }
                      </Typography>
                    }
                    { server.serverPassword ? 
                      <Stack direction="row">
                        <Typography sx={{lineHeight: "38px"}}>{showPassword ? server.serverPassword : server.serverPassword.replace(/./g, "\u25CF")}</Typography>
                        <IconButton onClick={() => setShowPassword(!showPassword)}>
                          <FontAwesomeIcon icon={faEye} />
                        </IconButton>
                      </Stack> : <></>
                    }
                  </Stack>
                </Grid>
                <Grid item md={2} textAlign="end" display="flex" justifyContent="end" alignContent="end">
                  <Stack direction="column" justifyContent="center">
                    <Box display="flex">
                      <IconButton 
                            aria-label="IP teilen" 
                            size="large" 
                            sx={{display: "flex", flexDirection: "column"}} 
                            onMouseDown={(e) => {e.stopPropagation()}} 
                            onClick={(e) => {
                              e.stopPropagation();
                              e.preventDefault();
                              navigator.clipboard.writeText(`${server.containerAddress}:${server.containerPort}`);
                              setShowLinkCopiedMessage(true);
                      }}>
                          <FontAwesomeIcon icon={faShare}/>
                          <Typography variant="button">IP teilen</Typography>
                      </IconButton>
                    </Box>
                  </Stack>
                </Grid>
              </Grid>
              <Stack direction="row" sx={{justifyContent: 'space-between'}}>
                <Typography variant="h6">Projektdaten:</Typography>
                <IconButton 
                            onMouseDown={(e) => {e.stopPropagation()}} 
                            onClick={(e) => {e.stopPropagation();
                                            e.preventDefault();
                                            downloadFiles();
                        }}>
                          <FontAwesomeIcon icon={faDownload}/>
                      </IconButton>
              </Stack>
              <Card sx={{borderRadius: "25px", backgroundColor: grey[200], flexGrow: 1, overflow: "auto", minHeight: "100px"}}>
                <CardContent>
                  <List>
                    {
                      files?.map((projectFile) => 
                        <ListItem sx={{backgroundColor: "white", borderRadius:"25px", marginBottom:"1em"}} key={projectFile.id}>
                          <ListItemText> <Typography variant="subtitle2">{projectFile.originalFileName}</Typography> </ListItemText>
                        </ListItem>
                      )
                    }
                  </List>
                </CardContent>
              </Card>
            </Stack>
          </CardContent>
        </Card>
      </Container>
    )
  }

  export default ServerView;