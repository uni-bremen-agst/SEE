import { Alert, Box, Card, CardActionArea, CardContent, Chip, IconButton, Snackbar, Stack, Typography } from "@mui/material";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faClipboard } from '@fortawesome/free-solid-svg-icons'
import { useNavigate } from "react-router";
import Avatar from "./Avatar";
import Server from "../types/Server";
import { useState } from "react";

function getServerStatus(serverStatusType: string) {
  if (serverStatusType == "ONLINE") {
    return <Chip color="success" label="Online" />;
  }
  if (serverStatusType == "OFFLINE") {
    return <Chip color="error" label="Offline" />;
  }
  if (serverStatusType == "STARTING") {
    return <Chip color="warning" label="Startet" />;
  }
  if (serverStatusType == "STOPPING") {
    return <Chip color="warning" label="Stoppt" />;
  }
}

function ServerListItem(props: { server: Server }) {
  const navigate = useNavigate();

  const [showLinkCopiedMessage, setShowLinkCopiedMessage] = useState(false);

  const server = props.server;

  return (
    <Box width={"100%"}>
      <Snackbar open={showLinkCopiedMessage} autoHideDuration={5000} onClose={() => setShowLinkCopiedMessage(false)}>
        <Alert onClose={() => setShowLinkCopiedMessage(false)} severity="success" sx={{ width: "100%", borderRadius: "25px" }}>
          Address was copied to clipboard.
        </Alert>
      </Snackbar>
      <Card
        sx={{ cursor: "pointer", borderRadius: "25px" }}
        onClick={() => navigate('/server#' + server.id)}>
        <CardActionArea onClick={() => navigate('/server#' + server.id)}>
          <CardContent>
            <Stack direction="row" spacing={2}>
              {/* Avatar */}
              <Box width={100} height={100}>
                <Card sx={{ width: "100%", height: "100%" }}>
                  <Avatar width={100} height={100} avatarSeed={server.avatarSeed} avatarColor={server.avatarColor} />
                </Card>
              </Box>
              {/* Details */}
              <Stack direction="column" spacing={1}>
                <Stack direction="row" spacing={1}>
                  <Typography variant="h6">{server.name}</Typography>
                  {getServerStatus(server.serverStatusType)}
                </Stack>
                {server.serverStatusType == "ONLINE"
                  ? <Typography>Online since: {new Date(server.startTime * 1000).toLocaleString()}</Typography>
                  : <Typography>Offline since:
                    {server.stopTime
                      ? ` ${new Date(server.stopTime * 1000).toLocaleString()}`
                      : ` ${new Date(server.creationTime * 1000).toLocaleString()}`
                    }
                  </Typography>
                }
                <Stack direction="row" spacing={1}>
                  <Typography>Address:&nbsp;</Typography>
                  <Typography sx={{ fontFamily: "monospace" }}>{server.containerAddress}:{server.containerPort}</Typography>
                  <IconButton
                    size="small"
                    component="span"
                    onMouseDown={(e) => { e.stopPropagation(); e.preventDefault(); }}
                    onClick={(e) => {
                      e.stopPropagation();
                      e.preventDefault();
                      navigator.clipboard.writeText(`${server.containerAddress}:${server.containerPort}`);
                      setShowLinkCopiedMessage(true);
                    }}>
                    <FontAwesomeIcon icon={faClipboard} />
                  </IconButton>
                </Stack>
              </Stack>
            </Stack>
          </CardContent>
        </CardActionArea>
      </Card>
    </Box>
  )
}

export default ServerListItem;
