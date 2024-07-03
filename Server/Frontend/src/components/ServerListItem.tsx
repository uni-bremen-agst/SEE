import {Alert, Box, Card, CardActionArea, CardContent, Chip, Grid, IconButton, Snackbar, Stack, Typography} from "@mui/material";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faShare } from '@fortawesome/free-solid-svg-icons'
import { useNavigate } from "react-router";
import Avatar from "./Avatar";
import Server from "../types/Server";
import { useState } from "react";

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

function ServerListItem(props: {server: Server}) {
    const navigate = useNavigate();

    const [showLinkCopiedMessage, setShowLinkCopiedMessage] = useState(false);

    const server = props.server;

    return (
        <Box width={"100%"}>
            <Snackbar open={showLinkCopiedMessage} autoHideDuration={5000} onClose={() => setShowLinkCopiedMessage(false)}>
              <Alert onClose={() => setShowLinkCopiedMessage(false)} severity="success" sx={{width: "100%", borderRadius: "25px"}}>
                Link in Zwischenablage kopiert.
              </Alert>
            </Snackbar>
            <Card sx={{borderRadius:"25px"}}> 
              <CardActionArea onClick={() => navigate('/server', {state: {serverID : server.id}})}>
                <CardContent>
                  <Grid container spacing={2}>
                    <Grid item md={2} xs={12}>
                      <Stack direction="column" spacing={1}>
                        
                        <Box width={100} height={100}>
                          <Card sx={{width: "100%", height: "100%"}}>
                            <Avatar width={100} height={100} avatarSeed={server.avatarSeed} avatarColor={server.avatarColor}/>
                          </Card>
                        </Box>
                      </Stack>
                    </Grid>
                    <Grid item md={8} xs={12}>
                      <Stack direction="column" spacing={1}>
                        <Typography variant="h6">{server.name}</Typography>
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
                        {getServerStatus(server.serverStatusType)}
                      </Stack>
                    </Grid>
                    <Grid item md={2} textAlign="end" display="flex" justifyContent="end" alignContent="end">
                      <Stack direction="column" spacing={1}>
                       
                        <Box display="flex" height="100%">
                          <IconButton 
                                aria-label="IP teilen" 
                                size="large" 
                                sx={{display: "flex", flexDirection: "column"}} 
                                onMouseDown={(e) => {e.stopPropagation()}} 
                                onClick={(e) => {e.stopPropagation();
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
                </CardContent>
              </CardActionArea> 
            </Card>
          </Box>
    )
}

export default ServerListItem;